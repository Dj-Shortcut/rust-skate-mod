# Compass and dismount camera: current-build investigation

The compass must remain stable while riding with a controller. After dismount,
the native view must be level and face the travel direction. Neither behavior is
currently accepted in-game. This work tracks issue #337, compass/camera point 2
of the owner's 28 October follow-up (TODO items 8 and 9).

## What the source actually does

`SkateRig.SetView` searches fields and non-indexed properties on the local
player and its base types. It looks for a declared member type exposing an
instance `SetViewVars(UnityEngine.Quaternion)` method, reads that member's current
value, and invokes the method with a level yaw. It does not discover a native
object hidden behind a declared `Il2CppSystem.Object` member.

`SteadyView` uses that route during a controller ride; a missing or throwing
route stops stabilization for that ride and displays the unavailable notice.
`LevelView` uses it after dismount. The cached route resets when the local player
is released. None of this is part of `AcquireRootProxy`: the historical mount
refusal and unavailable view binding are separate failures.

The privately cached 26 October game/Core wrappers contain no method definition
named exactly `SetViewVars`. Their selected player/input/eyes declarations do not
provide a proven replacement. This finding is limited to those files; it is not
an inspection of the next installed Windows build. Generic/obfuscated member
names and erased object types do not establish native semantics. No guessed
Quaternion method has been substituted, and no compass fix is claimed.

## Inspect a matching interop set without starting Rust

The source-only [inspector](../scripts/skate_view_inspector/Program.cs) uses
`System.Reflection.Metadata`/`PEReader`. It neither loads the supplied game
assemblies into the CLR nor invokes their members. It requires a .NET 8 SDK to
build and has no NuGet packages or game dependencies. Generated tool files stay
under ignored `context/view-inspector/`.

First obtain the interop generated for the actual installed Rust build; keep
game binaries and generation logs private. Record the installed build identity
and whether that generation completed without errors. Merely copying an old
interop folder does not establish a match.

From the repository root in PowerShell:

```powershell
dotnet build scripts/skate_view_inspector/SkateViewInspector.csproj --configfile scripts/skate_view_inspector/NuGet.Config
if ($LASTEXITCODE -ne 0) { throw 'Inspector build failed' }

# Use the actual installed build label and matching private wrapper paths.
$viewBuild = 'REPLACE-WITH-INSTALLED-BUILD-ID'
$viewInterop = 'C:\private\matching-interop'
$viewReport = 'context/view-inspector/current-build.json'
dotnet context/view-inspector/bin/net8.0/SkateViewInspector.dll `
  --build-id $viewBuild --output $viewReport `
  --assembly "$viewInterop\Assembly-CSharp.dll" `
  --assembly "$viewInterop\UnityEngine.CoreModule.dll"
if ($LASTEXITCODE -ne 0) { throw 'Inspection failed; no successful report' }
```

Choose a **new** report path for each attempt; existing reports are never
overwritten. On macOS/Linux use the same arguments with local paths. No install,
network request, process attach, game launch or client configuration change is
performed. Exit 0 means metadata inspection completed, even when no view method
exists. Exit 1 means inspection failed; it never means a binding was fixed.

Each assembly report includes its SHA-256, length, assembly identity and module
MVID. The operator's build label is recorded as supplied, not independently
verified. Every method definition in the supplied assemblies is searched for
the exact `SetViewVars` name, independently of display limits. The report lists
selected player/input/eyes types and the same-assembly BasePlayer base chain,
their declared members, and named/signature clues. Missing types are explicit.
Those clues are **metadata, not replacement bindings**; even the expected name
and signature would not prove native callability or compass behavior.

Input is limited to 16 assemblies of at most 256 MiB each. Member/match lists
default to 32 entries each (`--max-members 1..128`); total counts and truncation
are explicit. A zero count is scoped to the supplied files. Truncated lists
must not be treated as complete; the inspector does not resolve dependencies or
inspect native implementation bodies. It deliberately excludes generated native
method/field-pointer fields and captures no field values or player state.

Keep the JSON, wrappers and native logs under ignored `context/` or another
private location. Do not attach dumps or game DLLs to issues/PRs. Share only a
short conclusion, the inspected build label, and whether its identity is verified.

## Evidence needed before changing the binding

1. Establish the installed build and a successful matching interop generation.
   Compare the private report to the cached finding; do not mix wrapper versions.
2. Identify the actual input/view carrier, its lifetime, and the method/fields
   used by that game's own view update. Prove yaw/pitch units, authority and
   side effects from matching implementation evidence. Names, tokens or a
   Quaternion parameter alone do not answer these questions.
3. Implement only that evidenced route, preserving unavailable reporting,
   fresh carrier lookup, lifecycle reset and mount/collision checks. Check native
   callability in a controlled attempt before relying on it each frame.
4. In the actual client, mount successfully first. With a controller, ride,
   steer and flick tricks while checking compass and chase-camera direction.
   Dismount after looking up/down and verify level native view toward travel.
   Repeat after respawn/reconnect and verify ordinary on-foot mouse control.

A metadata scan, source compile, successful mount or moving rendered camera
does not close these view acceptance gates. If the implementation evidence is
still missing, retain the unavailable route and record the open source question
instead of trying arbitrary setters.
