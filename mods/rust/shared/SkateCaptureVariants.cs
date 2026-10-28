using System;

namespace Shortcut.RustMod
{
    // Presentation recipes only. Each derived movement still needs its own
    // reviewed pack entry and exact owner-approved id; a source never approves it.
    public readonly struct SkateCaptureVariant
    {
        public readonly string MovementKind, SourceMovementKind;
        public readonly SkateCaptureTrickSignature Signature;
        public bool Derived { get { return MovementKind != SourceMovementKind; } }
        internal SkateCaptureVariant(string kind, string source, SkatePopKind pop, bool sw, bool tail,
            double flip, double shove = 0, double spin = 0)
        { MovementKind = kind; SourceMovementKind = source; Signature = new SkateCaptureTrickSignature(pop, sw, tail, flip, shove, spin, 0); }
    }

    public static class SkateCaptureVariants
    {
        public static int Count { get { return Entries.Length; } }
        private static readonly SkateCaptureVariant[] Entries = {
            new SkateCaptureVariant("switch-heelflip", "switch-heelflip", SkatePopKind.Ollie, true, false, -360),
            new SkateCaptureVariant("nollie-heelflip", "nollie-heelflip", SkatePopKind.Nollie, false, false, -360),
            new SkateCaptureVariant("fakie-kickflip", "fakie-kickflip", SkatePopKind.Ollie, true, true, 360),
            new SkateCaptureVariant("nollie-backside-180-heelflip", "nollie-backside-180-heelflip", SkatePopKind.Nollie, false, false, -360, 0, -180),
            new SkateCaptureVariant("nollie-360-flip", "nollie-360-flip", SkatePopKind.Nollie, false, false, 360, -360),
            new SkateCaptureVariant("switch-double-heelflip", "switch-heelflip", SkatePopKind.Ollie, true, false, -720),
            new SkateCaptureVariant("switch-triple-heelflip", "switch-heelflip", SkatePopKind.Ollie, true, false, -1080),
            new SkateCaptureVariant("nollie-double-heelflip", "nollie-heelflip", SkatePopKind.Nollie, false, false, -720),
            new SkateCaptureVariant("nollie-triple-heelflip", "nollie-heelflip", SkatePopKind.Nollie, false, false, -1080),
            new SkateCaptureVariant("fakie-double-kickflip", "fakie-kickflip", SkatePopKind.Ollie, true, true, 720),
            new SkateCaptureVariant("fakie-triple-kickflip", "fakie-kickflip", SkatePopKind.Ollie, true, true, 1080)
        };
        public static SkateCaptureVariant Get(int index)
        { return index >= 0 && index < Entries.Length ? Entries[index] : default(SkateCaptureVariant); }
        public static int IndexOf(string movementKind)
        {
            for (int n = 0; n < Entries.Length; n++) if (Entries[n].MovementKind == movementKind) return n;
            return -1;
        }
        public static string MovementKind(SkateCaptureTrickSignature signature)
        {
            for (int n = 0; n < Entries.Length; n++) if (Entries[n].Signature.Same(signature)) return Entries[n].MovementKind;
            return null;
        }
        public static string SourceMovementKind(string movementKind)
        { int index = IndexOf(movementKind); return index < 0 ? movementKind : Entries[index].SourceMovementKind; }
        public static bool IsFlipExtension(SkateCaptureTrickSignature from, SkateCaptureTrickSignature to)
        {
            int a = IndexOf(MovementKind(from)), b = IndexOf(MovementKind(to));
            if (a < 0 || b < 0 || Entries[a].SourceMovementKind != Entries[b].SourceMovementKind) return false;
            return Math.Abs(to.Flip) > Math.Abs(from.Flip) && Math.Sign(from.Flip) == Math.Sign(to.Flip) &&
                Math.Abs(from.Shove) < .01 && Math.Abs(to.Shove) < .01 && Math.Abs(from.Spin) < .01 && Math.Abs(to.Spin) < .01;
        }
    }
}
