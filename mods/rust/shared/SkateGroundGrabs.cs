using System;

namespace Shortcut.RustMod
{
    public enum SkateGroundGrabKind { None = 0, Frontside = 1, Backside = 2, Double = 3, Coffin = 4 }
    public readonly struct SkateGroundGrabState
    {
        public readonly SkateGroundGrabKind Kind;
        public readonly double Weight, Seconds;
        internal SkateGroundGrabState(SkateGroundGrabKind kind, double weight, double seconds)
        { Kind = kind; Weight = weight; Seconds = seconds; }
        public bool Active { get { return Weight > 0; } }
    }
    public readonly struct SkateGroundGrabResult
    {
        public readonly SkateGroundGrabState State;
        public readonly SkateGroundGrabKind Ended;
        public readonly double EndedSeconds;
        internal SkateGroundGrabResult(SkateGroundGrabState state, SkateGroundGrabKind ended, double seconds)
        { State = state; Ended = ended; EndedSeconds = seconds; }
    }
    public static class SkateGroundGrabs
    {
        public const double BlendSeconds = 0.24, MinimumScoreSeconds = 0.3;
        private static readonly string[] Names = { "", "Frontside Ground Grab", "Backside Ground Grab", "Double Ground Grab", "Coffin" };
        public static string Name(SkateGroundGrabKind kind) { return KindValid(kind) ? Names[(int)kind] : ""; }
        // LB deliberately separates these from the existing trigger pump and trigger+A/X plants.
        public static SkateGroundGrabKind Select(bool ground, bool modifier, bool left, bool right, bool a, bool x)
        { return !ground || !modifier ? SkateGroundGrabKind.None : left && right ? a && x ? SkateGroundGrabKind.Coffin : SkateGroundGrabKind.Double :
            left ? SkateGroundGrabKind.Frontside : right ? SkateGroundGrabKind.Backside : SkateGroundGrabKind.None; }
        public static bool Valid(SkateGroundGrabState state)
        { return KindValid(state.Kind) && Finite(state.Weight) && state.Weight >= 0 && state.Weight <= 1 && Finite(state.Seconds) && state.Seconds >= 0 && state.Seconds <= 86400 &&
            (state.Kind != SkateGroundGrabKind.None || (state.Weight == 0 && state.Seconds == 0)); }
        public static bool TryStep(SkateGroundGrabState state, SkateGroundGrabKind request, bool eligible, double dt,
            out SkateGroundGrabResult result, out string error)
        {
            result = new SkateGroundGrabResult(state, SkateGroundGrabKind.None, 0); error = null;
            if (!Valid(state) || !KindValid(request) || !Finite(dt) || dt <= 0 || dt > 0.1)
            { error = "Invalid ground grab step."; return false; }
            if (!eligible) request = SkateGroundGrabKind.None;
            var visible = state.Kind; double weight = state.Weight, seconds = state.Seconds;
            var ended = SkateGroundGrabKind.None; double endedSeconds = 0;
            if (request != visible && visible != SkateGroundGrabKind.None)
            {
                weight = Math.Max(0, weight - dt / BlendSeconds);
                if (weight <= 0) { ended = visible; endedSeconds = seconds; visible = SkateGroundGrabKind.None; seconds = 0; }
            }
            else if (request != SkateGroundGrabKind.None)
            { weight = Math.Min(1, weight + dt / BlendSeconds); seconds = Math.Min(86400, seconds + dt); }
            if (visible == SkateGroundGrabKind.None && request != SkateGroundGrabKind.None && ended == SkateGroundGrabKind.None)
            { visible = request; weight = Math.Min(1, dt / BlendSeconds); seconds = dt; }
            result = new SkateGroundGrabResult(new SkateGroundGrabState(visible, weight, seconds), ended, endedSeconds); return true;
        }
        private static bool KindValid(SkateGroundGrabKind kind) { return kind >= SkateGroundGrabKind.None && kind <= SkateGroundGrabKind.Coffin; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
