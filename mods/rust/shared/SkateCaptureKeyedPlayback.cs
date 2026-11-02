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
        public const string HardflipClipId = "hardflip-source-keyed-v1";
        public const string SwitchHardflipClipId = "switch-hardflip-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature SwitchHardflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, false, 360, 180, 0, 0);
        public const string NollieTreFlipClipId = "nollie-360-flip-source-keyed-v1";
        public const string NollieKickflipClipId = "nollie-kickflip-source-keyed-v1";
        public const string SwitchKickflipClipId = "switch-kickflip-source-keyed-v1";
        public const string FakieHeelflipClipId = "fakie-heelflip-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieHeelflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, -360, 0, 0, 0);
        public const string InwardHeelflipClipId = "inward-heelflip-derived-heel-v1";
        private static readonly SkateCaptureTrickSignature InwardHeelflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, -360, -180, 0, 0);
        public const string OllieClipId = "ollie-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature OllieSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, 0, 0, 0);
        public const string Fs180ClipId = "fs-180-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature Fs180Signature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, 0, 180, 0);
        public const string Bs180ClipId = "bs-180-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature Bs180Signature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, 0, -180, 0);
        public const string NollieOllieClipId = "nollie-ollie-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature NollieOllieSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Nollie, false, false, 0, 0, 0, 0);
        public const string FakieOllieClipId = "fakie-ollie-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieOllieSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, 0, 0, 0, 0);
        public const string SwitchOllieClipId = "switch-ollie-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature SwitchOllieSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, false, 0, 0, 0, 0);
        public const string FakieFs180ClipId = "fakie-fs180-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieFs180Signature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, 0, 0, 180, 0);
        public const string FakieBs180ClipId = "fakie-bs180-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieBs180Signature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, 0, 0, -180, 0);
        private static readonly SkateCaptureTrickSignature NollieKickflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Nollie, false, false, 360, 0, 0, 0);
        private static readonly SkateCaptureTrickSignature SwitchKickflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, false, 360, 0, 0, 0);
        public const string BsPopShoveClipId = "bs-pop-shove-it-source-keyed-v1";
        public const string DoubleKickflipClipId = "double-kickflip-source-keyed-v1";
        public static readonly SkateCaptureTrickSignature DoubleKickflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 720, 0, 0, 0);
        public const string VarialKickflipClipId = "varial-kickflip-derived-tre-v1";
        private static readonly SkateCaptureTrickSignature VarialKickflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 360, -180, 0, 0);
        public const string Bs360ShoveClipId = "bs-360-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature Bs360ShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, -360, 0, 0);
        public const string Fs360ShoveClipId = "fs-360-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature Fs360ShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, 360, 0, 0);
        public const string FsPopShoveClipId = "fs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, 180, 0, 0);
        public const string NollieBsPopShoveClipId = "nollie-bs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature NollieBsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Nollie, false, false, 0, -180, 0, 0);
        public const string SwitchBsPopShoveClipId = "switch-bs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature SwitchBsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, false, 0, -180, 0, 0);
        public const string FakieBsPopShoveClipId = "fakie-bs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieBsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, 0, -180, 0, 0);
        public const string NollieFsPopShoveClipId = "nollie-fs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature NollieFsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Nollie, false, false, 0, 180, 0, 0);
        public const string SwitchFsPopShoveClipId = "switch-fs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature SwitchFsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, false, 0, 180, 0, 0);
        public const string FakieFsPopShoveClipId = "fakie-fs-pop-shove-it-source-keyed-v1";
        private static readonly SkateCaptureTrickSignature FakieFsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, true, true, 0, 180, 0, 0);
        private static readonly SkateCaptureTrickSignature BsPopShoveSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 0, -180, 0, 0);
        private static readonly SkateCaptureTrickSignature NollieTreFlipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Nollie, false, false, 360, -360, 0, 0);
        private static readonly SkateCaptureTrickSignature HardflipSignature =
            new SkateCaptureTrickSignature(SkatePopKind.Ollie, false, false, 360, 180, 0, 0);
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
        private bool continuedDouble;
        private double continuationAngle, continuationOffset, lastNativeAngle, doubleReturnStart;
        private static SkateVector[] hardflipSupport;
        public double GroundClearanceCorrection { get; private set; }
        public double MaximumGroundClearanceCorrection { get; private set; }

        private SkateCaptureKeyedPlayback(SkateCaptureClip clip, double pop, double caught, double land)
        { Clip = clip; SourcePop = pop; SourceCatch = caught; SourceLand = land; }

        public static bool Accepts(SkateCaptureClip clip, SkateCaptureTrickSignature signature)
        {
            return clip != null && (clip.Id == RequiredClipId && clip.MovementKind == "kickflip" && RequiredSignature.Same(signature) ||
                clip.Id == HeelflipClipId && clip.MovementKind == "heelflip" && HeelflipSignature.Same(signature) ||
                clip.Id == TreFlipClipId && clip.MovementKind == "360-flip" && TreFlipSignature.Same(signature) ||
                clip.Id == HardflipClipId && clip.MovementKind == "hardflip" && HardflipSignature.Same(signature) ||
                clip.Id == SwitchHardflipClipId && clip.MovementKind == "switch-hardflip" && SwitchHardflipSignature.Same(signature) ||
                clip.Id == NollieTreFlipClipId && clip.MovementKind == "nollie-360-flip" && NollieTreFlipSignature.Same(signature) ||
                clip.Id == NollieKickflipClipId && clip.MovementKind == "nollie-kickflip" && NollieKickflipSignature.Same(signature) ||
                clip.Id == SwitchKickflipClipId && clip.MovementKind == "switch-kickflip" && SwitchKickflipSignature.Same(signature) ||
                clip.Id == FakieHeelflipClipId && clip.MovementKind == "fakie-heelflip" && FakieHeelflipSignature.Same(signature) ||
                clip.Id == InwardHeelflipClipId && clip.MovementKind == "inward-heelflip" && InwardHeelflipSignature.Same(signature) ||
                clip.Id == OllieClipId && clip.MovementKind == "ollie" && OllieSignature.Same(signature) ||
                clip.Id == Fs180ClipId && clip.MovementKind == "fs-180" && Fs180Signature.Same(signature) ||
                clip.Id == Bs180ClipId && clip.MovementKind == "bs-180" && Bs180Signature.Same(signature) ||
                clip.Id == NollieOllieClipId && clip.MovementKind == "nollie-ollie" && NollieOllieSignature.Same(signature) ||
                clip.Id == FakieOllieClipId && clip.MovementKind == "fakie-ollie" && FakieOllieSignature.Same(signature) ||
                clip.Id == SwitchOllieClipId && clip.MovementKind == "switch-ollie" && SwitchOllieSignature.Same(signature) ||
                clip.Id == FakieFs180ClipId && clip.MovementKind == "fakie-fs180" && FakieFs180Signature.Same(signature) ||
                clip.Id == FakieBs180ClipId && clip.MovementKind == "fakie-bs180" && FakieBs180Signature.Same(signature) ||
                clip.Id == BsPopShoveClipId && clip.MovementKind == "bs-pop-shove-it" && BsPopShoveSignature.Same(signature) ||
                clip.Id == FakieFsPopShoveClipId && clip.MovementKind == "fakie-fs-pop-shove-it" && FakieFsPopShoveSignature.Same(signature) ||
                clip.Id == SwitchFsPopShoveClipId && clip.MovementKind == "switch-fs-pop-shove-it" && SwitchFsPopShoveSignature.Same(signature) ||
                clip.Id == NollieFsPopShoveClipId && clip.MovementKind == "nollie-fs-pop-shove-it" && NollieFsPopShoveSignature.Same(signature) ||
                clip.Id == FakieBsPopShoveClipId && clip.MovementKind == "fakie-bs-pop-shove-it" && FakieBsPopShoveSignature.Same(signature) ||
                clip.Id == SwitchBsPopShoveClipId && clip.MovementKind == "switch-bs-pop-shove-it" && SwitchBsPopShoveSignature.Same(signature) ||
                clip.Id == NollieBsPopShoveClipId && clip.MovementKind == "nollie-bs-pop-shove-it" && NollieBsPopShoveSignature.Same(signature) ||
                clip.Id == DoubleKickflipClipId && clip.MovementKind == "double-kickflip" && DoubleKickflipSignature.Same(signature) ||
                clip.Id == VarialKickflipClipId && clip.MovementKind == "varial-kickflip" && VarialKickflipSignature.Same(signature) ||
                clip.Id == Fs360ShoveClipId && clip.MovementKind == "fs-360-shove-it" && Fs360ShoveSignature.Same(signature) ||
                clip.Id == FsPopShoveClipId && clip.MovementKind == "fs-pop-shove-it" && FsPopShoveSignature.Same(signature) ||
                clip.Id == Bs360ShoveClipId && clip.MovementKind == "bs-360-shove-it" && Bs360ShoveSignature.Same(signature));
        }

        public static bool TryCreate(SkateCaptureClip clip, out SkateCaptureKeyedPlayback playback, out string error)
        {
            playback = null; error = null;
            if (clip == null || !clip.Ready || !Accepts(clip, clip.Id == HeelflipClipId ? HeelflipSignature : clip.Id == TreFlipClipId ? TreFlipSignature : clip.Id == HardflipClipId ? HardflipSignature : clip.Id == SwitchHardflipClipId ? SwitchHardflipSignature : clip.Id == NollieTreFlipClipId ? NollieTreFlipSignature : clip.Id == NollieKickflipClipId ? NollieKickflipSignature : clip.Id == SwitchKickflipClipId ? SwitchKickflipSignature : clip.Id == FakieHeelflipClipId ? FakieHeelflipSignature : clip.Id == InwardHeelflipClipId ? InwardHeelflipSignature : clip.Id == OllieClipId ? OllieSignature : clip.Id == Fs180ClipId ? Fs180Signature : clip.Id == Bs180ClipId ? Bs180Signature : clip.Id == NollieOllieClipId ? NollieOllieSignature : clip.Id == FakieOllieClipId ? FakieOllieSignature : clip.Id == SwitchOllieClipId ? SwitchOllieSignature : clip.Id == FakieFs180ClipId ? FakieFs180Signature : clip.Id == FakieBs180ClipId ? FakieBs180Signature : clip.Id == BsPopShoveClipId ? BsPopShoveSignature : clip.Id == FakieFsPopShoveClipId ? FakieFsPopShoveSignature : clip.Id == SwitchFsPopShoveClipId ? SwitchFsPopShoveSignature : clip.Id == NollieFsPopShoveClipId ? NollieFsPopShoveSignature : clip.Id == FakieBsPopShoveClipId ? FakieBsPopShoveSignature : clip.Id == SwitchBsPopShoveClipId ? SwitchBsPopShoveSignature : clip.Id == NollieBsPopShoveClipId ? NollieBsPopShoveSignature : clip.Id == DoubleKickflipClipId ? DoubleKickflipSignature : clip.Id == VarialKickflipClipId ? VarialKickflipSignature : clip.Id == Fs360ShoveClipId ? Fs360ShoveSignature : clip.Id == FsPopShoveClipId ? FsPopShoveSignature : clip.Id == Bs360ShoveClipId ? Bs360ShoveSignature : RequiredSignature))
            { error = "An exact bounded source-keyed flip clip is required."; return false; }
            // Reuse the existing strict event/contact contract. Building its
            // optional inverse clocks does not make those clocks authoritative.
            SkateCapturePlayback events;
            if (!SkateCapturePlayback.TryCreate(clip, out events, out error)) return false;
            playback = new SkateCaptureKeyedPlayback(clip, events.SourceTime(SkateCapturePhase.Pop, 0),
                events.SourceTime(SkateCapturePhase.Catch, 0), events.SourceTime(SkateCapturePhase.Land, 0));
            if (clip.Id != RequiredClipId && clip.Id != DoubleKickflipClipId && !playback.TryFamilyPressWindow(out error))
            { playback = null; return false; }
            if ((clip.Id == HardflipClipId || clip.Id == SwitchHardflipClipId || clip.Id == BsPopShoveClipId || clip.Id == FakieFsPopShoveClipId || clip.Id == SwitchFsPopShoveClipId || clip.Id == NollieFsPopShoveClipId || clip.Id == FakieBsPopShoveClipId || clip.Id == SwitchBsPopShoveClipId || clip.Id == NollieBsPopShoveClipId || clip.Id == VarialKickflipClipId || clip.Id == Fs360ShoveClipId || clip.Id == FsPopShoveClipId || clip.Id == Bs360ShoveClipId || clip.Id == NollieKickflipClipId || clip.Id == SwitchKickflipClipId || clip.Id == FakieHeelflipClipId || clip.Id == InwardHeelflipClipId || clip.Id == OllieClipId || clip.Id == Fs180ClipId || clip.Id == Bs180ClipId || clip.Id == NollieOllieClipId || clip.Id == FakieOllieClipId || clip.Id == SwitchOllieClipId || clip.Id == FakieFs180ClipId || clip.Id == FakieBs180ClipId) && !TryHardflipSupport(out error))
            { playback = null; return false; }
            return true;
        }

        // Change only the approved same-pop destination. The original entry,
        // physical flight and source clock survive; no second BeginEntry runs.
        public bool TryContinue(SkateCaptureClip next, int budgetedTurns, double elapsedSeconds, double flightSeconds, double executedFlip,
            SkateBoardPose lastShownBoard, SkateBoardPose lastCarrier, SkateStance stance,
            out SkateCaptureKeyedPlayback continued, out string error)
        {
            continued = null; error = null;
            if (Clip.Id != RequiredClipId || next == null || next.Id != DoubleKickflipClipId || budgetedTurns != 2 ||
                !entry || !groundOrigin.HasValue || !Finite(elapsedSeconds) || elapsedSeconds < 0 ||
                !ValidFlight(flightSeconds) || elapsedSeconds > flightSeconds * .3125 ||
                !Finite(executedFlip) || executedFlip < 0 || executedFlip >= 720 ||
                !SkateBoardPath.Valid(lastShownBoard) || !SkateBoardPath.Valid(lastCarrier))
            { error = "Double capture requires the same observed single-pop owner and a budgeted held confirmation."; return false; }
            SkateCaptureKeyedPlayback candidate;
            if (!TryCreate(next, out candidate, out error)) return false;
            if (SourcePop != candidate.SourcePop || SourceLand != candidate.SourceLand || Clip.Duration != next.Duration)
            { error = "Double continuation cannot replace the established physical/source clock."; return false; }
            // Retain the original C1 target and its interpolation neighbours.
            double join = SourcePop + (SourceLand - SourcePop) * .3125;
            bool crossed = false; int row = 0;
            for (; row < Clip.SampleCount && !crossed; row++)
            {
                if (row >= next.SampleCount || Clip.Number(row, 0) != next.Number(row, 0))
                { error = "Double continuation has a different entry sample clock."; return false; }
                crossed = Clip.Number(row, 0) > join;
                for (int column = 8; column < 83; column++)
                    if (Clip.Number(row, column) != next.Number(row, column))
                    { error = "Double continuation changes the established entry body or feet."; return false; }
            }
            candidate.doubleReturnStart = 0;
            for (int n = 0; n < next.EventCount; n++)
            {
                var e = next.GetEvent(n);
                if (e.Phase == SkateCapturePhase.Air && e.Contacts == SkateCaptureContacts.None && e.Time < candidate.SourceCatch)
                    candidate.doubleReturnStart = e.Time;
            }
            if (!(candidate.doubleReturnStart > join && candidate.doubleReturnStart < candidate.SourceCatch))
            { error = "Double capture needs its own late free-foot return marker."; return false; }
            var right = Local(lastShownBoard.Right, lastCarrier);
            if (right.X * right.X + right.Y * right.Y < .000001)
            { error = "Double continuation has no bounded board roll basis."; return false; }
            double angle = Math.Atan2(right.Y, right.X) * 180 / Math.PI;
            if (stance == SkateStance.Goofy) angle = -angle;
            angle += Math.Round((executedFlip - angle) / 360) * 360;
            candidate.continuedDouble = true;
            candidate.continuationAngle = candidate.lastNativeAngle = executedFlip;
            candidate.continuationOffset = angle - executedFlip;
            candidate.entry = entry; candidate.entrySeconds = entrySeconds;
            candidate.entryRoot = entryRoot; candidate.entryVelocity = entryVelocity;
            candidate.entryTarget = entryTarget; candidate.entryTargetVelocity = entryTargetVelocity;
            candidate.entryUpper = entryUpper; candidate.entryUpperSeconds = entryUpperSeconds;
            candidate.groundOrigin = groundOrigin; candidate.pressStart = pressStart; candidate.pressEnd = pressEnd;
            continued = candidate; return true;
        }

        private static SkateVector Local(SkateVector v, SkateBoardPose carrier)
        { return new SkateVector(SkateVector.Dot(v, carrier.Right), SkateVector.Dot(v, carrier.Up), SkateVector.Dot(v, carrier.Forward)); }

        private bool TryFamilyPressWindow(out string error)
        {
            error = null; bool released = false;
            // These explicit nollie clips pop with their canonical nose/Left foot.
            var poppingFoot = Clip.Id == NollieTreFlipClipId || Clip.Id == NollieKickflipClipId || Clip.Id == NollieOllieClipId || Clip.Id == NollieBsPopShoveClipId || Clip.Id == NollieFsPopShoveClipId
                ? SkateCaptureContacts.LeftFoot : SkateCaptureContacts.RightFoot;
            for (int n = 0; n < Clip.EventCount; n++)
            {
                var e = Clip.GetEvent(n);
                if (e.Time <= SourcePop) continue;
                if (!released && (e.Contacts & poppingFoot) == 0)
                { pressStart = (e.Time - SourcePop) / (SourceLand - SourcePop); released = true; }
                else if (released && e.Phase == SkateCapturePhase.Air && e.Contacts == SkateCaptureContacts.None)
                {
                    pressEnd = (e.Time - SourcePop) / (SourceLand - SourcePop);
                    if (pressStart >= 0 && pressEnd > pressStart && pressEnd < SourceCatchFraction) return true;
                    break;
                }
            }
            error = "Source-keyed family needs an explicit popping-sole release and later free-Air press endpoint.";
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
            GroundClearanceCorrection = MaximumGroundClearanceCorrection = 0;
            if (!ValidFlight(frozenFlightSeconds) || !Finite(localRoot.LengthSquared) || !Finite(localVelocity.LengthSquared) ||
                groundCarrierOrigin.HasValue && !Finite(groundCarrierOrigin.Value.LengthSquared))
            { error = "Invalid source-keyed entry baseline."; return false; }
            if (Clip.Id == Fs360ShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "FS360 Shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "FS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchHardflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Switch Hardflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == VarialKickflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Varial Kickflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == HardflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Hardflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieTreFlipClipId && !groundCarrierOrigin.HasValue)
            { error = "Nollie360 needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieKickflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Nollie Kickflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchKickflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Switch Kickflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieHeelflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie Heelflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == InwardHeelflipClipId && !groundCarrierOrigin.HasValue)
            { error = "Inward Heelflip needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == OllieClipId && !groundCarrierOrigin.HasValue)
            { error = "Ollie needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == Fs180ClipId && !groundCarrierOrigin.HasValue)
            { error = "FS180 needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == Bs180ClipId && !groundCarrierOrigin.HasValue)
            { error = "BS180 needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieOllieClipId && !groundCarrierOrigin.HasValue)
            { error = "Nollie needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieOllieClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie Ollie needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchOllieClipId && !groundCarrierOrigin.HasValue)
            { error = "Switch Ollie needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieFs180ClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie FS180 needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieBs180ClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie BS180 needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == BsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "BS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieFsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie FS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchFsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Switch FS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieFsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Nollie FS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieBsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Fakie BS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchBsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Switch BS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieBsPopShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "Nollie BS pop shove-it needs an observed flatground takeoff plane."; return false; }
            if (Clip.Id == Bs360ShoveClipId && !groundCarrierOrigin.HasValue)
            { error = "BS360 shove-it needs an observed flatground takeoff plane."; return false; }
            // Family joins: compact Air for Kick264/Heel354/Tre408; Hard142
            // keeps the still-supported rear leg low before its later Air148.
            // Nollie4598 joins after its nose foot is unambiguously free.
            // BS-shove137 / BS360-source93 join after their own rear scoops release.
            // The independent upper entry and exact foot/board paths stay short.
            double rootFraction = Clip.Id == RequiredClipId ? .3125 :
                Clip.Id == HeelflipClipId && groundCarrierOrigin.HasValue ? 1.0 / 3.0 :
                (Clip.Id == TreFlipClipId || Clip.Id == VarialKickflipClipId) && groundCarrierOrigin.HasValue ? 5.0 / 21.0 :
                Clip.Id == HardflipClipId && groundCarrierOrigin.HasValue ? 2.0 / 11.0 :
                Clip.Id == SwitchHardflipClipId && groundCarrierOrigin.HasValue ? .125 :
                Clip.Id == NollieTreFlipClipId && groundCarrierOrigin.HasValue ? 6.0 / 17.0 :
                (Clip.Id == NollieKickflipClipId || Clip.Id == SwitchKickflipClipId) && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FakieHeelflipClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == InwardHeelflipClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == OllieClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == Fs180ClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == Bs180ClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == NollieOllieClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FakieOllieClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == SwitchOllieClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FakieFs180ClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FakieBs180ClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == BsPopShoveClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FakieFsPopShoveClipId && groundCarrierOrigin.HasValue ? .125 :
                Clip.Id == SwitchFsPopShoveClipId && groundCarrierOrigin.HasValue ? .125 :
                Clip.Id == NollieFsPopShoveClipId && groundCarrierOrigin.HasValue ? .125 :
                Clip.Id == FakieBsPopShoveClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == SwitchBsPopShoveClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == NollieBsPopShoveClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == FsPopShoveClipId && groundCarrierOrigin.HasValue ? pressEnd :
                Clip.Id == Fs360ShoveClipId && groundCarrierOrigin.HasValue ? 5.0 / 15.0 :
                Clip.Id == Bs360ShoveClipId && groundCarrierOrigin.HasValue ? 8.0 / 18.0 : .125;
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
            if ((Clip.Id == RequiredClipId || Clip.Id == HeelflipClipId || Clip.Id == TreFlipClipId || Clip.Id == HardflipClipId || Clip.Id == SwitchHardflipClipId || Clip.Id == NollieTreFlipClipId || Clip.Id == NollieKickflipClipId || Clip.Id == SwitchKickflipClipId || Clip.Id == FakieHeelflipClipId || Clip.Id == InwardHeelflipClipId || Clip.Id == OllieClipId || Clip.Id == Fs180ClipId || Clip.Id == Bs180ClipId || Clip.Id == NollieOllieClipId || Clip.Id == FakieOllieClipId || Clip.Id == SwitchOllieClipId || Clip.Id == FakieFs180ClipId || Clip.Id == FakieBs180ClipId || Clip.Id == BsPopShoveClipId || Clip.Id == FakieFsPopShoveClipId || Clip.Id == SwitchFsPopShoveClipId || Clip.Id == NollieFsPopShoveClipId || Clip.Id == FakieBsPopShoveClipId || Clip.Id == SwitchBsPopShoveClipId || Clip.Id == NollieBsPopShoveClipId || Clip.Id == VarialKickflipClipId || Clip.Id == Fs360ShoveClipId || Clip.Id == FsPopShoveClipId || Clip.Id == Bs360ShoveClipId) && groundCarrierOrigin.HasValue)
                entryVelocity -= new SkateVector(0, SkateFlatgroundPop.Gravity * frozenFlightSeconds * .5, 0);
            entryUpper = localBody;
            entryUpperSeconds = frozenFlightSeconds * (Clip.Id == FsPopShoveClipId ? .0625 : .125);
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

        private static bool TryHardflipSupport(out string error)
        {
            error = null;
            if (hardflipSupport == null)
            {
                var mesh = SkateBoardMesh.CreateDetailed();
                if (mesh.Parts.Length != 13) { error = "Hardflip needs the complete original board hull."; return false; }
                var points = new SkateVector[mesh.Positions.Length / 3];
                for (int n = 0; n < points.Length; n++) points[n] = new SkateVector(
                    mesh.Positions[n * 3], mesh.Positions[n * 3 + 1] - .092, mesh.Positions[n * 3 + 2]);
                hardflipSupport = points;
            }
            return true;
        }

        private bool TryHardflipFloor(SkateCaptureFrame frame, bool actuallyLanded, SkateVector up,
            double fraction, out SkateCaptureFrame result, out string error)
        {
            result = frame; error = null; GroundClearanceCorrection = 0;
            // Only this exact flatground family, with its observed takeoff plane.
            // The caller already requires the frozen flatground flight span.
            bool hardflip = Clip.Id == HardflipClipId || Clip.Id == SwitchHardflipClipId;
            bool varial = Clip.Id == VarialKickflipClipId;
            bool nollieKickflip = Clip.Id == NollieKickflipClipId, switchKickflip = Clip.Id == SwitchKickflipClipId;
            if ((!hardflip && !varial && !nollieKickflip && !switchKickflip && Clip.Id != FakieHeelflipClipId && Clip.Id != InwardHeelflipClipId && Clip.Id != OllieClipId && Clip.Id != Fs180ClipId && Clip.Id != Bs180ClipId && Clip.Id != NollieOllieClipId && Clip.Id != FakieOllieClipId && Clip.Id != SwitchOllieClipId && Clip.Id != FakieFs180ClipId && Clip.Id != FakieBs180ClipId && Clip.Id != BsPopShoveClipId && Clip.Id != FakieFsPopShoveClipId && Clip.Id != SwitchFsPopShoveClipId && Clip.Id != NollieFsPopShoveClipId && Clip.Id != FakieBsPopShoveClipId && Clip.Id != SwitchBsPopShoveClipId && Clip.Id != NollieBsPopShoveClipId && Clip.Id != Fs360ShoveClipId && Clip.Id != FsPopShoveClipId && Clip.Id != Bs360ShoveClipId) || actuallyLanded) return true;
            string family = Clip.Id == SwitchHardflipClipId ? "Switch Hardflip" : Clip.Id == FakieFsPopShoveClipId ? "Fakie FS pop shove-it" : Clip.Id == SwitchFsPopShoveClipId ? "Switch FS pop shove-it" : Clip.Id == NollieFsPopShoveClipId ? "Nollie FS pop shove-it" : Clip.Id == FakieBsPopShoveClipId ? "Fakie BS pop shove-it" : Clip.Id == SwitchBsPopShoveClipId ? "Switch BS pop shove-it" : Clip.Id == NollieBsPopShoveClipId ? "Nollie BS pop shove-it" : hardflip ? "Hardflip" : varial ? "Varial Kickflip" : nollieKickflip ? "Nollie Kickflip" : switchKickflip ? "Switch Kickflip" : Clip.Id == FakieHeelflipClipId ? "Fakie Heelflip" : Clip.Id == InwardHeelflipClipId ? "Inward Heelflip" : Clip.Id == OllieClipId ? "Ollie" : Clip.Id == Fs180ClipId ? "FS180" : Clip.Id == Bs180ClipId ? "BS180" : Clip.Id == NollieOllieClipId ? "Nollie" : Clip.Id == FakieOllieClipId ? "Fakie Ollie" : Clip.Id == SwitchOllieClipId ? "Switch Ollie" : Clip.Id == FakieFs180ClipId ? "Fakie FS180" : Clip.Id == FakieBs180ClipId ? "Fakie BS180" : Clip.Id == Fs360ShoveClipId ? "FS360 Shove-it" : Clip.Id == Bs360ShoveClipId ? "BS360 shove-it" : Clip.Id == FsPopShoveClipId ? "FS pop shove-it" : "BS pop shove-it";
            if (!groundOrigin.HasValue)
            { error = family + " has no observed flatground takeoff plane."; return false; }
            if (!TryHardflipSupport(out error)) return false;
            double lowest = double.PositiveInfinity;
            var localUp = new SkateVector(SkateVector.Dot(frame.Board.Right, up),
                SkateVector.Dot(frame.Board.Up, up), SkateVector.Dot(frame.Board.Forward, up));
            double originHeight = SkateVector.Dot(frame.Board.Origin - groundOrigin.Value, up);
            foreach (var point in hardflipSupport)
                lowest = Math.Min(lowest, originHeight + SkateVector.Dot(point, localUp));
            double margin = .003 * (1 - PressRelease(fraction, pressStart, pressEnd));
            double amount = Math.Max(0, margin - lowest);
            if (!Finite(amount)) { error = "Invalid " + family + " observed-floor envelope."; return false; }
            GroundClearanceCorrection = amount;
            MaximumGroundClearanceCorrection = Math.Max(MaximumGroundClearanceCorrection, amount);
            if (amount > .12) { error = family + " presentation exceeds the bounded observed-floor correction."; return false; }
            if (amount == 0) return true;
            if ((frame.Observation.Contacts & (SkateCaptureContacts.LeftHand | SkateCaptureContacts.RightHand)) != 0)
            { error = family + " floor translation cannot move a held hand."; return false; }
            var lift = up * amount;
            SkateRiderPose pose;
            if (!SkateRider.TryTransformPose(frame.Pose, SourceCarrier,
                new SkateBoardPose(lift, SourceCarrier.Forward, SourceCarrier.Up), out pose))
            { error = "Invalid coupled " + family + " floor translation."; return false; }
            result = new SkateCaptureFrame(pose,
                new SkateBoardPose(frame.Board.Origin + lift, frame.Board.Forward, frame.Board.Up), frame.Rotation,
                frame.LeftSoleUp, frame.RightSoleUp, frame.HasEvent, frame.Observation,
                frame.PelvisCorrection + lift, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            return true;
        }

        public bool TrySample(SkateRiderRig rig, SkateStance stance, double elapsedSeconds,
            double frozenFlightSeconds, bool actuallyLanded, double secondsSinceLand, SkateBoardPose uprightCarrier,
            out SkateCaptureFrame shown, out string error, double? executedFlip = null)
        {
            shown = default(SkateCaptureFrame); double seconds;
            if (!TrySourceTime(elapsedSeconds, frozenFlightSeconds, actuallyLanded, secondsSinceLand, out seconds, out error)) return false;
            if (!SkateBoardPath.Valid(uprightCarrier) || uprightCarrier.Up.Y < .98)
            { error = "Source-keyed playback requires the current upright physical carrier."; return false; }
            if (Clip.Id == Fs360ShoveClipId && !groundOrigin.HasValue)
            { error = "FS360 Shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FsPopShoveClipId && !groundOrigin.HasValue)
            { error = "FS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchHardflipClipId && !groundOrigin.HasValue)
            { error = "Switch Hardflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == VarialKickflipClipId && !groundOrigin.HasValue)
            { error = "Varial Kickflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieTreFlipClipId && !groundOrigin.HasValue)
            { error = "Nollie360 has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieKickflipClipId && !groundOrigin.HasValue)
            { error = "Nollie Kickflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchKickflipClipId && !groundOrigin.HasValue)
            { error = "Switch Kickflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieHeelflipClipId && !groundOrigin.HasValue)
            { error = "Fakie Heelflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == InwardHeelflipClipId && !groundOrigin.HasValue)
            { error = "Inward Heelflip has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == OllieClipId && !groundOrigin.HasValue)
            { error = "Ollie has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == Fs180ClipId && !groundOrigin.HasValue)
            { error = "FS180 has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == Bs180ClipId && !groundOrigin.HasValue)
            { error = "BS180 has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieOllieClipId && !groundOrigin.HasValue)
            { error = "Nollie has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieOllieClipId && !groundOrigin.HasValue)
            { error = "Fakie Ollie has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchOllieClipId && !groundOrigin.HasValue)
            { error = "Switch Ollie has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieFs180ClipId && !groundOrigin.HasValue)
            { error = "Fakie FS180 has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieBs180ClipId && !groundOrigin.HasValue)
            { error = "Fakie BS180 has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == BsPopShoveClipId && !groundOrigin.HasValue)
            { error = "BS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieFsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Fakie FS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchFsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Switch FS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieFsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Nollie FS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == FakieBsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Fakie BS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == SwitchBsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Switch BS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == NollieBsPopShoveClipId && !groundOrigin.HasValue)
            { error = "Nollie BS pop shove-it has no observed flatground takeoff plane."; return false; }
            if (Clip.Id == Bs360ShoveClipId && !groundOrigin.HasValue)
            { error = "BS360 shove-it has no observed flatground takeoff plane."; return false; }
            SkateCaptureFrame source; SkateRiderPose pose;
            if (!SkateCaptureSampler.TrySample(Clip, seconds, rig, stance, out source, out error)) return false;
            if (Clip.Id == DoubleKickflipClipId &&
                !TryDoubleFrame(rig, stance, seconds, actuallyLanded, executedFlip, source, out source, out error)) return false;
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
                    bool familyPress = Clip.Id != RequiredClipId && Clip.Id != DoubleKickflipClipId;
                    double front = familyPress ? 1 : PressRelease(fraction, 2.0 / 16, 3.0 / 16);
                    double rear = familyPress ? 1 : PressRelease(fraction, 1.0 / 16, 2.5 / 16);
                    bool goofy = stance == SkateStance.Goofy;
                    var left = boardShift * (goofy ? rear : front);
                    var right = boardShift * (goofy ? front : rear);
                    var beforePress = pose;
                    // These exact contacts keep grounded feet through their short root entries.
                    // Fit the pelvis to those measured targets continuously during the press,
                    // using the same closest reachable fit as shaped-deck support.
                    bool fitPressPelvis = Clip.Id == FakieFsPopShoveClipId || Clip.Id == SwitchHardflipClipId;
                    if (!SkateCaptureFootSupport.TryShiftFeet(rig, pose, left, right, fitPressPelvis, out pose, out error))
                    {
                        // A shallow observed charge can reach the source feet but
                        // not their final downward press compensation. Keep every
                        // successful solve unchanged; only during this single-entry
                        // window use the same measured pelvis fit as deck conforming.
                        if (Clip.Id != RequiredClipId || !entry || elapsedSeconds >= entrySeconds ||
                            !SkateCaptureFootSupport.TryShiftFeet(rig, beforePress, left, right, true, out pose, out error))
                            return false;
                        entryShift += pose.Pelvis - beforePress.Pelvis;
                    }
                    if (fitPressPelvis) entryShift += pose.Pelvis - beforePress.Pelvis;
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
            return TryHardflipFloor(shown, actuallyLanded, uprightCarrier.Up,
                (seconds - SourcePop) / (SourceLand - SourcePop), out shown, out error);
        }

        private bool TryDoubleFrame(SkateRiderRig rig, SkateStance stance, double seconds, bool landed,
            double? executed, SkateCaptureFrame source, out SkateCaptureFrame frame, out string error)
        {
            frame = source; error = null;
            if (!continuedDouble || !executed.HasValue || !Finite(executed.Value) || executed.Value < lastNativeAngle - .001 ||
                executed.Value > 720 + SkateTricks.FlipTolerance || landed && executed.Value < 720 - SkateTricks.FlipTolerance)
            { error = "Double capture has no matching monotone executed winding."; return false; }
            double native = executed.Value; lastNativeAngle = Math.Max(lastNativeAngle, native);
            var body = source.Pose; var leftUp = source.LeftSoleUp; var rightUp = source.RightSoleUp;
            var observation = source.Observation;
            if (!landed && seconds >= doubleReturnStart && (seconds < SourceCatch || native < 719.999))
            {
                SkateCaptureFrame free, caught;
                if (!SkateCaptureSampler.TrySample(Clip, doubleReturnStart, rig, stance, out free, out error) ||
                    !SkateCaptureSampler.TrySample(Clip, Math.Max(SourceCatch, seconds), rig, stance, out caught, out error)) return false;
                // The body clock continues. Only the authored free-foot return
                // waits for the actual last sixth-turn; it cannot catch at360.
                double weight = SkateCaptureFootSupport.CatchPreparationWeight(native - 660, 60);
                var freeShift = source.Board.Origin - free.Board.Origin;
                var catchShift = source.Board.Origin - caught.Board.Origin;
                var la = (free.Pose.LeftAnkle + freeShift) * (1 - weight) + (caught.Pose.LeftAnkle + catchShift) * weight;
                var ra = (free.Pose.RightAnkle + freeShift) * (1 - weight) + (caught.Pose.RightAnkle + catchShift) * weight;
                var lf = (free.Pose.LeftToe - free.Pose.LeftAnkle) * (1 - weight) + (caught.Pose.LeftToe - caught.Pose.LeftAnkle) * weight;
                var rf = (free.Pose.RightToe - free.Pose.RightAnkle) * (1 - weight) + (caught.Pose.RightToe - caught.Pose.RightAnkle) * weight;
                if (lf.LengthSquared < .000001 || rf.LengthSquared < .000001 ||
                    !SkateCaptureFootSupport.TrySetFeet(rig, body, la, la + lf * (rig.FootLength / lf.Length),
                        ra, ra + rf * (rig.FootLength / rf.Length), false, out body, out error)) return false;
                leftUp = free.LeftSoleUp * (1 - weight) + caught.LeftSoleUp * weight;
                rightUp = free.RightSoleUp * (1 - weight) + caught.RightSoleUp * weight;
                leftUp = leftUp * (1 / leftUp.Length); rightUp = rightUp * (1 / rightUp.Length);
            }
            if (!landed && native < 719.999)
                observation = new SkateCaptureEvent(seconds, SkateCapturePhase.Air, SkateCaptureContacts.None);
            // Anchor the previous displayed angle and consume only the real
            // additional winding. The offset vanishes C2 at native completion;
            // neither the body clock nor a quaternion loop creates another turn.
            double u = Math.Max(0, Math.Min(1, (native - continuationAngle) / (720 - continuationAngle)));
            double release = u * u * u * (10 + u * (-15 + 6 * u));
            double angle = landed ? 720 : native + continuationOffset * (1 - release);
            if (stance == SkateStance.Goofy) angle = -angle;
            // The shared baker's original order is Z(roll)*Y(yaw)*X(pitch).
            // A rolled pitched nose has nonzero Forward.X even with zero shove.
            var right = source.Board.Right;
            if (right.X * right.X + right.Y * right.Y < .000001)
            { error = "Double capture has no finite source roll basis."; return false; }
            double sourceAngle = Math.Atan2(right.Y, right.X) * 180 / Math.PI;
            double half = (angle - sourceAngle) * Math.PI / 360;
            var rotation = Multiply(new SkateCaptureRotation(0, 0, Math.Sin(half), Math.Cos(half)), source.Rotation);
            var board = new SkateBoardPose(source.Board.Origin, rotation.Rotate(new SkateVector(0, 0, 1)), rotation.Rotate(new SkateVector(0, 1, 0)));
            frame = new SkateCaptureFrame(body, board, rotation, leftUp, rightUp,
                source.HasEvent, observation, source.PelvisCorrection, source.LeftHandCorrection, source.RightHandCorrection, source.RiderUp);
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
