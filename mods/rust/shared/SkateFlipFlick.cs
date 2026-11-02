using System;

namespace Shortcut.RustMod
{
    // Authored grip-slide and instep/heel edge release, in real flight seconds.
    // This gates longitudinal rotation only; input and the vertical pop are immediate.
    public static class SkateFlipFlick
    {
        public const double EdgeReleaseSeconds = .0625;

        public static double RemainingDelay(double elapsed)
        {
            if (!Finite(elapsed) || elapsed < 0) return EdgeReleaseSeconds;
            return Math.Max(0, EdgeReleaseSeconds - elapsed);
        }

        // elapsed is the END of this physics step. A crossing step spends only
        // its post-release fraction turning, without changing the scorer's dt.
        public static double ActiveSeconds(double elapsed, double stepSeconds)
        {
            if (!Finite(elapsed) || elapsed < 0 || !Finite(stepSeconds) || stepSeconds <= 0 || stepSeconds > .050001) return 0;
            double before = Math.Max(0, elapsed - stepSeconds);
            double active = Math.Max(0, elapsed - EdgeReleaseSeconds) - Math.Max(0, before - EdgeReleaseSeconds);
            return Math.Max(0, Math.Min(stepSeconds, active));
        }

        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
