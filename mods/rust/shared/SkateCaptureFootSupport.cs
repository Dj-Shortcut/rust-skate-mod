using System;

namespace Shortcut.RustMod
{
    // Per measured rig, stance and clip. A support fit is model adaptation, not
    // another source observation or a gameplay contact/landing event.
    public sealed class SkateCaptureFootSupport
    {
        public const double MaximumOffset = .08, ContactBand = .00002;
        // The native writer caps a straight leg at .999 of measured reach.
        // Leave a smaller, explicit fitting radius instead of asking that writer
        // to shorten an otherwise exact board contact. Also avoids a singular
        // perfectly straight knee in the independent skin evaluator.
        public const double ReachShare = .998;
        private readonly SkateCaptureClip clip;
        private readonly SkateStance stance;
        private readonly SkateDeckFootFitter left, right;
        private readonly double[] release = new double[2], caught = new double[2];
        private readonly SkateVector[] releasedLocal = new SkateVector[2], caughtLocal = new SkateVector[2];
        private SkateCaptureFootSupport(SkateCaptureClip source, SkateStance side, SkateDeckFootFitter l, SkateDeckFootFitter r)
        { clip = source; stance = side; left = l; right = r; }
        internal bool Matches(SkateCaptureClip source, SkateStance side) { return ReferenceEquals(clip, source) && stance == side; }

        public static bool TryCreate(SkateCapturePlayback playback, SkateRiderRig rig, SkateStance stance,
            SkateDeckFootFitter left, SkateDeckFootFitter right, out SkateCaptureFootSupport support, out string error)
        {
            support = null; error = null;
            if (playback == null || left == null || right == null ||
                (stance != SkateStance.Regular && stance != SkateStance.Goofy))
            { error = "A measured foot profile is required for captured support."; return false; }
            var pending = new SkateCaptureFootSupport(playback.Clip, stance, left, right);
            for (int foot = 0; foot < 2; foot++)
            {
                var bit = (SkateCaptureContacts)(1 << (stance == SkateStance.Goofy ? 1 - foot : foot));
                bool wasContact = true, foundRelease = false, foundCatch = false;
                for (int n = 0; n < pending.clip.EventCount; n++)
                {
                    var e = pending.clip.GetEvent(n); bool contact = (e.Contacts & bit) != 0;
                    if (wasContact && !contact && !foundRelease) { pending.release[foot] = e.Time; foundRelease = true; }
                    if (!wasContact && contact && foundRelease && !foundCatch) { pending.caught[foot] = e.Time; foundCatch = true; }
                    wasContact = contact;
                }
                if (!foundRelease || !foundCatch || pending.caught[foot] <= pending.release[foot])
                { error = "Captured support needs each foot's release and catch."; return false; }
                SkateCaptureFrame frame;
                // Stay on the attached side of the observed release. Never alter
                // an event mask to obtain a convenient contact reconstruction.
                double before = Math.Max(0, pending.release[foot] - .0000001);
                if (!SkateCaptureSampler.TrySample(pending.clip, before, rig, stance, out frame, out error) ||
                    !pending.Boundary(frame, foot, out pending.releasedLocal[foot], out error))
                { error = "Foot " + foot + " at source release: " + error; return false; }
                if (!SkateCaptureSampler.TrySample(pending.clip, pending.caught[foot], rig, stance, out frame, out error) ||
                    !pending.Boundary(frame, foot, out pending.caughtLocal[foot], out error))
                { error = "Foot " + foot + " at source catch: " + error; return false; }
            }
            support = pending; return true;
        }

        private bool Boundary(SkateCaptureFrame frame, int side, out SkateVector local, out string error)
        {
            local = default(SkateVector); SkateBoardPose foot;
            if (!TryFootFrame(side == 0 ? frame.Pose.LeftAnkle : frame.Pose.RightAnkle,
                side == 0 ? frame.Pose.LeftToe : frame.Pose.RightToe,
                side == 0 ? frame.LeftSoleUp : frame.RightSoleUp, out foot))
            { error = "Capture support boundary has a degenerate foot frame."; return false; }
            double offset;
            if (!Fit(side, frame.Board, foot, out offset, out error)) return false;
            var correction = frame.Board.Up * offset;
            local = new SkateVector(SkateVector.Dot(correction, foot.Right), SkateVector.Dot(correction, foot.Up), SkateVector.Dot(correction, foot.Forward));
            return true;
        }

        public bool TryOffsets(double sourceTime, SkateCaptureContacts contacts, SkateBoardPose board,
            SkateBoardPose leftFoot, SkateBoardPose rightFoot, out SkateVector leftOffset, out SkateVector rightOffset, out string error)
        {
            leftOffset = rightOffset = default(SkateVector); error = null;
            if (!Finite(sourceTime) || sourceTime < 0 || sourceTime > clip.Duration ||
                (contacts & ~(SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot)) != 0 ||
                !board.Initialized || !leftFoot.Initialized || !rightFoot.Initialized ||
                !SkateBoardPath.Valid(board) || !SkateBoardPath.Valid(leftFoot) || !SkateBoardPath.Valid(rightFoot))
            { error = "Invalid captured foot support sample."; return false; }
            return Offset(0, sourceTime, (contacts & SkateCaptureContacts.LeftFoot) != 0, board, leftFoot, out leftOffset, out error) &&
                Offset(1, sourceTime, (contacts & SkateCaptureContacts.RightFoot) != 0, board, rightFoot, out rightOffset, out error);
        }

