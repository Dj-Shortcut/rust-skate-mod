using System;
using System.Collections.Generic;

namespace Shortcut.RustMod
{
    public enum SkateBoardPartKind { Deck = 0, Truck = 1, Wheel = 2, Fixed = 3, Bushing = 4 }

    public struct SkateBoardPart
    {
        public readonly SkateBoardPartKind Kind;
        public readonly int ParentIndex, VertexStart, VertexCount, IndexStart, IndexCount;
        public readonly float PivotX, PivotY, PivotZ, AxisX, AxisY, AxisZ;
        public readonly float RestQX, RestQY, RestQZ, RestQW;
        public readonly int BushingSeat; // +1 top, -1 bottom, otherwise zero

        internal SkateBoardPart(SkateBoardPartKind kind, int parent, int vertexStart, int vertexCount,
                                int indexStart, int indexCount, float px, float py, float pz,
                                float ax, float ay, float az, float qx = 0, float qy = 0, float qz = 0, float qw = 1, int seat = 0)
        {
            Kind = kind; ParentIndex = parent; VertexStart = vertexStart; VertexCount = vertexCount;
            IndexStart = indexStart; IndexCount = indexCount;
            PivotX = px; PivotY = py; PivotZ = pz; AxisX = ax; AxisY = ay; AxisZ = az;
            RestQX = qx; RestQY = qy; RestQZ = qz; RestQW = qw; BushingSeat = seat;
        }
    }

    public sealed class SkateBoardMesh
    {
        public readonly float[] Positions;
        public readonly float[] Normals;
        public readonly int[] Triangles;
        public readonly float[] Colours;
        public readonly float[] UVs;
        public readonly SkateBoardPart[] Parts;
        public readonly byte[] TextureRgba;
        public readonly int TextureWidth, TextureHeight;

        private SkateBoardMesh(float[] positions, float[] normals, int[] triangles, float[] colours)
            : this(positions, normals, triangles, colours, new float[0], new SkateBoardPart[0], new byte[0], 0, 0) { }

        private SkateBoardMesh(float[] positions, float[] normals, int[] triangles, float[] colours,
                               float[] uvs, SkateBoardPart[] parts, byte[] texture, int width, int height)
        {
            Positions = positions; Normals = normals; Triangles = triangles; Colours = colours;
            UVs = uvs; Parts = parts; TextureRgba = texture; TextureWidth = width; TextureHeight = height;
        }

        public static SkateBoardMesh Create()
        {
            var builder = new Builder();
            builder.Deck();
            builder.Truck(-0.245f);
            builder.Truck(0.245f);
            return builder.Build();
        }

        public static SkateBoardMesh CreateDetailed() { return CreateDetailed(0x534b4154); }

        public static SkateBoardMesh CreateDetailed(int seed)
        {
            var builder = new DetailedBuilder();
            builder.Deck();
            builder.Truck(-0.245f);
            builder.Truck(0.245f);
            builder.Wheel(-0.082f, -0.245f, 1);
            builder.Wheel(0.082f, -0.245f, 1);
            builder.Wheel(-0.082f, 0.245f, 2);
            builder.Wheel(0.082f, 0.245f, 2);
            builder.FixedTruck(-0.245f);
            builder.FixedTruck(0.245f);
            builder.Bushing(-0.245f, true);
            builder.Bushing(-0.245f, false);
            builder.Bushing(0.245f, true);
            builder.Bushing(0.245f, false);
            return builder.Build(seed);
        }

        private sealed class DetailedBuilder
        {
            private readonly List<float> positions = new List<float>(6000);
            private readonly List<float> normals = new List<float>(6000);
            private readonly List<int> triangles = new List<int>(9000);
            private readonly List<float> colours = new List<float>(8000);
            private readonly List<float> uvs = new List<float>(4000);
            private readonly List<SkateBoardPart> parts = new List<SkateBoardPart>(13);
            private int vertexStart, indexStart;
            private const int GripTile = 0, GraphicTile = 1, EdgeTile = 2, MetalTile = 3, WheelTile = 4;

            public SkateBoardMesh Build(int seed)
            {
                return new SkateBoardMesh(positions.ToArray(), normals.ToArray(), triangles.ToArray(), colours.ToArray(),
                    uvs.ToArray(), parts.ToArray(), Atlas(seed), 256, 256);
            }

            private void Begin() { vertexStart = positions.Count / 3; indexStart = triangles.Count; }

            // Ranges index the full board-space arrays; IndexCount counts entries, not triangle faces.
            private void End(SkateBoardPartKind kind, int parent, Point pivot, Point axis, float qx = 0, float qw = 1, int seat = 0)
            {
                parts.Add(new SkateBoardPart(kind, parent, vertexStart, positions.Count / 3 - vertexStart,
                    indexStart, triangles.Count - indexStart, pivot.X, pivot.Y, pivot.Z, axis.X, axis.Y, axis.Z, qx, 0, 0, qw, seat));
            }

