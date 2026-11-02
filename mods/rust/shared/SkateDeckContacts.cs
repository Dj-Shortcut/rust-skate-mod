using System;
using System.Collections.Generic;

namespace Shortcut.RustMod
{
    // Rigid shoe geometry in its measured sole frame: +Y sole up, +Z flat toe
    // forward, +X right. The caller owns that binding; no ankle offset is guessed.
    public sealed class SkateFootContactProfile
    {
        public const int MaximumVertices = 4096, MaximumTriangles = 8192;
        private readonly SkateVector[] vertices;
        private readonly int[] indices;
        private SkateFootContactProfile(SkateVector[] points, int[] triangles)
        { vertices = points; indices = triangles; }
        public int VertexCount { get { return vertices.Length; } }
        public int TriangleCount { get { return indices.Length / 3; } }
        internal SkateVector Vertex(int n) { return vertices[n]; }
        internal int Index(int n) { return indices[n]; }

        public static bool TryCreate(double[] xyz, int[] triangles, out SkateFootContactProfile profile, out string error)
        {
            profile = null; error = null;
            if (xyz == null || triangles == null || xyz.Length < 9 || xyz.Length % 3 != 0 ||
                xyz.Length / 3 > MaximumVertices || triangles.Length < 3 || triangles.Length % 3 != 0 ||
                triangles.Length / 3 > MaximumTriangles)
            { error = "Invalid foot contact geometry size."; return false; }
            var points = new SkateVector[xyz.Length / 3];
            for (int n = 0; n < points.Length; n++)
            {
                var p = new SkateVector(xyz[n * 3], xyz[n * 3 + 1], xyz[n * 3 + 2]);
                if (!SkateDeckContactMath.Local(p)) { error = "Foot contact vertices exceed their local bounds."; return false; }
                points[n] = p;
            }
            for (int n = 0; n < triangles.Length; n += 3)
            {
                int a = triangles[n], b = triangles[n + 1], c = triangles[n + 2];
                if (a < 0 || b < 0 || c < 0 || a >= points.Length || b >= points.Length || c >= points.Length ||
                    SkateBoardPath.Cross(points[b] - points[a], points[c] - points[a]).LengthSquared <= 1e-24)
                { error = "Foot contact triangles are invalid or degenerate."; return false; }
            }
            profile = new SkateFootContactProfile(points, (int[])triangles.Clone()); return true;
        }
    }

