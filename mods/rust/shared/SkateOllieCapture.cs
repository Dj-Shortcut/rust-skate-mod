using System;

namespace Shortcut.RustMod
{
    // One observed motion, replayed only by the guarded rigging showcase. All results are
    // visual targets; this helper has no movement, input, player-root or animation ownership.
    public static class SkateOllieCapture
    {
        public static double Duration { get { return SkateOllieCaptureData.Duration; } }
        private static bool checkedData, usableData;

        private struct Sample
        {
            public int A, B;
            public double Weight;
            public bool Mirror;
            public double Number(int column)
            { return SkateOllieCaptureData.Samples[A, column] * (1 - Weight) + SkateOllieCaptureData.Samples[B, column] * Weight; }
            public SkateVector Vector(int column)
            { return new SkateVector(Number(column) * (Mirror ? -1 : 1), Number(column + 1), Number(column + 2)); }
            public SkateVector Joint(int joint)
            {
                // Reflect the body across the board's longitudinal plane and exchange the
                // anatomical limbs. Reflection alone would leave the wrong foot leading.
                if (Mirror && joint >= 4) joint = (joint % 2 == 0) ? joint + 1 : joint - 1;
                return Vector(5 + joint * 3);
            }
            public SkateVector Sole(bool left)
            { return Vector((left != Mirror) ? 74 : 77); }
        }