            public void Deck()
            {
                Begin();
                var outline = new Point[40];
                int cursor = 0;
                for (int i = 0; i <= 18; i++)
                {
                    double angle = i * Math.PI / 18;
                    outline[cursor++] = new Point(0.102f * (float)Math.Cos(angle), 0, 0.288f + 0.112f * (float)Math.Sin(angle));
                }
                outline[cursor++] = new Point(-0.102f, 0, 0);
                for (int i = 0; i <= 18; i++)
                {
                    double angle = Math.PI + i * Math.PI / 18;
                    outline[cursor++] = new Point(0.102f * (float)Math.Cos(angle), 0, -0.288f + 0.112f * (float)Math.Sin(angle));
                }
                outline[cursor] = new Point(0.102f, 0, 0);
                var outward = new Point[40];
                for (int i = 0; i < 40; i++)
                {
                    Point tangent = outline[(i + 1) % 40] - outline[(i + 39) % 40];
                    outward[i] = Unit(new Point(tangent.Z, 0, -tangent.X));
                }
                Surface(outline, outward, false);
                Surface(outline, outward, true);
                var edge = new int[5, 41];
                float[] expansion = { 0, 0.0014142f, 0.002f, 0.0014142f, 0 };
                float[] depth = { 0, 0.0013f, 0.0045f, 0.0077f, 0.009f };
                for (int layer = 0; layer < 5; layer++)
                    for (int i = 0; i <= 40; i++)
                    {
                        int at = i % 40;
                        Point p = outline[at], h = outward[at];
                        var point = new Point(p.X + expansion[layer] * h.X, Height(p.X, p.Z) - depth[layer], p.Z + expansion[layer] * h.Z);
                        Point normal;
                        if (layer == 0 || layer == 4) normal = EdgeNormal(p, h, layer == 4);
                        else
                        {
                            Point tangent = EdgePoint(outline[(at + 1) % 40], outward[(at + 1) % 40], expansion[layer], depth[layer]) -
                                EdgePoint(outline[(at + 39) % 40], outward[(at + 39) % 40], expansion[layer], depth[layer]);
                            Point across = EdgePoint(p, h, expansion[layer + 1], depth[layer + 1]) - EdgePoint(p, h, expansion[layer - 1], depth[layer - 1]);
                            normal = Unit(Point.Cross(tangent, across));
                        }
                        edge[layer, i] = Vertex(point, normal, EdgeTile, i / 40f, layer / 4f);
                    }
                for (int layer = 0; layer < 4; layer++)
                    for (int i = 0; i < 40; i++) Quad(edge[layer, i], edge[layer, i + 1], edge[layer + 1, i + 1], edge[layer + 1, i]);
                foreach (float z in new[] { -0.245f, 0.245f })
                {
                    // Mounting plate retains the old top face, footprint and bolt
                    // geometry. Its underside makes room for the actual socket.
                    Box(new Point(0, 0.08225f, z), new Point(0.031f, 0.00075f, 0.025f), MetalTile);
                    foreach (float x in new[] { -0.022f, 0.022f }) foreach (float dz in new[] { -0.018f, 0.018f })
                        Cylinder(new Point(x, Height(x, z + dz) + 0.0007f, z + dz), new Point(0, 1, 0), 0.0028f, 0.0014f, 6, MetalTile);
                }
                End(SkateBoardPartKind.Deck, -1, new Point(0, 0.092f, 0), new Point(0, 1, 0));
            }

            private void Surface(Point[] outline, Point[] outward, bool bottom)
            {
                float[] scales = { 0.17f, 0.33f, 0.5f, 0.67f, 0.8f, 0.9f, 1 };
                int tile = bottom ? GraphicTile : GripTile;
                int centre = Vertex(new Point(0, 0.092f - (bottom ? 0.009f : 0), 0), new Point(0, bottom ? -1 : 1, 0), tile, 0.5f, 0.5f);
                var rings = new int[scales.Length, 40];
                for (int ring = 0; ring < scales.Length; ring++)
                    for (int i = 0; i < 40; i++)
                    {
                        var p = new Point(outline[i].X * scales[ring], 0, outline[i].Z * scales[ring]);
                        Point normal = ring == scales.Length - 1 ? EdgeNormal(p, outward[i], bottom) : SurfaceNormal(p.X, p.Z, bottom);
                        rings[ring, i] = Vertex(new Point(p.X, Height(p.X, p.Z) - (bottom ? 0.009f : 0), p.Z), normal, tile,
                            0.5f + p.X / 0.208f, 0.5f + p.Z / 0.804f);
                    }
                for (int i = 0; i < 40; i++) Face(centre, rings[0, i], rings[0, (i + 1) % 40]);
                for (int ring = 0; ring < scales.Length - 1; ring++)
                    for (int i = 0; i < 40; i++) Quad(rings[ring, i], rings[ring, (i + 1) % 40], rings[ring + 1, (i + 1) % 40], rings[ring + 1, i]);
            }

