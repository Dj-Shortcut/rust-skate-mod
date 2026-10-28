using System;

namespace Shortcut.RustMod
{
    public struct SkateBoardTruckState
    {
        public readonly double Steering, SteeringVelocity, Axial, AxialVelocity;
        internal SkateBoardTruckState(double turn, double turnVelocity, double axial, double axialVelocity)
        { Steering = turn; SteeringVelocity = turnVelocity; Axial = axial; AxialVelocity = axialVelocity; }
    }

    // Authored opposed elastomer load model, not a multibody tyre/grip solver.
    // Units: metres, seconds, newtons, kilograms; no measured material claim.
    public static class SkateBoardTruckMotion
    {
        public const double SteeringRatio = .8, MaximumSteering = 20, MaximumAxial = .0015;
        public const double TopLength = .0185, BottomLength = .0165, Preload = .28, PreloadForce = 800, EffectiveMass = 30;
        public static readonly double Stiffness = PreloadForce / (TopLength / (1 - Preload) - TopLength) +
            PreloadForce / (BottomLength / (1 - Preload) - BottomLength);
        private static readonly double AxialRate = Math.Sqrt(Stiffness / EffectiveMass);
        public static bool TryStep(SkateBoardTruckState state, double leanDegrees, bool groundSteering,
            double supportedLoad, double landing, double seconds, out SkateBoardTruckState result, out string error)
        {
            result = state; error = null;
            if (!Finite(leanDegrees) || Math.Abs(leanDegrees) > 25 || !Fraction(supportedLoad) || !Fraction(landing) ||
                !Finite(seconds) || seconds < 0 || seconds > .1 ||
                !Finite(state.Steering) || Math.Abs(state.Steering) > MaximumSteering ||
                !Finite(state.SteeringVelocity) || Math.Abs(state.SteeringVelocity) > 1440 ||
                !Finite(state.Axial) || Math.Abs(state.Axial) > MaximumAxial ||
                !Finite(state.AxialVelocity) || Math.Abs(state.AxialVelocity) > 2)
            { error = "Invalid observed truck contact state."; return false; }
            if (seconds == 0) return true;
            // State is displayed deck lean; the cup geometry derives hanger yaw
            // and camber, rather than applying the old kingpin-angle multiplier.
            double turnTarget = groundSteering ? Math.Max(-MaximumSteering, Math.Min(MaximumSteering, leanDegrees)) : 0;
            // Reference load and landing deceleration are explicit authored force
            // proxies. Preload cancels at rest; the net spring rate is kt + kb.
            double referenceForce = EffectiveMass * 9.81 / Math.Sqrt(2);
            double force = referenceForce * supportedLoad + EffectiveMass * (8 / .15) / Math.Sqrt(2) * landing;
            double target = Math.Max(-MaximumAxial, Math.Min(MaximumAxial, force / Stiffness));
            double turn, turnV, axial, axialV;
            Spring(state.Steering, state.SteeringVelocity, turnTarget, 18, seconds, -MaximumSteering, MaximumSteering, out turn, out turnV);
            Spring(state.Axial, state.AxialVelocity, target, AxialRate, seconds, -MaximumAxial, MaximumAxial, out axial, out axialV);
            result = new SkateBoardTruckState(turn, turnV, axial, axialV); return true;
        }
        private static void Spring(double value, double velocity, double target, double rate, double dt,
            double low, double high, out double position, out double speed)
        {
            double delta = value - target, step = (velocity + rate * delta) * dt, decay = Math.Exp(-rate * dt);
            position = target + (delta + step) * decay;
            speed = (velocity - rate * step) * decay;
            // Inelastic bounded bump stops, without an outward hidden velocity.
            if (position < low) { position = low; speed = Math.Max(0, speed); }
            if (position > high) { position = high; speed = Math.Min(0, speed); }
        }
        private static bool Fraction(double x) { return Finite(x) && x >= 0 && x <= 1; }
        private static bool Finite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
    }
}
