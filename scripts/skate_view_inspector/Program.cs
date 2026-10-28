using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

// Offline metadata only: never load the inspected assemblies or invoke their members.
const string usage = "SkateViewInspector --build-id <installed-build-label> --output <new-private.json> --assembly <interop.dll> [--assembly <core.dll>] [--max-members <1..128>]";
try
{
    var paths = new List<string>();
    string? build = null, output = null;
    var limit = 32;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--help") { Console.WriteLine(usage); return 0; }
        if (i + 1 >= args.Length) throw new ArgumentException("Missing option value.");
        switch (args[i++])
        {
            case "--assembly": paths.Add(Path.GetFullPath(args[i])); break;
            case "--build-id": build = args[i]; break;
            case "--output": output = Path.GetFullPath(args[i]); break;
            case "--max-members" when int.TryParse(args[i], out var n) && n is >= 1 and <= 128: limit = n; break;
            default: throw new ArgumentException("Unknown option or invalid bound.");
        }
    }
    paths = paths.Distinct(StringComparer.Ordinal).ToList();
    if (paths.Count is < 1 or > 16 || string.IsNullOrWhiteSpace(build) || build.Length > 128 || output == null)
        throw new ArgumentException("Supply 1..16 assemblies, a build label of at most 128 characters and a new output path.");
    if (File.Exists(output)) throw new ArgumentException("Output exists; choose a new report path.");

    var reports = paths.Select(path => Inspect(path, limit)).ToArray();
    var report = new
    {
        schemaVersion = 1, buildLabelProvidedByOperator = build,
        inspectedAtUtc = DateTime.UtcNow,
        scope = "Only supplied managed metadata; completeness and installed-build match are not verified.",
        semanticViewBinding = "unproven; no method has been invoked or selected as a replacement",
        memberLimitPerList = limit, assemblies = reports
    };
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    using var target = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    JsonSerializer.Serialize(target, report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine($"Inspected {reports.Length} assemblies. Private report written. View binding remains unproven.");
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
{
    // Do not print private paths, metadata payloads or exception messages.
    Console.Error.WriteLine($"Inspection failed ({e.GetType().Name}); no successful inspection is claimed.\n{usage}");
    return 1;
}

static object Inspect(string path, int limit)
{
    using var input = File.OpenRead(path);
    if (input.Length is <= 0 or > 268435456) throw new ArgumentException("Assembly exceeds the 256 MiB bound.");
    using var pe = new PEReader(input, PEStreamOptions.LeaveOpen);
    if (!pe.HasMetadata) throw new BadImageFormatException();
    var r = pe.GetMetadataReader();
    if (!r.IsAssembly) throw new BadImageFormatException();
    var names = new TypeNames();
    string[] requested = ["BasePlayer", "PlayerInput", "PlayerEyes", "PlayerWalkMovement", "InputState", "InputMessage"];
    var selected = new HashSet<TypeDefinitionHandle>();
    var found = new HashSet<string>();
    foreach (var h in r.TypeDefinitions)
    {
        var t = r.GetTypeDefinition(h);
        var shortName = r.GetString(t.Name);
        if (!requested.Contains(shortName)) continue;
        selected.Add(h); found.Add(shortName);
        // Include the same-assembly player base chain inspected by SetView's member walk.
        if (shortName != "BasePlayer") continue;
        var parent = t.BaseType;
        for (var depth = 0; parent.Kind == HandleKind.TypeDefinition && depth < 8; depth++)
        {
            var ph = (TypeDefinitionHandle)parent;
            selected.Add(ph); parent = r.GetTypeDefinition(ph).BaseType;
        }
    }

    var exact = new List<object>(); var exactCount = 0;
    // Search every method definition, independently of the selected type/member caps.
    foreach (var mh in r.MethodDefinitions)
    {
        var m = r.GetMethodDefinition(mh);
        if (r.GetString(m.Name) != "SetViewVars") continue;
        exactCount++;
        if (exact.Count < limit) exact.Add(Method(r, names, mh));
    }

    var types = new List<object>();
    foreach (var h in selected.OrderBy(h => MetadataTokens.GetToken(h)))
    {
        var t = r.GetTypeDefinition(h);
        var properties = new List<object>(); var propertyCount = 0;
        var fields = new List<object>(); var fieldCount = 0;
        var methods = new List<object>(); var candidateCount = 0;
        foreach (var ph in t.GetProperties())
        {
            propertyCount++;
            if (properties.Count >= limit) continue;
            var p = r.GetPropertyDefinition(ph); var a = p.GetAccessors();
            properties.Add(new { name = r.GetString(p.Name), token = Token(ph), signature = Signature(p.DecodeSignature(names, (object?)null)),
                getter = a.Getter.IsNil ? null : Token(a.Getter), setter = a.Setter.IsNil ? null : Token(a.Setter) });
        }
        foreach (var fh in t.GetFields())
        {
            var f = r.GetFieldDefinition(fh); var name = r.GetString(f.Name);
            if (name.StartsWith("NativeMethodInfoPtr_", StringComparison.Ordinal) || name.StartsWith("NativeFieldInfoPtr_", StringComparison.Ordinal)) continue;
            fieldCount++;
            if (fields.Count < limit) fields.Add(new { name, token = Token(fh), type = f.DecodeSignature(names, (object?)null), attributes = f.Attributes.ToString() });
        }
        foreach (var mh in t.GetMethods())
        {
            var m = r.GetMethodDefinition(mh); var name = r.GetString(m.Name);
            var sig = m.DecodeSignature(names, (object?)null);
            // A name/type clue only, not proof of semantics or native callability.
            if (!ViewName(name) && !sig.ParameterTypes.Any(QuaternionType) && !QuaternionType(sig.ReturnType)) continue;
            candidateCount++;
            if (methods.Count < limit) methods.Add(Method(r, names, mh));
        }
        types.Add(new { type = names.Name(r, h), token = Token(h), baseType = names.Name(r, t.BaseType),
            declaredMethodCount = t.GetMethods().Count,
            properties = Bounded(properties, propertyCount), fields = Bounded(fields, fieldCount),
            metadataCluesNotReplacementBindings = Bounded(methods, candidateCount) });
    }
    var assembly = r.GetAssemblyDefinition(); var module = r.GetModuleDefinition();
    input.Position = 0;
    return new { fileName = Path.GetFileName(path), length = input.Length,
        sha256 = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant(),
        assembly = r.GetString(assembly.Name), assemblyVersion = assembly.Version.ToString(),
        moduleVersionId = module.Mvid.IsNil ? null : r.GetGuid(module.Mvid).ToString(),
        totalTypeDefinitions = r.TypeDefinitions.Count, totalMethodDefinitions = r.MethodDefinitions.Count,
        missingRequestedTypes = requested.Where(n => !found.Contains(n)).ToArray(),
        exactSetViewVarsDefinitions = Bounded(exact, exactCount), selectedTypes = types };
}

static object Bounded(List<object> items, int total) => new { total, listed = items.Count, truncated = total > items.Count, items };
static string Token(EntityHandle h) => $"0x{MetadataTokens.GetToken(h):x8}";
static bool QuaternionType(string type) => type is "UnityEngine.Quaternion" or "UnityEngine.Quaternion&";
static bool ViewName(string name) => new[] { "View", "Angle", "Aim", "Look" }.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));
static object Signature(MethodSignature<string> s) => new { returnType = s.ReturnType, parameterTypes = s.ParameterTypes.ToArray(),
    instance = s.Header.IsInstance, genericParameters = s.GenericParameterCount };