            private static float Height(float x, float z)
            {
                float kick = Math.Max(0, (Math.Abs(z) - 0.245f) / 0.155f);
                return 0.092f + 0.05f * kick * kick + 0.008f * (x / 0.102f) * (x / 0.102f);
            }
            private static Point SurfaceNormal(float x, float z, bool bottom)
            {
                float dyz = 0.1f * Math.Max(0, (Math.Abs(z) - 0.245f) / 0.155f) / 0.155f * Math.Sign(z);
                var normal = Unit(new Point(-0.016f * x / (0.102f * 0.102f), 1, -dyz));
                return bottom ? new Point(-normal.X, -normal.Y, -normal.Z) : normal;
            }
            private static Point EdgeNormal(Point p, Point outward, bool bottom)
            {
                Point normal = SurfaceNormal(p.X, p.Z, bottom);
                return Unit(new Point(normal.X + 0.35f * outward.X, normal.Y, normal.Z + 0.35f * outward.Z));
            }
            private static Point EdgePoint(Point p, Point outward, float expansion, float depth)
            { return new Point(p.X + expansion * outward.X, Height(p.X, p.Z) - depth, p.Z + expansion * outward.Z); }

            public void Truck(float z)
            {
                Begin();
                float side = Math.Sign(z);
                var axis = new Point(0, 0.70710677f, -side * 0.70710677f);
                var pivot = new Point(0, 0.059f, z);
                Cylinder(new Point(0, 0.032f, z), new Point(1, 0, 0), 0.005f, 0.125f, 12, MetalTile);
                Cylinder(new Point(0, 0.027f, z), new Point(1, 0, 0), 0.0035f, 0.178f, 12, MetalTile);
                Tube(pivot, axis, (float)SkateBoardTruckKinematics.EyeRadius, 0.0065f, 0.005f, 16, MetalTile);
                // Outboard yoke joins the eye rim, leaving its lower washer's
                // complete steering/load envelope clear of the diagonal arms.
                Link(new Point(-0.015f, 0.055f, z), new Point(-0.044f, 0.034f, z), 0.0048f);
                Link(new Point(0.015f, 0.055f, z), new Point(0.044f, 0.034f, z), 0.0048f);
                var cup = new Point(0, (float)SkateBoardTruckKinematics.CupY, z + side * (float)SkateBoardTruckKinematics.CupForward);
                var steerAxis = Unit(new Point(0, (float)SkateBoardTruckKinematics.CupY - pivot.Y, side * (float)SkateBoardTruckKinematics.CupForward));
                var neck = Along(cup, steerAxis, -.008f);
                Link(cup, neck, .0025f);
                Link(neck, new Point(0, .0682f, z + side * .0092f), .003f);
                Sphere(cup, .0043f);
                End(SkateBoardPartKind.Truck, 0, pivot, steerAxis);
            }

            public void FixedTruck(float z)
            {
                Begin();
                var pivot = new Point(0, 0.059f, z);
                var axis = new Point(0, 0.70710677f, -Math.Sign(z) * 0.70710677f);
                Cylinder(Along(pivot, axis, 0.001f), axis, 0.0025f, 0.046f, 12, MetalTile);
                Tube(Along(pivot, axis, -0.020f), axis, 0.010f, 0.0026f, 0.002f, 16, MetalTile);
                Tube(Along(pivot, axis, 0.022f), axis, 0.010f, 0.0026f, 0.002f, 16, MetalTile);
                Tube(Along(pivot, axis, 0.024f), axis, 0.0046f, 0.0026f, 0.004f, 6, MetalTile);
                var cup = new Point(0, (float)SkateBoardTruckKinematics.CupY, z + Math.Sign(z) * (float)SkateBoardTruckKinematics.CupForward);
                var cupAxis = Unit(new Point(0, (float)SkateBoardTruckKinematics.CupY - pivot.Y, Math.Sign(z) * (float)SkateBoardTruckKinematics.CupForward));
                // The triangulated inner planes, not just their vertex radius,
                // must clear the complete 4.3mm pivot ball.
                Socket(cup, cupAxis, .0062f, .00445f, 32, 8);
                End(SkateBoardPartKind.Fixed, 0, pivot, new Point(0, 1, 0));
            }

            public void Bushing(float z, bool top)
            {
                Begin();
                float sign = Math.Sign(z), seat = top ? 0.021f : -0.019f, tip = top ? 0.0025f : -0.0025f;
                var pivot = new Point(0, 0.059f, z);
                var axis = new Point(0, 0.70710677f, -sign * 0.70710677f);
                var origin = Along(pivot, axis, seat);
                Barrel(origin, axis, tip - seat, 0.0093f, 0.003f, 16);
                // Geometry remains in board space; the adapter inverse-transforms
                // it before installing this fixed local-Y kingpin alignment.
                End(SkateBoardPartKind.Bushing, 0, origin, new Point(0, 1, 0), -sign * 0.38268343f, 0.92387953f, top ? 1 : -1);
            }

