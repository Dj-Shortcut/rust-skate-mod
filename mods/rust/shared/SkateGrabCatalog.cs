using System;

namespace Shortcut.RustMod
{
    public enum SkateGrabHand { Leading, Trailing, Nose, Tail }

    // Authored contact targets in the rider's frame: X toward the toes, Y up, Z toward the nose.
    // A released foot returns through the same contact blend as the hands, before landing.
    public readonly struct SkateGrabDefinition
    {
        public readonly SkateGrabHand Hand;
        public readonly bool Both, End, FeetOnTail;
        public readonly double Across, Along, SecondAcross, SecondAlong, FreeUp, Tuck, Extension,
            LegShift, BodyShift, ChestTurn, HeelTurn, Roll, Pitch, Yaw;
        public readonly SkateVector FrontFoot, BackFoot;
        public bool FrontReleased { get { return FrontFoot.LengthSquared > 0; } }
        public bool BackReleased { get { return BackFoot.LengthSquared > 0; } }
        public SkateGrabDefinition(SkateGrabHand hand, double across, double along, double roll = 0, double pitch = 0,
            bool both = false, bool end = false, double secondAcross = 0.1, double secondAlong = -0.06,
            double freeUp = 0.25, double tuck = 0.05, double extension = 0, double legShift = 0,
            double bodyShift = 0, double chestTurn = 0, double heelTurn = 1.2, double yaw = 0,
            SkateVector frontFoot = default(SkateVector), SkateVector backFoot = default(SkateVector), bool feetOnTail = false)
        {
            Hand = hand; Across = across; Along = along; Roll = roll; Pitch = pitch; Both = both; End = end;
            SecondAcross = secondAcross; SecondAlong = secondAlong; FreeUp = freeUp; Tuck = tuck;
            Extension = extension; LegShift = legShift; BodyShift = bodyShift; ChestTurn = chestTurn;
            HeelTurn = heelTurn; Yaw = yaw; FrontFoot = frontFoot; BackFoot = backFoot; FeetOnTail = feetOnTail;
        }
        public bool LeftHand(bool backIsLeft, bool noseIsLeft)
        { return Hand == SkateGrabHand.Nose ? noseIsLeft : Hand == SkateGrabHand.Tail ? !noseIsLeft :
            Hand == SkateGrabHand.Trailing ? backIsLeft : !backIsLeft; }
    }

    public static class SkateGrabCatalog
    {
        public const int Count = (int)SkateGrabKind.Superdude + 1;
        public const int ModifierButtons = 0x200 | 0x1000 | 0x2000 | 0x4000;
        private static readonly string[] Names = {
            "", "Grab", "Backside Grab", "Frontside Grab", "Double Grab", "Tail Grab", "Nose Grab",
            "Stalefish", "Crail", "Seatbelt", "Melon", "Method", "Nosebone", "Tailbone",
            "Weddle", "Rocket Air", "Tuck Knee", "Stiffy", "Backside Tweak", "Crossbone", "Tail Nosebone",
            "Frontside Shifty Tail Grab", "Backside Shifty Tail Grab", "Nose Tailbone", "Frontside Shifty Nose Grab", "Backside Shifty Nose Grab",
            "Stale Frontside Tuck Knee", "Stale Backside Tuck Knee", "Stale Tailbone", "Stale Nosebone", "Japan Air",
            "Weddle Tailbone", "Weddle Nosebone", "Frontside Shifty Crail", "Backside Shifty Crail", "Frontside Shifty Seatbelt", "Backside Shifty Seatbelt",
            "Double Nose Grab", "Double Tail Grab", "Double Left Grab", "Double Right Grab", "Frigid Air", "Frontside One-Foot Air",
            "Judo Air", "Backside One-Foot Air", "Dog Piss", "Judo Nose Grab", "Benihana", "One-Foot Tail Grab", "Airwalk", "Tailwalk", "Christ Air", "No-Foot Air", "Superdude"
        };
        private static readonly string[] Suffixes = BuildSuffixes();
        private static string[] BuildSuffixes()
        { var a = new string[Count]; a[0] = ""; for (int i = 1; i < a.Length; i++) a[i] = " " + Names[i]; return a; }
        public static bool Valid(SkateGrabKind kind) { return (uint)kind < Count; }
        public static string Name(SkateGrabKind kind) { return Valid(kind) ? Names[(int)kind] : ""; }
        public static string Suffix(SkateGrabKind kind) { return Valid(kind) ? Suffixes[(int)kind] : ""; }

        // Ground-held plant/push buttons must be released or freshly pressed before owning an air grab.
        public static int AirButtons(ref int blocked, bool air, int buttons, int pressed)
        {
            if (!air) blocked = buttons & ModifierButtons;
            else blocked &= buttons & ~pressed;
            return air ? buttons & ModifierButtons & ~blocked : 0;
        }

