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
        public const double LandingImpactSeconds = .065, LandingReleaseSeconds = .30;
        public const double LandingPriorInternalDegrees = 127.5, LandingMaximumDropShare = .14 / .87;
        public const double CatchPreparationSeconds = .04, CatchPriorInternalDegrees = 145;
        public const string LandingPriorProvenance = "Authored minimum catch preparation and landing absorption; fixed feet and canonical rig, not measured capture.";
        public const string SupportedTorsoProvenance = "Authored BS180 supported torso cleanup for ambiguous source lean; not measured capture.";
        private readonly SkateCaptureClip clip;
        private readonly SkateStance stance;
        private readonly SkateDeckFootFitter left, right;
        private readonly double[] release = new double[2], caught = new double[2];
        private readonly SkateVector[] releasedLocal = new SkateVector[2], caughtLocal = new SkateVector[2];
        private SkateCaptureFootSupport(SkateCaptureClip source, SkateStance side, SkateDeckFootFitter l, SkateDeckFootFitter r)
        { clip = source; stance = side; left = l; right = r; }
        internal bool Matches(SkateCaptureClip source, SkateStance side) { return ReferenceEquals(clip, source) && stance == side; }

        // Attached feet use the complete board frame. A free trajectory uses the
        // upright riding carrier. Transport between these frames at the observed
        // release/catch boundaries rather than jumping between coordinate systems.
        // The middle of flight remains independent of the flipping board.
        internal double FreeBoundaryWeight(int side, double sourceTime)
        {
            double span = Math.Min(.08, (caught[side] - release[side]) * .25);
            if (span <= 0 || sourceTime < release[side] || sourceTime > caught[side]) return 0;
            // The coupled body-spin/flip track keeps its free shoe space in
            // the full deck frame while the body's upright carrier stays fixed.
            if (clip.MovementKind == "nollie-backside-180-heelflip") return 1;
            double edge = Math.Min(sourceTime - release[side], caught[side] - sourceTime);
            if (edge >= span) return 0;
            double u = Math.Max(0, edge / span);
            return 1 - u * u * u * (10 + u * (-15 + 6 * u));
        }

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

        // Crossfading does not preserve support on a concave deck. Attached feet
        // retain their measured support; entry and catch approach also clear a
        // released shoe using only a positive, geometry-measured offset.
        public bool TryConform(SkateRiderRig rig, SkateCaptureFrame frame, out SkateCaptureFrame fitted, out string error, bool protectFreeFeet = false)
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
            else if (protectFreeFeet && TryFootFrame(frame.Pose.LeftAnkle, frame.Pose.LeftToe, frame.LeftSoleUp, out foot))
            {
                string ignored;
                if (Fit(0, frame.Board, foot, out amount, out ignored)) l = frame.Board.Up * Math.Max(0, amount);
            }
            if ((frame.Observation.Contacts & SkateCaptureContacts.RightFoot) != 0)
            {
                if (!TryFootFrame(frame.Pose.RightAnkle, frame.Pose.RightToe, frame.RightSoleUp, out foot) ||
                    !Fit(1, frame.Board, foot, out amount, out error)) return false;
                r = frame.Board.Up * amount;
            }
            else if (protectFreeFeet && TryFootFrame(frame.Pose.RightAnkle, frame.Pose.RightToe, frame.RightSoleUp, out foot))
            {
                string ignored;
                if (Fit(1, frame.Board, foot, out amount, out ignored)) r = frame.Board.Up * Math.Max(0, amount);
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
            return TrySetFeet(rig, body, body.LeftAnkle + leftShift, body.LeftToe + leftShift,
                body.RightAnkle + rightShift, body.RightToe + rightShift, adjustPelvis, out pose, out error);
        }

        // Keep the foot frames fixed while the body recovers to ordinary riding.
        // Re-solving a measured chain must transport its bend plane; projecting
        // its old thigh directly onto a new ankle axis can collapse that plane.
        public static bool TrySetFeet(SkateRiderRig rig, SkateRiderPose body, SkateVector la, SkateVector lt,
            SkateVector ra, SkateVector rt, bool adjustPelvis, out SkateRiderPose pose, out string error)
        {
            pose = body; error = null;
            if (!Finite(la.LengthSquared) || !Finite(lt.LengthSquared) || !Finite(ra.LengthSquared) || !Finite(rt.LengthSquared) ||
                Math.Abs((lt - la).Length - rig.FootLength) > .00001 || Math.Abs((rt - ra).Length - rig.FootLength) > .00001)
            { error = "Invalid measured foot targets."; return false; }
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
            SkateVector lk, rk, lp, rp;
            if (!TryBendPlane(body.LeftHip, body.LeftKnee, body.LeftAnkle, lh, la, out lp) ||
                !TryBendPlane(body.RightHip, body.RightKnee, body.RightAnkle, rh, ra, out rp))
            { error = "Measured leg bend plane is underdetermined."; return false; }
            if (!SkateOllieStyles.TryLimb(lh, la, rig.ThighLength, rig.ShinLength, lp, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, ra, rig.ThighLength, rig.ShinLength, rp, out rk, out error)) return false;
            pose = new SkateRiderPose(pelvis, lh, rh, lk, rk, la, ra, lt, rt,
                body.Chest + shift, body.Neck + shift, body.Head + shift, body.LeftShoulder + shift, body.RightShoulder + shift,
                body.LeftElbow + shift, body.RightElbow + shift, body.LeftHand + shift, body.RightHand + shift,
                body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp, body.HeadForward);
            return true;
        }

        public static double CatchPreparationWeight(double secondsSinceCatch)
        {
            double u = Math.Max(0, Math.Min(1, secondsSinceCatch / CatchPreparationSeconds));
            return u * u * u * (10 + u * (-15 + 6 * u));
        }

        public static bool TryCatchPreparation(SkateRiderRig rig, SkateRiderPose body, SkateVector supportUp,
            double weight, out SkateRiderPose pose, out SkateVector pelvisShift, out string error)
        {
            if (!Finite(weight) || weight < 0 || weight > 1)
            { pose = body; pelvisShift = default(SkateVector); error = "Invalid authored catch preparation weight."; return false; }
            return TryKneeAbsorption(rig, body, supportUp, (180 - CatchPriorInternalDegrees) * weight,
                out pose, out pelvisShift, out error);
        }

        public static double ReachedCatchPreparationWeight(SkateRiderRig rig, SkateRiderPose body, double requestedWeight)
        {
            // A strongly offset hip can hit the drop cap before the requested
            // angle. Carry the reached floor into Land, not that unreachable goal.
            double distanceSquared = Math.Max((body.LeftHip - body.LeftAnkle).LengthSquared,
                (body.RightHip - body.RightAnkle).LengthSquared);
            double denominator = 2 * rig.ThighLength * rig.ShinLength;
            if (!Finite(distanceSquared) || !Finite(denominator) || denominator <= 0 || !Finite(requestedWeight)) return 0;
            double cosine = (rig.ThighLength * rig.ThighLength + rig.ShinLength * rig.ShinLength - distanceSquared) / denominator;
            double internalDegrees = Math.Acos(Math.Max(-1, Math.Min(1, cosine))) * 180 / Math.PI;
            return Math.Max(0, Math.Min(requestedWeight, (180 - internalDegrees) / (180 - CatchPriorInternalDegrees)));
        }

        public static bool TryLandingAbsorption(SkateRiderRig rig, SkateRiderPose body, SkateVector supportUp,
            double secondsSinceLand, out SkateRiderPose pose, out SkateVector pelvisShift, out string error,
            double catchPreparationWeight = 0)
        {
            pose = body; pelvisShift = default(SkateVector); error = null;
            if (!Finite(secondsSinceLand) || !Finite(catchPreparationWeight) || catchPreparationWeight < 0 || catchPreparationWeight > 1)
            { error = "Invalid authored landing absorption frame."; return false; }
            if (secondsSinceLand < 0 || secondsSinceLand >= LandingImpactSeconds + LandingReleaseSeconds) return true;
            double u = secondsSinceLand <= LandingImpactSeconds ? secondsSinceLand / LandingImpactSeconds :
                1 - (secondsSinceLand - LandingImpactSeconds) / LandingReleaseSeconds;
            double weight = u * u * u * (10 + u * (-15 + 6 * u));
            double flex = (180 - LandingPriorInternalDegrees) * weight;
            if (secondsSinceLand <= LandingImpactSeconds)
                flex += (180 - CatchPriorInternalDegrees) * catchPreparationWeight * (1 - weight);
            return TryKneeAbsorption(rig, body, supportUp, flex, out pose, out pelvisShift, out error);
        }

        // An angular minimum is idempotent: the prepared catch already present
        // in the impact blend is not compressed again at the Land boundary.
        private static bool TryKneeAbsorption(SkateRiderRig rig, SkateRiderPose body, SkateVector supportUp,
            double flexDegrees, out SkateRiderPose pose, out SkateVector pelvisShift, out string error)
        {
            pose = body; pelvisShift = default(SkateVector); error = null;
            if (!Finite(flexDegrees) || flexDegrees < 0 || flexDegrees > 180 - LandingPriorInternalDegrees ||
                !Finite(supportUp.LengthSquared) || supportUp.LengthSquared < .5 ||
                !Finite(rig.ThighLength) || !Finite(rig.ShinLength) || rig.ThighLength <= 0 || rig.ShinLength <= 0)
            { error = "Invalid authored knee absorption frame."; return false; }
            if (flexDegrees <= 0) return true;
            var up = supportUp * (1 / supportUp.Length);
            double flex = flexDegrees * Math.PI / 180;
            double radius = Math.Sqrt(rig.ThighLength * rig.ThighLength + rig.ShinLength * rig.ShinLength +
                2 * rig.ThighLength * rig.ShinLength * Math.Cos(flex));
            double leftDown, rightDown, leftSafe, rightSafe;
            if (!LandingDrop(body.LeftHip - body.LeftAnkle, up, radius, out leftDown, out leftSafe) ||
                !LandingDrop(body.RightHip - body.RightAnkle, up, radius, out rightDown, out rightSafe))
            { error = "Authored landing absorption has invalid leg targets."; return false; }
            double bound = (rig.ThighLength + rig.ShinLength) * LandingMaximumDropShare;
            double down = Math.Min(Math.Max(leftDown, rightDown), Math.Min(bound, Math.Min(leftSafe, rightSafe)));
            if (down <= 0) return true;
            var shift = up * -down; var lh = body.LeftHip + shift; var rh = body.RightHip + shift;
            SkateVector lk, rk, lp, rp;
            if (!TryBendPlane(body.LeftHip, body.LeftKnee, body.LeftAnkle, lh, body.LeftAnkle, out lp) ||
                !TryBendPlane(body.RightHip, body.RightKnee, body.RightAnkle, rh, body.RightAnkle, out rp) ||
                !SkateOllieStyles.TryLimb(lh, body.LeftAnkle, rig.ThighLength, rig.ShinLength, lp, out lk, out error) ||
                !SkateOllieStyles.TryLimb(rh, body.RightAnkle, rig.ThighLength, rig.ShinLength, rp, out rk, out error))
            { if (error == null) error = "Authored landing absorption has an ambiguous knee plane."; return false; }
            pose = new SkateRiderPose(body.Pelvis + shift, lh, rh, lk, rk,
                body.LeftAnkle, body.RightAnkle, body.LeftToe, body.RightToe,
                body.Chest + shift, body.Neck + shift, body.Head + shift,
                body.LeftShoulder + shift, body.RightShoulder + shift, body.LeftElbow + shift, body.RightElbow + shift,
                body.LeftHand + shift, body.RightHand + shift,
                body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp, body.HeadForward);
            pelvisShift = shift; return true;
        }

        private static bool LandingDrop(SkateVector hipFromAnkle, SkateVector up, double radius, out double down, out double safe)
        {
            down = safe = 0;
            if (!Finite(hipFromAnkle.LengthSquared)) return false;
            double vertical = SkateVector.Dot(hipFromAnkle, up);
            if (vertical <= 0) return true;
            safe = vertical;
            if (hipFromAnkle.LengthSquared <= radius * radius) return true;
            double tangentSquared = Math.Max(0, hipFromAnkle.LengthSquared - vertical * vertical);
            down = Math.Max(0, vertical - Math.Sqrt(Math.Max(0, radius * radius - tangentSquared)));
            return true;
        }

        public static bool TryBendPlane(SkateVector sourceHip, SkateVector sourceKnee, SkateVector sourceAnkle,
            SkateVector targetHip, SkateVector targetAnkle, out SkateVector bend)
        {
            bend = default(SkateVector);
            var a = sourceAnkle - sourceHip; var b = targetAnkle - targetHip;
            if (!Finite(a.LengthSquared) || !Finite(b.LengthSquared) || a.LengthSquared < 1e-12 || b.LengthSquared < 1e-12) return false;
            a *= 1 / a.Length; b *= 1 / b.Length;
            var thigh = sourceKnee - sourceHip;
            var source = thigh - a * SkateVector.Dot(thigh, a);
            double cosine = SkateVector.Dot(a, b);
            if (!Finite(source.LengthSquared) || source.LengthSquared < 1e-12 || cosine < -.9995) return false;
            var cross = SkateBoardPath.Cross(a, b);
            bend = source + SkateBoardPath.Cross(cross, source) +
                SkateBoardPath.Cross(cross, SkateBoardPath.Cross(cross, source)) * (1 / (1 + cosine));
            return Finite(bend.LengthSquared) && bend.LengthSquared > 1e-12;
        }

        internal static bool TryBlendBendPlanes(SkateVector fromHip, SkateVector fromKnee, SkateVector fromAnkle,
            SkateVector toHip, SkateVector toKnee, SkateVector toAnkle, SkateVector hip, SkateVector ankle,
            double weight, out SkateVector bend)
        {
            bend = default(SkateVector); SkateVector a, b;
            if (!TryBendPlane(fromHip, fromKnee, fromAnkle, hip, ankle, out a) ||
                !TryBendPlane(toHip, toKnee, toAnkle, hip, ankle, out b)) return false;
            bend = TurnDirection(a, b, weight, ankle - hip);
            return Finite(bend.LengthSquared) && bend.LengthSquared > .5;
        }

        // Once both feet are planted, preserve the observed impact's bend
        // planes, then return them to ordinary supported riding. Single-camera
        // depth jitter must not twist a planted knee around its hip/ankle axis.
        // Feet and torso are unchanged; optional head transport follows the gaze.
        public static bool TryGroundRecovery(SkateRiderRig rig, SkateRiderPose impact, SkateRiderPose ordinary,
            SkateRiderPose body, SkateVector supportUp, double weight, double gazeWeight, out SkateRiderPose pose, out string error, bool followHeadGaze = false)
        {
            pose = body; error = null;
            if (!Finite(weight) || weight < 0 || weight > 1 || !Finite(gazeWeight) || gazeWeight < 0 || gazeWeight > 1 ||
                !Finite(impact.HeadForward.LengthSquared) || impact.HeadForward.LengthSquared < 1e-12 ||
                !Finite(ordinary.HeadForward.LengthSquared) || ordinary.HeadForward.LengthSquared < 1e-12 ||
                !Finite(supportUp.LengthSquared) || supportUp.LengthSquared < .5 ||
                !Finite(body.ChestForward.LengthSquared) || body.ChestForward.LengthSquared < .5)
            { error = "Invalid supported recovery blend."; return false; }
            SkateVector la, lb, ra, rb, lk, rk;
            if (!TryBendPlane(impact.LeftHip, impact.LeftKnee, impact.LeftAnkle, body.LeftHip, body.LeftAnkle, out la) ||
                !TryBendPlane(ordinary.LeftHip, ordinary.LeftKnee, ordinary.LeftAnkle, body.LeftHip, body.LeftAnkle, out lb) ||
                !TryBendPlane(impact.RightHip, impact.RightKnee, impact.RightAnkle, body.RightHip, body.RightAnkle, out ra) ||
                !TryBendPlane(ordinary.RightHip, ordinary.RightKnee, ordinary.RightAnkle, body.RightHip, body.RightAnkle, out rb))
            { error = "Supported recovery has an ambiguous knee plane."; return false; }
            if (!SkateOllieStyles.TryLimb(body.LeftHip, body.LeftAnkle, rig.ThighLength, rig.ShinLength,
                    TurnDirection(la, lb, weight, body.LeftAnkle - body.LeftHip), out lk, out error) ||
                !SkateOllieStyles.TryLimb(body.RightHip, body.RightAnkle, rig.ThighLength, rig.ShinLength,
                    TurnDirection(ra, rb, weight, body.RightAnkle - body.RightHip), out rk, out error)) return false;
            var gaze = RecoveryGaze(impact.HeadForward, ordinary.HeadForward, body.ChestForward, supportUp, gazeWeight);
            if (!Finite(gaze.LengthSquared) || Math.Abs(gaze.LengthSquared - 1) > .00001)
            { error = "Supported recovery gaze is degenerate."; return false; }
            var head = body.Head;
            if (followHeadGaze)
            {
                var from = body.HeadForward;
                if (!Finite(from.LengthSquared) || from.LengthSquared < 1e-12)
                { error = "Supported recovery head gaze is degenerate."; return false; }
                from *= 1 / from.Length;
                double cosine = Math.Max(-1, Math.Min(1, SkateVector.Dot(from, gaze)));
                if (cosine < -.9995)
                { error = "Supported recovery head gaze turn is ambiguous."; return false; }
                var axis = SkateBoardPath.Cross(from, gaze);
                var offset = body.Head - body.Neck;
                // Shortest-arc gaze rotation: keep the declared head roll and length.
                offset += SkateBoardPath.Cross(axis, offset) +
                    SkateBoardPath.Cross(axis, SkateBoardPath.Cross(axis, offset)) * (1 / (1 + cosine));
                head = body.Neck + offset;
            }
            pose = new SkateRiderPose(body.Pelvis, body.LeftHip, body.RightHip, lk, rk,
                body.LeftAnkle, body.RightAnkle, body.LeftToe, body.RightToe, body.Chest, body.Neck, head,
                body.LeftShoulder, body.RightShoulder, body.LeftElbow, body.RightElbow, body.LeftHand, body.RightHand,
                body.PelvisForward, body.PelvisUp, body.ChestForward, body.ChestUp,
                gaze);
            return true;
        }

        public static bool TrySupportedTorso(SkateRiderPose body, SkateVector supportUp, double secondsSinceCatch,
            out SkateRiderPose pose, out string error)
        {
            pose = body; error = null;
            var torso = body.Neck - body.Pelvis;
            if (!Finite(secondsSinceCatch) || !Finite(torso.LengthSquared) || torso.LengthSquared < 1e-12 ||
                !Finite(supportUp.LengthSquared) || supportUp.LengthSquared < .5)
            { error = "Invalid authored supported torso frame."; return false; }
            var from = torso * (1 / torso.Length); var up = supportUp * (1 / supportUp.Length);
            double angle = Math.Acos(Math.Max(-1, Math.Min(1, SkateVector.Dot(from, up))));
            double u = Math.Max(0, Math.Min(1, secondsSinceCatch / .12));
            double weight = u * u * u * (10 + u * (-15 + 6 * u));
            double turn = Math.Min(25 * Math.PI / 180, Math.Max(0, angle - 30 * Math.PI / 180)) * weight;
            if (turn <= 0) return true;
            var axis = SkateBoardPath.Cross(from, up);
            if (axis.LengthSquared < 1e-12)
            { error = "Authored supported torso swing is ambiguous."; return false; }
            axis *= 1 / axis.Length;
            double c = Math.Cos(turn), s = Math.Sin(turn);
            var chest = body.Pelvis + SwingVector(body.Chest - body.Pelvis, axis, c, s);
            var neck = body.Pelvis + SwingVector(body.Neck - body.Pelvis, axis, c, s);
            // Rotate the upper-body chain as a unit. Keep the source pelvis
            // heading, all leg/foot points and the already solved world gaze.
            pose = new SkateRiderPose(body.Pelvis, body.LeftHip, body.RightHip, body.LeftKnee, body.RightKnee,
                body.LeftAnkle, body.RightAnkle, body.LeftToe, body.RightToe, chest, neck, neck + body.Head - body.Neck,
                body.Pelvis + SwingVector(body.LeftShoulder - body.Pelvis, axis, c, s),
                body.Pelvis + SwingVector(body.RightShoulder - body.Pelvis, axis, c, s),
                body.Pelvis + SwingVector(body.LeftElbow - body.Pelvis, axis, c, s),
                body.Pelvis + SwingVector(body.RightElbow - body.Pelvis, axis, c, s),
                body.Pelvis + SwingVector(body.LeftHand - body.Pelvis, axis, c, s),
                body.Pelvis + SwingVector(body.RightHand - body.Pelvis, axis, c, s),
                body.PelvisForward, body.PelvisUp, SwingVector(body.ChestForward, axis, c, s),
                SwingVector(body.ChestUp, axis, c, s), body.HeadForward);
            return true;
        }

        private static SkateVector SwingVector(SkateVector v, SkateVector axis, double c, double s)
        { return v * c + SkateBoardPath.Cross(axis, v) * s + axis * (SkateVector.Dot(axis, v) * (1 - c)); }

        private static SkateVector TurnDirection(SkateVector a, SkateVector b, double weight, SkateVector legAxis)
        {
            a *= 1 / a.Length; b *= 1 / b.Length;
            if (weight <= 0) return a;
            if (weight >= 1) return b;
            double cosine = Math.Max(-1, Math.Min(1, SkateVector.Dot(a, b)));
            var axis = SkateBoardPath.Cross(a, b);
            if (axis.LengthSquared < 1e-12)
            {
                if (cosine >= 0) return a;
                // Both bend directions share the current leg's normal plane.
                // Keep an antipodal turn in that plane as well.
                axis = legAxis * (1 / legAxis.Length);
            }
            axis *= 1 / axis.Length;
            double angle = Math.Acos(cosine) * weight, c = Math.Cos(angle), s = Math.Sin(angle);
            return a * c + SkateBoardPath.Cross(axis, a) * s + axis * (SkateVector.Dot(axis, a) * (1 - c));
        }

        private static SkateVector RecoveryGaze(SkateVector start, SkateVector target, SkateVector chest, SkateVector support, double weight)
        {
            var up = support * (1 / support.Length);
            start *= 1 / start.Length; target *= 1 / target.Length;
            double sy = Math.Max(-1, Math.Min(1, SkateVector.Dot(start, up)));
            double ty = Math.Max(-1, Math.Min(1, SkateVector.Dot(target, up)));
            var b = target - up * ty;
            if (b.LengthSquared < 1e-12) b = chest - up * SkateVector.Dot(chest, up);
            b *= 1 / b.Length;
            var a = start - up * sy;
            if (a.LengthSquared < 1e-12) a = b;
            a *= 1 / a.Length;
            var facing = chest - up * SkateVector.Dot(chest, up);
            if (facing.LengthSquared < 1e-12) facing = b;
            facing *= 1 / facing.Length;
            double turn = SignedHeading(a, b, up);
            if (Math.Abs(Math.Abs(turn) - Math.PI) < .00001)
                turn = Math.PI * (SkateVector.Dot(SkateBoardPath.Cross(up, a), facing) >= 0 ? 1 : -1);
            var direction = a * Math.Cos(turn * weight) + SkateBoardPath.Cross(up, a) * Math.Sin(turn * weight);
            // Yaw and elevation are independent. A near-opposite horizontal look
            // must not take a shortest 3D arc through a vertical head dive.
            double relative = SignedHeading(facing, direction, up);
            double initial = Math.Abs(SignedHeading(facing, a, up));
            double limit = Math.PI * .5 + Math.Max(0, initial - Math.PI * .5) * (1 - weight);
            relative = Math.Max(-limit, Math.Min(limit, relative));
            direction = facing * Math.Cos(relative) + SkateBoardPath.Cross(up, facing) * Math.Sin(relative);
            double elevation = Math.Asin(sy) * (1 - weight) + Math.Asin(ty) * weight;
            return direction * Math.Cos(elevation) + up * Math.Sin(elevation);
        }

        private static double SignedHeading(SkateVector a, SkateVector b, SkateVector up)
        { return Math.Atan2(SkateVector.Dot(up, SkateBoardPath.Cross(a, b)), Math.Max(-1, Math.Min(1, SkateVector.Dot(a, b)))); }

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
