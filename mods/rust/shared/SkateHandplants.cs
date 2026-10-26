using System;

namespace Shortcut.RustMod
{
    public enum SkateHandplantKind { None = 0, Tailbone = 1, TuckKnee = 2, Stiff = 3, Boned = 4, Frigid = 5, OneFoot = 6, Gymnast = 7, Crossbone = 8, Method = 9, Tweak = 10, Judo = 11 }
    public readonly struct SkateHandplantState
    {
        public readonly SkateHandplantKind Kind;
        public readonly double Weight, Seconds;
        internal SkateHandplantState(SkateHandplantKind kind, double weight, double seconds)
        { Kind = kind; Weight = weight; Seconds = seconds; }
    }
    public readonly struct SkateHandplantProfile
    {
        public readonly double Pitch, Roll, Out, Up;
        public readonly SkateVector FrontFoot, BackFoot;
        public readonly bool Gymnast;
        internal SkateHandplantProfile(double pitch, double roll, double outward, double upward, SkateVector front = default(SkateVector), SkateVector back = default(SkateVector), bool gymnast = false)
        { Pitch = pitch; Roll = roll; Out = outward; Up = upward; FrontFoot = front; BackFoot = back; Gymnast = gymnast; }
    }
    public static class SkateHandplants
    {
        private static readonly string[] Titles = { "", "Tailbone", "Tuck Knee", "Stiff", "Boned", "Frigid", "One-Foot", "Gymnast", "Crossbone", "Method", "Tweak", "Judo" };
        private static readonly string[] Names = BuildNames();
        private static string[] BuildNames()
        { var a = new string[Titles.Length * 2]; for (int side = 0; side < 2; side++) for (int i = 0; i < Titles.Length; i++) a[side * Titles.Length + i] = (side == 0 ? "Frontside " : "Backside ") + (i == 0 ? "" : Titles[i] + " ") + "Handplant"; return a; }
        public static string Name(SkateHandplantKind kind, bool frontside)
        { return Valid(kind, frontside) ? Names[(frontside ? 0 : Titles.Length) + (int)kind] : ""; }
        public static bool Valid(SkateHandplantKind kind, bool frontside)
        { return kind >= SkateHandplantKind.None && kind <= SkateHandplantKind.Judo && (kind == SkateHandplantKind.None || kind == SkateHandplantKind.TuckKnee || kind == SkateHandplantKind.OneFoot || kind == SkateHandplantKind.Gymnast ||
            (frontside ? kind <= SkateHandplantKind.Frigid : kind >= SkateHandplantKind.Crossbone)); }
        public static bool Valid(SkateHandplantState state, bool frontside)
        { return Valid(state.Kind, frontside) && Finite(state.Weight) && state.Weight >= 0 && state.Weight <= 1 && Finite(state.Seconds) && state.Seconds >= 0 && state.Seconds <= 86400 &&
            (state.Kind != SkateHandplantKind.None || state.Weight == 0 && state.Seconds == 0); }
        public static SkateHandplantKind Select(bool frontside, bool left, bool right, double x, double y, bool a, bool foot)
        {
            if (!left && !right) return SkateHandplantKind.None;
            if (left && right) return SkateHandplantKind.Gymnast;
            if (a) return frontside ? SkateHandplantKind.Frigid : SkateHandplantKind.OneFoot;
            if (foot) return frontside ? SkateHandplantKind.OneFoot : SkateHandplantKind.Judo;
            if (Math.Abs(x) > Math.Abs(y) && Math.Abs(x) > 0.6) return x < 0 ? frontside ? SkateHandplantKind.Boned : SkateHandplantKind.Method : frontside ? SkateHandplantKind.Stiff : SkateHandplantKind.Tweak;
            if (Math.Abs(y) > 0.6) return y > 0 ? frontside ? SkateHandplantKind.Tailbone : SkateHandplantKind.TuckKnee : frontside ? SkateHandplantKind.TuckKnee : SkateHandplantKind.Crossbone;
            return SkateHandplantKind.None;
        }
        public static SkateHandplantProfile Get(SkateHandplantKind kind)
        {
            switch (kind)
            {
                case SkateHandplantKind.Tailbone: return new SkateHandplantProfile(24, 0, .025, -.02);
                case SkateHandplantKind.TuckKnee: return new SkateHandplantProfile(-10, 12, -.07, .05);
                case SkateHandplantKind.Stiff: return new SkateHandplantProfile(6, 0, -.03, .8);
                case SkateHandplantKind.Boned: return new SkateHandplantProfile(-26, 0, .05, 0);
                case SkateHandplantKind.Crossbone: return new SkateHandplantProfile(-12, -22, .04, .02);
                case SkateHandplantKind.Method: return new SkateHandplantProfile(10, -32, -.02, .04);
                case SkateHandplantKind.Tweak: return new SkateHandplantProfile(18, 26, .015, .025);
                case SkateHandplantKind.Frigid: return new SkateHandplantProfile(8, -12, 0, 0, new SkateVector(.2, .12, .18));
                case SkateHandplantKind.OneFoot: return new SkateHandplantProfile(0, 8, 0, .02, back: new SkateVector(.15, .08, -.12));
                case SkateHandplantKind.Judo: return new SkateHandplantProfile(-8, -20, 0, 0, new SkateVector(-.28, .16, .22));
                case SkateHandplantKind.Gymnast: return new SkateHandplantProfile(0, 0, -.04, .07, gymnast: true);
                default: return default(SkateHandplantProfile);
            }
        }
        public static bool TryStep(SkateHandplantState state, SkateHandplantKind request, bool frontside, bool supported, double dt, out SkateHandplantState next, out string error)
        {
            next = state; error = null;
            if (!Valid(state, frontside) || !Valid(request, frontside) || !Finite(dt) || dt <= 0 || dt > .1)
            { error = "Invalid handplant contact request."; return false; }
            if (!supported) { next = default(SkateHandplantState); return true; }
            var kind = state.Kind; double weight = state.Weight, seconds = state.Seconds;
            if (request != kind && kind != SkateHandplantKind.None)
            { weight = Math.Max(0, weight - dt / .16); if (weight == 0) { kind = SkateHandplantKind.None; seconds = 0; } }
            else if (request != SkateHandplantKind.None)
            { weight = Math.Min(1, weight + dt / .16); seconds = Math.Min(86400, seconds + dt); }
            else { weight = 0; seconds = 0; }
            if (kind == SkateHandplantKind.None && state.Kind == SkateHandplantKind.None && request != SkateHandplantKind.None)
            { kind = request; weight = Math.Min(1, dt / .16); seconds = dt; }
            next = new SkateHandplantState(kind, weight, seconds); return true;
        }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }
}