        // Preserve the existing basic layout. RB owns the extended contact/tweak palette in air;
        // A/X free one foot and B frees both. A naked bumper still belongs to a handplant.
        public static SkateGrabKind Select(bool lt, bool rt, float x, float y, bool mirrored, int buttons, SkateGrabKind basic)
        {
            if (!lt && !rt) return SkateGrabKind.None;
            if (mirrored) x = -x;
            bool a = (buttons & 0x1000) != 0, b = (buttons & 0x2000) != 0, foot = (buttons & 0x4000) != 0;
            if (b || (a && foot))
                return lt && rt ? SkateGrabKind.Superdude : basic == SkateGrabKind.Nose ? SkateGrabKind.Airwalk :
                    basic == SkateGrabKind.Tail ? SkateGrabKind.Tailwalk : rt ? SkateGrabKind.ChristAir : SkateGrabKind.NoFootAir;
            if (a || foot)
            {
                if (basic == SkateGrabKind.Nose) return a ? SkateGrabKind.DogPiss : SkateGrabKind.JudoNose;
                if (basic == SkateGrabKind.Tail) return a ? SkateGrabKind.OneFootTail : SkateGrabKind.Benihana;
                return lt ? a ? SkateGrabKind.FrigidAir : SkateGrabKind.FrontsideOneFoot :
                    a ? SkateGrabKind.BacksideOneFoot : SkateGrabKind.JudoAir;
            }
            if ((buttons & 0x200) == 0) return basic;
            int sx = x < -0.6f ? -1 : x > 0.6f ? 1 : 0, sy = y < -0.6f ? -1 : y > 0.6f ? 1 : 0;
            if (sx != 0 && sy != 0)
            {
                if (lt && rt) return sy > 0 ? sx < 0 ? SkateGrabKind.StaleFrontsideTuckKnee : SkateGrabKind.StaleBacksideTuckKnee :
                    sx < 0 ? SkateGrabKind.StaleNosebone : SkateGrabKind.StaleTailbone;
                if (lt) return sy > 0 ? sx < 0 ? SkateGrabKind.FrontsideShiftyTail : SkateGrabKind.BacksideShiftyTail :
                    sx < 0 ? SkateGrabKind.FrontsideShiftyCrail : SkateGrabKind.BacksideShiftyCrail;
                return sy > 0 ? sx < 0 ? SkateGrabKind.FrontsideShiftyNose : SkateGrabKind.BacksideShiftyNose :
                    sx < 0 ? SkateGrabKind.FrontsideShiftySeatbelt : SkateGrabKind.BacksideShiftySeatbelt;
            }
            if (lt && rt) return sy > 0 ? SkateGrabKind.DoubleNose : sy < 0 ? SkateGrabKind.DoubleTail :
                sx < 0 ? SkateGrabKind.DoubleLeft : sx > 0 ? SkateGrabKind.DoubleRight : SkateGrabKind.RocketAir;
            if (lt) return sy > 0 ? SkateGrabKind.WeddleNosebone : sy < 0 ? SkateGrabKind.WeddleTailbone :
                sx < 0 ? SkateGrabKind.Stiffy : sx > 0 ? SkateGrabKind.TuckKnee : SkateGrabKind.Weddle;
            return sy > 0 ? SkateGrabKind.NoseTailbone : sy < 0 ? SkateGrabKind.TailNosebone :
                sx < 0 ? SkateGrabKind.Crossbone : sx > 0 ? SkateGrabKind.JapanAir : SkateGrabKind.BacksideTweak;
        }