        private bool Offset(int side, double t, bool attached, SkateBoardPose board, SkateBoardPose foot, out SkateVector correction, out string error)
        {
            correction = default(SkateVector); error = null;
            if (attached)
            {
                double offset;
                if (!Fit(side, board, foot, out offset, out error)) return false;
                correction = board.Up * offset; return true;
            }
            // Carry model-specific calibration through the free-foot interval.
            // Abruptly removing it at release would jump an ankle by centimetres.
            // This changes neither the contact flag nor the live catch time.
            if (t < release[side] || t > caught[side])
            { error = "A free foot is outside its observed release/catch interval."; return false; }
            double u = (t - release[side]) / (caught[side] - release[side]);
            u = u * u * u * (10 + u * (-15 + 6 * u));
            var local = releasedLocal[side] * (1 - u) + caughtLocal[side] * u;
            correction = foot.Rotate(local); return true;
        }

        private bool Fit(int side, SkateBoardPose board, SkateBoardPose foot, out double offset, out string error)
        {
            if (!(side == 0 ? left : right).TryOffset(board, foot, MaximumOffset - ContactBand, out offset, out error)) return false;
            offset += ContactBand;
            return true;
        }

        // Crossfading two individually supported poses does not preserve support
        // on a concave deck. Refit only the feet the live sample says are attached.
        public bool TryConform(SkateRiderRig rig, SkateCaptureFrame frame, out SkateCaptureFrame fitted, out string error)
        {
            fitted = frame; var l = default(SkateVector); var r = default(SkateVector);
            SkateBoardPose foot; double amount; error = null;
            if (!frame.Board.Initialized || !SkateBoardPath.Valid(frame.Board))
            { error = "Captured support has no valid live deck frame."; return false; }
            if ((frame.Observation.Contacts & SkateCaptureContacts.LeftFoot) != 0)
            {
                if (!TryFootFrame(frame.Pose.LeftAnkle, frame.Pose.LeftToe, frame.LeftSoleUp, out foot) ||
                    !Fit(0, frame.Board, foot, out amount, out error)) return false;
                l = frame.Board.Up * amount;
            }
            if ((frame.Observation.Contacts & SkateCaptureContacts.RightFoot) != 0)
            {
                if (!TryFootFrame(frame.Pose.RightAnkle, frame.Pose.RightToe, frame.RightSoleUp, out foot) ||
                    !Fit(1, frame.Board, foot, out amount, out error)) return false;
                r = frame.Board.Up * amount;
            }
            SkateRiderPose pose;
            if (!TryShiftFeet(rig, frame.Pose, l, r, true, out pose, out error)) return false;
            fitted = new SkateCaptureFrame(pose, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection + pose.Pelvis - frame.Pose.Pelvis,
                frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            return true;
        }

        // Preserve measured lengths. A failed solve leaves the original pose intact;
        // no unreachable ankle target is handed to a silently clamping native IK.
        public static bool TryShiftFeet(SkateRiderRig rig, SkateRiderPose body, SkateVector leftShift, SkateVector rightShift,
            bool adjustPelvis, out SkateRiderPose pose, out string error)
        {
            pose = body; error = null;
            var la = body.LeftAnkle + leftShift; var ra = body.RightAnkle + rightShift;
            var pelvis = body.Pelvis;
            if (adjustPelvis)
            {
                var across = body.RightHip - body.LeftHip;
                if (across.LengthSquared < .000001) { error = "Invalid measured hip frame."; return false; }
                var hipOffset = across * (rig.HipWidth * .5 / across.Length);
                if (!SkateCaptureSampler.FitPelvis(pelvis, la + hipOffset, ra - hipOffset,
                    (rig.ThighLength + rig.ShinLength) * ReachShare, out pelvis))
                { error = "Shaped-deck support exceeds measured leg reach."; return false; }
            }
            var shift = pelvis - body.Pelvis; var lh = body.LeftHip + shift; var rh = body.RightHip + shift;
            SkateVector lk, rk;
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, body.LeftKnee - body.LeftHip, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, body.RightKnee - body.RightHip, out rk, out error)) return false;
            pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, body.LeftToe + leftShift, body.RightToe + rightShift,
                body.Chest + shift, body.Neck + shift, body.Head + shift, body.LeftShoulder + shift, body.RightShoulder + shift,
                body.LeftElbow + shift, body.RightElbow + shift, body.LeftHand + shift, body.RightHand + shift,
                body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp, body.HeadForward);
            return true;
        }

        public static bool TryFootFrame(SkateVector ankle, SkateVector toe, SkateVector soleUp, out SkateBoardPose frame)
        {
            frame = default(SkateBoardPose);
            if (!Finite(soleUp.LengthSquared) || soleUp.LengthSquared < .000001) return false;
            var up = soleUp * (1 / soleUp.Length);
            var along = toe - ankle; along -= up * SkateVector.Dot(along, up);
            if (!Finite(along.LengthSquared) || along.LengthSquared < .000001) return false;
            frame = new SkateBoardPose(ankle, along * (1 / along.Length), up);
            return SkateBoardPath.Valid(frame);
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
