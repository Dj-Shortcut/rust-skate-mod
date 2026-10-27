using System;

namespace Shortcut.RustMod
{
    public enum SkateCapturePhase : byte { Unknown, Load, Pop, Air, Catch, Land, Recovery }
    [Flags] public enum SkateCaptureContacts : byte { None = 0, LeftFoot = 1, RightFoot = 2, LeftHand = 4, RightHand = 8 }
    public readonly struct SkateCaptureEvent
    {
        public readonly double Time;
        public readonly SkateCapturePhase Phase;
        public readonly SkateCaptureContacts Contacts;
        public SkateCaptureEvent(double time, SkateCapturePhase phase, SkateCaptureContacts contacts)
        { Time = time; Phase = phase; Contacts = contacts; }
    }

    // Immutable after the bounded reader validates it. No caller receives the sample arrays.
    public sealed class SkateCaptureClip
    {
        public readonly string Id, Label, SourceUrl, SourceAuthor, MovementKind;
        public readonly bool Reviewed;
        public readonly double Duration;
        public readonly int SampleCount;
        private readonly double[] rows;
        private readonly SkateCaptureEvent[] events;
        internal bool Ready;
        internal SkateCaptureClip(string id, string label, string url, string author, string kind, bool reviewed,
            double duration, int samples, double[] values, SkateCaptureEvent[] observations)
        { Id = id; Label = label; SourceUrl = url; SourceAuthor = author; MovementKind = kind; Reviewed = reviewed;
          Duration = duration; SampleCount = samples; rows = values; events = observations; }
        internal double Number(int row, int column) { return rows[row * 83 + column]; }
        public int EventCount { get { return events.Length; } }
        public SkateCaptureEvent GetEvent(int index) { return events[index]; }
        internal bool Observe(double seconds, out SkateCaptureEvent observation)
        {
            int low = 0, high = events.Length;
            while (low < high) { int middle = (low + high) / 2; if (events[middle].Time <= seconds) low = middle + 1; else high = middle; }
            observation = low > 0 ? events[low - 1] : default(SkateCaptureEvent);
            return low > 0; // Events are complete observed states, not gameplay callbacks.
        }
    }
    public sealed class SkateCaptureLibrary
    {
        private readonly SkateCaptureClip[] clips;
        internal SkateCaptureLibrary(SkateCaptureClip[] entries) { clips = entries; }
        public int Count { get { return clips.Length; } }
        public SkateCaptureClip GetClip(int index) { return index >= 0 && index < clips.Length ? clips[index] : null; }
    }

    public readonly struct SkateCaptureRotation
    {
        public readonly double X, Y, Z, W;
        public SkateCaptureRotation(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }
        internal double LengthSquared { get { return X * X + Y * Y + Z * Z + W * W; } }
        internal static double Dot(SkateCaptureRotation a, SkateCaptureRotation b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W; }
        internal SkateCaptureRotation Unit()
        { double k = 1 / Math.Sqrt(LengthSquared); return new SkateCaptureRotation(X * k, Y * k, Z * k, W * k); }
        internal SkateCaptureRotation MirrorX() { return new SkateCaptureRotation(X, -Y, -Z, W); }
        public SkateVector Rotate(SkateVector v)
        {
            var axis = new SkateVector(X, Y, Z); var first = SkateBoardPath.Cross(axis, v);
            return v + first * (2 * W) + SkateBoardPath.Cross(axis, first) * 2;
        }
        internal static SkateCaptureRotation Slerp(SkateCaptureRotation a, SkateCaptureRotation b, double t)
        {
            a = a.Unit(); b = b.Unit(); double dot = Dot(a, b);
            if (dot < 0) { b = new SkateCaptureRotation(-b.X, -b.Y, -b.Z, -b.W); dot = -dot; }
            dot = Math.Max(-1, Math.Min(1, dot));
            double left = 1 - t, right = t;
            if (dot < .9995)
            { double angle = Math.Acos(dot), denominator = Math.Sin(angle); left = Math.Sin((1 - t) * angle) / denominator; right = Math.Sin(t * angle) / denominator; }
            return new SkateCaptureRotation(a.X * left + b.X * right, a.Y * left + b.Y * right,
                a.Z * left + b.Z * right, a.W * left + b.W * right).Unit();
        }
    }
    public readonly struct SkateCaptureFrame
    {
        public readonly SkateRiderPose Pose;
        public readonly SkateBoardPose Board;
        public readonly SkateCaptureRotation Rotation;
        public readonly SkateVector RiderUp, LeftSoleUp, RightSoleUp;
        // Additional measured-rig fitting, not a claim about the observed source motion.
        public readonly SkateVector PelvisCorrection, LeftHandCorrection, RightHandCorrection;
        public readonly bool HasEvent;
        public readonly SkateCaptureEvent Observation;
        internal SkateCaptureFrame(SkateRiderPose pose, SkateBoardPose board, SkateCaptureRotation rotation,
            SkateVector left, SkateVector right, bool hasEvent, SkateCaptureEvent observation,
            SkateVector pelvisCorrection, SkateVector leftHandCorrection, SkateVector rightHandCorrection)
        { Pose = pose; Board = board; Rotation = rotation; RiderUp = new SkateVector(0, 1, 0);
          LeftSoleUp = left; RightSoleUp = right; HasEvent = hasEvent; Observation = observation;
          PelvisCorrection = pelvisCorrection; LeftHandCorrection = leftHandCorrection; RightHandCorrection = rightHandCorrection; }
    }

