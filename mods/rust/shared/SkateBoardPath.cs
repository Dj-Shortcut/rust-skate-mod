using System;

namespace Shortcut.RustMod
{
    public enum SkateBoardTrickKind { None = 0, Impossible = 1, FrontFootImpossible = 2, Underflip = 3 }

    // The deck's top-centre frame, independent of the support/player frame. Up may be inverted.
    public readonly struct SkateBoardPose
    {
        public readonly bool Initialized;
        public readonly SkateVector Origin, Forward, Up;
        public SkateBoardPose(SkateVector origin, SkateVector forward, SkateVector up)
        { Initialized = true; Origin = origin; Forward = forward; Up = up; }
        public SkateVector Right { get { return SkateBoardPath.Cross(Up, Forward); } }
        public SkateVector Rotate(SkateVector local) { return Right * local.X + Up * local.Y + Forward * local.Z; }
        public SkateVector Point(SkateVector local) { return Origin + Rotate(local); }
        public SkateVector Project(SkateVector point, double above)
        { return point + Up * (above - SkateVector.Dot(point - Origin, Up)); }
        public SkateBoardPose WithPath(SkateBoardPathSample path)
        { return !path.Known ? this : new SkateBoardPose(Point(path.Point(default(SkateVector))), Rotate(path.Rotate(new SkateVector(0, 0, 1))), Rotate(path.Rotate(new SkateVector(0, 1, 0)))); }
        public SkateBoardPose WithoutPath(SkateBoardPathSample path)
        {
            if (!path.Known) return this;
            var forward = Rotate(path.InverseRotate(new SkateVector(0, 0, 1)));
            var up = Rotate(path.InverseRotate(new SkateVector(0, 1, 0)));
            var basis = new SkateBoardPose(Origin, forward, up);
            return new SkateBoardPose(Origin - basis.Rotate(path.Point(default(SkateVector))), forward, up);
        }
    }

    // Authored presentation trajectory. It never moves the player's physics body.
    public readonly struct SkateBoardPathSample
    {
        public readonly SkateBoardTrickKind Kind;
        public readonly double Progress;
        public readonly int Direction;
        public readonly bool Nollie, Reversed;
        internal SkateBoardPathSample(SkateBoardTrickKind kind, double progress, int direction, bool nollie)
            : this(kind, progress, direction, nollie, false) { }
        internal SkateBoardPathSample(SkateBoardTrickKind kind, double progress, int direction, bool nollie, bool reversed)
        { Kind = kind; Progress = progress; Direction = direction; Nollie = nollie; Reversed = reversed; }
        public bool Known { get { return Kind != SkateBoardTrickKind.None; } }
        public bool Complete { get { return Known && Progress >= 1; } }
        public bool Active { get { return Known && !Complete; } }
        public double Duration { get { return SkateBoardPath.Duration(Kind); } }
        // Physical front/nose foot is +1, rear/tail foot -1; nollie exchanges the popped end.
        public int Foot { get { return (Kind == SkateBoardTrickKind.FrontFootImpossible || Kind == SkateBoardTrickKind.Underflip ? 1 : -1) * (Nollie ? -1 : 1) * (Reversed ? -1 : 1); } }
        public SkateVector Pivot { get { return new SkateVector(0, SkateBoardPath.FootRadius, SkateBoardPath.Axle * Foot); } }
        public double Contact
        {
            get
            {
                if (!Active) return 0;
                if (Kind != SkateBoardTrickKind.Underflip) return 1;
                // A short underside kick reverses the half-flipped board; neither foot rides its roll.
                return Progress < 0.25 ? SkateBoardPath.Clamp((Progress - 0.18) / 0.07) : SkateBoardPath.Clamp((0.4 - Progress) / 0.15);
            }
        }
        public double Roll
        { get { return Kind != SkateBoardTrickKind.Underflip ? 0 : Direction * (Progress <= 0.25 ? Progress * 720 : 180 - (Progress - 0.25) * 720); } }
        private double WrapAngle { get { return Foot * Direction * 360 * Progress; } }
        public SkateVector Rotate(SkateVector value)
        { return SkateBoardPath.Turn(value, Kind == SkateBoardTrickKind.Underflip ? new SkateVector(0, 0, 1) : new SkateVector(1, 0, 0), Kind == SkateBoardTrickKind.Underflip ? Roll : WrapAngle); }
        public SkateVector InverseRotate(SkateVector value)
        { return SkateBoardPath.Turn(value, Kind == SkateBoardTrickKind.Underflip ? new SkateVector(0, 0, 1) : new SkateVector(1, 0, 0), Kind == SkateBoardTrickKind.Underflip ? -Roll : -WrapAngle); }
        public SkateVector Point(SkateVector value)
        {
            if (!Known) return value;
            if (Kind != SkateBoardTrickKind.Underflip) return Pivot + Rotate(value - Pivot);
            double u = Progress <= 0.25 ? 0 : (Progress - 0.25) / 0.75;
            double kick = Math.Sin(u * Math.PI);
            return Rotate(value) + new SkateVector(Direction * 0.06 * kick, 0.03 * kick, 0);
        }
        public SkateVector KickPoint
        { get { return Kind == SkateBoardTrickKind.Underflip ? Point(new SkateVector(0, -0.025, Foot * SkatePose.FootOffset)) : Pivot; } }
    }