            private static Point Along(Point p, Point axis, float distance)
            { return new Point(p.X + axis.X * distance, p.Y + axis.Y * distance, p.Z + axis.Z * distance); }

            private void Barrel(Point seat, Point axis, float length, float outer, float inner, int segments)
            {
                var tangent = new Point(1, 0, 0); var forward = Point.Cross(tangent, axis);
                float[] height = { 0, .5f, 1, 1, .5f, 0, 1, 1, 0, 0 };
                var rings = new int[10, segments + 1];
                for (int ring = 0; ring < 10; ring++) for (int i = 0; i <= segments; i++)
                {
                    double a = (i % segments) * Math.PI * 2 / segments;
                    var radial = new Point((float)Math.Cos(a), forward.Y * (float)Math.Sin(a), forward.Z * (float)Math.Sin(a));
                    bool outside = ring < 3 || ring == 6 || ring == 9; float radius = outside ? outer : inner;
                    var normal = ring < 3 ? radial : ring < 6 ? new Point(-radial.X, -radial.Y, -radial.Z) :
                        ring < 8 ? Along(new Point(0, 0, 0), axis, Math.Sign(length)) : Along(new Point(0, 0, 0), axis, -Math.Sign(length));
                    rings[ring, i] = Vertex(Along(Along(seat, axis, height[ring] * length), radial, radius), normal, WheelTile, i / (float)segments, height[ring]);
                }
                foreach (int start in new[] { 0, 1, 3, 4, 6, 8 }) for (int i = 0; i < segments; i++)
                    Quad(rings[start, i], rings[start, i + 1], rings[start + 1, i + 1], rings[start + 1, i]);
            }

            private void Sphere(Point centre, float radius)
            {
                // Closed octahedron subdivisions avoid polar zero-area faces.
                var p = new[] { new Point(1, 0, 0), new Point(-1, 0, 0), new Point(0, 1, 0), new Point(0, -1, 0), new Point(0, 0, 1), new Point(0, 0, -1) };
                int[] faces = { 0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0, 4, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3, 5 };
                for (int i = 0; i < faces.Length; i += 3)
                {
                    var a = p[faces[i]]; var b = p[faces[i + 1]]; var c = p[faces[i + 2]];
                    var ab = Unit(new Point(a.X + b.X, a.Y + b.Y, a.Z + b.Z)); var bc = Unit(new Point(b.X + c.X, b.Y + c.Y, b.Z + c.Z)); var ca = Unit(new Point(c.X + a.X, c.Y + a.Y, c.Z + a.Z));
                    foreach (var tri in new[] { new[] { a, ab, ca }, new[] { ab, b, bc }, new[] { ca, bc, c }, new[] { ab, bc, ca } })
                        Face(Vertex(Along(centre, tri[0], radius), tri[0], MetalTile, 0, 0), Vertex(Along(centre, tri[1], radius), tri[1], MetalTile, 0, 0), Vertex(Along(centre, tri[2], radius), tri[2], MetalTile, 0, 0));
                }
            }

            private void Socket(Point centre, Point axis, float outer, float inner, int segments, int steps)
            {
                // Closed hemispherical shell: opening faces the moving stem,
                // while the back of the socket joins the fixed mounting plate.
                // A complete axial tube would cut through the tilted ball/stem.
                var tangent = new Point(1, 0, 0); var around = Point.Cross(axis, tangent);
                var rings = new int[2, steps, segments + 1]; var poles = new int[2];
                for (int surface = 0; surface < 2; surface++)
                {
                    float radius = surface == 0 ? outer : inner, sign = surface == 0 ? 1 : -1;
                    poles[surface] = Vertex(Along(centre, axis, radius), Along(new Point(0, 0, 0), axis, sign), MetalTile, .5f, 1);
                    for (int ring = 0; ring < steps; ring++) for (int i = 0; i <= segments; i++)
                    {
                        double polar = (ring + 1) * Math.PI / (2 * steps), angle = (i % segments) * Math.PI * 2 / segments;
                        var direction = new Point(
                            (float)(Math.Sin(polar) * Math.Cos(angle)),
                            (float)(axis.Y * Math.Cos(polar) + around.Y * Math.Sin(polar) * Math.Sin(angle)),
                            (float)(axis.Z * Math.Cos(polar) + around.Z * Math.Sin(polar) * Math.Sin(angle)));
                        rings[surface, ring, i] = Vertex(Along(centre, direction, radius), Along(new Point(0, 0, 0), direction, sign), MetalTile, i / (float)segments, (ring + 1) / (float)steps);
                    }
                    for (int i = 0; i < segments; i++) Face(poles[surface], rings[surface, 0, i], rings[surface, 0, i + 1]);
                    for (int ring = 0; ring < steps - 1; ring++) for (int i = 0; i < segments; i++)
                        Quad(rings[surface, ring, i], rings[surface, ring, i + 1], rings[surface, ring + 1, i + 1], rings[surface, ring + 1, i]);
                }
                var rim = new int[2, segments + 1]; var rimNormal = Along(new Point(0, 0, 0), axis, -1);
                for (int side = 0; side < 2; side++) for (int i = 0; i <= segments; i++)
                {
                    double angle = (i % segments) * Math.PI * 2 / segments;
                    var direction = new Point((float)Math.Cos(angle), around.Y * (float)Math.Sin(angle), around.Z * (float)Math.Sin(angle));
                    rim[side, i] = Vertex(Along(centre, direction, side == 0 ? outer : inner), rimNormal, MetalTile, i / (float)segments, side);
                }
                for (int i = 0; i < segments; i++) Quad(rim[0, i], rim[0, i + 1], rim[1, i + 1], rim[1, i]);
            }