    public static class SkateCaptureSampler
    {
        public const double MaximumPelvisCorrection = .12;
        private struct Sample
        {
            public SkateCaptureClip Clip;
            public int A, B;
            public double Weight;
            public bool Mirror;
            public double Number(int column) { return Clip.Number(A, column) * (1 - Weight) + Clip.Number(B, column) * Weight; }
            public SkateVector Vector(int column) { return new SkateVector(Number(column) * (Mirror ? -1 : 1), Number(column + 1), Number(column + 2)); }
            public SkateVector Joint(int index)
            { if (Mirror && index >= 4) index += index % 2 == 0 ? 1 : -1; return Vector(8 + index * 3); }
            public SkateVector Sole(bool left) { return Vector(left != Mirror ? 77 : 80); }
        }
        public static bool TrySample(SkateCaptureClip clip, double seconds, SkateRiderRig rig, SkateStance stance,
            out SkateCaptureFrame frame, out string error)
        {
            frame = default(SkateCaptureFrame); error = null;
            if (clip == null || !clip.Ready) { error = "The capture clip has not passed pack validation."; return false; }
            return SampleCore(clip, seconds, rig, stance, out frame, out error);
        }
        private static bool SampleCore(SkateCaptureClip clip, double seconds, SkateRiderRig rig, SkateStance stance,
            out SkateCaptureFrame frame, out string error)
        {
            frame = default(SkateCaptureFrame); error = null;
            if (!Finite(seconds) || seconds < 0 || seconds > clip.Duration || !ValidRig(rig) ||
                (stance != SkateStance.Regular && stance != SkateStance.Goofy))
            { error = "Invalid capture time, stance or measured rig."; return false; }
            int low = 0, high = clip.SampleCount - 1;
            while (high - low > 1) { int middle = (low + high) / 2; if (clip.Number(middle, 0) <= seconds) low = middle; else high = middle; }
            var s = new Sample { Clip = clip, A = low, B = high, Weight = Math.Max(0, Math.Min(1,
                (seconds - clip.Number(low, 0)) / (clip.Number(high, 0) - clip.Number(low, 0)))), Mirror = stance == SkateStance.Goofy };
            var rotation = SkateCaptureRotation.Slerp(Rotation(clip, low), Rotation(clip, high), s.Weight);
            if (s.Mirror) rotation = rotation.MirrorX();
            var deck = new SkateBoardPose(s.Vector(1), rotation.Rotate(new SkateVector(0, 0, 1)), rotation.Rotate(new SkateVector(0, 1, 0)));
            var hipRight = Unit(s.Joint(5) - s.Joint(4)); var shoulderRight = Unit(s.Joint(13) - s.Joint(12));
            var pu = Unit(Project(s.Vector(65), hipRight)); var cu = Unit(Project(s.Vector(71), shoulderRight));
            var pf = Unit(SkateBoardPath.Cross(hipRight, pu)); var cf = Unit(SkateBoardPath.Cross(shoulderRight, cu));
            var hf = Unit(s.Vector(74)); var headUp = Unit(s.Joint(3) - s.Joint(2));
            var leftUp = Unit(s.Sole(true)); var rightUp = Unit(s.Sole(false));
            if (!UnitVector(pu) || !UnitVector(cu) || !UnitVector(pf) || !UnitVector(cf) || !UnitVector(hf) ||
                !UnitVector(headUp) || !UnitVector(leftUp) || !UnitVector(rightUp) || !SkateBoardPath.Valid(deck))
            { error = "The capture body, board or sole frame is degenerate."; return false; }
            SkateCaptureEvent observation; bool hasEvent = clip.Observe(seconds, out observation);
            if (s.Mirror) observation = new SkateCaptureEvent(observation.Time, observation.Phase, MirrorContacts(observation.Contacts));
            var la = s.Joint(8); var ra = s.Joint(9); var ankles = (la + ra) * .5;
            var pelvis = ankles + (s.Joint(0) - ankles) * ((rig.ThighLength + rig.ShinLength) / .87);
            SkateVector fitted;
            if (!FitPelvis(pelvis, la + hipRight * (rig.HipWidth * .5), ra - hipRight * (rig.HipWidth * .5),
                rig.ThighLength + rig.ShinLength, out fitted))
            { error = "Captured ankle contacts cannot be reached within the measured pelvis fitting bound."; return false; }
            var pelvisCorrection = fitted - pelvis; pelvis = fitted;
            var lh = pelvis - hipRight * (rig.HipWidth * .5); var rh = pelvis + hipRight * (rig.HipWidth * .5);
            var chest = pelvis + cu * rig.SpineLength; var neck = pelvis + cu * (rig.SpineLength * 1.12); var head = neck + headUp * rig.NeckToHeadLength;
            var shoulders = chest + cu * rig.ShoulderRise;
            var ls = shoulders - shoulderRight * (rig.ShoulderWidth * .5); var rs = shoulders + shoulderRight * (rig.ShoulderWidth * .5);
            double armScale = (rig.UpperArmLength + rig.ForearmLength) / .56;
            var lhand = (observation.Contacts & SkateCaptureContacts.LeftHand) != 0 ? s.Joint(16) : ls + (s.Joint(16) - s.Joint(12)) * armScale;
            var rhand = (observation.Contacts & SkateCaptureContacts.RightHand) != 0 ? s.Joint(17) : rs + (s.Joint(17) - s.Joint(13)) * armScale;
            var originalLeft = lhand; var originalRight = rhand;
            if ((observation.Contacts & SkateCaptureContacts.LeftHand) == 0) lhand = BoundFreeHand(ls, lhand, rig.UpperArmLength + rig.ForearmLength);
            if ((observation.Contacts & SkateCaptureContacts.RightHand) == 0) rhand = BoundFreeHand(rs, rhand, rig.UpperArmLength + rig.ForearmLength);
            SkateVector lk, rk, le, re;
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, s.Joint(6) - s.Joint(4), out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, s.Joint(7) - s.Joint(5), out rk, out error) ||
                !SkateOllieStyles.TryLimb(ls, lhand, rig.UpperArmLength, rig.ForearmLength, s.Joint(14) - s.Joint(12), out le, out error) ||
                !SkateOllieStyles.TryLimb(rs, rhand, rig.UpperArmLength, rig.ForearmLength, s.Joint(15) - s.Joint(13), out re, out error)) return false;
            var lf = Unit(Project(s.Joint(10) - la, leftUp)); var rf = Unit(Project(s.Joint(11) - ra, rightUp));
            if (!UnitVector(lf) || !UnitVector(rf)) { error = "The capture foot axis is unavailable."; return false; }
            var pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, la + lf * rig.FootLength, ra + rf * rig.FootLength,
                chest, neck, head, ls, rs, le, re, lhand, rhand, pf, pu, cf, cu, hf);
            frame = new SkateCaptureFrame(pose, deck, rotation, leftUp, rightUp, hasEvent, observation,
                pelvisCorrection, lhand - originalLeft, rhand - originalRight);
            return true;
        }
        // Closest point in the intersection of the two equal leg-reach balls. Their centres
        // include the measured hip offsets, so the ankle/contact targets never move.
        private static bool FitPelvis(SkateVector point, SkateVector left, SkateVector right, double radius, out SkateVector fitted)
        {
            fitted = point;
            if ((point - left).Length <= radius + .000000001 && (point - right).Length <= radius + .000000001) return true;
            var between = right - left; double distance = between.Length;
            if (distance > radius * 2) return false; // No shared reachable pelvis exists.
            var first = ProjectBall(point, left, radius);
            if ((first - right).Length <= radius + .000000001) fitted = first;
            else
            {
                var second = ProjectBall(point, right, radius);
                if ((second - left).Length <= radius + .000000001) fitted = second;
                else
                {
                    if (distance < .000000001) return false;
                    var axis = between * (1 / distance); var centre = (left + right) * .5;
                    var radial = Project(point - centre, axis);
                    if (radial.LengthSquared < .000000000001) return false;
                    double circle = Math.Sqrt(Math.Max(0, radius * radius - distance * distance * .25));
                    fitted = centre + Unit(radial) * circle;
                }
            }
            return (fitted - point).Length <= MaximumPelvisCorrection &&
                (fitted - left).Length <= radius + .000000001 && (fitted - right).Length <= radius + .000000001;
        }
        private static SkateVector ProjectBall(SkateVector point, SkateVector centre, double radius)
        { var offset = point - centre; double length = offset.Length; return length <= radius ? point : centre + offset * (radius / length); }
        private static SkateVector BoundFreeHand(SkateVector shoulder, SkateVector hand, double radius)
        {
            var offset = hand - shoulder; double length = offset.Length;
            // Correct floating-point overshoot only. Larger differences and every pinned hand
            // still go through the unchanged strict measured-chain rejection below.
            return length > radius && length - radius <= .0000001 ? shoulder + offset * ((radius - .0000000001) / length) : hand;
        }
        internal static bool Validate(SkateCaptureClip clip, out string error)
        {
            error = null;
            if (!Finite(clip.Duration) || clip.Duration <= 0 || clip.Duration > 10 || clip.SampleCount < 2 || clip.SampleCount > 2401 ||
                Math.Abs(clip.Number(0, 0)) > .0000001 || Math.Abs(clip.Number(clip.SampleCount - 1, 0) - clip.Duration) > .0000001)
            { error = "Invalid capture duration or endpoint times."; return false; }
            for (int row = 0; row < clip.SampleCount; row++)
            {
                double time = clip.Number(row, 0);
                if (time < 0 || time > clip.Duration || row > 0 && time <= clip.Number(row - 1, 0))
                { error = "Capture sample times must increase within the clip duration."; return false; }
                for (int col = 0; col < 83; col++) if (!Finite(clip.Number(row, col)) || Math.Abs(clip.Number(row, col)) > 100)
                { error = "Capture samples contain invalid or excessive values."; return false; }
                var q = Rotation(clip, row);
                if (Math.Abs(q.LengthSquared - 1) > .000001)
                { error = "Capture board quaternions must be unit rotations."; return false; }
                if (row > 0 && Math.Abs(SkateCaptureRotation.Dot(Rotation(clip, row - 1).Unit(), q.Unit())) < Math.Cos(75 * Math.PI / 180) - .0000001)
                { error = "Capture board rotation steps exceed 150 degrees; supply denser keys."; return false; }
                for (int col = 62; col <= 80; col += 3)
                {
                    double x = clip.Number(row, col), y = clip.Number(row, col + 1), z = clip.Number(row, col + 2);
                    if (Math.Abs(x * x + y * y + z * z - 1) > .0001)
                    { error = "Capture directions must be unit vectors."; return false; }
                }
            }
            var reference = new SkateRiderRig(.87 * .945, .18, .44, .43, Math.Sqrt(.14 * .14 - .07 * .07), .52 / 1.12,
                .36, .29, .27, .12, .44 - .52 / 1.12);
            SkateCaptureFrame frame;
            for (int row = 0; row < clip.SampleCount; row++)
            {
                if (!SampleCore(clip, clip.Number(row, 0), reference, SkateStance.Regular, out frame, out error)) return false;
                if (frame.PelvisCorrection.Length > .000001)
                { error = "Canonical capture legs exceed the reference rig reach."; return false; }
                if (row > 0)
                {
                    if (!SampleCore(clip, (clip.Number(row - 1, 0) + clip.Number(row, 0)) * .5,
                        reference, SkateStance.Regular, out frame, out error)) return false;
                    if (frame.PelvisCorrection.Length > .000001)
                    { error = "Interpolated canonical capture legs exceed the reference rig reach."; return false; }
                }
            }
            clip.Ready = true; return true;
        }
        private static SkateCaptureRotation Rotation(SkateCaptureClip clip, int row)
        { return new SkateCaptureRotation(clip.Number(row, 4), clip.Number(row, 5), clip.Number(row, 6), clip.Number(row, 7)); }
        private static SkateCaptureContacts MirrorContacts(SkateCaptureContacts c)
        { int v = (int)c; return (SkateCaptureContacts)(((v & 1) << 1) | ((v & 2) >> 1) | ((v & 4) << 1) | ((v & 8) >> 1)); }
        private static bool ValidRig(SkateRiderRig r)
        { return Measure(r.PelvisHeight) && Measure(r.HipWidth) && Measure(r.ThighLength) && Measure(r.ShinLength) && Measure(r.FootLength) &&
            Measure(r.SpineLength) && Measure(r.ShoulderWidth) && Measure(r.UpperArmLength) && Measure(r.ForearmLength) && Measure(r.NeckToHeadLength) &&
            Finite(r.ShoulderRise) && Math.Abs(r.ShoulderRise) <= SkateRider.MaximumMeasurement; }
        private static SkateVector Unit(SkateVector v) { return v.LengthSquared > .00000001 ? v * (1 / v.Length) : default(SkateVector); }
        private static SkateVector Project(SkateVector v, SkateVector n) { return v - n * SkateVector.Dot(v, n); }
        private static bool UnitVector(SkateVector v) { return Finite(v.X) && Finite(v.Y) && Finite(v.Z) && Math.Abs(v.LengthSquared - 1) < .000001; }
        private static bool Measure(double v) { return Finite(v) && v >= SkateRider.MinimumMeasurement && v <= SkateRider.MaximumMeasurement; }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}
