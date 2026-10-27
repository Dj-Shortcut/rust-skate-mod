using System;
using System.Collections.Generic;
using Shortcut.RustMod;
using UnityEngine;

// Main-thread bridge only: measure the already-written visible native skin.
// It never writes bones, renderers or gameplay state and never guesses a sole offset.
public static class SkateFootContacts
{
    private const int MaximumRenderers = 64, MaximumMeshVertices = 262144, MaximumIndices = 1572864, MaximumBones = 1024;
    private const double CheckSeconds = 1, PoseTolerance = .00001, PositionTolerance = .0001, StableTolerance = .0001;
    private const int MaximumFailures = 3, MaximumSnapshots = 4;
    private static readonly View first = new View(), third = new View();
    private static View active;
    private static SkateDeckContactSurface surface;
    private static SkateBoardMesh board;
    private static double boardTop;
    private static int failures, snapshots, version;
    private static bool viewKnown, thirdView;
    private static string boardStatus = "Foot contacts unavailable: board surface not set.";
    public static bool Ready { get { return surface != null && active != null && active.Fresh && active.Ready; } }
    public static SkateDeckFootFitter LeftFitter { get { return Ready ? active.Left : null; } }
    public static SkateDeckFootFitter RightFitter { get { return Ready ? active.Right : null; } }
    public static int Version { get { return version; } }
    public static string Status { get { return surface == null ? boardStatus : active == null ? "Foot contacts waiting for a view." : active.Note; } }

