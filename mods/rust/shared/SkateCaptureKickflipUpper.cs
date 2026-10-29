using System;

namespace Shortcut.RustMod
{
    // Authored normal-Kickflip upper body. Reference directions are torso-local;
    // they never turn the pelvis, change foot targets or drive gameplay state.
    public sealed class SkateCaptureKickflipUpper
    {
        public const double ForwardLeanDegrees = 32, ElbowInternalDegrees = 130;
        public const double SetupSeconds = .08, ReturnSeconds = .18;
        public const string Provenance = "Authored 32-degree over-nose torso swing; actual Kickflip Catch upper-arm/bend planes relaxed to 130-degree elbows, measured arm IK and source world gaze retained.";
        private bool ready;
        private SkateVector leftUpper, leftFore, rightUpper, rightFore;
        public bool Ready { get { return ready; } }
        public void Reset()
        { ready = false; leftUpper = leftFore = rightUpper = rightFore = default(SkateVector); }

        public bool Begin(SkateRiderRig rig, SkateRiderPose referenceCatch, out string error)
        {
            Reset(); error = null;
            if (!ValidRig(rig) || !Valid(referenceCatch))
            { error = "Invalid Kickflip upper-body reference."; return false; }
            var torso = new SkateBoardPose(referenceCatch.Chest, referenceCatch.ChestForward, referenceCatch.ChestUp);
            if (!SkateBoardPath.Valid(torso) ||
                !ArmReference(rig, torso, referenceCatch.LeftShoulder, referenceCatch.LeftElbow, referenceCatch.LeftHand,
                    out leftUpper, out leftFore) ||
                !ArmReference(rig, torso, referenceCatch.RightShoulder, referenceCatch.RightElbow, referenceCatch.RightHand,
                    out rightUpper, out rightFore))
            { Reset(); error = "Kickflip Catch arms are not a usable relaxed reference."; return false; }
            ready = true; return true;
        }

        // Ordinary charge setup is deliberately family-neutral. The caller
        // excludes action/held-hand owners; arms simply follow the rigid swing.
        public static bool TryChargeFold(SkateRiderPose pose, SkateBoardPose uprightCarrier, double chargeWeight,
            out SkateRiderPose folded, out string error)
        {
            folded = pose; error = null;
            if (!Valid(pose) || !SkateBoardPath.Valid(uprightCarrier) || uprightCarrier.Up.Y < .98 ||
                !Finite(chargeWeight) || chargeWeight < 0 || chargeWeight > 1)
            { error = "Invalid ordinary charge upper-body fold."; return false; }
            double weight = Smooth(chargeWeight);
            if (weight <= 0) return true;
            var facing = Unit(pose.ChestForward - uprightCarrier.Up * SkateVector.Dot(pose.ChestForward, uprightCarrier.Up));
            var targetUp = uprightCarrier.Up * Math.Cos(ForwardLeanDegrees * Math.PI / 180) +
                facing * Math.Sin(ForwardLeanDegrees * Math.PI / 180);
            double cosine = SkateVector.Dot(pose.ChestUp, targetUp);
            if (!Normal(facing) || cosine < -.9999)
            { error = "Ordinary charge facing is degenerate."; return false; }
            var axis = SkateBoardPath.Cross(pose.ChestUp, targetUp);
            var turn = SkateCaptureRotation.Slerp(new SkateCaptureRotation(0, 0, 0, 1),
                new SkateCaptureRotation(axis.X, axis.Y, axis.Z, 1 + cosine), weight);
            var p = pose;
            folded = new SkateRiderPose(p.Pelvis, p.LeftHip, p.RightHip, p.LeftKnee, p.RightKnee,
                p.LeftAnkle, p.RightAnkle, p.LeftToe, p.RightToe,
                p.Pelvis + turn.Rotate(p.Chest - p.Pelvis), p.Pelvis + turn.Rotate(p.Neck - p.Pelvis),
                p.Pelvis + turn.Rotate(p.Head - p.Pelvis),
                p.Pelvis + turn.Rotate(p.LeftShoulder - p.Pelvis), p.Pelvis + turn.Rotate(p.RightShoulder - p.Pelvis),
                p.Pelvis + turn.Rotate(p.LeftElbow - p.Pelvis), p.Pelvis + turn.Rotate(p.RightElbow - p.Pelvis),
                p.Pelvis + turn.Rotate(p.LeftHand - p.Pelvis), p.Pelvis + turn.Rotate(p.RightHand - p.Pelvis),
                p.PelvisForward, p.PelvisUp, turn.Rotate(p.ChestForward), turn.Rotate(p.ChestUp), turn.Rotate(p.HeadForward));
            return true;
        }

