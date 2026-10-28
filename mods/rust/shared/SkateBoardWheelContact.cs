using System;
using System.Collections.Generic;

namespace Shortcut.RustMod
{
    // Exact support of the circular, finite-width tyre profile authored by the
    // mesh generator. Rendering discretizes its circumference to 24 segments.
    public sealed class SkateBoardWheelContact
    {
        private sealed class Wheel
        {
            internal SkateVector Centre, Pivot, Hinge;
            internal int Side;
            internal double[] Offsets, Radii;
        }
        private readonly Wheel[] wheels;
        private SkateBoardWheelContact(Wheel[] value) { wheels = value; }
        public static bool TryCreate(SkateBoardMesh mesh, out SkateBoardWheelContact result, out string error)
        {
            result = null; error = null;
            if (mesh == null || mesh.Parts == null || mesh.Positions == null || mesh.Positions.Length % 3 != 0)
            { error = "Wheel support needs the actual part mesh."; return false; }
            var found = new List<Wheel>(4);
            foreach (var part in mesh.Parts)
            {
                if (part.Kind != SkateBoardPartKind.Wheel) continue;
                if (part.ParentIndex < 0 || part.ParentIndex >= mesh.Parts.Length ||
                    mesh.Parts[part.ParentIndex].Kind != SkateBoardPartKind.Truck || part.VertexCount < 4 || part.VertexCount > 4096 ||
                    part.VertexStart < 0 || (long)part.VertexStart + part.VertexCount > mesh.Positions.Length / 3 ||
                    !Finite(part.AxisX) || !Finite(part.AxisY) || !Finite(part.AxisZ) ||
                    Math.Abs(part.AxisX - 1) > .00001 || Math.Abs(part.AxisY) > .00001 || Math.Abs(part.AxisZ) > .00001)
                { error = "Wheel support requires bounded local-X wheel geometry."; return false; }
                var parent = mesh.Parts[part.ParentIndex];
                var wheel = new Wheel { Centre = new SkateVector(part.PivotX, part.PivotY, part.PivotZ),
                    Pivot = new SkateVector(parent.PivotX, parent.PivotY, parent.PivotZ),
                    Hinge = new SkateVector(parent.AxisX, parent.AxisY, parent.AxisZ), Side = parent.PivotZ < 0 ? -1 : 1 };
                if (!Point(wheel.Centre) || !Point(wheel.Pivot) || !Finite(wheel.Hinge.Length) || Math.Abs(wheel.Hinge.Length - 1) > .00001)
                { error = "Invalid wheel or hinge frame."; return false; }
                wheel.Hinge *= 1 / wheel.Hinge.Length;
                var expectedEye = SkateBoardTruckKinematics.Eye(wheel.Side);
                var expectedAxis = SkateBoardTruckKinematics.Cup(wheel.Side) - expectedEye;
                expectedAxis *= 1 / expectedAxis.Length;
                if ((wheel.Pivot - expectedEye).Length > .000001 || (wheel.Hinge - expectedAxis).Length > .00001 ||
                    Math.Abs(Math.Abs(wheel.Centre.X) - .082) > .000001 || Math.Abs(wheel.Centre.Y - .027) > .000001 ||
                    Math.Abs(wheel.Centre.Z - wheel.Side * SkateBoardTruckKinematics.AxleZ) > .000001)
                { error = "Wheel support requires the matching authored cup rig."; return false; }
                foreach (var other in found) if ((other.Centre - wheel.Centre).Length < .000001)
                { error = "Wheel support has a duplicate physical station."; return false; }
                var offsets = new List<double>(4); var radii = new List<double>(4);
                for (int i = part.VertexStart; i < part.VertexStart + part.VertexCount; i++)
                {
                    var v = new SkateVector(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]) - wheel.Centre;
                    if (!Point(v) || v.Length > .1) { error = "Invalid wheel profile vertex."; return false; }
                    int at = -1;
                    for (int k = 0; k < offsets.Count; k++) if (Math.Abs(offsets[k] - v.X) < .000001) { at = k; break; }
                    double radius = Math.Sqrt(v.Y * v.Y + v.Z * v.Z);
                    if (at < 0) { if (offsets.Count == 8) { error = "Wheel profile has too many axial rings."; return false; } offsets.Add(v.X); radii.Add(radius); }
                    else radii[at] = Math.Max(radii[at], radius);
                }
                if (offsets.Count < 2) { error = "Wheel profile has no finite width."; return false; }
                wheel.Offsets = offsets.ToArray(); wheel.Radii = radii.ToArray(); found.Add(wheel);
            }
            if (found.Count != 4) { error = "Wheel support needs exactly four wheels."; return false; }
            result = new SkateBoardWheelContact(found.ToArray()); return true;
        }
        public bool TryOffset(SkateBoardPose deck, double deckTop, SkateBoardTruckState rear, SkateBoardTruckState front,
            SkateVector planePoint, SkateVector planeNormal, double maximum, out SkateVector offset, out string error)
        {
            offset = default(SkateVector); error = null; double gap, highest;
            if (!Finite(maximum) || maximum < 0 || maximum > .05)
            { error = "Invalid supported display travel bound."; return false; }
            if (!TryGapRange(deck, deckTop, rear, front, planePoint, planeNormal, out gap, out highest, out error)) return false;
            if (highest - gap > .0005) { error = "The articulated wheels cannot share this observed plane."; return false; }
            if (Math.Abs(gap) > maximum) { error = "Observed wheel plane is outside bounded display travel."; return false; }
            offset = planeNormal * (-gap / planeNormal.Length); return true;
        }
        public bool TryMinimumGap(SkateBoardPose deck, double deckTop, SkateBoardTruckState rear, SkateBoardTruckState front,
            SkateVector planePoint, SkateVector planeNormal, out double gap, out string error)
        { double highest; return TryGapRange(deck, deckTop, rear, front, planePoint, planeNormal, out gap, out highest, out error); }
        public bool TryGapRange(SkateBoardPose deck, double deckTop, SkateBoardTruckState rear, SkateBoardTruckState front,
            SkateVector planePoint, SkateVector planeNormal, out double gap, out double highest, out string error)
        {
            gap = highest = 0; error = null;
            if (!deck.Initialized || !SkateBoardPath.Valid(deck) || !Finite(deckTop) || deckTop < 0 || deckTop > .5 ||
                !Point(planePoint) || !Finite(planeNormal.Length) || planeNormal.Length < .9 || planeNormal.Length > 1.1)
            { error = "Invalid supported wheel plane."; return false; }
            var n = planeNormal * (1 / planeNormal.Length); double minimum = double.PositiveInfinity; highest = double.NegativeInfinity;
            foreach (var wheel in wheels)
            {
                var state = wheel.Side < 0 ? rear : front;
                if (!Finite(state.Steering) || Math.Abs(state.Steering) > SkateBoardTruckMotion.MaximumSteering ||
                    !Finite(state.Axial) || Math.Abs(state.Axial) > SkateBoardTruckMotion.MaximumAxial)
                { error = "Invalid supported hanger state."; return false; }
                SkateTruckPose hanger;
                if (!SkateBoardTruckKinematics.TryPose(wheel.Side, state, out hanger, out error)) return false;
                var local = hanger.Point(wheel.Centre);
                var centre = deck.Origin + deck.Rotate(local - new SkateVector(0, deckTop, 0));
                var axle = deck.Rotate(hanger.Rotate(new SkateVector(1, 0, 0)));
                double along = SkateVector.Dot(axle, n), radial = Math.Sqrt(Math.Max(0, 1 - along * along));
                double centreGap = SkateVector.Dot(centre - planePoint, n);
                double wheelGap = double.PositiveInfinity;
                for (int k = 0; k < wheel.Offsets.Length; k++)
                    wheelGap = Math.Min(wheelGap, centreGap + wheel.Offsets[k] * along - wheel.Radii[k] * radial);
                minimum = Math.Min(minimum, wheelGap); highest = Math.Max(highest, wheelGap);
            }
            gap = minimum;
            return Finite(gap);
        }
        private static bool Point(SkateVector v) { return Finite(v.X) && Finite(v.Y) && Finite(v.Z) && Math.Abs(v.X) <= 100000 && Math.Abs(v.Y) <= 100000 && Math.Abs(v.Z) <= 100000; }
        private static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }
}
