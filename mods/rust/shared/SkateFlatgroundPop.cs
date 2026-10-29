namespace Shortcut.RustMod
{
    public static class SkateFlatgroundPop
    {
        public const double ReferencePop = 1.35;
        public const double Gravity = 3.6;
        public const double LegacyReferencePop = 6.0;
        public const double ReferenceFlightSeconds = .75;

        public static double ScaleImpulse(double requestedPop)
        { return requestedPop * (ReferencePop / LegacyReferencePop); }
    }
}