        // setupElapsed is real elapsed owner/charge time, never a predicted trick
        // cursor. Negative catch/land ages mean that event has not happened.
        public bool TryApply(SkateRiderRig rig, SkateCaptureFrame frame, SkateBoardPose uprightCarrier,
            double setupElapsed, double secondsSinceCatch, double secondsSinceLand, SkateCaptureContacts heldContacts,
            out SkateCaptureFrame shown, out string error)
        {
            shown = frame; error = null;
            if (!ready || !ValidRig(rig) || !Valid(frame.Pose) || !SkateBoardPath.Valid(uprightCarrier) ||
                uprightCarrier.Up.Y < .98 || !Finite(setupElapsed) || setupElapsed < 0 ||
                !Finite(secondsSinceCatch) || !Finite(secondsSinceLand) || ((int)heldContacts & ~15) != 0)
            { error = "Invalid Kickflip upper-body frame."; return false; }
            double weight = Smooth(setupElapsed / SetupSeconds);
            // Keep the over-board trunk through Catch and wheel contact; the
            // actual landing starts the return to ordinary rollaway.
            if (secondsSinceLand >= 0) weight = Math.Min(weight, 1 - Smooth(secondsSinceLand / ReturnSeconds));
            if (weight <= 0) return true;
            var p = frame.Pose; var turn = Swing(p, uprightCarrier, weight);
            var chest = p.Pelvis + turn.Rotate(p.Chest - p.Pelvis);
            var neck = p.Pelvis + turn.Rotate(p.Neck - p.Pelvis);
            var chestForward = turn.Rotate(p.ChestForward); var chestUp = turn.Rotate(p.ChestUp);
            var ls = p.Pelvis + turn.Rotate(p.LeftShoulder - p.Pelvis);
            var rs = p.Pelvis + turn.Rotate(p.RightShoulder - p.Pelvis);
            var torsoNow = new SkateBoardPose(chest, chestForward, chestUp);
            if (!SkateBoardPath.Valid(torsoNow))
            { error = "Kickflip torso frame is degenerate."; return false; }
            var held = heldContacts | frame.Observation.Contacts;
            SkateVector le, re, lh, rh;
            if (!Arm(rig, torsoNow, p.Pelvis, turn, p.LeftShoulder, p.LeftElbow, p.LeftHand, ls,
                    leftUpper, leftFore, weight, (held & SkateCaptureContacts.LeftHand) != 0, out le, out lh, out error) ||
                !Arm(rig, torsoNow, p.Pelvis, turn, p.RightShoulder, p.RightElbow, p.RightHand, rs,
                    rightUpper, rightFore, weight, (held & SkateCaptureContacts.RightHand) != 0, out re, out rh, out error)) return false;
            // Head position follows the bent neck. Preserve the source world gaze
            // rather than tipping it down again with the chest or adding a yaw.
            var gaze = p.HeadForward;
            var headUp = Unit(chestUp - gaze * SkateVector.Dot(chestUp, gaze));
            var oldHeadUp = turn.Rotate(Unit(p.Head - p.Neck));
            var blendedHeadUp = Unit(oldHeadUp * (1 - weight) + headUp * weight);
            if (!Normal(headUp) || !Normal(blendedHeadUp))
            { error = "Kickflip head frame is degenerate."; return false; }
            var head = neck + blendedHeadUp * rig.NeckToHeadLength;
            var pose = new SkateRiderPose(p.Pelvis, p.LeftHip, p.RightHip, p.LeftKnee, p.RightKnee,
                p.LeftAnkle, p.RightAnkle, p.LeftToe, p.RightToe, chest, neck, head, ls, rs, le, re, lh, rh,
                p.PelvisForward, p.PelvisUp, chestForward, chestUp, p.HeadForward);
            shown = new SkateCaptureFrame(pose, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            return true;
        }

        private static SkateCaptureRotation Swing(SkateRiderPose pose, SkateBoardPose carrier, double weight)
        {
            double currentLean = Math.Atan2(SkateVector.Dot(pose.ChestUp, carrier.Forward),
                SkateVector.Dot(pose.ChestUp, carrier.Up)) * 180 / Math.PI;
            double angle = (ForwardLeanDegrees - currentLean) * weight * Math.PI / 180;
            var axis = carrier.Right; double sine = Math.Sin(angle * .5);
            return new SkateCaptureRotation(axis.X * sine, axis.Y * sine, axis.Z * sine, Math.Cos(angle * .5));
        }

        private static bool ArmReference(SkateRiderRig rig, SkateBoardPose torso, SkateVector shoulder, SkateVector elbow,
            SkateVector hand, out SkateVector upperLocal, out SkateVector foreLocal)
        {
            upperLocal = foreLocal = default(SkateVector);
            var upper = elbow - shoulder; var fore = hand - elbow;
            if (Math.Abs(upper.Length - rig.UpperArmLength) > .00001 || Math.Abs(fore.Length - rig.ForearmLength) > .00001 ||
                SkateVector.Dot(hand - shoulder, torso.Up) > -.1 || SkateVector.Dot(upper, torso.Up) >= -.02) return false;
            upper = Unit(upper); fore = Unit(fore);
            var bend = Unit(fore - upper * SkateVector.Dot(fore, upper));
            if (!Normal(upper) || !Normal(bend)) return false;
            double angle = (180 - ElbowInternalDegrees) * Math.PI / 180;
            fore = upper * Math.Cos(angle) + bend * Math.Sin(angle);
            upperLocal = Local(upper, torso); foreLocal = Local(fore, torso); return true;
        }
        private static bool Arm(SkateRiderRig rig, SkateBoardPose torso, SkateVector pelvis, SkateCaptureRotation turn,
            SkateVector oldShoulder, SkateVector oldElbow, SkateVector oldHand, SkateVector shoulder,
            SkateVector upperLocal, SkateVector foreLocal, double weight, bool held,
            out SkateVector elbow, out SkateVector hand, out string error)
        {
            var upper = torso.Rotate(upperLocal); var fore = torso.Rotate(foreLocal);
            var target = shoulder + upper * rig.UpperArmLength + fore * rig.ForearmLength;
            var carried = pelvis + turn.Rotate(oldHand - pelvis);
            hand = held ? oldHand : carried * (1 - weight) + target * weight;
            var pole = turn.Rotate(oldElbow - oldShoulder) * (1 - weight) + upper * (rig.UpperArmLength * weight);
            return SkateOllieStyles.TryLimb(shoulder, hand, rig.UpperArmLength, rig.ForearmLength, pole, out elbow, out error);
        }
        private static SkateVector Local(SkateVector value, SkateBoardPose frame)
        { return new SkateVector(SkateVector.Dot(value, frame.Right), SkateVector.Dot(value, frame.Up), SkateVector.Dot(value, frame.Forward)); }
        private static SkateVector Unit(SkateVector value)
        { double length = value.Length; return Finite(length) && length > .00000001 ? value * (1 / length) : default(SkateVector); }
        private static double Smooth(double value)
        { double u = Math.Max(0, Math.Min(1, value)); return u * u * u * (10 + u * (-15 + 6 * u)); }
        private static bool ValidRig(SkateRiderRig r)
        { return Finite(r.UpperArmLength) && r.UpperArmLength > 0 && Finite(r.ForearmLength) && r.ForearmLength > 0 &&
            Finite(r.NeckToHeadLength) && r.NeckToHeadLength > 0 && Finite(r.SpineLength) && r.SpineLength > 0 &&
            Finite(r.ShoulderWidth) && r.ShoulderWidth > 0; }
        private static bool Valid(SkateRiderPose p)
        {
            return Finite(p.Pelvis.LengthSquared) && Finite(p.LeftHip.LengthSquared) && Finite(p.RightHip.LengthSquared) &&
                Finite(p.LeftKnee.LengthSquared) && Finite(p.RightKnee.LengthSquared) && Finite(p.LeftAnkle.LengthSquared) &&
                Finite(p.RightAnkle.LengthSquared) && Finite(p.LeftToe.LengthSquared) && Finite(p.RightToe.LengthSquared) && Finite(p.Chest.LengthSquared) && Finite(p.Neck.LengthSquared) && Finite(p.Head.LengthSquared) &&
                Finite(p.LeftShoulder.LengthSquared) && Finite(p.RightShoulder.LengthSquared) && Finite(p.LeftElbow.LengthSquared) &&
                Finite(p.RightElbow.LengthSquared) && Finite(p.LeftHand.LengthSquared) && Finite(p.RightHand.LengthSquared) &&
                Normal(p.PelvisForward) && Normal(p.PelvisUp) && Normal(p.ChestForward) && Normal(p.ChestUp) && Normal(p.HeadForward);
        }
        private static bool Normal(SkateVector value) { return Finite(value.LengthSquared) && Math.Abs(value.LengthSquared - 1) < .0001; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
