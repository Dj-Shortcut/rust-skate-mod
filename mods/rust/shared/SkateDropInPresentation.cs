using System;

namespace Shortcut.RustMod
{
    // Presentation begins only after the movement adapter confirms the requested transition.
    // This is the supported entry, not a tail-on-coping setup or another movement owner.
    public enum SkateDropInPhase { None, Committing, Absorbing, Recovering }

    public struct SkateDropInFrame
    {
        public readonly bool Initialized;
        public readonly SkateDropInPhase Phase;
        public readonly double Seconds, ForwardWeight, CompressionWeight;
        public readonly SkateSurfaceContact Support;
        public bool Active { get { return Initialized && Math.Max(ForwardWeight, CompressionWeight) > 0; } }
        internal SkateDropInFrame(SkateDropInPhase phase, double seconds, double forward, double compression,
            SkateSurfaceContact support)
        { Initialized = true; Phase = phase; Seconds = seconds; ForwardWeight = forward; CompressionWeight = compression; Support = support; }
    }

    public static class SkateDropInPresentation
    {
        public const double Duration = .7;

        public static bool TrySample(double seconds, SkateSurfaceContact support, out SkateDropInFrame frame, out string error)
        {
            frame = default(SkateDropInFrame); error = null;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 ||
                !Vector(support.Point) || !Vector(support.Normal) || Math.Abs(support.Normal.LengthSquared - 1) > .0001)
            { error = "Invalid drop-in presentation observation."; return false; }
            if (seconds >= Duration || !support.Found || !support.Ramp || support.Coping ||
                support.Normal.Y <= .015 || support.Normal.Y >= .9) return true;
            // Overlapping C2 envelopes keep the weight shift moving while the knees absorb.
            // Intermediate phase labels do not restart an ease or stop every body control.
            double forward = Ease(seconds / .18) * (1 - Ease((seconds - .22) / .4));
            double compression = Ease(seconds / .32) * (1 - Ease((seconds - .32) / .38));
            var phase = seconds < .18 ? SkateDropInPhase.Committing :
                seconds < .42 ? SkateDropInPhase.Absorbing : SkateDropInPhase.Recovering;
            frame = new SkateDropInFrame(phase, seconds, forward, compression, support);
            return true;
        }

        private static double Ease(double t)
        { t = Math.Max(0, Math.Min(1, t)); return t * t * t * (10 + t * (-15 + 6 * t)); }
        private static bool Vector(SkateVector v)
        { return Finite(v.X) && Finite(v.Y) && Finite(v.Z); }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
    }

    public static partial class SkateRider
    {
        // Keep the measured riding soles exactly where the real, currently rendered deck put
        // them. Shift the upper body and solve the same measured leg/arm chains again.
        public static bool TryCreateDropIn(SkateRiderRig rig, SkateRiderInput input, SkateRiderPose basis,
            SkateDropInFrame frame, out SkateRiderPose pose, out string error)
        {
            pose = basis; error = null;
            if (!frame.Active) return true;
            if (!Valid(rig) || !Valid(input) || !Finite(basis) || input.Airborne || input.Bailed ||
                input.Pushing || input.Grab || input.Handplant > 0 || input.FootPlant > 0 || input.BoardPath.Active ||
                !input.DeckFrame.Initialized || !SkateBoardPath.Valid(input.DeckFrame) ||
                SkateVector.Dot(input.DeckFrame.Up, frame.Support.Normal) < .5)
            { error = "Drop-in presentation needs the current supported riding deck."; return false; }
            var up = Unit(input.BoardUp);
            var travel = Unit(Project(input.BoardForward * (input.Switch ? -1 : 1), up));
            if (travel.LengthSquared < .5) { error = "Drop-in travel is unavailable."; return false; }
            var shift = travel * (.075 * frame.ForwardWeight) - up * (.11 * frame.CompressionWeight);
            var pelvisUp = Unit(basis.PelvisUp + travel * (.1 * frame.ForwardWeight));
            var pelvisForward = Unit(Project(basis.PelvisForward, pelvisUp));
            var chestUp = Unit(basis.ChestUp + travel * (.32 * frame.ForwardWeight + .12 * frame.CompressionWeight));
            var chestForward = Unit(Project(basis.ChestForward, chestUp));
            var leftHand = basis.LeftHand + shift + travel * (.07 * frame.ForwardWeight) +
                up * (.06 * frame.ForwardWeight - .03 * frame.CompressionWeight);
            var rightHand = basis.RightHand + shift - travel * (.04 * frame.ForwardWeight) +
                up * (.11 * frame.ForwardWeight + .02 * frame.CompressionWeight);
            var headForward = Unit(basis.HeadForward + travel * (.18 * frame.ForwardWeight) - up * (.08 * frame.ForwardWeight));
            var target = new SkateRiderPose(basis.Pelvis + shift, basis.LeftHip, basis.RightHip,
                basis.LeftKnee, basis.RightKnee, basis.LeftAnkle, basis.RightAnkle, basis.LeftToe, basis.RightToe,
                basis.Chest, basis.Neck, basis.Head, basis.LeftShoulder, basis.RightShoulder,
                basis.LeftElbow, basis.RightElbow, leftHand, rightHand, pelvisForward, pelvisUp, chestForward, chestUp, headForward);
            return BlendContact(rig, input, basis, target, 1, out pose, out error, input.DeckFrame.Origin);
        }
    }
}
