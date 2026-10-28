using System;

namespace Shortcut.RustMod
{
    // Closed, reusable annulus. Parent rest quaternion is never changed. Cap0 is
    // fixed to its washer; cap1 lies on the actual articulated hanger plane.
    public sealed class SkateBoardBushing
    {
        public readonly float[] Positions, Normals;
        public readonly int[] Triangles;
        public readonly int Side, Seat;
        private readonly SkateVector origin, pin, forward;
        private readonly double[] x, z, u;
        private readonly double volume, length;
        public double MinimumStrain { get; private set; }
        public double MaximumStrain { get; private set; }
        public double VolumeError { get; private set; }
        public double CapError { get; private set; }
        public double RodClearance { get; private set; }
        public double WallThickness { get; private set; }
        public double ContactOuterMargin { get; private set; }
        private SkateBoardBushing(SkateBoardMesh mesh, SkateBoardPart part)
        {
            Side = part.PivotZ < 0 ? -1 : 1; Seat = part.BushingSeat;
            origin = new SkateVector(part.PivotX, part.PivotY, part.PivotZ); pin = SkateBoardTruckKinematics.Kingpin(Side);
            forward = SkateBoardPath.Cross(new SkateVector(1, 0, 0), pin);
            length = Seat > 0 ? SkateBoardTruckMotion.TopLength : SkateBoardTruckMotion.BottomLength;
            x = new double[part.VertexCount]; z = new double[x.Length]; u = new double[x.Length];
            Positions = new float[x.Length * 3]; Normals = new float[Positions.Length]; Triangles = new int[part.IndexCount];
            for (int i = 0; i < x.Length; i++)
            {
                int j = (part.VertexStart + i) * 3;
                var v = new SkateVector(mesh.Positions[j], mesh.Positions[j + 1], mesh.Positions[j + 2]) - origin;
                x[i] = v.X; z[i] = SkateVector.Dot(v, forward); u[i] = Math.Max(0, Math.Min(1, SkateVector.Dot(v, pin) / (-Seat * length)));
                Positions[i * 3] = (float)x[i]; Positions[i * 3 + 1] = (float)(-Seat * length * u[i]); Positions[i * 3 + 2] = (float)z[i];
            }
            for (int i = 0; i < Triangles.Length; i++) Triangles[i] = mesh.Triangles[part.IndexStart + i] - part.VertexStart;
            volume = Math.Abs(SignedVolume());
        }
        public static bool TryCreate(SkateBoardMesh mesh, int index, out SkateBoardBushing result, out string error)
        {
            result = null; error = null;
            if (mesh == null || mesh.Parts == null || mesh.Positions == null || mesh.Triangles == null ||
                index < 0 || index >= mesh.Parts.Length || mesh.Parts[index].Kind != SkateBoardPartKind.Bushing)
            { error = "Bushing deformation requires its actual annulus mesh."; return false; }
            var part = mesh.Parts[index];
            if (part.VertexCount != 170 || part.IndexCount != 576 || part.VertexStart < 0 || part.IndexStart < 0 ||
                (long)part.VertexStart + part.VertexCount > mesh.Positions.Length / 3 ||
                (long)part.IndexStart + part.IndexCount > mesh.Triangles.Length || Math.Abs(part.BushingSeat) != 1 ||
                !Finite(part.PivotX) || !Finite(part.PivotY) || !Finite(part.PivotZ))
            { error = "Unsupported bounded bushing ring topology."; return false; }
            for (int i = part.IndexStart; i < part.IndexStart + part.IndexCount; i++)
                if (mesh.Triangles[i] < part.VertexStart || mesh.Triangles[i] >= part.VertexStart + part.VertexCount)
                { error = "Invalid bushing triangle range."; return false; }
            for (int i = part.VertexStart * 3; i < (part.VertexStart + part.VertexCount) * 3; i++)
                if (!Finite(mesh.Positions[i]) || Math.Abs(mesh.Positions[i]) > 1)
                { error = "Invalid bushing vertex."; return false; }
            var pending = new SkateBoardBushing(mesh, part);
            if (!Finite(pending.volume) || pending.volume < 1e-9) { error = "Invalid bounded bushing volume."; return false; }
            result = pending; return true;
        }
        public bool TryUpdate(SkateTruckPose hanger, out string error)
        {
            error = null;
            var face = hanger.Point(SkateBoardTruckKinematics.Eye(Side) + pin * (Seat * SkateBoardTruckKinematics.EyeHalf));
            var normal = hanger.Rotate(pin); var delta = face - origin;
            double nx = normal.X, ny = SkateVector.Dot(normal, pin), nz = SkateVector.Dot(normal, forward);
            double plane = SkateVector.Dot(delta, normal);
            if (ny < .7 || !Finite(ny) || !Finite(nx) || !Finite(nz) || !Finite(plane)) { error = "Bushing contact plane exceeds its tilt stop."; return false; }
            MinimumStrain = 1; MaximumStrain = 0; CapError = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double h = -Seat * (plane - nx * x[i] - nz * z[i]) / ny;
                double strain = 1 - h / (length / (1 - SkateBoardTruckMotion.Preload));
                MinimumStrain = Math.Min(MinimumStrain, strain); MaximumStrain = Math.Max(MaximumStrain, strain);
                if (h <= .003 || strain < 0 || strain > .65) { error = "Bushing wedge exceeds authored compressive strain."; return false; }
            }
            Fill(0, nx, ny, nz, plane); double c = Math.Abs(SignedVolume());
            Fill(.25, nx, ny, nz, plane); double plus = Math.Abs(SignedVolume());
            Fill(-.25, nx, ny, nz, plane); double minus = Math.Abs(SignedVolume());
            double a = (plus + minus - 2 * c) / .125, b = (plus - minus) / .5;
            double discriminant = b * b - 4 * a * (c - volume);
            if (discriminant < 0 || Math.Abs(b) < 1e-12) { error = "Bushing volume constraint is singular."; return false; }
            double bulge = Math.Abs(a) < 1e-12 ? (volume - c) / b : 2 * (volume - c) / (b + Math.Sqrt(discriminant));
            if (double.IsNaN(bulge) || bulge < -.4 || bulge > .8) { error = "Bushing radial volume stop exceeded."; return false; }
            Fill(bulge, nx, ny, nz, plane);
            VolumeError = Math.Abs(Math.Abs(SignedVolume()) / volume - 1);
            if (!Finite(VolumeError) || VolumeError > .00001) { error = "Bushing closed-volume solve failed."; return false; }
            double innerMin = double.PositiveInfinity, innerMax = 0, outerMin = double.PositiveInfinity;
            double ringMin = double.PositiveInfinity, ringMax = 0;
            var contactCentre = new SkateVector(delta.X, SkateVector.Dot(delta, pin), SkateVector.Dot(delta, forward));
            for (int i = 0; i < x.Length; i++)
            {
                var v = Get(i); double radius = Math.Sqrt(v.X * v.X + v.Z * v.Z);
                if (-Seat * v.Y < -1e-7 || Seat * (nx * v.X + ny * v.Y + nz * v.Z - plane) < -1e-7)
                { error = "Bushing side crossed a contact cap."; return false; }
                int ring = i / 17;
                if (ring < 3) outerMin = Math.Min(outerMin, radius);
                if (ring >= 3 && ring < 6) { innerMin = Math.Min(innerMin, radius); innerMax = Math.Max(innerMax, radius); }
                if (ring == 2) { double r = (v - contactCentre).Length; ringMin = Math.Min(ringMin, r); ringMax = Math.Max(ringMax, r); }
            }
            // Chord bound covers entire triangulated inner wall, not only vertices.
            RodClearance = innerMin * Math.Cos(Math.PI / 16) - .0025;
            WallThickness = outerMin * Math.Cos(Math.PI / 16) - innerMax;
            ContactOuterMargin = SkateBoardTruckKinematics.EyeRadius - ringMax;
            if (RodClearance < .0001 || WallThickness < .0005 || ContactOuterMargin < .0001 || ringMin < .0066)
            { error = "Bushing bore, wall or hanger contact ring exceeded its stop."; return false; }
            Array.Clear(Normals, 0, Normals.Length);
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                int ia = Triangles[i], ib = Triangles[i + 1], ic = Triangles[i + 2];
                var cross = SkateBoardPath.Cross(Get(ib) - Get(ia), Get(ic) - Get(ia));
                Add(ia, cross); Add(ib, cross); Add(ic, cross);
            }
            // Ring texture seams duplicate the same position, not a hard edge.
            for (int first = 0; first < x.Length; first += 17)
            {
                int startNormal = first * 3, endNormal = (first + 16) * 3;
                for (int axis = 0; axis < 3; axis++) { float sum = Normals[startNormal + axis] + Normals[endNormal + axis]; Normals[startNormal + axis] = Normals[endNormal + axis] = sum; }
            }
            for (int i = 0; i < x.Length; i++)
            {
                int j = i * 3; double n = Math.Sqrt(Normals[j] * Normals[j] + Normals[j + 1] * Normals[j + 1] + Normals[j + 2] * Normals[j + 2]);
                if (n < 1e-10) { error = "Degenerate deformed bushing normal."; return false; }
                Normals[j] /= (float)n; Normals[j + 1] /= (float)n; Normals[j + 2] /= (float)n;
                if (u[i] > .999999) CapError = Math.Max(CapError, Math.Abs(nx * Positions[j] + ny * Positions[j + 1] + nz * Positions[j + 2] - plane));
            }
            return true;
        }
        private void Fill(double bulge, double nx, double ny, double nz, double plane)
        {
            for (int i = 0; i < x.Length; i++)
            {
                double radial = 1 + bulge * 4 * u[i] * (1 - u[i]); int j = i * 3;
                Positions[j] = (float)(x[i] * radial);
                Positions[j + 1] = (float)(u[i] * (plane - nx * x[i] - nz * z[i]) / ny);
                Positions[j + 2] = (float)(z[i] * radial);
            }
        }
        private SkateVector Get(int i) { int j = i * 3; return new SkateVector(Positions[j], Positions[j + 1], Positions[j + 2]); }
        private void Add(int i, SkateVector v) { int j = i * 3; Normals[j] += (float)v.X; Normals[j + 1] += (float)v.Y; Normals[j + 2] += (float)v.Z; }
        private double SignedVolume()
        { double v = 0; for (int i = 0; i < Triangles.Length; i += 3) v += SkateVector.Dot(Get(Triangles[i]), SkateBoardPath.Cross(Get(Triangles[i + 1]), Get(Triangles[i + 2]))) / 6; return v; }
        private static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }
}