            private void Link(Point a, Point b, float radius)
            {
                var delta = b - a;
                Cylinder(new Point((a.X + b.X) * .5f, (a.Y + b.Y) * .5f, (a.Z + b.Z) * .5f),
                    Unit(delta), radius, (float)Math.Sqrt(Point.Dot(delta, delta)), 10, MetalTile);
            }

            private void Tube(Point centre, Point axis, float outer, float inner, float length, int segments, int tile)
            {
                var u = Math.Abs(axis.X) < .9f ? Unit(Point.Cross(axis, new Point(1, 0, 0))) : new Point(0, 1, 0);
                var v = Point.Cross(axis, u);
                var rings = new int[8, segments + 1];
                // Separate end-ring normals: sharing radial side normals with a
                // washer face gives neither a correct cap normal nor reliable winding.
                for (int ring = 0; ring < 8; ring++) for (int i = 0; i <= segments; i++)
                {
                    double angle = (i % segments) * Math.PI * 2 / segments;
                    var radial = new Point(u.X * (float)Math.Cos(angle) + v.X * (float)Math.Sin(angle),
                        u.Y * (float)Math.Cos(angle) + v.Y * (float)Math.Sin(angle), u.Z * (float)Math.Cos(angle) + v.Z * (float)Math.Sin(angle));
                    int surface = ring / 2, end = ring % 2;
                    float along = surface == 0 ? (end == 0 ? -length * .5f : length * .5f) :
                        surface == 1 ? length * .5f : surface == 2 ? (end == 0 ? length * .5f : -length * .5f) : -length * .5f;
                    float radius = surface == 0 || surface == 1 && end == 0 || surface == 3 && end == 1 ? outer : inner;
                    var p = Along(centre, axis, along);
                    var normal = surface == 0 ? radial : surface == 1 ? axis : surface == 2 ? new Point(-radial.X, -radial.Y, -radial.Z) : new Point(-axis.X, -axis.Y, -axis.Z);
                    rings[ring, i] = Vertex(Along(p, radial, radius), normal, tile, i / (float)segments, ring / 3f);
                }
                for (int ring = 0; ring < 8; ring += 2) for (int i = 0; i < segments; i++)
                    Quad(rings[ring, i], rings[ring, i + 1], rings[ring + 1, i + 1], rings[ring + 1, i]);
            }

            public void Wheel(float x, float z, int parent)
            {
                Begin();
                const int segments = 24;
                float[] offsets = { -0.016f, -0.0125f, 0.0125f, 0.016f };
                float[] radius = { 0.0235f, 0.027f, 0.027f, 0.0235f };
                var rings = new int[4, segments + 1];
                for (int ring = 0; ring < 4; ring++)
                    for (int i = 0; i <= segments; i++)
                    {
                        double angle = (i % segments) * Math.PI * 2 / segments;
                        float cy = (float)Math.Cos(angle), sz = (float)Math.Sin(angle);
                        float axial = ring == 0 ? -1 : ring == 3 ? 1 : 0;
                        Point normal = Unit(new Point(axial, cy, sz));
                        rings[ring, i] = Vertex(new Point(x + offsets[ring], 0.027f + radius[ring] * cy, z + radius[ring] * sz), normal,
                            WheelTile, i / (float)segments, 0.3f + ring * 0.12f);
                    }
                for (int ring = 0; ring < 3; ring++)
                    for (int i = 0; i < segments; i++) Quad(rings[ring, i], rings[ring, i + 1], rings[ring + 1, i + 1], rings[ring + 1, i]);
                for (int side = 0; side < 2; side++)
                {
                    float offset = offsets[side == 0 ? 0 : 3];
                    var normal = new Point(side == 0 ? -1 : 1, 0, 0);
                    int centre = Vertex(new Point(x + offset, 0.027f, z), normal, WheelTile, 0.5f, 0.5f);
                    var face = new int[segments];
                    for (int i = 0; i < segments; i++)
                    {
                        double angle = i * Math.PI * 2 / segments;
                        float cy = (float)Math.Cos(angle), sz = (float)Math.Sin(angle);
                        face[i] = Vertex(new Point(x + offset, 0.027f + 0.0235f * cy, z + 0.0235f * sz),
                            Unit(new Point(normal.X, cy, sz)), WheelTile, 0.5f + cy * 0.45f, 0.5f + sz * 0.45f);
                    }
                    for (int i = 0; i < segments; i++) Face(centre, face[i], face[(i + 1) % segments]);
                }
                End(SkateBoardPartKind.Wheel, parent, new Point(x, 0.027f, z), new Point(1, 0, 0));
            }