    // The actual primary Deck part, including its bolt geometry. A legacy mesh
    // without part metadata cannot silently become a box or a flat support plane.
    public sealed class SkateDeckContactSurface
    {
        private readonly SkateDeckContactMath.Triangle[] triangles;
        internal readonly double MinimumX, MaximumX, MinimumZ, MaximumZ;
        public int VertexCount { get; private set; }
        public int TriangleCount { get { return triangles.Length; } }
        internal SkateDeckContactMath.Triangle Triangle(int n) { return triangles[n]; }
        private SkateDeckContactSurface(int vertices, SkateDeckContactMath.Triangle[] top)
        {
            VertexCount = vertices; triangles = top;
            double minX = double.PositiveInfinity, minZ = minX, maxX = double.NegativeInfinity, maxZ = maxX;
            for (int n = 0; n < top.Length; n++)
            {
                minX = Math.Min(minX, top[n].MinX); maxX = Math.Max(maxX, top[n].MaxX);
                minZ = Math.Min(minZ, top[n].MinZ); maxZ = Math.Max(maxZ, top[n].MaxZ);
            }
            MinimumX = minX; MaximumX = maxX; MinimumZ = minZ; MaximumZ = maxZ;
        }
        public static bool TryCreate(SkateBoardMesh mesh, double deckTop, out SkateDeckContactSurface surface, out string error)
        {
            surface = null; error = null;
            if (mesh == null || mesh.Positions == null || mesh.Triangles == null || mesh.Parts == null ||
                mesh.Positions.Length % 3 != 0 || mesh.Triangles.Length % 3 != 0 ||
                !SkateDeckContactMath.Finite(deckTop) || deckTop < 0 || deckTop > .5)
            { error = "Invalid deck contact mesh or reference height."; return false; }
            int primary = -1;
            for (int n = 0; n < mesh.Parts.Length; n++)
                if (mesh.Parts[n].Kind == SkateBoardPartKind.Deck && mesh.Parts[n].ParentIndex == -1)
                {
                    if (primary >= 0) { error = "Deck contact mesh has ambiguous primary parts."; return false; }
                    primary = n;
                }
            if (primary < 0) { error = "Deck contact mesh has no primary Deck part."; return false; }
            var part = mesh.Parts[primary]; int vertices = mesh.Positions.Length / 3;
            if (part.VertexStart < 0 || part.VertexCount < 3 || part.VertexCount > 16384 ||
                part.VertexStart > vertices - part.VertexCount || part.IndexStart < 0 || part.IndexCount < 3 ||
                part.IndexCount % 3 != 0 || part.IndexCount / 3 > 32768 ||
                part.IndexStart > mesh.Triangles.Length - part.IndexCount)
            { error = "Deck contact part ranges exceed their bounds."; return false; }
            var points = new SkateVector[part.VertexCount];
            for (int n = 0; n < points.Length; n++)
            {
                int at = (part.VertexStart + n) * 3;
                var p = new SkateVector(mesh.Positions[at], mesh.Positions[at + 1] - deckTop, mesh.Positions[at + 2]);
                if (!SkateDeckContactMath.Local(p)) { error = "Deck contact vertices exceed their local bounds."; return false; }
                points[n] = p;
            }
            var top = new List<SkateDeckContactMath.Triangle>(part.IndexCount / 3);
            for (int n = part.IndexStart; n < part.IndexStart + part.IndexCount; n += 3)
            {
                int a = mesh.Triangles[n] - part.VertexStart, b = mesh.Triangles[n + 1] - part.VertexStart,
                    c = mesh.Triangles[n + 2] - part.VertexStart;
                if (a < 0 || b < 0 || c < 0 || a >= points.Length || b >= points.Length || c >= points.Length)
                { error = "Deck contact indices leave the primary part."; return false; }
                var normal = SkateBoardPath.Cross(points[b] - points[a], points[c] - points[a]);
                if (normal.LengthSquared <= 1e-24) { error = "Deck contact mesh has degenerate triangles."; return false; }
                if (normal.Y > 1e-12) top.Add(new SkateDeckContactMath.Triangle(points[a], points[b], points[c], normal));
            }
            if (top.Count == 0) { error = "Deck contact mesh has no top-facing support triangles."; return false; }
            surface = new SkateDeckContactSurface(part.VertexCount, top.ToArray()); return true;
        }
    }

    // The actual overlap feature producing an accepted support offset. FootPoint
    // is before applying that offset; FootPoint + deck.Up * offset meets DeckPoint.
    // Triangle IDs refer to the bound profiles, including a rigid re-expression.
    public readonly struct SkateDeckContactWitness
    {
        public readonly bool Initialized;
        public readonly int FootTriangle, DeckTriangle;
        public readonly SkateVector FootPoint, DeckPoint;
        internal SkateDeckContactWitness(int footTriangle, int deckTriangle, SkateVector foot, SkateVector deck)
        { Initialized = true; FootTriangle = footTriangle; DeckTriangle = deckTriangle; FootPoint = foot; DeckPoint = deck; }
    }

