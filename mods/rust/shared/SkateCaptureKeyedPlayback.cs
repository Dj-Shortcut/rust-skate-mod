using System;

namespace Shortcut.RustMod
{
    // Exact opt-ins for carrier-local body AND authored-board clips. The caller
    // retains pack/owner approval, action ownership, physics and scoring.
    public sealed class SkateCaptureKeyedPlayback
    {
        public const string RequiredClipId = "kickflip-source-keyed-v1";
        public const string HeelflipClipId = "heelflip-source-keyed-v1";
        public const string TreFlipClipId = "360-flip-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature TreFlipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 360, -360, 0, 0);
        private static readonly SkateCaptureTrickSignature HeelflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, -360, 0, 0, 0);
        public const string Provenance = "Source-keyed flatground flip family with a captured-body basis, authored board/contact repairs and actual clip event ratios, uniformly warped to the physical flight; reconstructed motion, not measured 3D capture.";
        public static readonly SkateCaptureTrickSignature RequiredSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 360, 0, 0, 0);
        private static readonly SkateBoardPose SourceCarrier = new SkateBoardPose(
            new SkateVector(0, 0, 0), new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
        public readonly SkateCaptureClip Clip;
        public double SourcePop { get; private set; }
        public double SourceCatch { get; private set; }
        public double SourceLand { get; private set; }
        public double SourceCatchFraction { get { return (SourceCatch - SourcePop) / (SourceLand - SourcePop); } }
        private bool entry;
        private double entrySeconds;
        private SkateVector entryRoot, entryVelocity, entryTarget, entryTargetVelocity;
        private SkateRiderPose? entryUpper;
        private double entryUpperSeconds;
        private double pressStart = 1.0 / 16, pressEnd = 3.0 / 16;
        private SkateVector? groundOrigin;

        private SkateCaptureKeyedPlayback(SkateCaptureClip clip, double pop, double caught, double land)
        { Clip = clip; SourcePop = pop; SourceCatch = caught; SourceLand = land; }

        public static bool Accepts(SkateCaptureClip clip, SkateCaptureTrickSignature signature)
        {
            return clip != null && (clip.Id == RequiredClipId && clip.MovementKind == "kickflip" && RequiredSignature.Same(signature) ||
                clip.Id == HeelflipClipId && clip.MovementKind == "heelflip" && HeelflipSignature.Same(signature) ||
                clip.Id == TreFlipClipId && clip.MovementKind == "360-flip" && TreFlipSignature.Same(signature));
        }

        public static bool TryCreate(SkateCaptureClip clip, out SkateCaptureKeyedPlayback playback, out string error)
        {
            playback = null; error = null;
            if (clip == null || !clip.Ready || !Accepts(clip, clip.Id == HeelflipClipId ? HeelflipSignature : clip.Id == TreFlipClipId ? TreFlipSignature : RequiredSignature))
            { error = "An exact bounded source-keyed flip clip is required."; return false; }
            // Reuse the existing strict event/contact contract. Building its
            // optional inverse clocks does not make those clocks authoritative.
            SkateCapturePlayback events;
            if (!SkateCapturePlayback.TryCreate(clip, out events, out error)) return false;
            playback = new SkateCaptureKeyedPlayback(clip, events.SourceTime(SkateCapturePhase.Pop, 0),
                events.SourceTime(SkateCapturePhase.Catch, 0), events.SourceTime(SkateCapturePhase.Land, 0));
            if (clip.Id != RequiredClipId && !playback.TryFamilyPressWindow(out error))
            { playback = null; return false; }
            return true;
        }

        private bool TryFamilyPressWindow(out string error)
        {
            error = null; bool released = false;
            for (int n = 0; n < Clip.EventCount; n++)
            {
                var e = Clip.GetEvent(n);
                if (e.Time <= SourcePop) continue;
                if (!released && (e.Contacts & SkateCaptureContacts.RightFoot) == 0)
                { pressStart = (e.Time - SourcePop) / (SourceLand - SourcePop); released = true; }
                else if (released && e.Phase == SkateCapturePhase.Air && e.Contacts == SkateCaptureContacts.None)
                {
                    pressEnd = (e.Time - SourcePop) / (SourceLand - SourcePop);
                    if (pressStart >= 0 && pressEnd > pressStart && pressEnd < SourceCatchFraction) return true;
                    break;
                }
            }
            error = "Source-keyed family needs an explicit rear-sole release and later free-Air press endpoint.";
            return false;
        }

        public bool Finished(double secondsSinceLand, double frozenFlightSeconds)
        {
            return ValidFlight(frozenFlightSeconds) && Finite(secondsSinceLand) && secondsSinceLand >= 0 &&
                secondsSinceLand / frozenFlightSeconds >= (Clip.Duration - SourceLand) / (SourceLand - SourcePop);
        }

        public bool BeginEntry(SkateRiderRig rig, SkateStance stance, SkateVector localRoot,
            SkateVector localVelocity, double frozenFlightSeconds, out string error, SkateRiderPose? localBody = null,
            SkateVector? groundCarrierOrigin = null)
        {
            entry = false; entryUpper = null; groundOrigin = null; error = null;
            if (!ValidFlight(frozenFlightSeconds) || !Finite(localRoot.LengthSquared) || !Finite(localVelocity.LengthSquared) ||
                groundCarrierOrigin.HasValue && !Finite(groundCarrierOrigin.Value.LengthSquared))
            { error = "Invalid source-keyed entry baseline."; return false; }
            // Source264 is the compact coupled Air after the early pressure rise.
            // The independent upper entry and exact foot/board paths stay short.
            double rootFraction = Clip.Id == RequiredClipId ? .3125 : .125;
            double span = (SourceLand - SourcePop) * rootFraction;
            double targetTime = SourcePop + span, step = Math.Min(.005, span * .1);
            SkateCaptureFrame target, before, after;
            if (!SkateCaptureSampler.TrySample(Clip, targetTime, rig, stance, out target, out error) ||
                !SkateCaptureSampler.TrySample(Clip, targetTime - step, rig, stance, out before, out error) ||
                !SkateCaptureSampler.TrySample(Clip, targetTime + step, rig, stance, out after, out error)) return false;
            entrySeconds = frozenFlightSeconds * rootFraction;
            entryRoot = localRoot; entryVelocity = localVelocity; entryTarget = target.Pose.Pelvis;
            // The observed flatground launch span supplies its actual takeoff
            // speed. Subtract the carrier derivative in local space so adding
            // that carrier preserves the held body's world velocity at release.
            if (Clip.Id == RequiredClipId && groundCarrierOrigin.HasValue)
                entryVelocity -= new SkateVector(0, SkateFlatgroundPop.Gravity * frozenFlightSeconds * .5, 0);
            entryUpper = localBody; entryUpperSeconds = frozenFlightSeconds * .125;
            groundOrigin = groundCarrierOrigin;
            entryTargetVelocity = (after.Pose.Pelvis - before.Pose.Pelvis) *
                ((SourceLand - SourcePop) / (2 * step * frozenFlightSeconds));
            entry = true; return true;
        }

        public bool TrySourceTime(double elapsedSeconds, double frozenFlightSeconds, bool actuallyLanded,
            double secondsSinceLand, out double sourceSeconds, out string error)
        {
            sourceSeconds = 0; error = null;
            if (!Finite(elapsedSeconds) || elapsedSeconds < 0 || !ValidFlight(frozenFlightSeconds) ||
                !Finite(secondsSinceLand) || actuallyLanded && secondsSinceLand < 0)
            { error = "Invalid source-keyed physical flight clock."; return false; }
            double span = SourceLand - SourcePop;
            if (actuallyLanded)
            {
                // The same uniform time scale continues through authored
                // recovery, but the physical landing starts that segment.
                double recovery = Math.Min((Clip.Duration - SourceLand) / span, secondsSinceLand / frozenFlightSeconds);
                sourceSeconds = Math.Min(Clip.Duration, SourceLand + recovery * span);
            }
            else
            {
                // A longer real flight holds before the Land event. No source
                // timestamp may announce a gameplay landing ahead of physics.
                double beforeLand = SourceLand - span * .0000001;
                sourceSeconds = Math.Min(beforeLand, SourcePop + Math.Min(1, elapsedSeconds / frozenFlightSeconds) * span);
            }
            return true;
        }

        public bool TrySample(SkateRiderRig rig, SkateStance stance, double elapsedSeconds,
            double frozenFlightSeconds, bool actuallyLanded, double secondsSinceLand, SkateBoardPose uprightCarrier,
            out SkateCaptureFrame shown, out string error)
        {
            shown = default(SkateCaptureFrame); double seconds;
            if (!TrySourceTime(elapsedSeconds, frozenFlightSeconds, actuallyLanded, secondsSinceLand, out seconds, out error)) return false;
            if (!SkateBoardPath.Valid(uprightCarrier) || uprightCarrier.Up.Y < .98)
            { error = "Source-keyed playback requires the current upright physical carrier."; return false; }
            SkateCaptureFrame source; SkateRiderPose pose;
            if (!SkateCaptureSampler.TrySample(Clip, seconds, rig, stance, out source, out error)) return false;
            // Source coordinates are already carrier-local: do NOT subtract the
            // current board centre or add another procedural pitch/pop envelope.
            if (!SkateRider.TryTransformPose(source.Pose, SourceCarrier, uprightCarrier, out pose))
            { error = "The source-keyed carrier transform is unavailable."; return false; }
            var entryShift = default(SkateVector);
            if (entry && !actuallyLanded && elapsedSeconds < entrySeconds)
            {
                double u = elapsedSeconds / entrySeconds, u2 = u * u, u3 = u2 * u;
                var distance = entryTarget - entryRoot;
                var a = entryVelocity * entrySeconds; var b = entryTargetVelocity * entrySeconds;
                var root = entryRoot + a * u + (distance * 10 - a * 6 - b * 4) * u3 +
                    (distance * -15 + a * 8 + b * 7) * (u3 * u) +
                    (distance * 6 - a * 3 - b * 3) * (u3 * u2);
                var shift = uprightCarrier.Point(root) - pose.Pelvis; entryShift = shift;
                var translated = new SkateRiderPose(pose.Pelvis + shift, pose.LeftHip + shift, pose.RightHip + shift,
                    pose.LeftKnee + shift, pose.RightKnee + shift, pose.LeftAnkle + shift, pose.RightAnkle + shift,
                    pose.LeftToe + shift, pose.RightToe + shift, pose.Chest + shift, pose.Neck + shift, pose.Head + shift,
                    pose.LeftShoulder + shift, pose.RightShoulder + shift, pose.LeftElbow + shift, pose.RightElbow + shift,
                    pose.LeftHand + shift, pose.RightHand + shift, pose.PelvisForward, pose.PelvisUp,
                    pose.ChestForward, pose.ChestUp, pose.HeadForward);
                // Feet and board stay on their source paths across the root-owner boundary.
                if (!SkateCaptureFootSupport.TrySetFeet(rig, translated, pose.LeftAnkle, pose.LeftToe,
                    pose.RightAnkle, pose.RightToe, false, out pose, out error)) return false;
            }
            var boardShift = default(SkateVector);
            if (groundOrigin.HasValue && !actuallyLanded)
            {
                // The authored press cancels the reference carrier only here.
                // Replace that rise with the actual carrier; never add a second jump.
                double fraction = (seconds - SourcePop) / (SourceLand - SourcePop);
                double press = PressRelease(fraction, pressStart, pressEnd);
                if (press > 0)
                {
                    double referenceFlight = SkateFlatgroundPop.ReferenceFlightSeconds;
                    double referenceHeight = .5 * SkateFlatgroundPop.Gravity * referenceFlight * referenceFlight * fraction * (1 - fraction);
                    double actualRise = SkateVector.Dot(uprightCarrier.Origin - groundOrigin.Value, uprightCarrier.Up);
                    boardShift = uprightCarrier.Up * ((referenceHeight - actualRise) * press);
                    // Family metadata describes the whole authored press, including
                    // an upper-shoe contact that is not a sole-contact bit.
                    // Keep its relative foot/board placement during compensation.
                    bool familyPress = Clip.Id != RequiredClipId;
                    double front = familyPress ? 1 : PressRelease(fraction, 2.0 / 16, 3.0 / 16);
                    double rear = familyPress ? 1 : PressRelease(fraction, 1.0 / 16, 2.5 / 16);
                    bool goofy = stance == SkateStance.Goofy;
                    var left = boardShift * (goofy ? rear : front);
                    var right = boardShift * (goofy ? front : rear);
                    if (!SkateCaptureFootSupport.TryShiftFeet(rig, pose, left, right, false, out pose, out error)) return false;
                }
            }
            var board = new SkateBoardPose(uprightCarrier.Point(source.Board.Origin) + boardShift,
                uprightCarrier.Rotate(source.Board.Forward), uprightCarrier.Rotate(source.Board.Up));
            if (!SkateBoardPath.Valid(board))
            { error = "The coupled source-keyed board frame is invalid."; return false; }
            var rotation = Multiply(FrameRotation(uprightCarrier), source.Rotation);
            shown = new SkateCaptureFrame(pose, board, rotation,
                uprightCarrier.Rotate(source.LeftSoleUp), uprightCarrier.Rotate(source.RightSoleUp),
                source.HasEvent, source.Observation, uprightCarrier.Rotate(source.PelvisCorrection) + entryShift,
                uprightCarrier.Rotate(source.LeftHandCorrection), uprightCarrier.Rotate(source.RightHandCorrection), uprightCarrier.Up);
            if (entryUpper.HasValue && !actuallyLanded && elapsedSeconds < entryUpperSeconds)
            {
                SkateRiderPose incoming;
                if (!SkateRider.TryTransformPose(entryUpper.Value, SourceCarrier, uprightCarrier, out incoming))
                { error = "The observed upper-body entry frame is invalid."; return false; }
                var shift = pose.Pelvis - incoming.Pelvis;
                var from = new SkateRiderPose(pose.Pelvis, pose.LeftHip, pose.RightHip, pose.LeftKnee, pose.RightKnee,
                    pose.LeftAnkle, pose.RightAnkle, pose.LeftToe, pose.RightToe,
                    incoming.Chest + shift, incoming.Neck + shift, incoming.Head + shift,
                    incoming.LeftShoulder + shift, incoming.RightShoulder + shift, incoming.LeftElbow + shift,
                    incoming.RightElbow + shift, incoming.LeftHand + shift, incoming.RightHand + shift,
                    pose.PelvisForward, pose.PelvisUp, incoming.ChestForward, incoming.ChestUp, incoming.HeadForward);
                double weight = SkateCaptureFootSupport.CatchPreparationWeight(elapsedSeconds, entryUpperSeconds);
                SkateCaptureFrame blended;
                if (!SkateCapturePlayback.TryBlend(rig, from, shown.LeftSoleUp, shown.RightSoleUp,
                    shown, weight, out blended, out error, true)) return false;
                var upper = blended.Pose; var align = pose.Pelvis - upper.Pelvis;
                var joined = new SkateRiderPose(pose.Pelvis, pose.LeftHip, pose.RightHip, pose.LeftKnee, pose.RightKnee,
                    pose.LeftAnkle, pose.RightAnkle, pose.LeftToe, pose.RightToe,
                    upper.Chest + align, upper.Neck + align, upper.Head + align,
                    upper.LeftShoulder + align, upper.RightShoulder + align, upper.LeftElbow + align,
                    upper.RightElbow + align, upper.LeftHand + align, upper.RightHand + align,
                    pose.PelvisForward, pose.PelvisUp, upper.ChestForward, upper.ChestUp, upper.HeadForward);
                shown = new SkateCaptureFrame(joined, shown.Board, shown.Rotation, shown.LeftSoleUp, shown.RightSoleUp,
                    shown.HasEvent, shown.Observation, shown.PelvisCorrection,
                    shown.LeftHandCorrection, shown.RightHandCorrection, shown.RiderUp);
            }
            return true;
        }

        private static bool ValidFlight(double value) { return Finite(value) && value > 0 && value <= 10; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private static double PressRelease(double fraction, double full, double end)
        {
            double u = Math.Max(0, Math.Min(1, (fraction - full) / (end - full)));
            return 1 - u * u * u * (10 + u * (-15 + 6 * u));
        }
        private static SkateCaptureRotation Multiply(SkateCaptureRotation a, SkateCaptureRotation b)
        {
            return new SkateCaptureRotation(a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z).Unit();
        }
        private static SkateCaptureRotation FrameRotation(SkateBoardPose frame)
        {
            var r = frame.Right; var u = frame.Up; var f = frame.Forward; double x, y, z, w, s, trace = r.X + u.Y + f.Z;
            if (trace > 0) { s = Math.Sqrt(trace + 1) * 2; w = .25 * s; x = (u.Z - f.Y) / s; y = (f.X - r.Z) / s; z = (r.Y - u.X) / s; }
            else if (r.X > u.Y && r.X > f.Z) { s = Math.Sqrt(1 + r.X - u.Y - f.Z) * 2; w = (u.Z - f.Y) / s; x = .25 * s; y = (u.X + r.Y) / s; z = (f.X + r.Z) / s; }
            else if (u.Y > f.Z) { s = Math.Sqrt(1 + u.Y - r.X - f.Z) * 2; w = (f.X - r.Z) / s; x = (u.X + r.Y) / s; y = .25 * s; z = (f.Y + u.Z) / s; }
            else { s = Math.Sqrt(1 + f.Z - r.X - u.Y) * 2; w = (r.Y - u.X) / s; x = (f.X + r.Z) / s; y = (f.Y + u.Z) / s; z = .25 * s; }
            return new SkateCaptureRotation(x, y, z, w).Unit();
        }
    }
}