            private void Cylinder(Point centre, Point axis, float radius, float length, int segments, int tile)
            {
                Point u = Math.Abs(axis.X) < 0.9f ? Unit(Point.Cross(axis, new Point(1, 0, 0))) : new Point(0, 1, 0);
                Point v = Point.Cross(axis, u);
                var rings = new int[2, segments + 1];
                for (int side = 0; side < 2; side++)
                    for (int i = 0; i <= segments; i++)
                    {
                        double angle = (i % segments) * Math.PI * 2 / segments;
                        var normal = new Point(u.X * (float)Math.Cos(angle) + v.X * (float)Math.Sin(angle),
                            u.Y * (float)Math.Cos(angle) + v.Y * (float)Math.Sin(angle), u.Z * (float)Math.Cos(angle) + v.Z * (float)Math.Sin(angle));
                        float axial = (side == 0 ? -0.5f : 0.5f) * length;
                        rings[side, i] = Vertex(new Point(centre.X + axial * axis.X + radius * normal.X,
                            centre.Y + axial * axis.Y + radius * normal.Y, centre.Z + axial * axis.Z + radius * normal.Z), normal, tile, i / (float)segments, side);
                    }
                for (int i = 0; i < segments; i++) Quad(rings[0, i], rings[0, i + 1], rings[1, i + 1], rings[1, i]);
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1 : 1;
                    var normal = new Point(sign * axis.X, sign * axis.Y, sign * axis.Z);
                    int middle = Vertex(new Point(centre.X + normal.X * length / 2, centre.Y + normal.Y * length / 2, centre.Z + normal.Z * length / 2), normal, tile, 0.5f, 0.5f);
                    var rim = new int[segments];
                    for (int i = 0; i < segments; i++)
                    {
                        int old = rings[side, i] * 3;
                        rim[i] = Vertex(new Point(positions[old], positions[old + 1], positions[old + 2]), normal, tile,
                            0.5f + 0.45f * (float)Math.Cos(i * Math.PI * 2 / segments), 0.5f + 0.45f * (float)Math.Sin(i * Math.PI * 2 / segments));
                    }
                    for (int i = 0; i < segments; i++) Face(middle, rim[i], rim[(i + 1) % segments]);
                }
            }

            private void Box(Point centre, Point half, int tile)
            {
                for (int axis = 0; axis < 3; axis++) for (int sign = -1; sign <= 1; sign += 2)
                {
                    var ids = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i == 0 || i == 3 ? -1 : 1, b = i < 2 ? -1 : 1;
                        Point p = axis == 0 ? new Point(centre.X + sign * half.X, centre.Y + a * half.Y, centre.Z + b * half.Z) :
                            axis == 1 ? new Point(centre.X + a * half.X, centre.Y + sign * half.Y, centre.Z + b * half.Z) :
                            new Point(centre.X + a * half.X, centre.Y + b * half.Y, centre.Z + sign * half.Z);
                        Point n = axis == 0 ? new Point(sign, 0, 0) : axis == 1 ? new Point(0, sign, 0) : new Point(0, 0, sign);
                        ids[i] = Vertex(p, n, tile, (a + 1) / 2, (b + 1) / 2);
                    }
                    Quad(ids[0], ids[1], ids[2], ids[3]);
                }
            }