    public static class SkateBoardPath
    {
        public const double Axle = 0.23, FootRadius = 0.045;
        public static double Duration(SkateBoardTrickKind kind)
        { return kind == SkateBoardTrickKind.None ? 0 : (kind == SkateBoardTrickKind.Underflip ? 720 : 360) / SkateMotion.FlipRate; }
        public static bool TrySample(SkateBoardTrickKind kind, double progress, int direction, bool nollie,
            out SkateBoardPathSample sample, out string error)
        { return TrySample(kind, progress, direction, nollie, false, out sample, out error); }
        public static bool TrySample(SkateBoardTrickKind kind, double progress, int direction, bool nollie, bool reversed,
            out SkateBoardPathSample sample, out string error)
        {
            sample = default(SkateBoardPathSample); error = null;
            if (kind <= SkateBoardTrickKind.None || kind > SkateBoardTrickKind.Underflip || !Finite(progress) || progress < 0 || progress > 1 || (direction != -1 && direction != 1))
            { error = "Invalid special board path."; return false; }
            sample = new SkateBoardPathSample(kind, progress, direction, nollie, reversed); return true;
        }
        public static bool Valid(SkateBoardPathSample sample)
        { return sample.Kind == SkateBoardTrickKind.None ? sample.Progress == 0 && sample.Direction == 0 && !sample.Nollie && !sample.Reversed :
            sample.Kind > SkateBoardTrickKind.None && sample.Kind <= SkateBoardTrickKind.Underflip && Finite(sample.Progress) && sample.Progress >= 0 && sample.Progress <= 1 && (sample.Direction == -1 || sample.Direction == 1); }
        public static bool Valid(SkateBoardPose pose)
        {
            if (!pose.Initialized) return pose.Origin.LengthSquared == 0 && pose.Forward.LengthSquared == 0 && pose.Up.LengthSquared == 0;
            return Vector(pose.Origin) && Math.Abs(pose.Origin.X) <= 100002 && Math.Abs(pose.Origin.Y) <= 100002 && Math.Abs(pose.Origin.Z) <= 100002 && Vector(pose.Forward) && Vector(pose.Up) &&
                Math.Abs(pose.Forward.LengthSquared - 1) <= 0.0001 && Math.Abs(pose.Up.LengthSquared - 1) <= 0.0001 && Math.Abs(SkateVector.Dot(pose.Forward, pose.Up)) <= 0.0001;
        }
        public static SkateVector Cross(SkateVector a, SkateVector b)
        { return new SkateVector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
        internal static SkateVector Turn(SkateVector value, SkateVector axis, double degrees)
        { double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a); return value * c + Cross(axis, value) * s + axis * (SkateVector.Dot(axis, value) * (1 - c)); }
        internal static double Clamp(double v) { return Math.Max(0, Math.Min(1, v)); }
        private static bool Vector(SkateVector v) { return Finite(v.X) && Finite(v.Y) && Finite(v.Z); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