        public static bool TrySample(double seconds, SkateRiderRig rig, SkateStance stance,
            out SkateRiderPose pose, out SkateBoardPose board, out SkateVector riderUp,
            out SkateVector leftSoleUp, out SkateVector rightSoleUp, out string error)
        {
            pose = default(SkateRiderPose); board = default(SkateBoardPose);
            riderUp = leftSoleUp = rightSoleUp = new SkateVector(0, 1, 0); error = null;
            if (!checkedData) { usableData = ValidData(); checkedData = true; }
            if (!usableData || !Finite(seconds) || seconds < 0 || seconds > Duration || !ValidRig(rig) ||
                (stance != SkateStance.Regular && stance != SkateStance.Goofy))
            { error = "The captured Ollie or measured rig is invalid."; return false; }
            var data = SkateOllieCaptureData.Samples;
            int low = 0, high = data.GetLength(0) - 1;
            while (high - low > 1)
            { int middle = (low + high) / 2; if (data[middle, 0] <= seconds) low = middle; else high = middle; }
            var s = new Sample { A = low, B = high, Weight = (seconds - data[low, 0]) / (data[high, 0] - data[low, 0]), Mirror = stance == SkateStance.Goofy };
            double pitch = s.Number(4) * Math.PI / 180;
            var deck = new SkateBoardPose(s.Vector(1), new SkateVector(0, -Math.Sin(pitch), Math.Cos(pitch)), new SkateVector(0, Math.Cos(pitch), Math.Sin(pitch)));
            var hipRight = Unit(s.Joint(5) - s.Joint(4));
            var shoulderRight = Unit(s.Joint(13) - s.Joint(12));
            // Orthogonalize the observed trunk up to the anatomical lines, preserving hip
            // centres rather than moving a nearly extended leg's root outside its reach.
            var pu = Unit(Project(s.Vector(62), hipRight));
            var cu = Unit(Project(s.Vector(68), shoulderRight));
            // The anatomical lines determine the native handedness, independently of a
            // source renderer's forward-axis convention. The game bone writer uses these axes.
            var pf = Unit(SkateBoardPath.Cross(hipRight, pu));
            var cf = Unit(SkateBoardPath.Cross(shoulderRight, cu));
            var hf = Unit(s.Vector(71));
            var headUp = Unit(s.Joint(3) - s.Joint(2));
            var leftUp = Unit(s.Sole(true)); var rightUp = Unit(s.Sole(false));
            if (!UnitVector(pu) || !UnitVector(cu) || !UnitVector(pf) || !UnitVector(cf) || !UnitVector(hf) ||
                !UnitVector(headUp) || !UnitVector(leftUp) || !UnitVector(rightUp) || !SkateBoardPath.Valid(deck))
            { error = "The captured Ollie has an invalid body or foot frame."; return false; }

            var la = s.Joint(8); var ra = s.Joint(9);
            // Feet stay on their observed board-relative paths. Retarget the hip offset from
            // their centre to the measured leg length; do not scale the skateboard or its rise.
            var ankles = (la + ra) * .5;
            var pelvis = ankles + (s.Joint(0) - ankles) * ((rig.ThighLength + rig.ShinLength) / (.44 + .43));
            var lh = pelvis - hipRight * (rig.HipWidth * .5); var rh = pelvis + hipRight * (rig.HipWidth * .5);
            var chest = pelvis + cu * rig.SpineLength; var neck = pelvis + cu * (rig.SpineLength * 1.12);
            var head = neck + headUp * rig.NeckToHeadLength;
            var shoulders = chest + cu * rig.ShoulderRise;
            var ls = shoulders - shoulderRight * (rig.ShoulderWidth * .5); var rs = shoulders + shoulderRight * (rig.ShoulderWidth * .5);
            double armScale = (rig.UpperArmLength + rig.ForearmLength) / (.29 + .27);
            var lhand = ls + (s.Joint(16) - s.Joint(12)) * armScale;
            var rhand = rs + (s.Joint(17) - s.Joint(13)) * armScale;
            SkateVector lk, rk, le, re;
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, s.Joint(6) - s.Joint(4), out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, s.Joint(7) - s.Joint(5), out rk, out error) ||
                !SkateOllieStyles.TryLimb(ls, lhand, rig.UpperArmLength, rig.ForearmLength, s.Joint(14) - s.Joint(12), out le, out error) ||
                !SkateOllieStyles.TryLimb(rs, rhand, rig.UpperArmLength, rig.ForearmLength, s.Joint(15) - s.Joint(13), out re, out error)) return false;
            // Pose toes are the horizontal-length contact datum expected by RiderRig.Apply,
            // which subtracts the measured ankle-to-toe drop when it aims the real foot bone.
            var lf = Unit(Project(s.Joint(10) - la, leftUp)); var rf = Unit(Project(s.Joint(11) - ra, rightUp));
            if (!UnitVector(lf) || !UnitVector(rf)) { error = "The captured Ollie foot axis is unavailable."; return false; }
            pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, la + lf * rig.FootLength, ra + rf * rig.FootLength,
                chest, neck, head, ls, rs, le, re, lhand, rhand, pf, pu, cf, cu, hf);
            board = deck; leftSoleUp = leftUp; rightSoleUp = rightUp;
            return true;
        }

        private static bool ValidData()
        {
            var data = SkateOllieCaptureData.Samples;
            if (data == null || data.GetLength(1) != 80 || data.GetLength(0) < 2 || data.GetLength(0) > 2401 ||
                !Finite(Duration) || Duration <= 0 || Duration > 10 || Math.Abs(data[0, 0]) > .0000001 ||
                Math.Abs(data[data.GetLength(0) - 1, 0] - Duration) > .0000001) return false;
            for (int row = 0; row < data.GetLength(0); row++)
            {
                if (row > 0 && data[row, 0] <= data[row - 1, 0]) return false;
                for (int col = 0; col < 80; col++) if (!Finite(data[row, col]) || Math.Abs(data[row, col]) > 100) return false;
                for (int col = 59; col <= 77; col += 3)
                {
                    double length = data[row, col] * data[row, col] + data[row, col + 1] * data[row, col + 1] + data[row, col + 2] * data[row, col + 2];
                    if (length < .5 || length > 1.5) return false;
                }
            }
            return true;
        }
        private static bool ValidRig(SkateRiderRig r)
        { return Measure(r.PelvisHeight) && Measure(r.HipWidth) && Measure(r.ThighLength) && Measure(r.ShinLength) && Measure(r.FootLength) &&
            Measure(r.SpineLength) && Measure(r.ShoulderWidth) && Measure(r.UpperArmLength) && Measure(r.ForearmLength) && Measure(r.NeckToHeadLength) &&
            Finite(r.ShoulderRise) && Math.Abs(r.ShoulderRise) <= SkateRider.MaximumMeasurement; }
        private static SkateVector Unit(SkateVector v) { return v.LengthSquared > .00000001 ? v * (1 / v.Length) : default(SkateVector); }
        private static SkateVector Project(SkateVector v, SkateVector up) { return v - up * SkateVector.Dot(v, up); }
        private static bool UnitVector(SkateVector v) { return Finite(v.X) && Finite(v.Y) && Finite(v.Z) && Math.Abs(v.LengthSquared - 1) < .000001; }
        private static bool Measure(double v) { return Finite(v) && v >= SkateRider.MinimumMeasurement && v <= SkateRider.MaximumMeasurement; }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}