    private sealed class Unsupported : Exception
    { internal Unsupported(string message) : base(message) { } }
    private sealed class View
    {
        internal long SkinSet;
        internal bool HasSkinSet, Fresh, Ready, Stable;
        internal double NextCheck, NextAttempt;
        internal long Key;
        internal double[] Pose, LeftPoints, RightPoints;
        internal int[] LeftIndices, RightIndices;
        internal SkateDeckFootFitter Left, Right;
        internal string Note = "Foot contacts waiting for a fresh visible model sample.";
        internal void Clear()
        {
            Fresh = Ready = Stable = false; Pose = LeftPoints = RightPoints = null;
            LeftIndices = RightIndices = null; Left = Right = null; Key = 0; NextCheck = NextAttempt = 0;
            Note = "Foot contacts waiting for a fresh visible model sample.";
        }
    }
    private sealed class MeshData
    {
        internal SkinnedMeshRenderer Renderer;
        internal Mesh Mesh;
        internal int Count;
        internal int[] Indices, MapLeft, MapRight;
        internal byte[] Seeds, Rigid;
        internal bool Relevant;
    }
    private sealed class Read
    {
        internal long Key = 1469598103934665603;
        internal readonly List<double> Pose = new List<double>();
        internal readonly List<MeshData> Meshes = new List<MeshData>();
    }
    private struct Affine
    {
        private readonly double[] values;
        private Affine(bool identity)
        { values = new double[12]; if (identity) values[0] = values[5] = values[10] = 1; }
        internal double At(int row, int col) { return values[row * 4 + col]; }
        internal static Affine Linear(Matrix4x4 matrix)
        {
            var result = new Affine(false);
            for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++) result.values[r * 4 + c] = Element(matrix, r, c);
            return result;
        }
        private static Affine Local(Transform bone)
        {
            Vector3 p = bone.localPosition, scale = bone.localScale; Quaternion q = bone.localRotation;
            if (!Finite(p) || !Finite(scale) || !Finite(q.x) || !Finite(q.y) || !Finite(q.z) || !Finite(q.w))
                throw new Unsupported("Local toe transform is not finite.");
            double x = q.x, y = q.y, z = q.z, w = q.w, norm = x * x + y * y + z * z + w * w;
            if (Math.Abs(norm - 1) > .0001) throw new Unsupported("Local toe rotation is not unit length.");
            double inv = 1 / Math.Sqrt(norm); x *= inv; y *= inv; z *= inv; w *= inv;
            var result = new Affine(false); double[] m = result.values;
            m[0] = (1 - 2 * (y * y + z * z)) * scale.x; m[1] = 2 * (x * y - z * w) * scale.y; m[2] = 2 * (x * z + y * w) * scale.z;
            m[4] = 2 * (x * y + z * w) * scale.x; m[5] = (1 - 2 * (x * x + z * z)) * scale.y; m[6] = 2 * (y * z - x * w) * scale.z;
            m[8] = 2 * (x * z - y * w) * scale.x; m[9] = 2 * (y * z + x * w) * scale.y; m[10] = (1 - 2 * (x * x + y * y)) * scale.z;
            m[3] = p.x; m[7] = p.y; m[11] = p.z; return result;
        }
        internal static Affine Multiply(Affine a, Affine b)
        {
            var result = new Affine(false);
            for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++)
            {
                double value = c == 3 ? a.At(r, 3) : 0;
                for (int k = 0; k < 3; k++) value += a.At(r, k) * b.At(k, c);
                result.values[r * 4 + c] = value;
            }
            return result;
        }
        internal static Affine ToeInFoot(Transform foot, Transform toe)
        {
            var result = new Affine(true); Transform current = toe; int depth = 0;
            // Walk only the known Toe->Foot link. No child/hierarchy search and
            // no world-position subtraction can quantize the short toe offset.
            while (current != foot)
            {
                if (current == null || depth++ >= 16) throw new Unsupported("Toe is not within the bounded Foot ancestor chain.");
                result = Multiply(Local(current), result); current = current.parent;
            }
            return result;
        }
    }
    private struct SoleFrame
    {
        internal Vector3 Origin, Right, Up, Forward;
        internal Affine Foot, Toe;
        internal static SoleFrame Create(Transform foot, Transform toe, Vector3 sole)
        {
            if (foot == null || toe == null || !Finite(sole) || sole.sqrMagnitude < .25f)
                throw new Unsupported("Foot frame is unavailable.");
            Affine linear = Affine.Linear(foot.localToWorldMatrix);
            Affine preciseToe = Affine.Multiply(linear, Affine.ToeInFoot(foot, toe));
            Vector3 origin = foot.position, delta = new Vector3((float)preciseToe.At(0, 3), (float)preciseToe.At(1, 3), (float)preciseToe.At(2, 3)), up = sole.normalized;
            Vector3 forward = delta - up * Vector3.Dot(delta, up);
            if (!Finite(origin) || !Finite(delta) || !Finite(forward) || forward.sqrMagnitude < .000001f)
                throw new Unsupported("Foot frame has no measured toe tangent.");
            forward = forward.normalized;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            return new SoleFrame { Origin = origin, Right = right, Up = Vector3.Cross(forward, right).normalized, Forward = forward, Foot = linear, Toe = preciseToe };
        }
        internal void Point(Matrix4x4 matrix, Vector3 point, out double x, out double y, out double z)
        {
            // Subtract translations in double before accumulation: no native call
            // or large-world float TransformPoint rounding for every shoe vertex.
            double wx = (double)matrix.m03 - Origin.x + (double)matrix.m00 * point.x + (double)matrix.m01 * point.y + (double)matrix.m02 * point.z;
            double wy = (double)matrix.m13 - Origin.y + (double)matrix.m10 * point.x + (double)matrix.m11 * point.y + (double)matrix.m12 * point.z;
            double wz = (double)matrix.m23 - Origin.z + (double)matrix.m20 * point.x + (double)matrix.m21 * point.y + (double)matrix.m22 * point.z;
            x = wx * Right.x + wy * Right.y + wz * Right.z;
            y = wx * Up.x + wy * Up.y + wz * Up.z;
            z = wx * Forward.x + wy * Forward.y + wz * Forward.z;
            if (!Finite(x) || !Finite(y) || !Finite(z) || Math.Abs(x) > .5 || Math.Abs(y) > .5 || Math.Abs(z) > .5)
                throw new Unsupported("Complete foot geometry exceeds its local bounds.");
        }
        internal void Bone(Read read, Affine matrix)
        {
            Scale(read, matrix);
            // Full basis, not just Toe-Foot: animation may rotate a weighted toe
            // without moving its origin. Keep translation in the same sole frame.
            for (int col = 0; col < 3; col++)
            {
                double x = matrix.At(0, col), y = matrix.At(1, col), z = matrix.At(2, col);
                read.Pose.Add(x * Right.x + y * Right.y + z * Right.z);
                read.Pose.Add(x * Up.x + y * Up.y + z * Up.z);
                read.Pose.Add(x * Forward.x + y * Forward.y + z * Forward.z);
            }
            // Foot position is exactly zero; Toe position is the local-chain
            // offset transformed by Foot's linear matrix, never world floats.
            double px = matrix.At(0, 3), py = matrix.At(1, 3), pz = matrix.At(2, 3);
            read.Pose.Add(px * Right.x + py * Right.y + pz * Right.z);
            read.Pose.Add(px * Up.x + py * Up.y + pz * Up.z);
            read.Pose.Add(px * Forward.x + py * Forward.y + pz * Forward.z);
        }
    }
    private sealed class Patch
    {
        internal readonly List<double> Points = new List<double>();
        internal readonly List<int> Indices = new List<int>();
        internal void Triangle(MeshData mesh, int a, int b, int c, byte side, Matrix4x4 matrix,
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> vertices, SoleFrame frame)
        {
            if (Indices.Count / 3 >= SkateFootContactProfile.MaximumTriangles)
                throw new Unsupported("Complete foot patch exceeds its triangle bound.");
            Indices.Add(Vertex(mesh, a, side, matrix, vertices, frame));
            Indices.Add(Vertex(mesh, b, side, matrix, vertices, frame));
            Indices.Add(Vertex(mesh, c, side, matrix, vertices, frame));
        }
        private int Vertex(MeshData mesh, int index, byte side, Matrix4x4 matrix,
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> vertices, SoleFrame frame)
        {
            // Include the entire incident triangle. A neighbouring shin or other
            // moving influence cannot silently be cut off and called a rigid shoe.
            if (mesh.Rigid[index] != side)
                throw new Unsupported("Foot patch has non-rigid or cross-foot influences.");
            int[] map = side == 1 ? mesh.MapLeft : mesh.MapRight;
            if (map[index] != 0) return map[index] - 1;
            int count = Points.Count / 3;
            if (count >= SkateFootContactProfile.MaximumVertices)
                throw new Unsupported("Complete foot patch exceeds its vertex bound.");
            double x, y, z; frame.Point(matrix, vertices[index], out x, out y, out z);
            Points.Add(x); Points.Add(y); Points.Add(z); map[index] = count + 1; return count;
        }
    }

    public static void SetBoard(SkateBoardMesh mesh, double deckTop)
    {
        try
        {
            if (ReferenceEquals(board, mesh) && boardTop == deckTop && surface != null) return;
            board = mesh; boardTop = deckTop; surface = null;
            string error; SkateDeckContactSurface next;
            if (!SkateDeckContactSurface.TryCreate(mesh, deckTop, out next, out error))
            { boardStatus = "Foot contacts unavailable: " + error; InvalidateAll(); return; }
            surface = next; boardStatus = ""; InvalidateAll();
        }
        catch (Exception e) { surface = null; boardStatus = "Foot contacts unavailable: board " + e.GetType().Name; InvalidateAll(); }
    }
    public static void Reset()
    {
        // The board may have been built before Bind. It is immutable and retained;
        // only ride/model ownership, profiles and bounded acquisition budgets reset.
        failures = snapshots = 0; viewKnown = false; active = null;
        first.Clear(); third.Clear(); first.HasSkinSet = third.HasSkinSet = false; Bump();
    }
    public static void UseView(bool thirdPerson, long skinSet)
    {
        View next = thirdPerson ? third : first;
        if (!viewKnown || thirdView != thirdPerson)
        {
            active = next; thirdView = thirdPerson; viewKnown = true;
            active.Fresh = false; active.Ready = false;
            active.Note = "Foot contacts waiting for this visible view to be observed."; Bump();
        }
        if (!active.HasSkinSet || active.SkinSet != skinSet)
        { active.Clear(); active.SkinSet = skinSet; active.HasSkinSet = true; Bump(); }
    }
    public static void ObserveAfterApply(SkinnedMeshRenderer[] visibleSet, Transform lf, Transform lt, Transform rf, Transform rt,
        Vector3 soleL, Vector3 soleR, bool eligible)
    {
        if (active == null || surface == null) return;
        if (snapshots >= MaximumSnapshots && (!active.Stable || active.Left == null || active.Right == null))
        { Unavailable("Foot contacts unavailable: calibration snapshot budget exhausted."); return; }
        if (failures >= MaximumFailures) { Unavailable("Foot contacts unavailable: acquisition failure budget exhausted."); return; }
        try
        {
            double now = Time.realtimeSinceStartup;
            if (!Finite(now)) throw new Unsupported("Foot contact clock is unavailable.");
            bool confirming = eligible && active.Ready && !active.Stable;
            if (active.Fresh && now < active.NextCheck && !confirming && (active.Ready || !eligible || now < active.NextAttempt)) return;
            if (now < active.NextAttempt && !active.Ready) return;
            active.NextCheck = now + CheckSeconds;
            if (lf == lt || lf == rf || lf == rt || lt == rf || lt == rt || rf == rt)
                throw new Unsupported("Foot and toe mappings must be distinct.");
            SoleFrame left = SoleFrame.Create(lf, lt, soleL), right = SoleFrame.Create(rf, rt, soleR);
            Read read = Inspect(visibleSet, lf, lt, rf, rt, left, right);
            bool same = active.Pose != null && active.Key == read.Key && Near(active.Pose, read.Pose);
            if (!same)
            {
                active.Ready = active.Stable = false; active.Left = active.Right = null;
                active.LeftPoints = active.RightPoints = null; active.LeftIndices = active.RightIndices = null;
                active.Key = read.Key; active.Pose = read.Pose.ToArray(); Bump();
            }
            active.Fresh = true;
            if (same && active.Stable && active.Left != null && active.Right != null)
            { active.Ready = true; active.Note = "Foot contacts measured for the current visible view."; return; }
            if (!eligible) { active.Note = "Foot contacts waiting for ordinary flat-ground calibration."; return; }
            if (snapshots >= MaximumSnapshots)
            { Unavailable("Foot contacts unavailable: calibration snapshot budget exhausted."); return; }
            snapshots++;
            Patch lp, rp; Bake(read, left, right, out lp, out rp);
            double[] lxyz = lp.Points.ToArray(), rxyz = rp.Points.ToArray(); int[] li = lp.Indices.ToArray(), ri = rp.Indices.ToArray();
            SkateFootContactProfile lprofile, rprofile; string error;
            if (!SkateFootContactProfile.TryCreate(lxyz, li, out lprofile, out error) ||
                !SkateFootContactProfile.TryCreate(rxyz, ri, out rprofile, out error))
                throw new Unsupported("Measured foot geometry is unsupported: " + error);
            bool stable = active.LeftPoints != null && SameIndices(active.LeftIndices, li) && SameIndices(active.RightIndices, ri) &&
                Near(active.LeftPoints, lxyz, StableTolerance) && Near(active.RightPoints, rxyz, StableTolerance);
            var lfit = new SkateDeckFootFitter(surface, lprofile); var rfit = new SkateDeckFootFitter(surface, rprofile);
            active.Left = lfit; active.Right = rfit; active.LeftPoints = lxyz; active.RightPoints = rxyz;
            active.LeftIndices = li; active.RightIndices = ri; active.Ready = true; active.Stable = stable;
            active.Note = stable ? "Foot contacts measured for the current visible view." : "Foot contacts measured; calibration confirmation pending.";
            if (!stable && snapshots >= MaximumSnapshots) Unavailable("Foot contacts unavailable: measured geometry did not stabilize.");
            Bump();
        }
        catch (Exception e)
        {
            failures++; active.NextAttempt = active.NextCheck;
            Unavailable("Foot contacts unavailable: " + (e is Unsupported ? e.Message : "native geometry read " + e.GetType().Name) + ".");
        }
    }

    private static Read Inspect(SkinnedMeshRenderer[] set, Transform lf, Transform lt, Transform rf, Transform rt, SoleFrame left, SoleFrame right)
    {
        if (set == null || set.Length == 0 || set.Length > MaximumRenderers)
            throw new Unsupported("Visible skinned renderer set is empty or exceeds its bound.");
        var read = new Read(); Mix(read, set.Length);
        Mix(read, lf.Pointer.ToInt64()); Mix(read, lt.Pointer.ToInt64()); Mix(read, rf.Pointer.ToInt64()); Mix(read, rt.Pointer.ToInt64());
        left.Bone(read, left.Foot); left.Bone(read, left.Toe); right.Bone(read, right.Foot); right.Bone(read, right.Toe);
        int totalVertices = 0, totalIndices = 0;
        for (int n = 0; n < set.Length; n++)
        {
            var renderer = set[n];
            if (renderer == null) throw new Unsupported("A visible renderer was destroyed.");
            for (int previous = 0; previous < n; previous++)
                if (set[previous] == renderer) throw new Unsupported("Visible renderer set contains a duplicate.");
            Mix(read, renderer.Pointer.ToInt64()); var mesh = renderer.sharedMesh;
            if (mesh == null) throw new Unsupported("A visible renderer has no mesh.");
            Mix(read, mesh.Pointer.ToInt64()); var bones = renderer.bones;
            if (bones == null || bones.Length == 0 || bones.Length > MaximumBones)
                throw new Unsupported("Visible skin has invalid bone mapping.");
            Mix(read, bones.Length); bool candidate = false;
            for (int i = 0; i < bones.Length; i++)
            { var bone = bones[i]; Mix(read, bone == null ? 0 : bone.Pointer.ToInt64()); if (bone == lf || bone == lt || bone == rf || bone == rt) candidate = true; }
            if (!candidate) continue;
            int count = mesh.vertexCount;
            if (count < 3 || count > MaximumMeshVertices - totalVertices || !mesh.isReadable)
                throw new Unsupported("Foot candidate mesh is unreadable or exceeds its vertex bound.");
            totalVertices += count; Mix(read, count);
            var layout = mesh.skinWeightBufferLayout;
            if (layout != SkinWeights.OneBone && layout != SkinWeights.TwoBones && layout != SkinWeights.FourBones)
                throw new Unsupported("Foot candidate requires unsupported bone weight layout.");
            Mix(read, (int)layout); var weights = mesh.boneWeights;
            if (weights == null || weights.Length != count) throw new Unsupported("Foot weight count differs from vertex count.");
            var data = new MeshData { Renderer = renderer, Mesh = mesh, Count = count, Seeds = new byte[count], Rigid = new byte[count] };
            for (int i = 0; i < count; i++)
            {
                var w = weights[i]; double sum = 0, l = 0, r = 0, other = 0;
                Weight(read, w.boneIndex0, w.weight0, bones, lf, lt, rf, rt, ref sum, ref l, ref r, ref other);
                Weight(read, w.boneIndex1, w.weight1, bones, lf, lt, rf, rt, ref sum, ref l, ref r, ref other);
                Weight(read, w.boneIndex2, w.weight2, bones, lf, lt, rf, rt, ref sum, ref l, ref r, ref other);
                Weight(read, w.boneIndex3, w.weight3, bones, lf, lt, rf, rt, ref sum, ref l, ref r, ref other);
                if (Math.Abs(sum - 1) > .0001) throw new Unsupported("Skin weights are not normalized.");
                data.Seeds[i] = (byte)((l > 0 ? 1 : 0) | (r > 0 ? 2 : 0));
                data.Rigid[i] = l > 0 && r == 0 && other == 0 ? (byte)1 : r > 0 && l == 0 && other == 0 ? (byte)2 : (byte)0;
            }
            var indices = mesh.triangles;
            if (indices == null || indices.Length % 3 != 0 || indices.Length > MaximumIndices - totalIndices)
                throw new Unsupported("Foot candidate indices exceed their bound.");
            totalIndices += indices.Length; Mix(read, indices.Length); data.Indices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i]; if (index < 0 || index >= count) throw new Unsupported("Skin triangle has an invalid vertex index.");
                data.Indices[i] = index; Mix(read, index); if (data.Seeds[index] != 0) data.Relevant = true;
            }
            // A morph mesh with no actual drawn foot weights is harmless. Do not
            // call the stubbed GetBlendShapeWeight/GetBonesPerVertex methods.
            if (!data.Relevant) continue;
            if (mesh.blendShapeCount != 0) throw new Unsupported("Relevant foot morph geometry is unsupported.");
            Mix(read, (int)renderer.quality);
            var bind = mesh.bindposes;
            if (bind == null || bind.Length != bones.Length) throw new Unsupported("Foot bind poses differ from the bone mapping.");
            for (int i = 0; i < bind.Length; i++)
                for (int row = 0; row < 4; row++) for (int col = 0; col < 4; col++)
                {
                    float value = Element(bind[i], row, col);
                    if (!Finite(value)) throw new Unsupported("Foot bind pose is not finite.");
                    Mix(read, value.GetHashCode());
                }
            Scale(read, renderer.transform.localToWorldMatrix);
            var source = mesh.vertices;
            if (source == null || source.Length != count) throw new Unsupported("Source skin vertex count changed.");
            Bounds bounds = mesh.bounds; Vector3 min = bounds.min, max = bounds.max;
            if (!Finite(min) || !Finite(max)) throw new Unsupported("Foot mesh bounds are not finite.");
            for (int i = 0; i < count; i++)
            {
                Vector3 p = source[i];
                if (!Finite(p) || p.x < min.x - .001f || p.y < min.y - .001f || p.z < min.z - .001f ||
                    p.x > max.x + .001f || p.y > max.y + .001f || p.z > max.z + .001f)
                    throw new Unsupported("Source skin vertex exceeds its bounds.");
                Mix(read, p.x.GetHashCode()); Mix(read, p.y.GetHashCode()); Mix(read, p.z.GetHashCode());
            }
            data.MapLeft = new int[count]; data.MapRight = new int[count]; read.Meshes.Add(data);
        }
        if (read.Meshes.Count == 0) throw new Unsupported("Visible skin has no actual foot geometry.");
        return read;
    }
    private static void Weight(Read read, int index, float weight,
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform> bones,
        Transform lf, Transform lt, Transform rf, Transform rt, ref double sum, ref double left, ref double right, ref double other)
    {
        if (!Finite(weight) || weight < 0 || weight > 1) throw new Unsupported("Skin weight is invalid.");
        Mix(read, index); Mix(read, weight.GetHashCode()); sum += weight;
        if (weight == 0) return;
        if (index < 0 || index >= bones.Length || bones[index] == null) throw new Unsupported("Skin weight has an invalid bone index.");
        var bone = bones[index];
        if (bone == lf || bone == lt) left += weight; else if (bone == rf || bone == rt) right += weight; else other += weight;
    }
    private static void Bake(Read read, SoleFrame left, SoleFrame right, out Patch lp, out Patch rp)
    {
        lp = new Patch(); rp = new Patch();
        for (int n = 0; n < read.Meshes.Count; n++)
        {
            var data = read.Meshes[n]; Mesh baked = null;
            try
            {
                baked = new Mesh(); data.Renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                if (vertices == null || vertices.Length != data.Count || baked.vertexCount != data.Count)
                    throw new Unsupported("Baked skin vertex count changed.");
                var topology = baked.triangles;
                if (topology == null || topology.Length != data.Indices.Length)
                    throw new Unsupported("Baked skin topology count changed.");
                for (int i = 0; i < topology.Length; i++)
                    if (topology[i] != data.Indices[i]) throw new Unsupported("Baked skin topology changed.");
                Matrix4x4 matrix = data.Renderer.transform.localToWorldMatrix;
                for (int i = 0; i < data.Indices.Length; i += 3)
                {
                    int a = data.Indices[i], b = data.Indices[i + 1], c = data.Indices[i + 2];
                    int seeds = data.Seeds[a] | data.Seeds[b] | data.Seeds[c];
                    if ((seeds & 1) != 0) lp.Triangle(data, a, b, c, 1, matrix, vertices, left);
                    if ((seeds & 2) != 0) rp.Triangle(data, a, b, c, 2, matrix, vertices, right);
                }
            }
            finally { if (baked != null) UnityEngine.Object.Destroy(baked); }
        }
    }
    private static void Scale(Read read, Matrix4x4 matrix)
    { Scale(read, Affine.Linear(matrix)); }
    private static void Scale(Read read, Affine matrix)
    {
        double x = 0, y = 0, z = 0, xy = 0, xz = 0, yz = 0;
        for (int r = 0; r < 3; r++)
        {
            double a = matrix.At(r, 0), b = matrix.At(r, 1), c = matrix.At(r, 2);
            if (!Finite(a) || !Finite(b) || !Finite(c)) throw new Unsupported("Foot scale is not finite.");
            x += a * a; y += b * b; z += c * c; xy += a * b; xz += a * c; yz += b * c;
        }
        double max = Math.Max(x, Math.Max(y, z));
        if (Math.Min(x, Math.Min(y, z)) < 1e-12 || max > 1e8 ||
            Math.Abs(x - y) > max * .0001 || Math.Abs(x - z) > max * .0001 ||
            Math.Abs(xy) > max * .0001 || Math.Abs(xz) > max * .0001 || Math.Abs(yz) > max * .0001)
            throw new Unsupported("Nonuniform or sheared foot geometry is unsupported.");
        double determinant = matrix.At(0, 0) * (matrix.At(1, 1) * matrix.At(2, 2) - matrix.At(1, 2) * matrix.At(2, 1)) -
            matrix.At(0, 1) * (matrix.At(1, 0) * matrix.At(2, 2) - matrix.At(1, 2) * matrix.At(2, 0)) +
            matrix.At(0, 2) * (matrix.At(1, 0) * matrix.At(2, 1) - matrix.At(1, 1) * matrix.At(2, 0));
        read.Pose.Add(x); read.Pose.Add(y); read.Pose.Add(z); read.Pose.Add(determinant < 0 ? -1 : 1);
    }
    private static float Element(Matrix4x4 m, int row, int col)
    {
        if (row == 0) return col == 0 ? m.m00 : col == 1 ? m.m01 : col == 2 ? m.m02 : m.m03;
        if (row == 1) return col == 0 ? m.m10 : col == 1 ? m.m11 : col == 2 ? m.m12 : m.m13;
        if (row == 2) return col == 0 ? m.m20 : col == 1 ? m.m21 : col == 2 ? m.m22 : m.m23;
        return col == 0 ? m.m30 : col == 1 ? m.m31 : col == 2 ? m.m32 : m.m33;
    }
    private static bool Near(double[] a, List<double> b)
    {
        if (a == null || a.Length != b.Count) return false;
        for (int i = 0; i < a.Length; i++)
        {
            // Four bone signatures each contain scale(4), basis(9), position(3).
            // Position and basis remain separate tolerances; the short toe offset
            // now comes from the local chain rather than large-world positions.
            double tolerance = i < 64 && i % 16 >= 13 ? PositionTolerance : PoseTolerance;
            if (!Finite(b[i]) || Math.Abs(a[i] - b[i]) > tolerance) return false;
        }
        return true;
    }
    private static bool Near(double[] a, double[] b, double tolerance)
    { if (a == null || b == null || a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (!Finite(b[i]) || Math.Abs(a[i] - b[i]) > tolerance) return false; return true; }
    private static bool SameIndices(int[] a, int[] b)
    { if (a == null || b == null || a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
    private static void Mix(Read read, long value) { unchecked { read.Key = (read.Key ^ value) * 1099511628211; } }
    private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    private static bool Finite(Vector3 value) { return Finite(value.x) && Finite(value.y) && Finite(value.z); }
    private static void Bump() { version = version == int.MaxValue ? 1 : version + 1; }
    private static void Unavailable(string note)
    {
        if (active == null) return;
        bool changed = active.Ready || active.Fresh || active.Note != note;
        active.Ready = active.Fresh = false; active.Note = note; active.NextAttempt = Math.Max(active.NextAttempt, active.NextCheck);
        if (changed) Bump();
    }
    private static void InvalidateAll() { first.Clear(); third.Clear(); Bump(); }
}
