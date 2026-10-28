using System;

namespace Shortcut.RustMod
{
    public readonly struct SkateTruckPose
    {
        public readonly SkateVector Cup, Eye, Right, Up, Forward;
        internal SkateTruckPose(SkateVector cup, SkateVector eye, SkateVector right, SkateVector up, SkateVector forward)
        { Cup = cup; Eye = eye; Right = right; Up = up; Forward = forward; }
        public SkateVector Rotate(SkateVector v) { return Right * v.X + Up * v.Y + Forward * v.Z; }
        public SkateVector Point(SkateVector v) { return Cup + Rotate(v - Cup); }
    }
    public static class SkateBoardTruckKinematics
    {
        // Explicit authored 45-degree caster. Socket geometry provides clearance;
        // its axis is not lowered to evade the former solid baseplate box.
        public const double EyeY = .059, AxleZ = .245, CupY = .0765, CupForward = .0175, EyeHalf = .0025, EyeRadius = .014;
        public static SkateVector Kingpin(int side) { return new SkateVector(0, Math.Sqrt(.5), -side * Math.Sqrt(.5)); }
        public static SkateVector Eye(int side) { return new SkateVector(0, EyeY, side * AxleZ); }
        public static SkateVector Cup(int side) { return new SkateVector(0, CupY, side * (AxleZ + CupForward)); }
        public static bool TryPose(int side, SkateBoardTruckState state, out SkateTruckPose pose, out string error)
        {
            pose = default(SkateTruckPose); error = null;
            if (side != -1 && side != 1 || !Finite(state.Steering) || Math.Abs(state.Steering) > SkateBoardTruckMotion.MaximumSteering ||
                !Finite(state.Axial) || Math.Abs(state.Axial) > SkateBoardTruckMotion.MaximumAxial)
            { error = "Invalid pivot-cup truck state."; return false; }
            var cup = Cup(side); var eye = Eye(side); var lever = eye - cup;
            var axis = lever * (-1 / lever.Length); var pin = Kingpin(side); var x = new SkateVector(1, 0, 0);
            // This angle exactly cancels displayed board camber on a flat plane.
            double steering = side * Math.Atan(Math.Tan(state.Steering * Math.PI / 180) / Math.Abs(axis.Z));
            double c = SkateVector.Dot(pin, lever);
            double d = side * SkateVector.Dot(pin, Turn(SkateBoardPath.Cross(x, lever), axis, steering));
            double bend = state.Axial / d;
            for (int i = 0; i < 6; i++)
            {
                double derivative = -c * Math.Sin(bend) + d * Math.Cos(bend);
                if (Math.Abs(derivative) < .01) { error = "Load articulation is singular."; return false; }
                bend -= (c * (Math.Cos(bend) - 1) + d * Math.Sin(bend) - state.Axial) / derivative;
            }
            if (!Finite(bend) || Math.Abs(bend) > .1) { error = "Pivot-cup articulation exceeds its stop."; return false; }
            var right = Turn(Turn(x, x, side * bend), axis, steering);
            var up = Turn(Turn(new SkateVector(0, 1, 0), x, side * bend), axis, steering);
            var forward = Turn(Turn(new SkateVector(0, 0, 1), x, side * bend), axis, steering);
            var e = eye - cup;
            var frame = new SkateTruckPose(cup, cup + right * e.X + up * e.Y + forward * e.Z, right, up, forward);
            double actual = SkateVector.Dot(frame.Point(eye) - eye, pin);
            if (Math.Abs(actual - state.Axial) > 1e-9) { error = "Axial load constraint did not converge."; return false; }
            pose = frame; return true;
        }
        internal static SkateVector Turn(SkateVector v, SkateVector axis, double radians)
        {
            double c = Math.Cos(radians), s = Math.Sin(radians);
            return v * c + SkateBoardPath.Cross(axis, v) * s + axis * (SkateVector.Dot(axis, v) * (1 - c));
        }
        private static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }
}
