using System;

namespace Shortcut.RustMod
{
    public static class SkateFlickPitch
    {
        public const double NoseDegrees = 55, ReleaseSeconds = .08;

        public static bool InitialRoute(SkateCaptureTrickSignature signature)
        { return UncombinedRoute(signature) && Near(Math.Abs(signature.Flip), 360); }

        public static bool UncombinedRoute(SkateCaptureTrickSignature signature)
        {
            return signature.Pop == SkatePopKind.Ollie && !signature.Switch && !signature.TailLeads &&
                Finite(signature.Flip) && Math.Abs(signature.Flip) >= 359.99 &&
                Near(signature.Shove, 0) && Near(signature.Spin, 0) && Near(signature.Body, 0);
        }

        // ordinaryPitch is the separately advancing ordinary channel, not the
        // previous pulse output; otherwise its filter would soften the release.
        public static bool TrySample(double elapsed, double ordinaryPitch, out double pitch)
        {
            pitch = ordinaryPitch;
            if (!Finite(elapsed) || elapsed < 0 || !Finite(ordinaryPitch) || Math.Abs(ordinaryPitch) > 90 ||
                elapsed >= SkateFlipFlick.EdgeReleaseSeconds + ReleaseSeconds) return false;
            if (elapsed <= SkateFlipFlick.EdgeReleaseSeconds)
                pitch = NoseDegrees * Smooth(elapsed / SkateFlipFlick.EdgeReleaseSeconds);
            else
            {
                double release = Smooth((elapsed - SkateFlipFlick.EdgeReleaseSeconds) / ReleaseSeconds);
                pitch = NoseDegrees * (1 - release) + ordinaryPitch * release;
            }
            return true;
        }

        private static double Smooth(double value)
        { return value * value * value * (10 + value * (-15 + 6 * value)); }
        private static bool Near(double a, double b) { return Finite(a) && Math.Abs(a - b) < .01; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
