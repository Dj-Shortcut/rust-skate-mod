using System;

namespace Shortcut.RustMod
{
    // One instance per presentation owner. This is an authored, causal body-height
    // channel, never a player force or a foot/contact filter.
    public sealed class SkateCaptureBodyMotion
    {
        // 40 rad/s settles about 95% of a held target in 0.12 s.
        // A slower 28 rad/s candidate exceeded fixed-foot reach at recorded Land.
        public const double Omega = 40;
        public const string Provenance = "Authored causal critically damped vertical body presentation; current foot targets and source torso directions retained.";
        private bool initialized;
        private SkateVector up;
        private double height, velocity;
        public bool Initialized { get { return initialized; } }
        public double Height { get { return height; } }
        public double VerticalVelocity { get { return velocity; } }

        public void Reset()
        { initialized = false; up = default(SkateVector); height = velocity = 0; }

        // The caller supplies the real procedural pose immediately before this
        // capture owner starts, not the first already-raised captured pelvis.
        public bool Begin(SkateRiderPose baseline, SkateVector worldUp, out string error)
        {
            Reset(); error = null;
            if (!Valid(baseline) || !Finite(worldUp) || worldUp.LengthSquared < .5 || worldUp.LengthSquared > 2)
            { error = "Invalid vertical body motion baseline."; return false; }
            up = worldUp * (1 / worldUp.Length); height = SkateVector.Dot(baseline.Pelvis, up);
            initialized = true; return true;
        }

        public bool TryStep(SkateRiderRig rig, SkateCaptureFrame frame, double seconds,
            out SkateCaptureFrame shown, out string error)
        {
            shown = frame; error = null;
            if (!initialized || !Finite(seconds) || seconds < 0 || seconds > .100001 || !Valid(frame.Pose) ||
                !Finite(rig.ThighLength) || !Finite(rig.ShinLength) || !Finite(rig.FootLength) ||
                rig.ThighLength <= 0 || rig.ShinLength <= 0 || rig.FootLength <= 0)
            { Reset(); error = "Invalid or uninitialized vertical body motion step."; return false; }
            double target = SkateVector.Dot(frame.Pose.Pelvis, up);
            // Exact critically damped position/velocity step for this frame's
            // held target. No future pose, fixed-step substitution or dt clamp.
            double offset = height - target, coupling = velocity + Omega * offset;
            double decay = Math.Exp(-Omega * seconds);
            double nextHeight = target + (offset + coupling * seconds) * decay;
            double nextVelocity = (velocity - Omega * coupling * seconds) * decay;
            var shift = up * (nextHeight - target);
            if (!Finite(nextHeight) || !Finite(nextVelocity) || !Finite(shift))
            { Reset(); error = "Invalid vertical body motion state."; return false; }
            // Translate the old measured chains coherently, then restore exactly
            // the current feet through the existing bend-plane transport/IK.
            // adjustPelvis=false preserves the existing reach refusal.
            var translated = Translate(frame.Pose, shift); SkateRiderPose pose;
            if (!SkateCaptureFootSupport.TrySetFeet(rig, translated, frame.Pose.LeftAnkle, frame.Pose.LeftToe,
                frame.Pose.RightAnkle, frame.Pose.RightToe, false, out pose, out error))
            { Reset(); if (error == null) error = "Vertical body motion exceeds unchanged foot reach."; return false; }
            shown = new SkateCaptureFrame(pose, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection + shift,
                frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            height = nextHeight; velocity = nextVelocity; return true;
        }

        private static SkateRiderPose Translate(SkateRiderPose p, SkateVector d)
        {
            return new SkateRiderPose(p.Pelvis + d, p.LeftHip + d, p.RightHip + d,
                p.LeftKnee + d, p.RightKnee + d, p.LeftAnkle + d, p.RightAnkle + d,
                p.LeftToe + d, p.RightToe + d, p.Chest + d, p.Neck + d, p.Head + d,
                p.LeftShoulder + d, p.RightShoulder + d, p.LeftElbow + d, p.RightElbow + d,
                p.LeftHand + d, p.RightHand + d, p.PelvisForward, p.PelvisUp, p.ChestForward, p.ChestUp, p.HeadForward);
        }
        private static bool Valid(SkateRiderPose p)
        {
            return Finite(p.Pelvis) && Finite(p.LeftHip) && Finite(p.RightHip) && Finite(p.LeftKnee) && Finite(p.RightKnee) &&
                Finite(p.LeftAnkle) && Finite(p.RightAnkle) && Finite(p.LeftToe) && Finite(p.RightToe) &&
                Finite(p.Chest) && Finite(p.Neck) && Finite(p.Head) && Finite(p.LeftShoulder) && Finite(p.RightShoulder) &&
                Finite(p.LeftElbow) && Finite(p.RightElbow) && Finite(p.LeftHand) && Finite(p.RightHand) &&
                Unit(p.PelvisForward) && Unit(p.PelvisUp) && Unit(p.ChestForward) && Unit(p.ChestUp) && Unit(p.HeadForward);
        }
        private static bool Unit(SkateVector value) { return Finite(value) && Math.Abs(value.LengthSquared - 1) < .0001; }
        private static bool Finite(SkateVector value) { return Finite(value.LengthSquared); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
