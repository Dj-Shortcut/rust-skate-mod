namespace Shortcut.RustMod
{
    public static class SkateFlatgroundPop
    {
        public const double ReferencePop = 2.4;
        public const double Gravity = 8.0;
        public const double LegacyReferencePop = 6.0;
        public const double VisualLift = .08;
        public const double ReferenceFlightSeconds = 2 * ReferencePop / Gravity;
        public const double RotationRateScale = .75 / ReferenceFlightSeconds;

        public static double ScaleImpulse(double requestedPop)
        { return requestedPop * (ReferencePop / LegacyReferencePop); }
    }
}