            private int Vertex(Point point, Point normal, int tile, float u, float v)
            {
                int index = positions.Count / 3;
                positions.Add(point.X); positions.Add(point.Y); positions.Add(point.Z);
                normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);
                colours.Add(1); colours.Add(1); colours.Add(1); colours.Add(1);
                float left = tile == GripTile || tile == EdgeTile ? 2 : tile == MetalTile ? 66 : 130;
                float bottom = tile <= GraphicTile ? 130 : 2;
                float width = tile == EdgeTile || tile == MetalTile ? 60 : 124;
                uvs.Add((left + 0.5f + Math.Max(0, Math.Min(1, u)) * (width - 1)) / 256);
                uvs.Add((bottom + 0.5f + Math.Max(0, Math.Min(1, v)) * 123) / 256);
                return index;
            }
            private Point Position(int index) { int at = index * 3; return new Point(positions[at], positions[at + 1], positions[at + 2]); }
            private Point Normal(int index) { int at = index * 3; return new Point(normals[at], normals[at + 1], normals[at + 2]); }
            private void Face(int a, int b, int c)
            {
                Point face = Point.Cross(Position(b) - Position(a), Position(c) - Position(a));
                Point na = Normal(a), nb = Normal(b), nc = Normal(c);
                if (Point.Dot(face, new Point(na.X + nb.X + nc.X, na.Y + nb.Y + nc.Y, na.Z + nb.Z + nc.Z)) < 0)
                { int swap = b; b = c; c = swap; }
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }
            private void Quad(int a, int b, int c, int d) { Face(a, b, c); Face(a, c, d); }
            private static Point Unit(Point p)
            { float length = (float)Math.Sqrt(Point.Dot(p, p)); return new Point(p.X / length, p.Y / length, p.Z / length); }

            // RGBA row zero corresponds to v=0.
            private static byte[] Atlas(int seed)
            {
                var rgba = new byte[256 * 256 * 4];
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    uint noise;
                    unchecked
                    {
                        noise = (uint)seed ^ ((uint)x * 0x9e3779b9u) ^ ((uint)y * 0x85ebca6bu);
                        noise ^= noise >> 16; noise *= 0x7feb352du; noise ^= noise >> 15; noise *= 0x846ca68bu; noise ^= noise >> 16;
                    }
                    int grain = (int)(noise & 31) - 15;
                    int r, g, b;
                    if (y >= 128 && x < 128) { r = 45 + grain; g = 48 + grain; b = 51 + grain; }
                    else if (y >= 128)
                    {
                        float u = (x - 128) / 127f, v = (y - 128) / 127f;
                        r = 218 + grain / 3; g = 187 + grain / 3; b = 133 + grain / 3;
                        if (Math.Abs(u - 0.5f) < 0.19f + 0.07f * (float)Math.Sin(v * 18)) { r = 25; g = 103; b = 110; }
                        if (Math.Abs(v - 0.25f - u * 0.5f) < 0.035f || Math.Abs(v - 0.55f - u * 0.27f) < 0.025f)
                        { r = 230; g = 116; b = 52; }
                        if (Math.Abs(u - 0.5f) + Math.Abs(v - 0.5f) < 0.085f) { r = 236; g = 224; b = 188; }
                    }
                    else if (x < 64)
                    { int ply = (y / 9) % 2; r = 157 + ply * 31 + grain / 3; g = 94 + ply * 28 + grain / 3; b = 45 + ply * 20 + grain / 3; }
                    else if (x < 128) { r = 130 + grain / 3; g = 140 + grain / 3; b = 147 + grain / 3; }
                    else
                    {
                        float u = (x - 128 - 63.5f) / 63.5f, v = (y - 63.5f) / 63.5f;
                        float circle = u * u + v * v;
                        int stripe = circle > 0.1f && circle < 0.15f || circle > 0.65f && circle < 0.73f ? 24 : 0;
                        r = 230 - stripe + grain / 5; g = 226 - stripe + grain / 5; b = 202 - stripe + grain / 5;
                    }
                    int at = (y * 256 + x) * 4;
                    rgba[at] = (byte)r; rgba[at + 1] = (byte)g; rgba[at + 2] = (byte)b; rgba[at + 3] = 255;
                }
                return rgba;
            }
        }

        private struct Point
        {
            public readonly float X, Y, Z;
            public Point(float x, float y, float z) { X = x; Y = y; Z = z; }
            public static Point operator -(Point a, Point b) { return new Point(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
            public static Point Cross(Point a, Point b)
            { return new Point(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
            public static float Dot(Point a, Point b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z; }
        }

        private struct Colour
        {
            public readonly float R, G, B;
            public Colour(float r, float g, float b) { R = r; G = g; B = b; }
        }

        private sealed class Builder
        {
            private readonly List<float> positions = new List<float>(4608);
            private readonly List<float> normals = new List<float>(4608);
            private readonly List<int> triangles = new List<int>(1536);
            private readonly List<float> colours = new List<float>(6144);
            private static readonly Colour Grip = new Colour(0.055f, 0.060f, 0.065f);
            private static readonly Colour Maple = new Colour(0.70f, 0.42f, 0.20f);
            private static readonly Colour Edge = new Colour(0.47f, 0.27f, 0.11f);
            private static readonly Colour Metal = new Colour(0.48f, 0.52f, 0.55f);
            private static readonly Colour Wheel = new Colour(0.80f, 0.82f, 0.76f);

            public SkateBoardMesh Build()
            { return new SkateBoardMesh(positions.ToArray(), normals.ToArray(), triangles.ToArray(), colours.ToArray()); }

            public void Deck()
            {
                float[] z = { -0.40f, -0.385f, -0.355f, -0.305f, -0.25f, 0, 0.25f, 0.305f, 0.355f, 0.385f, 0.40f };
                float[] width = { 0.02f, 0.055f, 0.085f, 0.095f, 0.10f, 0.10f, 0.10f, 0.095f, 0.085f, 0.055f, 0.02f };
                float[] height = { 0.142f, 0.134f, 0.120f, 0.102f, 0.092f, 0.092f, 0.092f, 0.102f, 0.120f, 0.134f, 0.142f };
                for (int i = 0; i < z.Length - 1; i++)
                {
                    var a = new Point(-width[i], height[i], z[i]);
                    var b = new Point(width[i], height[i], z[i]);
                    var c = new Point(width[i + 1], height[i + 1], z[i + 1]);
                    var d = new Point(-width[i + 1], height[i + 1], z[i + 1]);
                    var e = new Point(a.X, a.Y - 0.009f, a.Z);
                    var f = new Point(b.X, b.Y - 0.009f, b.Z);
                    var g = new Point(c.X, c.Y - 0.009f, c.Z);
                    var h = new Point(d.X, d.Y - 0.009f, d.Z);
                    Quad(a, b, c, d, new Point(0, 1, 0), Grip);
                    Quad(e, f, g, h, new Point(0, -1, 0), Maple);
                    Quad(a, d, h, e, new Point(-1, 0, 0), Edge);
                    Quad(b, c, g, f, new Point(1, 0, 0), Edge);
                    if (i == 0) Quad(a, b, f, e, new Point(0, 0, -1), Edge);
                    if (i == z.Length - 2) Quad(d, c, g, h, new Point(0, 0, 1), Edge);
                }
            }

            public void Truck(float z)
            {
                Box(new Point(0, 0.079f, z), new Point(0.0325f, 0.004f, 0.0275f), Metal);
                Box(new Point(0, 0.055f, z), new Point(0.0085f, 0.020f, 0.010f), Metal);
                Box(new Point(0, 0.033f, z), new Point(0.070f, 0.007f, 0.0125f), Metal);
                Cylinder(0, 0.0275f, z, 0.004f, 0.176f, 12, Metal);
                Cylinder(-0.090f, 0.0275f, z, 0.0275f, 0.024f, 16, Wheel);
                Cylinder(0.090f, 0.0275f, z, 0.0275f, 0.024f, 16, Wheel);
            }

            private void Box(Point centre, Point half, Colour colour)
            {
                var a = new Point(centre.X - half.X, centre.Y - half.Y, centre.Z - half.Z);
                var b = new Point(centre.X + half.X, centre.Y - half.Y, centre.Z - half.Z);
                var c = new Point(centre.X + half.X, centre.Y - half.Y, centre.Z + half.Z);
                var d = new Point(centre.X - half.X, centre.Y - half.Y, centre.Z + half.Z);
                var e = new Point(a.X, centre.Y + half.Y, a.Z);
                var f = new Point(b.X, centre.Y + half.Y, b.Z);
                var g = new Point(c.X, centre.Y + half.Y, c.Z);
                var h = new Point(d.X, centre.Y + half.Y, d.Z);
                Quad(a, b, c, d, new Point(0, -1, 0), colour);
                Quad(e, f, g, h, new Point(0, 1, 0), colour);
                Quad(a, b, f, e, new Point(0, 0, -1), colour);
                Quad(d, c, g, h, new Point(0, 0, 1), colour);
                Quad(a, d, h, e, new Point(-1, 0, 0), colour);
                Quad(b, c, g, f, new Point(1, 0, 0), colour);
            }

            private void Cylinder(float x, float y, float z, float radius, float length, int segments, Colour colour)
            {
                var left = new Point(x - length * 0.5f, y, z);
                var right = new Point(x + length * 0.5f, y, z);
                for (int i = 0; i < segments; i++)
                {
                    double angle = i * Math.PI * 2 / segments;
                    double next = (i + 1) % segments * Math.PI * 2 / segments;
                    float ay = radius * (float)Math.Cos(angle), az = radius * (float)Math.Sin(angle);
                    float by = radius * (float)Math.Cos(next), bz = radius * (float)Math.Sin(next);
                    var a = new Point(left.X, y + ay, z + az);
                    var b = new Point(left.X, y + by, z + bz);
                    var c = new Point(right.X, y + by, z + bz);
                    var d = new Point(right.X, y + ay, z + az);
                    Quad(a, b, c, d, new Point(0, ay + by, az + bz), colour);
                    Triangle(left, b, a, new Point(-1, 0, 0), colour);
                    Triangle(right, c, d, new Point(1, 0, 0), colour);
                }
            }

            private void Quad(Point a, Point b, Point c, Point d, Point outward, Colour colour)
            { Triangle(a, b, c, outward, colour); Triangle(a, c, d, outward, colour); }

            private void Triangle(Point a, Point b, Point c, Point outward, Colour colour)
            {
                var normal = Point.Cross(b - a, c - a);
                if (Point.Dot(normal, outward) < 0)
                {
                    var swap = b; b = c; c = swap;
                    normal = Point.Cross(b - a, c - a);
                }
                float length = (float)Math.Sqrt(Point.Dot(normal, normal));
                normal = new Point(normal.X / length, normal.Y / length, normal.Z / length);
                Vertex(a, normal, colour);
                Vertex(b, normal, colour);
                Vertex(c, normal, colour);
            }

            private void Vertex(Point point, Point normal, Colour colour)
            {
                triangles.Add(positions.Count / 3);
                positions.Add(point.X); positions.Add(point.Y); positions.Add(point.Z);
                normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);
                colours.Add(colour.R); colours.Add(colour.G); colours.Add(colour.B); colours.Add(1);
            }
        }
    }
}