    // One instance per foot/owner: cached grid, transformed vertices and clipping
    // buffers are reused. Do not call the same instance concurrently. Separate
    // fitters share immutable geometry and have no shared mutable scratch state.
    public sealed class SkateDeckFootFitter
    {
        internal SkateDeckContactSurface Surface { get { return surface; } }
        internal SkateFootContactProfile Profile { get { return profile; } }
        private const int MaximumGridReferences = 262144, MaximumWork = 1000000;
        private readonly SkateDeckContactSurface surface;
        private readonly SkateFootContactProfile profile;
        private readonly SkateVector[] transformed;
        private readonly int[] offsets, entries, visited;
        private readonly SkateDeckContactMath.Point[] polygonA = new SkateDeckContactMath.Point[8], polygonB = new SkateDeckContactMath.Point[8];
        private readonly int width, depth;
        private int stamp;
        public SkateDeckFootFitter(SkateDeckContactSurface deck, SkateFootContactProfile foot)
        {
            if (deck == null) throw new ArgumentNullException("deck");
            if (foot == null) throw new ArgumentNullException("foot");
            surface = deck; profile = foot; transformed = new SkateVector[foot.VertexCount]; visited = new int[deck.TriangleCount];
            int w = 16, d = 64, total;
            // Bound grid duplication even for deliberately huge overlapping faces.
            while (true)
            {
                total = GridReferences(w, d);
                if (total <= MaximumGridReferences) break;
                if (d >= w && d > 1) d /= 2; else w /= 2;
            }
            width = w; depth = d; offsets = new int[w * d + 1];
            for (int n = 0; n < deck.TriangleCount; n++)
            {
                var t = deck.Triangle(n); int x0, x1, z0, z1; Bins(t, w, d, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++) offsets[z * w + x + 1]++;
            }
            for (int n = 1; n < offsets.Length; n++) offsets[n] += offsets[n - 1];
            entries = new int[total]; var cursor = (int[])offsets.Clone();
            for (int n = 0; n < deck.TriangleCount; n++)
            {
                var t = deck.Triangle(n); int x0, x1, z0, z1; Bins(t, w, d, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++) entries[cursor[z * w + x]++] = n;
            }
        }

        // Centre the measured footprint, not its ankle, on an authored bolt region.
        // Vertical support still belongs to TryOffset and its existing bound.
        internal bool TryCenteredPlacement(SkateBoardPose deck, SkateBoardPose foot, double centreZ,
            out SkateBoardPose placed, out string error)
        {
            placed = default(SkateBoardPose); error = null;
            if (!SkateBoardPath.Valid(deck) || !SkateBoardPath.Valid(foot) || !deck.Initialized || !foot.Initialized ||
                !SkateDeckContactMath.Finite(centreZ) || Math.Abs(centreZ) > .25)
            { error = "Invalid authored landing placement frame."; return false; }
            double minX = double.PositiveInfinity, minZ = minX, maxX = double.NegativeInfinity, maxZ = maxX;
            for (int n = 0; n < profile.VertexCount; n++)
            {
                var p = foot.Rotate(profile.Vertex(n));
                double x = SkateVector.Dot(p, deck.Right), z = SkateVector.Dot(p, deck.Forward);
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z);
            }
            double height = SkateVector.Dot(foot.Origin - deck.Origin, deck.Up);
            var origin = deck.Origin + deck.Right * ((surface.MinimumX + surface.MaximumX - minX - maxX) * .5) +
                deck.Forward * (centreZ - (minZ + maxZ) * .5) + deck.Up * height;
            if ((origin - foot.Origin).Length > .25)
            { error = "Authored landing placement exceeds its horizontal bound."; return false; }
            placed = new SkateBoardPose(origin, foot.Forward, foot.Up); return true;
        }

