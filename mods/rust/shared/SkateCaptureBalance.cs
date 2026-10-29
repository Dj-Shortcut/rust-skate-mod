using System;

namespace Shortcut.RustMod
{
    // A labelled presentation prior for the normal Heelflip's ambiguous arms.
    // Feet, torso, head, contact masks and the source clock are retained.
    public static class SkateCaptureBalance
    {
        public const string Provenance = "Authored normal Heelflip balance arms; raised free hands settle beside/in front of the shoulders, not measured capture.";

        public static bool TryHeelflip(SkateRiderRig rig, SkateCaptureFrame frame,
            double sinceEntry, double untilCatch, out SkateCaptureFrame balanced, out string error)
        {
            balanced = frame; error = null;
            if (!Finite(sinceEntry) || !Finite(untilCatch) || !Finite(frame.RiderUp.LengthSquared) || frame.RiderUp.LengthSquared < .5)
            { error = "Invalid captured balance phase."; return false; }
            double weight = Smooth(sinceEntry / .08) * Smooth(untilCatch / .08);
            if (weight <= 0) return true;
            var body = frame.Pose; var up = frame.RiderUp * (1 / frame.RiderUp.Length);
            SkateVector lh, rh, le, re;
            if (!Arm(rig, body.LeftShoulder, body.LeftElbow, body.LeftHand,
                body.LeftShoulder - body.RightShoulder, body.ChestForward, up, weight,
                (frame.Observation.Contacts & SkateCaptureContacts.LeftHand) != 0, out le, out lh, out error) ||
                !Arm(rig, body.RightShoulder, body.RightElbow, body.RightHand,
                body.RightShoulder - body.LeftShoulder, body.ChestForward, up, weight,
                (frame.Observation.Contacts & SkateCaptureContacts.RightHand) != 0, out re, out rh, out error)) return false;
            var pose = new SkateRiderPose(body.Pelvis, body.LeftHip, body.RightHip, body.LeftKnee, body.RightKnee,
                body.LeftAnkle, body.RightAnkle, body.LeftToe, body.RightToe, body.Chest, body.Neck, body.Head,
                body.LeftShoulder, body.RightShoulder, le, re, lh, rh,
                body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp, body.HeadForward);
            balanced = new SkateCaptureFrame(pose, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection,
                frame.LeftHandCorrection + lh - body.LeftHand,
                frame.RightHandCorrection + rh - body.RightHand, frame.RiderUp);
            return true;
        }

        private static bool Arm(SkateRiderRig rig, SkateVector shoulder, SkateVector elbow, SkateVector hand,
            SkateVector outward, SkateVector forward, SkateVector up, double weight, bool attached,
            out SkateVector solvedElbow, out SkateVector solvedHand, out string error)
        {
            solvedElbow = elbow; solvedHand = hand; error = null;
            double excess = SkateVector.Dot(hand - shoulder, up) - rig.UpperArmLength * .25;
            if (attached || excess <= 0) return true;
            solvedHand = hand - up * (excess * weight);
            outward -= up * SkateVector.Dot(outward, up);
            forward -= up * SkateVector.Dot(forward, up);
            if (outward.LengthSquared < .000001 || forward.LengthSquared < .000001)
            { error = "Captured balance has no torso frame."; return false; }
            var pole = outward * (.5 / outward.Length) + forward * (.35 / forward.Length) - up * .2;
            pole = (elbow - shoulder) * (1 - weight) + pole * weight;
            return SkateOllieStyles.TryLimb(shoulder, solvedHand, rig.UpperArmLength, rig.ForearmLength,
                pole, out solvedElbow, out error);
        }

        private static double Smooth(double value)
        { double u = Math.Max(0, Math.Min(1, value)); return u * u * u * (10 + u * (-15 + 6 * u)); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
