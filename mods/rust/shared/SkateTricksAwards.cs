using System;

namespace Shortcut.RustMod
{
    public static partial class SkateTricks
    {
        // An independently completed physical action can describe and reward the landing
        // that already happened. This is not another trick, another multiplier or a fake
        // burst of air rotations; banking still belongs to the original combo clock.
        public static bool TryAwardLanding(SkateTrickResult landing, string name, long bonus,
            out SkateTrickResult awarded, out string error)
        {
            awarded = landing; error = null;
            if (string.IsNullOrWhiteSpace(name) || bonus < 0 || bonus > PointsLimit)
            { error = "Invalid completed landing award."; return false; }
            var state = landing.State;
            const SkateTrickEvents allowed = SkateTrickEvents.Landed | SkateTrickEvents.ManualEnded;
            if (!Valid(state) || state.Airborne || state.Grinding || state.ComboCount == 0 ||
                (landing.Events & SkateTrickEvents.Landed) == 0 || (landing.Events & ~allowed) != 0 ||
                landing.BankedPoints != 0 || landing.Points < 0 || landing.Points > PointsLimit ||
                landing.Points > state.ComboBasePoints || landing.ComboPoints != LiveComboValue(state) ||
                !AwardAngle(landing.BoardSpin, 3600000) || !AwardAngle(landing.BoardFlip, 3600000) ||
                !AwardAngle(landing.BoardShove, 3600000) || !AwardAngle(landing.BodySpin, 180) ||
                !AwardSeconds(landing.GrindSeconds) || !AwardSeconds(landing.ManualSeconds))
            { error = "An award needs an unbanked completed landing."; return false; }

            var next = new SkateTrickState(state.Spin, state.Flip, state.Shove, state.GrindSeconds, state.RollingSeconds,
                state.Airborne, state.Grinding, state.GrabKind, state.Switch, Add(state.ComboBasePoints, bonus),
                state.TotalPoints, state.ComboCount, state.ManualSeconds, state.Manualing, state.NoseManualing,
                state.PopKind, state.LegacyNaming, state.GrindKind, state.GrindFrontside, state.PopSwitch,
                state.Late, state.BodySpin, state.PopTailLeads, state.StanceNaming, state.BoardPath);
            awarded = new SkateTrickResult(next, landing.BoardSpin, landing.BoardFlip, landing.BoardShove,
                landing.GrindSeconds, landing.ManualSeconds, landing.Events, name, Add(landing.Points, bonus),
                landing.BankedPoints, landing.BodySpin);
            return true;
        }

        private static bool AwardAngle(double value, double maximum)
        { return Finite(value) && Math.Abs(value) <= maximum; }
        private static bool AwardSeconds(double value)
        { return Finite(value) && value >= 0 && value <= MaximumManualSeconds; }
    }
}