        // Charge setup has its own measured-footprint domain. This does not
        // widen the separate +/- .25m landing-placement contract above.
        internal bool TryChargePlacement(SkateBoardPose deck, SkateBoardPose foot, double centreZ,
            out SkateBoardPose placed, out string error)
        {
            placed = default(SkateBoardPose); error = null;
            if (!SkateBoardPath.Valid(deck) || !deck.Initialized || !SkateBoardPath.Valid(foot) || !foot.Initialized ||
                !SkateDeckContactMath.Finite(centreZ) || centreZ < surface.MinimumZ || centreZ > surface.MaximumZ)
            { error = "Invalid measured charge placement frame."; return false; }
            double minX = double.PositiveInfinity, minZ = minX, maxX = double.NegativeInfinity, maxZ = maxX;
            for (int n = 0; n < profile.VertexCount; n++)
            {
                var p = foot.Rotate(profile.Vertex(n));
                double x = SkateVector.Dot(p, deck.Right), z = SkateVector.Dot(p, deck.Forward);
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z);
            }
            double half = (maxZ - minZ) * .5, lo = surface.MinimumZ + half, hi = surface.MaximumZ - half;
            if (lo > hi) { error = "Measured shoe is longer than the charge support outline."; return false; }
            centreZ = Math.Max(lo, Math.Min(hi, centreZ));
            double centreX = (surface.MinimumX + surface.MaximumX) * .5;
            bool supported = false;
            for (int n = 0; n < surface.TriangleCount; n++)
            {
                var t = surface.Triangle(n);
                double a = (t.B.X - t.A.X) * (centreZ - t.A.Z) - (t.B.Z - t.A.Z) * (centreX - t.A.X);
                double b = (t.C.X - t.B.X) * (centreZ - t.B.Z) - (t.C.Z - t.B.Z) * (centreX - t.B.X);
                double c = (t.A.X - t.C.X) * (centreZ - t.C.Z) - (t.A.Z - t.C.Z) * (centreX - t.C.X);
                if ((a >= -1e-10 && b >= -1e-10 && c >= -1e-10) || (a <= 1e-10 && b <= 1e-10 && c <= 1e-10))
                { supported = true; break; }
            }
            if (!supported) { error = "Charge footprint centre leaves the actual deck outline."; return false; }
            double height = SkateVector.Dot(foot.Origin - deck.Origin, deck.Up);
            var origin = deck.Origin + deck.Right * (centreX - (minX + maxX) * .5) +
                deck.Forward * (centreZ - (minZ + maxZ) * .5) + deck.Up * height;
            if ((origin - foot.Origin).Length > .35)
            { error = "Charge placement exceeds its bounded setup travel."; return false; }
            placed = new SkateBoardPose(origin, foot.Forward, foot.Up); return true;
        }

        public bool TryOffset(SkateBoardPose deck, SkateBoardPose foot, double maximumOffset, out double offset, out string error)
        {
            SkateDeckContactWitness witness;
            return TryOffset(deck, foot, maximumOffset, out offset, out witness, out error);
        }

        public bool TryOffset(SkateBoardPose deck, SkateBoardPose foot, double maximumOffset,
            out double offset, out SkateDeckContactWitness witness, out string error)
        {
            offset = 0; witness = default(SkateDeckContactWitness); error = null;
            if (!SkateBoardPath.Valid(deck) || !deck.Initialized || !SkateBoardPath.Valid(foot) || !foot.Initialized ||
                !SkateDeckContactMath.Finite(maximumOffset) || maximumOffset < 0 || maximumOffset > .5)
            { error = "Invalid deck or foot support frame or offset bound."; return false; }
            // Normalize only accepted floating-point basis drift. A steep/inverted
            // sole is not silently replaced by a flat footprint or a convex hull.
            var up = Unit(deck.Up); var forward = Unit(deck.Forward - up * SkateVector.Dot(deck.Forward, up));
            var right = SkateBoardPath.Cross(up, forward);
            var footUp = Unit(foot.Up); var footForward = Unit(foot.Forward - footUp * SkateVector.Dot(foot.Forward, footUp));
            if (SkateVector.Dot(footUp, up) <= .2) { error = "Foot support projection is too steep or inverted."; return false; }
            var footRight = SkateBoardPath.Cross(footUp, footForward); var origin = foot.Origin - deck.Origin;
            if (origin.LengthSquared > 4) { error = "Foot support frame is outside the local fitting range."; return false; }
            for (int n = 0; n < transformed.Length; n++)
            {
                var p = profile.Vertex(n); var world = origin + footRight * p.X + footUp * p.Y + footForward * p.Z;
                transformed[n] = new SkateVector(SkateVector.Dot(world, right), SkateVector.Dot(world, up), SkateVector.Dot(world, forward));
            }
            double best = double.NegativeInfinity; int work = 0;
            for (int n = 0; n < profile.TriangleCount; n++)
            {
                var a = transformed[profile.Index(n * 3)]; var b = transformed[profile.Index(n * 3 + 1)]; var c = transformed[profile.Index(n * 3 + 2)];
                var normal = SkateBoardPath.Cross(b - a, c - a);
                // A zero-area projection does not define a finite height function.
                // Adjacent lower faces still contribute, but a vertical-only
                // profile has no measurable support footprint and fails below.
                if (Math.Abs(normal.Y) <= 1e-12) continue;
                var triangle = new SkateDeckContactMath.Triangle(a, b, c, normal);
                if (triangle.MaxX < surface.MinimumX - 1e-12 || triangle.MinX > surface.MaximumX + 1e-12 ||
                    triangle.MaxZ < surface.MinimumZ - 1e-12 || triangle.MinZ > surface.MaximumZ + 1e-12) continue;
                if (stamp == int.MaxValue) { Array.Clear(visited, 0, visited.Length); stamp = 0; }
                stamp++;
                int x0, x1, z0, z1; Bins(triangle, width, depth, out x0, out x1, out z0, out z1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    int cell = z * width + x;
                    for (int at = offsets[cell]; at < offsets[cell + 1]; at++)
                    {
                        if (++work > MaximumWork) { error = "Deck support work exceeds its bound."; return false; }
                        int id = entries[at]; if (visited[id] == stamp) continue; visited[id] = stamp;
                        var top = surface.Triangle(id);
                        if (top.MaxX < triangle.MinX - 1e-12 || top.MinX > triangle.MaxX + 1e-12 ||
                            top.MaxZ < triangle.MinZ - 1e-12 || top.MinZ > triangle.MaxZ + 1e-12 ||
                            top.MaxY - triangle.MinY < best - 1e-12) continue;
                        double gap; SkateDeckContactMath.Point point;
                        int result = Intersect(triangle, top, out gap, out point);
                        if (result < 0) { error = "Deck support projection is numerically unstable."; return false; }
                        if (result > 0)
                        {
                            if (gap > best)
                                witness = new SkateDeckContactWitness(n, id,
                                    deck.Origin + right * point.X + up * triangle.Height(point) + forward * point.Z,
                                    deck.Origin + right * point.X + up * top.Height(point) + forward * point.Z);
                            best = Math.Max(best, gap);
                        }
                    }
                }
            }
            if (double.IsNegativeInfinity(best)) { error = "Foot has no deck support footprint."; return false; }
            if (!SkateDeckContactMath.Finite(best) || Math.Abs(best) > maximumOffset)
            { error = "Required deck support offset exceeds its bound."; return false; }
            offset = best; return true;
        }

        private int Intersect(SkateDeckContactMath.Triangle foot, SkateDeckContactMath.Triangle deck, out double gap,
            out SkateDeckContactMath.Point point)
        {
            gap = 0; point = default(SkateDeckContactMath.Point); var source = polygonA; var target = polygonB; int count = 3;
            source[0] = new SkateDeckContactMath.Point(foot.A.X, foot.A.Z);
            source[1] = new SkateDeckContactMath.Point(foot.B.X, foot.B.Z);
            source[2] = new SkateDeckContactMath.Point(foot.C.X, foot.C.Z);
            for (int edge = 0; edge < 3; edge++)
            {
                var a = edge == 0 ? deck.A : edge == 1 ? deck.B : deck.C;
                var b = edge == 0 ? deck.B : edge == 1 ? deck.C : deck.A;
                int next = 0; var previous = source[count - 1];
                double pd = Distance(a, b, previous) * deck.Sign; bool pin = pd >= -1e-12;
                for (int n = 0; n < count; n++)
                {
                    var current = source[n]; double cd = Distance(a, b, current) * deck.Sign; bool cin = cd >= -1e-12;
                    if (cin != pin)
                    {
                        double t = pd / (pd - cd); t = Math.Max(0, Math.Min(1, t));
                        var hit = new SkateDeckContactMath.Point(previous.X + (current.X - previous.X) * t, previous.Z + (current.Z - previous.Z) * t);
                        if (!Append(target, ref next, hit)) return -1;
                    }
                    if (cin && !Append(target, ref next, current)) return -1;
                    previous = current; pd = cd; pin = cin;
                }
                if (next > 1 && Same(target[0], target[next - 1])) next--;
                if (next == 0) return 0;
                count = next; var swap = source; source = target; target = swap;
            }
            gap = double.NegativeInfinity;
            for (int n = 0; n < count; n++)
            {
                double value = deck.Height(source[n]) - foot.Height(source[n]);
                if (value > gap) point = source[n];
                gap = Math.Max(gap, value);
            }
            return 1;
        }
        private static double Distance(SkateVector a, SkateVector b, SkateDeckContactMath.Point p)
        { return (b.X - a.X) * (p.Z - a.Z) - (b.Z - a.Z) * (p.X - a.X); }
        private static bool Append(SkateDeckContactMath.Point[] points, ref int count, SkateDeckContactMath.Point p)
        {
            if (count > 0 && Same(points[count - 1], p)) return true;
            if (count >= points.Length) return false;
            points[count++] = p; return true;
        }
        private static bool Same(SkateDeckContactMath.Point a, SkateDeckContactMath.Point b)
        { double x = a.X - b.X, z = a.Z - b.Z; return x * x + z * z <= 1e-24; }
        private static SkateVector Unit(SkateVector value) { return value * (1 / Math.Sqrt(value.LengthSquared)); }
        private int GridReferences(int w, int d)
        {
            int total = 0;
            for (int n = 0; n < surface.TriangleCount; n++)
            {
                int x0, x1, z0, z1; Bins(surface.Triangle(n), w, d, out x0, out x1, out z0, out z1);
                total += (x1 - x0 + 1) * (z1 - z0 + 1);
                if (total > MaximumGridReferences) return total;
            }
            return total;
        }
        private void Bins(SkateDeckContactMath.Triangle triangle, int w, int d, out int x0, out int x1, out int z0, out int z1)
        {
            // Match the clipping tolerance at cell boundaries, so numerically
            // coincident edges are not excluded by the broad phase alone.
            x0 = Bin(triangle.MinX - 1e-12, surface.MinimumX, surface.MaximumX, w); x1 = Bin(triangle.MaxX + 1e-12, surface.MinimumX, surface.MaximumX, w);
            z0 = Bin(triangle.MinZ - 1e-12, surface.MinimumZ, surface.MaximumZ, d); z1 = Bin(triangle.MaxZ + 1e-12, surface.MinimumZ, surface.MaximumZ, d);
        }
        private static int Bin(double value, double lower, double upper, int count)
        { return Math.Max(0, Math.Min(count - 1, (int)Math.Floor((value - lower) / (upper - lower) * count))); }
    }

    internal static class SkateDeckContactMath
    {
        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        internal static bool Local(SkateVector p)
        { return Finite(p.X) && Finite(p.Y) && Finite(p.Z) && Math.Abs(p.X) <= .5 && Math.Abs(p.Y) <= .5 && Math.Abs(p.Z) <= .5; }
        internal readonly struct Point
        {
            internal readonly double X, Z;
            internal Point(double x, double z) { X = x; Z = z; }
        }
        internal readonly struct Triangle
        {
            internal readonly SkateVector A, B, C;
            internal readonly double MinX, MaxX, MinY, MaxY, MinZ, MaxZ, HeightX, HeightZ, Sign;
            internal Triangle(SkateVector a, SkateVector b, SkateVector c, SkateVector normal)
            {
                A = a; B = b; C = c;
                MinX = Math.Min(a.X, Math.Min(b.X, c.X)); MaxX = Math.Max(a.X, Math.Max(b.X, c.X));
                MinY = Math.Min(a.Y, Math.Min(b.Y, c.Y)); MaxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
                MinZ = Math.Min(a.Z, Math.Min(b.Z, c.Z)); MaxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
                HeightX = -normal.X / normal.Y; HeightZ = -normal.Z / normal.Y;
                Sign = normal.Y > 0 ? -1 : 1;
            }
            // Evaluate relative to a triangle vertex to avoid cancellation of a
            // large absolute plane constant on almost vertical shoe side faces.
            internal double Height(Point point) { return A.Y + HeightX * (point.X - A.X) + HeightZ * (point.Z - A.Z); }
        }
    }
}