static object Method(MetadataReader r, TypeNames names, MethodDefinitionHandle h)
{
    var m = r.GetMethodDefinition(h);
    return new { type = names.Name(r, m.GetDeclaringType()), name = r.GetString(m.Name), token = Token(h),
        signature = Signature(m.DecodeSignature(names, (object?)null)), attributes = m.Attributes.ToString() };
}

sealed class TypeNames : ISignatureTypeProvider<string, object?>
{
    public string Name(MetadataReader r, EntityHandle h) => h.IsNil ? "<none>" : h.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeFromDefinition(r, (TypeDefinitionHandle)h, 0),
        HandleKind.TypeReference => GetTypeFromReference(r, (TypeReferenceHandle)h, 0),
        HandleKind.TypeSpecification => GetTypeFromSpecification(r, null, (TypeSpecificationHandle)h, 0),
        _ => h.Kind.ToString()
    };
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte kind)
    {
        var t = r.GetTypeDefinition(h); var parent = t.GetDeclaringType();
        return parent.IsNil ? Qualified(r.GetString(t.Namespace), r.GetString(t.Name)) : Name(r, parent) + "+" + r.GetString(t.Name);
    }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte kind)
    {
        var t = r.GetTypeReference(h);
        return t.ResolutionScope.Kind == HandleKind.TypeReference ? Name(r, t.ResolutionScope) + "+" + r.GetString(t.Name)
            : Qualified(r.GetString(t.Namespace), r.GetString(t.Name));
    }
    static string Qualified(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;
    public string GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte kind) => r.GetTypeSpecification(h).DecodeSignature(this, c);
    public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
    public string GetByReferenceType(string e) => e + "&";
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr " + s.ReturnType;
    public string GetGenericInstantiation(string t, ImmutableArray<string> a) => t + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object? c, int n) => "!!" + n;
    public string GetGenericTypeParameter(object? c, int n) => "!" + n;
    public string GetModifiedType(string m, string u, bool required) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode code) => code.ToString();
    public string GetSZArrayType(string e) => e + "[]";
}