        public static SkateGrabDefinition Get(SkateGrabKind kind)
        {
            const SkateGrabHand lead = SkateGrabHand.Leading, back = SkateGrabHand.Trailing, nose = SkateGrabHand.Nose, tail = SkateGrabHand.Tail;
            switch (kind)
            {
                case SkateGrabKind.Legacy: return new SkateGrabDefinition(back, .1, -.12, -15);
                case SkateGrabKind.Backside: return new SkateGrabDefinition(lead, -.1, .06, -20, bodyShift: -.15);
                case SkateGrabKind.Frontside: return new SkateGrabDefinition(back, .1, -.08, 25);
                case SkateGrabKind.Double: return new SkateGrabDefinition(lead, -.1, .06, both: true, tuck: .035, bodyShift: .1);
                case SkateGrabKind.Tail: return new SkateGrabDefinition(tail, 0, -.36, pitch: -15, freeUp: .3, end: true);
                case SkateGrabKind.Nose: return new SkateGrabDefinition(nose, 0, .36, pitch: 15, freeUp: .3, end: true);
                case SkateGrabKind.Stalefish: return new SkateGrabDefinition(back, -.1, -.15, -20, -8, freeUp: .4);
                case SkateGrabKind.Crail: return new SkateGrabDefinition(back, .1, .3, 15, 12, freeUp: .2, end: true);
                case SkateGrabKind.Seatbelt: return new SkateGrabDefinition(lead, .1, -.3, 15, -12, freeUp: .35, end: true);
                case SkateGrabKind.Melon: return new SkateGrabDefinition(lead, -.1, -.06, -30, -6, freeUp: .35, tuck: .035, bodyShift: -.15, heelTurn: .3);
                case SkateGrabKind.Method: return new SkateGrabDefinition(lead, -.1, .04, -40, 8, freeUp: .45, tuck: .015, bodyShift: -.18);
                case SkateGrabKind.Nosebone: return new SkateGrabDefinition(tail, 0, -.36, pitch: -18, freeUp: .3, end: true, extension: .9, legShift: -.2, chestTurn: -.35);
                case SkateGrabKind.Tailbone: return new SkateGrabDefinition(nose, 0, .36, pitch: 18, freeUp: .3, end: true, extension: .9, legShift: .2, chestTurn: .35);
                case SkateGrabKind.Weddle: return new SkateGrabDefinition(lead, .1, .03, 22, -4, tuck: .025, bodyShift: .08);
                case SkateGrabKind.RocketAir: return new SkateGrabDefinition(nose, -.055, .34, pitch: 42, both: true, end: true, secondAcross: .055, secondAlong: .34, extension: .6, legShift: -.18, feetOnTail: true);
                case SkateGrabKind.TuckKnee: return new SkateGrabDefinition(back, .1, -.08, 35, 10, tuck: .005, legShift: .06, bodyShift: .12);
                case SkateGrabKind.Stiffy: return new SkateGrabDefinition(back, .1, -.08, 12, -8, extension: .9, legShift: -.08);
                case SkateGrabKind.BacksideTweak: return new SkateGrabDefinition(lead, -.1, .06, -38, -12, legShift: -.08, bodyShift: -.18, heelTurn: .5);
                case SkateGrabKind.Crossbone: return new SkateGrabDefinition(lead, -.1, .04, -28, 15, extension: .65, legShift: .12, chestTurn: -.4, yaw: -18);
                case SkateGrabKind.TailNosebone: return new SkateGrabDefinition(tail, 0, -.36, pitch: -28, end: true, extension: .85, legShift: -.22);
                case SkateGrabKind.NoseTailbone: return new SkateGrabDefinition(nose, 0, .36, pitch: 28, end: true, extension: .85, legShift: .22);
                case SkateGrabKind.FrontsideShiftyTail: return new SkateGrabDefinition(tail, 0, -.36, 8, -12, end: true, yaw: 32, chestTurn: -.35);
                case SkateGrabKind.BacksideShiftyTail: return new SkateGrabDefinition(tail, 0, -.36, -8, -12, end: true, yaw: -32, chestTurn: .35);
                case SkateGrabKind.FrontsideShiftyNose: return new SkateGrabDefinition(nose, 0, .36, 8, 12, end: true, yaw: 32, chestTurn: -.35);
                case SkateGrabKind.BacksideShiftyNose: return new SkateGrabDefinition(nose, 0, .36, -8, 12, end: true, yaw: -32, chestTurn: .35);
                case SkateGrabKind.StaleFrontsideTuckKnee: return new SkateGrabDefinition(back, -.1, -.15, -30, 12, tuck: .005, yaw: 20, bodyShift: -.12);
                case SkateGrabKind.StaleBacksideTuckKnee: return new SkateGrabDefinition(back, -.1, -.15, -36, -12, tuck: .008, yaw: -20, bodyShift: -.16);
                case SkateGrabKind.StaleTailbone: return new SkateGrabDefinition(back, -.1, -.15, -22, 24, extension: .8, legShift: .16);
                case SkateGrabKind.StaleNosebone: return new SkateGrabDefinition(back, -.1, -.15, -22, -24, extension: .8, legShift: -.16);
                case SkateGrabKind.JapanAir: return new SkateGrabDefinition(lead, .1, -.08, 42, 18, tuck: .005, legShift: -.1, chestTurn: .5, yaw: 20);
                case SkateGrabKind.WeddleTailbone: return new SkateGrabDefinition(lead, .1, .03, 20, 22, extension: .8, legShift: .18);
                case SkateGrabKind.WeddleNosebone: return new SkateGrabDefinition(lead, .1, .03, 20, -22, extension: .8, legShift: -.18);
                case SkateGrabKind.FrontsideShiftyCrail: return new SkateGrabDefinition(back, .1, .3, 18, 14, end: true, chestTurn: .6, yaw: 30);
                case SkateGrabKind.BacksideShiftyCrail: return new SkateGrabDefinition(back, .1, .3, -18, 14, end: true, chestTurn: -.6, yaw: -30);
                case SkateGrabKind.FrontsideShiftySeatbelt: return new SkateGrabDefinition(lead, .1, -.3, 18, -14, end: true, chestTurn: -.6, yaw: 30);
                case SkateGrabKind.BacksideShiftySeatbelt: return new SkateGrabDefinition(lead, .1, -.3, -18, -14, end: true, chestTurn: .6, yaw: -30);
                case SkateGrabKind.DoubleNose: return new SkateGrabDefinition(nose, -.06, .34, pitch: 15, both: true, end: true, secondAcross: .06, secondAlong: .34);
                case SkateGrabKind.DoubleTail: return new SkateGrabDefinition(tail, -.06, -.34, pitch: -15, both: true, end: true, secondAcross: .06, secondAlong: -.34);
                case SkateGrabKind.DoubleLeft: return new SkateGrabDefinition(lead, -.1, .12, -22, both: true, secondAcross: -.1, secondAlong: -.12, bodyShift: -.12);
                case SkateGrabKind.DoubleRight: return new SkateGrabDefinition(lead, .1, .12, 22, both: true, secondAcross: .1, secondAlong: -.12, bodyShift: .12);
                case SkateGrabKind.FrigidAir: return new SkateGrabDefinition(back, .1, -.08, 20, freeUp: .4, extension: .6, frontFoot: new SkateVector(-.22, .12, .22));
                case SkateGrabKind.FrontsideOneFoot: return new SkateGrabDefinition(back, .1, -.08, 16, -8, freeUp: .4, backFoot: new SkateVector(.18, .08, -.24));
                case SkateGrabKind.JudoAir: return new SkateGrabDefinition(lead, -.1, .04, -25, freeUp: .45, extension: .6, frontFoot: new SkateVector(-.25, .08, .2));
                case SkateGrabKind.BacksideOneFoot: return new SkateGrabDefinition(lead, -.1, .06, -18, 8, freeUp: .35, backFoot: new SkateVector(-.16, .1, -.22));
                case SkateGrabKind.DogPiss: return new SkateGrabDefinition(nose, 0, .36, pitch: 18, end: true, backFoot: new SkateVector(.2, .06, -.25));
                case SkateGrabKind.JudoNose: return new SkateGrabDefinition(nose, 0, .36, -15, 18, end: true, frontFoot: new SkateVector(-.2, .1, .22));
                case SkateGrabKind.Benihana: return new SkateGrabDefinition(tail, 0, -.36, 15, -18, end: true, backFoot: new SkateVector(.22, .08, -.2));
                case SkateGrabKind.OneFootTail: return new SkateGrabDefinition(tail, 0, -.36, -10, -18, end: true, frontFoot: new SkateVector(.15, .1, .24));
                case SkateGrabKind.Airwalk: return new SkateGrabDefinition(nose, 0, .36, pitch: 16, end: true, extension: .5, frontFoot: new SkateVector(.1, .12, .18), backFoot: new SkateVector(-.1, -.04, -.2));
                case SkateGrabKind.Tailwalk: return new SkateGrabDefinition(tail, 0, -.36, pitch: -16, end: true, extension: .5, frontFoot: new SkateVector(-.1, -.04, .2), backFoot: new SkateVector(.1, .12, -.18));
                case SkateGrabKind.ChristAir: return new SkateGrabDefinition(lead, -.1, .03, -55, freeUp: 1, extension: .8, frontFoot: new SkateVector(-.12, -.2, .16), backFoot: new SkateVector(-.12, -.2, -.16));
                case SkateGrabKind.NoFootAir: return new SkateGrabDefinition(back, .1, -.08, 20, extension: .6, frontFoot: new SkateVector(.15, .08, .22), backFoot: new SkateVector(-.15, .08, -.22));
                case SkateGrabKind.Superdude: return new SkateGrabDefinition(nose, -.06, .3, pitch: -35, both: true, end: true, secondAcross: .06, secondAlong: .3, extension: .7, frontFoot: new SkateVector(.12, .12, -.28), backFoot: new SkateVector(-.12, .12, -.3));
                default: return default(SkateGrabDefinition);
            }
        }

        public static SkateVector TurnPoint(SkateGrabDefinition grab, double fold, SkateVector value, SkateVector nose, SkateVector up)
        {
            var right = Cross(up, nose);
            value = Turn(value, up, grab.Yaw * fold);
            value = Turn(value, right, -grab.Pitch * fold);
            return Turn(value, nose, -grab.Roll * fold);
        }
        private static SkateVector Turn(SkateVector v, SkateVector axis, double degrees)
        { double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a); return v * c + Cross(axis, v) * s + axis * (SkateVector.Dot(axis, v) * (1 - c)); }
        private static SkateVector Cross(SkateVector a, SkateVector b)
        { return new SkateVector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
    }
}
