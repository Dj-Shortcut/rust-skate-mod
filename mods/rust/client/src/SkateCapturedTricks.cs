using System;
using System.IO;
using System.Security.Cryptography;
using Shortcut.RustMod;
using UnityEngine;

// Normal riding consumes only owner-approved, reviewed captures. No runtime menu can approve data.
public static class SkateCapturedTricks
{
    private const double GroundRecoverySpeed = 1.35;
    public const string HardflipEarlyTuckProvenance = "Authored early free-front-foot tuck for normal Hardflip; reference-scaled lift up to 0.28 m, not measured capture.";
    private static readonly SkateCapturePlayback[] clips = new SkateCapturePlayback[SkateCaptureVariants.Count];
    private static readonly SkateCaptureFootSupport[] supports = new SkateCaptureFootSupport[SkateCaptureVariants.Count * 2];
    private static readonly int[] supportVersions = new int[SkateCaptureVariants.Count * 2];
    private static readonly string[] supportErrors = new string[SkateCaptureVariants.Count * 2];
    private static bool loaded, airSeen, landed;
    private static double beganAt, landedAt, caughtAt = -1, previousAt, previousAirSourceTime, catchSourceLead;
    private static double preparedAt = -1, catchPreparationWeight;
    private static double catchKneeDegrees = SkateCaptureFootSupport.CatchPriorInternalDegrees;
    private static double landingKneeDegrees = SkateCaptureFootSupport.LandingPriorInternalDegrees;
    private static bool supportedKneePlanes;
    private static double previousAirProgress;
    private static bool flipExtended;
    private static SkateCaptureFrame previous, landingFrom;
    private static SkateBoardPose previousCarrier, landingCarrier;
    private static int token = -1, refusedToken = -1, lands;
    private static SkateCapturePlayback active;
    private static SkateCaptureFootSupport footSupport;
    private static int footVersion;
    private static SkateCaptureTrickSignature signature;
    private static SkateStance activeStance;
    private static SkateBoardPose displayed;
    private static string lastRefusal;
    private static SkateBoardPose plantedLeft, plantedRight;
    private static int plantedVersion, plantedPops;
    private static SkateStance plantedStance;
    private static SkateRiderRig plantedRig;
    private static double departedAt = -1;
    private static SkateCaptureFrame departedFrom;
    private static SkateCaptureFrame groundedImpact;
    private static SkateBoardPose departedCarrier;
    public static bool HasFrame { get { return displayed.Initialized; } }
    public static SkateBoardPose DisplayFrame { get { return displayed; } }
    public static string Status { get; private set; } = "Captured riding not loaded";
    public static void ClearFrame() { displayed = default(SkateBoardPose); }
    public static void Reset()
    { active = null; footSupport = null; token = refusedToken = -1; airSeen = landed = flipExtended = false; caughtAt = preparedAt = -1; catchPreparationWeight = 0; previousAirSourceTime = catchSourceLead = previousAirProgress = 0; previous = landingFrom = default(SkateCaptureFrame); ClearPlantedFeet(); ClearFrame(); }
    public static void Preload()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            string folder = BepInEx.Paths.PluginPath;
            string packPath = Path.Combine(folder, SkateCapturePack.FileName), approvalPath = Path.Combine(folder, SkateCaptureApproval.FileName);
            if (!File.Exists(packPath) || !File.Exists(approvalPath))
            { Status = "Captured riding disabled: reviewed pack and owner approval are required"; return; }
            SkateCaptureApproval approval; string error;
            using (var stream = File.OpenRead(approvalPath))
                if (!SkateCaptureApproval.TryRead(stream, out approval, out error)) { Status = error; return; }
            byte[] bytes;
            using (var stream = new FileStream(packPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 12 || stream.Length > SkateCapturePack.MaximumBytes) { Status = "Captured riding disabled: invalid pack size"; return; }
                bytes = new byte[(int)stream.Length]; int total = 0, got;
                while (total < bytes.Length && (got = stream.Read(bytes, total, bytes.Length - total)) > 0) total += got;
                if (total != bytes.Length || stream.ReadByte() != -1) { Status = "Captured riding disabled: pack changed while loading"; return; }
            }
            string digest;
            using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (digest != approval.Sha256) { Status = "Captured riding disabled: approval digest differs from pack"; return; }
            SkateCaptureLibrary library;
            using (var stream = new MemoryStream(bytes))
                if (!SkateCapturePack.TryRead(stream, out library, out error)) { Status = error; return; }
            var pending = new SkateCapturePlayback[SkateCaptureVariants.Count]; int accepted = 0, unsupported = 0, rotationClocks = 0, flightClocks = 0;
            for (int n = 0; n < approval.Count; n++)
            {
                SkateCaptureClip clip = null;
                for (int c = 0; c < library.Count; c++) if (library.GetClip(c).Id == approval.GetId(n)) clip = library.GetClip(c);
                if (clip == null || !clip.Reviewed) { Status = "Captured riding disabled: approved clip missing or unreviewed"; return; }
                int kind = SkateCaptureVariants.IndexOf(clip.MovementKind);
                if (kind < 0) { unsupported++; continue; }
                if (SkateCaptureVariants.Get(kind).Derived && clip.Id != clip.MovementKind)
                { Status = "Captured riding disabled: derived trick needs its own exact approved id"; return; }
                if (pending[kind] != null) { Status = "Captured riding disabled: ambiguous approved trick variant"; return; }
                SkateCapturePlayback playback;
                if (!SkateCapturePlayback.TryCreate(clip, out playback, out error)) { Status = error; return; }
                if (playback.UsesSourceRotationClock) rotationClocks++;
                if (playback.UsesSourceFlightClock) flightClocks++;
                pending[kind] = playback; accepted++;
            }
            Array.Copy(pending, clips, clips.Length);
            Status = "Captured riding ready: " + accepted + " approved variants; source rotation clocks=" + rotationClocks +
                "; source pop-to-catch clocks=" + flightClocks +
                (unsupported > 0 ? "; " + unsupported + " unsupported approvals ignored" : "") + "; restart client after replacing pack";
        }
        catch (Exception e) { Status = "Captured riding disabled: " + e.GetType().Name; }
        finally { Out.Say("CAPTURE " + Status); }
    }
    private static void Refuse(int pop, string reason, bool keepPlantedFeet = false)
    {
        active = null; footSupport = null; refusedToken = pop; if (!keepPlantedFeet) ClearPlantedFeet(); ClearFrame();
        if (reason != lastRefusal) { lastRefusal = reason; Out.Say("CAPTURE fallback: " + reason); }
    }

    private static void ClearPlantedFeet()
    { plantedLeft = plantedRight = default(SkateBoardPose); departedAt = -1; departedFrom = groundedImpact = default(SkateCaptureFrame); }

    private static SkateVector Local(SkateVector v, SkateBoardPose board)
    { return new SkateVector(SkateVector.Dot(v, board.Right), SkateVector.Dot(v, board.Up), SkateVector.Dot(v, board.Forward)); }

    private static bool RememberPlantedFeet(SkateRiderRig rig, SkateCaptureFrame frame)
    {
        SkateBoardPose left, right;
        if (!SkateCaptureFootSupport.TryFootFrame(frame.Pose.LeftAnkle, frame.Pose.LeftToe, frame.LeftSoleUp, out left) ||
            !SkateCaptureFootSupport.TryFootFrame(frame.Pose.RightAnkle, frame.Pose.RightToe, frame.RightSoleUp, out right)) return false;
        plantedLeft = new SkateBoardPose(Local(left.Origin - frame.Board.Origin, frame.Board), Local(left.Forward, frame.Board), Local(left.Up, frame.Board));
        plantedRight = new SkateBoardPose(Local(right.Origin - frame.Board.Origin, frame.Board), Local(right.Forward, frame.Board), Local(right.Up, frame.Board));
        plantedVersion = SkateFootContacts.Version; plantedPops = SkateRide.Pops;
        plantedRig = rig;
        plantedStance = SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular;
        return true;
    }

    private static bool PinPlantedFeet(SkateRiderRig rig, SkateRiderPose body, SkateBoardPose board, SkateVector baselineLeftUp, SkateVector baselineRightUp,
        out SkateRiderPose pose, out SkateVector leftUp, out SkateVector rightUp, out string error)
    {
        pose = body; leftUp = baselineLeftUp; rightUp = baselineRightUp; error = null;
        if ((!plantedLeft.Initialized && !plantedRight.Initialized) || !board.Initialized) return false;
        var la = body.LeftAnkle; var lt = body.LeftToe; var ra = body.RightAnkle; var rt = body.RightToe;
        if (plantedLeft.Initialized)
        { la = board.Origin + board.Rotate(plantedLeft.Origin); lt = la + board.Rotate(plantedLeft.Forward) * rig.FootLength; leftUp = board.Rotate(plantedLeft.Up); }
        if (plantedRight.Initialized)
        { ra = board.Origin + board.Rotate(plantedRight.Origin); rt = ra + board.Rotate(plantedRight.Forward) * rig.FootLength; rightUp = board.Rotate(plantedRight.Up); }
        return SkateCaptureFootSupport.TrySetFeet(rig, body, la, lt, ra, rt, true, out pose, out error);
    }

    // Preserve the landed placement through the return to ordinary rolling.
    // The collector still sees an ordinary ground pose; this does not keep a
    // capture active, approve a pack, or retain ownership over another action.
    public static bool TryGroundedFooting(SkateRiderRig rig, Transform player, SkateRiderPose baseline, SkateVector baselineLeftUp, SkateVector baselineRightUp,
        out SkateRiderPose pose, out SkateVector leftUp, out SkateVector rightUp, out SkateCaptureContacts pinned)
    {
        pose = baseline; leftUp = baselineLeftUp; rightUp = baselineRightUp; pinned = SkateCaptureContacts.None;
        if (!plantedLeft.Initialized && !plantedRight.Initialized) return false;
        if (player == null || !Eligible(false, true) || SkateRide.Mode != RideMode.Ground || SkateRide.PresentationQueued ||
            SkateRide.Pops != plantedPops || !SkateFootContacts.Ready || SkateFootContacts.Version != plantedVersion ||
            rig.FootLength != plantedRig.FootLength || rig.ThighLength != plantedRig.ThighLength ||
            rig.ShinLength != plantedRig.ShinLength || rig.HipWidth != plantedRig.HipWidth ||
            plantedStance != (SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular))
        { ClearPlantedFeet(); return false; }
        if (SkateRide.PushPhase > .001f)
        {
            bool trailingLeft = SkateRide.Goofy != SkateRide.TailFirst;
            bool pushLeft = SkateRide.PushLead ? !trailingLeft : trailingLeft;
            if (pushLeft) plantedLeft = default(SkateBoardPose); else plantedRight = default(SkateBoardPose);
        }
        // An interrupted recovery must not jump the pinned pelvis directly to
        // the procedural target. Only the body departs; a released push foot
        // already belongs to the current procedural push trajectory.
        if (active != null && !Eligible() && departedAt < 0 && previous.Board.Initialized)
        { departedAt = Time.realtimeSinceStartup; departedFrom = previous; departedCarrier = previousCarrier; }
        string error;
        var board = SkateBoard.PoseFor(player);
        if (!PinPlantedFeet(rig, baseline, board, baselineLeftUp, baselineRightUp, out pose, out leftUp, out rightUp, out error))
        { ClearPlantedFeet(); pose = baseline; return false; }
        // The departure target already includes any remaining absorption.
        double sinceLand = Time.realtimeSinceStartup - landedAt;
        double sourceRecovery = sinceLand * GroundRecoverySpeed;
        if (landed && SkateRide.PushPhase <= .001f &&
            (departedAt >= 0 || active == null || active.Finished(sourceRecovery) || active.RecoveryWeight(sourceRecovery) <= 0))
        {
            SkateRiderPose absorbed; SkateVector shift;
            if (!SkateCaptureFootSupport.TryLandingAbsorption(rig, pose, V(SkateRide.ViewNormal), sinceLand,
                out absorbed, out shift, out error, catchPreparationWeight, catchKneeDegrees, landingKneeDegrees, supportedKneePlanes))
            { ClearPlantedFeet(); pose = baseline; return false; }
            pose = absorbed;
        }
        if (departedAt >= 0)
        {
            double u = Math.Max(0, Math.Min(1, (Time.realtimeSinceStartup - departedAt) / .15));
            if (u >= 1) departedAt = -1;
            else
            {
                var carrier = new SkateBoardPose(board.Origin, V(SkateBoard.OnFace(SkateRide.ViewNormal, SkateRide.Dir(SkateRide.NoseYaw))), V(SkateRide.ViewNormal));
                var from = SkateCapturePlayback.Transport(departedFrom, departedCarrier, carrier);
                SkateRiderPose body;
                if (!from.Board.Initialized || !SkateCaptureFootSupport.TrySetFeet(rig, from.Pose, pose.LeftAnkle, pose.LeftToe,
                    pose.RightAnkle, pose.RightToe, true, out body, out error))
                { ClearPlantedFeet(); pose = baseline; return false; }
                var target = new SkateCaptureFrame(body, board, from.Rotation, leftUp, rightUp, from.HasEvent, from.Observation,
                    from.PelvisCorrection, from.LeftHandCorrection, from.RightHandCorrection, from.RiderUp);
                double weight = 1 - u * u * u * (10 + u * (-15 + 6 * u)); SkateCaptureFrame blended;
                weight = Math.Max(0, Math.Min(1, weight));
                if (!SkateCapturePlayback.TryBlend(rig, pose, leftUp, rightUp, target, weight, out blended, out error, true))
                { ClearPlantedFeet(); pose = baseline; return false; }
                pose = blended.Pose;
            }
        }
        pinned = (plantedLeft.Initialized ? SkateCaptureContacts.LeftFoot : SkateCaptureContacts.None) |
            (plantedRight.Initialized ? SkateCaptureContacts.RightFoot : SkateCaptureContacts.None);
        return true;
    }
    private static bool Eligible(bool requireControls = true, bool allowPush = false)
    {
        // These source clips are flat-ground footage. Transition, wall and contact-action motion owns its own presentation.
        return SkateRide.On && !SkateRide.Frozen && !SkateRiggingShowcase.Running && (!requireControls || SkateKeys.ControlsActive) && !SkateKeys.Scripted && !Scenarios.Active &&
            (SkateRide.Mode == RideMode.Air || SkateRide.Mode == RideMode.Ground) &&
            (SkateRide.Mode != RideMode.Ground || !SkateRide.Rotating) &&
            !SkateRide.Manual && !SkateRide.NoseManual && !SkateRide.GroundGrab.Active &&
            !SkateRide.Grabbing && SkateKeys.Grab == SkateGrabKind.None && !SkateRide.Handplant && !SkateRide.FootPlanted &&
            !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateRide.BoardPath.Known &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && SkateSurfaceRide.WallieWeight <= 0 &&
            !SkateRide.LateTrick && Math.Abs(SkateRide.PresentationBodyTarget) < .01 &&
            SkateRide.ViewNormal.y >= .98f && SkateRide.Normal.y >= .98f && !SkateRide.OnRamp &&
            Math.Abs(SkateBoard.Tip) < .001f && Math.Abs(SkateBoard.GrindPitch) < .01f && Math.Abs(SkateBoard.GrindRoll) < .01f &&
            SkateRide.RevertLeft == 0 && SkateRide.Powerslide <= .001f && (allowPush || SkateRide.PushPhase <= .001f);
    }

    private static bool PrepareSupport(int index, SkateRiderRig rig, SkateStance stance, out SkateCaptureFootSupport support, out string error)
    {
        support = null; error = SkateFootContacts.Status;
        if (!SkateFootContacts.Ready) return false;
        int slot = index * 2 + (stance == SkateStance.Goofy ? 1 : 0);
        if (supports[slot] == null && supportErrors[slot] == null || supportVersions[slot] != SkateFootContacts.Version)
        {
            SkateCaptureFootSupport.TryCreate(clips[index], rig, stance, SkateFootContacts.LeftFitter,
                SkateFootContacts.RightFitter, out supports[slot], out supportErrors[slot]);
            supportVersions[slot] = SkateFootContacts.Version;
        }
        support = supports[slot]; error = supportErrors[slot]; return support != null;
    }

    // At most one cold preparation per ordinary ground frame, never a whole
    // library rebuild on pop. Geometry/version changes invalidate cached fits.
    public static void WarmFootSupport(SkateRiderRig rig, SkateStance stance)
    {
        if (!loaded || !SkateFootContacts.Ready) return;
        for (int n = 0; n < clips.Length; n++)
        {
            if (clips[n] == null) continue;
            int slot = n * 2 + (stance == SkateStance.Goofy ? 1 : 0);
            if (supportVersions[slot] == SkateFootContacts.Version && (supports[slot] != null || supportErrors[slot] != null)) continue;
            SkateCaptureFootSupport ignored; string error;
            PrepareSupport(n, rig, stance, out ignored, out error); return;
        }
    }
    private static double SourceProgress(double liveProgress)
    {
        double value = flipExtended ? Math.Max(previousAirProgress, liveProgress) : liveProgress;
        // The controller can add a turn just before the old target finishes.
        // Do not enter the shoe-return adapter while that request is pending.
        // Keys retains an extra already drained from Pad until physics takes it.
        if ((SkatePad.HeldFlipExtensionPending || SkateKeys.HeldFlip != 0) &&
            Math.Abs(signature.Flip) < 1080 && Math.Abs(signature.Shove) < .01 && Math.Abs(signature.Spin) < .01 &&
            previousAirProgress <= active.FreeFlightEndProgress)
            value = Math.Min(value, active.FreeFlightEndProgress);
        return value;
    }
    private static bool TryExtendFlip(SkateRiderRig rig, SkateCaptureTrickSignature requested, out string error)
    {
        error = "the live trick variant changed";
        if (landed || caughtAt >= 0 || !SkateCaptureVariants.IsFlipExtension(signature, requested)) return false;
        if (previousAirProgress > active.FreeFlightEndProgress)
        { error = "extended flip capture has already begun its source foot return"; return false; }
        int index = SkateCaptureVariants.IndexOf(requested.MovementKind);
        if (index < 0 || clips[index] == null)
        { error = "extended flip capture has no separately approved variant"; return false; }
        if (!active.SharesSourceWith(clips[index]))
        { error = "extended flip capture has different source motion or contacts"; return false; }
        SkateCaptureFootSupport support;
        if (!PrepareSupport(index, rig, activeStance, out support, out error)) return false;
        double live = clips[index].PresentationProgress(requested.RotationProgress(SkateRide.FlipDeg, SkateRide.ShoveDeg, SkateRide.SpinDeg));
        if (live >= 1 || previousAirProgress >= 1)
        { error = "extended flip capture has no remaining free-flight interval"; return false; }
        // Hold the last shown free pose until the larger family's clock catches
        // up; stretching the remaining return would carry a low shoe through it.
        flipExtended = true;
        active = clips[index]; signature = requested; footSupport = support;
        return true;
    }
    public static bool TryFrame(SkateRiderRig rig, Transform player, SkateRiderPose baseline, SkateVector baselineLeftUp, SkateVector baselineRightUp, out SkateCaptureFrame frame)
    {
        frame = default(SkateCaptureFrame); ClearFrame();
        // Loading is done once by BeginRide, never by this per-frame path.
        if (!loaded || player == null) return false;
        bool airborne = SkateRide.Mode == RideMode.Air, queued = SkateRide.PresentationQueued;
        if (queued && !airborne) { active = null; ClearPlantedFeet(); return false; } // The flick is not known during most of the live load.
        int nextToken = SkateRide.Pops + (queued && !airborne ? 1 : 0);
        if (!Eligible())
        {
            bool retainGroundFeet = SkateRide.Mode == RideMode.Ground && Eligible(false, true);
            if (!retainGroundFeet) ClearPlantedFeet();
            if (active != null) Refuse(nextToken, "another riding action owns presentation", retainGroundFeet);
            return false;
        }
        if (nextToken == refusedToken) return false;
        if (active != null && token != nextToken) active = null;
        if (active == null)
        {
            if (!airborne && !queued || airborne && (!SkateRide.Jumped || SkateRide.AirTime > .1f)) return false;
            var requested = SkateRide.PresentationSignature; string kind = requested.MovementKind;
            int index = SkateCaptureVariants.IndexOf(kind);
            if (index < 0 || clips[index] == null) { if (airborne) refusedToken = nextToken; return false; }
            active = clips[index]; signature = requested; activeStance = SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular; token = nextToken; lands = SkateRide.Lands; airSeen = landed = flipExtended = false; beganAt = Time.realtimeSinceStartup; caughtAt = -1; previousAirSourceTime = catchSourceLead = previousAirProgress = 0; previous = default(SkateCaptureFrame);
            preparedAt = -1; catchPreparationWeight = 0;
            supportedKneePlanes = active.SourceMovementKind == "nollie-backside-180-heelflip";
            catchKneeDegrees = supportedKneePlanes ? 100 :
                active.SourceMovementKind == "360-flip" ? 135 : SkateCaptureFootSupport.CatchPriorInternalDegrees;
            landingKneeDegrees = supportedKneePlanes ? 100 : SkateCaptureFootSupport.LandingPriorInternalDegrees;
            string supportError = null;
            if (!PrepareSupport(index, rig, activeStance, out footSupport, out supportError))
            { Refuse(nextToken, supportError); return false; }
            footVersion = SkateFootContacts.Version;
        }
        if (!SkateFootContacts.Ready || footVersion != SkateFootContacts.Version)
        { Refuse(nextToken, "the visible foot geometry changed or is unavailable"); return false; }
        if (activeStance != (SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular))
        { Refuse(nextToken, "the rider stance changed"); return false; }
        if ((airborne || queued) && !signature.Same(SkateRide.PresentationSignature))
        {
            string extensionError = "the live trick variant changed";
            if (!airborne || !TryExtendFlip(rig, SkateRide.PresentationSignature, out extensionError))
            { Refuse(nextToken, airborne ? extensionError : "the live trick variant changed"); return false; }
        }
        SkateCapturePhase phase; double value;
        if (airborne)
        {
            airSeen = true;
            bool caught = !SkateRide.Rotating && SkateRide.AirTime > .03f;
            phase = caught ? SkateCapturePhase.Catch : SkateCapturePhase.Air;
            if (caught && caughtAt < 0)
            {
                caughtAt = Time.realtimeSinceStartup; catchSourceLead = 0;
                if (previous.Board.Initialized)
                {
                    // A slow source rotation tail can compress several source
                    // frames into the final physics step. Finish that observed
                    // foot return at source speed before adding catch contacts.
                    double gap = active.SourceTime(SkateCapturePhase.Catch, 0) - previousAirSourceTime;
                    catchSourceLead = Math.Max(0, gap - Math.Max(0, caughtAt - previousAt));
                    if (catchSourceLead > .08)
                    { Refuse(nextToken, "capture catch approach exceeds its source boundary"); return false; }
                }
            }
            value = caught ? Time.realtimeSinceStartup - caughtAt - catchSourceLead : SourceProgress(active.PresentationProgress(signature.RotationProgress(SkateRide.FlipDeg, SkateRide.ShoveDeg, SkateRide.SpinDeg)));
        }
        else if (queued) { phase = SkateCapturePhase.Load; value = SkateKeys.Charging ? SkateKeys.Charge : 1; }
        else
        {
            // Keep the physical impact, then leave the filmed deep crouch sooner
            // so ordinary rolling does not wait on a slow source recovery.
            double since = (Time.realtimeSinceStartup - SkateRide.LandAt) * GroundRecoverySpeed;
            if (!airSeen || SkateRide.Lands <= lands || since < 0 || active.Finished(since)) { active = null; return false; }
            phase = SkateCapturePhase.Land; value = since;
            if (!landed) { landed = true; landedAt = Time.realtimeSinceStartup; landingFrom = previous; landingCarrier = previousCarrier; ClearPlantedFeet(); }
        }
        var up = SkateRide.ViewNormal; var heading = SkateBoard.OnFace(up, SkateRide.Dir(SkateRide.NoseYaw));
        var board = SkateBoard.PoseFor(player); string error;
        if (!active.TrySample(phase, value, rig, SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular,
            board, V(heading), V(up), out frame, out error, footSupport, true, true)) { Refuse(nextToken, error); return false; }
        // The source's flip finishes ahead of its shove. Keep the flick foot
        // tucked through the last three source frames as the live board returns,
        // preserving its free shoe orientation and the ordinary catch guards.
        if (active.SourceMovementKind == "nollie-360-flip")
        {
            double sourceTime = phase == SkateCapturePhase.Air ? active.FlightSourceTime(value) : active.SourceTime(phase, value);
            double end = active.SourceTime(SkateCapturePhase.Catch, 0), start = end - .1001, peak = end - .0625625;
            var foot = activeStance == SkateStance.Regular ? SkateCaptureContacts.RightFoot : SkateCaptureContacts.LeftFoot;
            if (sourceTime > start && sourceTime < end && (frame.Observation.Contacts & foot) == 0)
            {
                double u = sourceTime <= peak ? (sourceTime - start) / (peak - start) : (end - sourceTime) / (end - peak);
                double tuck = Math.Min(.065, .06 * rig.ThighLength / .44) * u * u * u * (10 + u * (-15 + 6 * u));
                var shift = frame.RiderUp * tuck; var none = default(SkateVector); SkateRiderPose tucked;
                if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose,
                    foot == SkateCaptureContacts.LeftFoot ? shift : none,
                    foot == SkateCaptureContacts.RightFoot ? shift : none, false, out tucked, out error))
                { Refuse(nextToken, error ?? "capture late-return tuck exceeds measured reach"); return false; }
                frame = new SkateCaptureFrame(tucked, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                    frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            }
        }
        double now = Time.realtimeSinceStartup;
        var carrier = new SkateBoardPose(board.Origin, V(heading), V(up));
        if (phase == SkateCapturePhase.Catch && previous.Board.Initialized)
        {
            var approaching = SkateCapturePlayback.Transport(previous, previousCarrier, carrier);
            var newlyCaught = frame.Observation.Contacts & ~previous.Observation.Contacts;
            // Rotation observations advance at the physics cadence even between faster render frames.
            double observedDelta = Math.Max(now - previousAt, SkateRide.PresentationStepSeconds);
            double limit = .025 + 4 * Math.Max(.001, Math.Min(.05, observedDelta));
            if (!approaching.Board.Initialized ||
                (newlyCaught & SkateCaptureContacts.LeftFoot) != 0 && (frame.Pose.LeftAnkle - approaching.Pose.LeftAnkle).Length > limit ||
                (newlyCaught & SkateCaptureContacts.RightFoot) != 0 && (frame.Pose.RightAnkle - approaching.Pose.RightAnkle).Length > limit)
            { Refuse(nextToken, "capture feet did not approach the live catch safely"); return false; }
        }
        if (landed)
        {
            if (!plantedLeft.Initialized && !RememberPlantedFeet(rig, frame))
            { Refuse(nextToken, "landing has no valid measured foot frames"); return false; }
            SkateRiderPose planted; SkateVector lu, ru;
            if (!PinPlantedFeet(rig, frame.Pose, board, frame.LeftSoleUp, frame.RightSoleUp, out planted, out lu, out ru, out error))
            { Refuse(nextToken, error ?? "planted landing feet exceed measured reach"); return false; }
            frame = new SkateCaptureFrame(planted, frame.Board, frame.Rotation, lu, ru, frame.HasEvent, frame.Observation,
                frame.PelvisCorrection + planted.Pelvis - frame.Pose.Pelvis, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
        }
        if (landed && now - landedAt < .1 && landingFrom.Board.Initialized)
        {
            var start = SkateCapturePlayback.Transport(landingFrom, landingCarrier, carrier);
            if (!start.Board.Initialized || !SkateCapturePlayback.TryBlend(rig, start.Pose, start.LeftSoleUp, start.RightSoleUp,
                frame, Math.Max(0, (now - landedAt) / .1), out frame, out error, true))
            { Refuse(nextToken, error ?? "landing transition carrier unavailable"); return false; }
        }
        double weight = Math.Max(0, Math.Min(1, (now - beganAt) / .1));
        if (landed) weight = Math.Min(weight, active.RecoveryWeight(value));
        if (weight <= 0) return false; // The first live pop frame keeps the exact procedural pose.
        // Keep the pop-foot target through release until entry is complete;
        // dropping its owner on the release frame would jump back to a free baseline.
        var entryFoot = !landed && weight < 1 && active.SourceMovementKind == "bs-pop-shove-it"
            ? (activeStance == SkateStance.Goofy ? SkateCaptureContacts.LeftFoot : SkateCaptureContacts.RightFoot)
            : SkateCaptureContacts.None;
        if (!SkateCapturePlayback.TryBlend(rig, baseline, baselineLeftUp, baselineRightUp, frame, weight, out frame, out error, landed, entryFoot))
        { Refuse(nextToken, error); return false; }
        if (!footSupport.TryConform(rig, frame, out frame, out error, !landed && (weight < 1 || phase == SkateCapturePhase.Catch)))
        { Refuse(nextToken, error ?? "the blended foot frame cannot follow the shaped deck"); return false; }
        if (!landed && phase == SkateCapturePhase.Air && active.SourceMovementKind == "hardflip")
        {
            // An authored free-foot prior: lift the front ankle and toe together,
            // preserving the source torso and the canonical leg solve. Source
            // progress and the existing entry weight own its timing, not a new clock.
            double sourceTime = active.FlightSourceTime(value), start = active.SourceTime(SkateCapturePhase.Air, 0);
            double span = active.SourceTime(SkateCapturePhase.Catch, 0) - start;
            double peak = start + span * 2 / 7, finish = start + span * .5;
            var front = activeStance == SkateStance.Regular ? SkateCaptureContacts.LeftFoot : SkateCaptureContacts.RightFoot;
            if (span > .000001 && sourceTime > start && sourceTime < finish && (frame.Observation.Contacts & front) == 0)
            {
                double u = sourceTime <= peak ? (sourceTime - start) / (peak - start) : (finish - sourceTime) / (finish - peak);
                u = Math.Max(0, Math.Min(1, u));
                double lift = Math.Min(.28, .28 * rig.ThighLength / .44) * weight * u * u * u * (10 + u * (-15 + 6 * u));
                var shift = frame.RiderUp * lift; var none = default(SkateVector); SkateRiderPose tucked;
                if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose,
                    front == SkateCaptureContacts.LeftFoot ? shift : none,
                    front == SkateCaptureContacts.RightFoot ? shift : none, false, out tucked, out error))
                { Refuse(nextToken, error ?? "authored Hardflip early tuck exceeds measured reach"); return false; }
                frame = new SkateCaptureFrame(tucked, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                    frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            }
        }
        if (!landed && phase == SkateCapturePhase.Catch &&
            (frame.Observation.Contacts & (SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot)) ==
            (SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot))
        {
            // Start only after the source return and both real catch contacts.
            // Do not anticipate impact by altering free feet or an active flip.
            if (preparedAt < 0) preparedAt = now;
            catchPreparationWeight = SkateCaptureFootSupport.CatchPreparationWeight(now - preparedAt,
                supportedKneePlanes ? SkateCaptureFootSupport.SupportedCatchPreparationSeconds : SkateCaptureFootSupport.CatchPreparationSeconds);
            SkateRiderPose prepared; SkateVector preparationShift;
            if (!SkateCaptureFootSupport.TryCatchPreparation(rig, frame.Pose, frame.RiderUp, catchPreparationWeight,
                out prepared, out preparationShift, out error, catchKneeDegrees, supportedKneePlanes))
            { Refuse(nextToken, error ?? "authored catch preparation unavailable"); return false; }
            catchPreparationWeight = SkateCaptureFootSupport.ReachedCatchPreparationWeight(rig, prepared, catchPreparationWeight, catchKneeDegrees);
            frame = new SkateCaptureFrame(prepared, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection + preparationShift,
                frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
        }
        if (!landed && phase == SkateCapturePhase.Catch && previous.Board.Initialized)
        {
            // The physical catch can precede the source's contact label. Keep
            // its source stand-up from lifting the pelvis while the feet return.
            var prior = SkateCapturePlayback.Transport(previous, previousCarrier, carrier);
            SkateRiderPose settled; SkateVector riseShift;
            if (!prior.Board.Initialized || !SkateCaptureFootSupport.TryLimitCatchRise(rig, frame.Pose, prior.Pose.Pelvis,
                frame.RiderUp, out settled, out riseShift, out error))
            { Refuse(nextToken, error ?? "catch rise carrier unavailable"); return false; }
            frame = new SkateCaptureFrame(settled, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection + riseShift,
                frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            catchPreparationWeight = SkateCaptureFootSupport.ReachedCatchPreparationWeight(rig, settled, 1, catchKneeDegrees);
        }
        if (landed)
        {
            if (!groundedImpact.Board.Initialized) groundedImpact = frame;
            else
            {
                var impact = SkateCapturePlayback.Transport(groundedImpact, groundedImpact.Board, board);
                double u = Math.Max(0, Math.Min(1, (value - .12) / .35));
                u = u * u * u * (10 + u * (-15 + 6 * u));
                u = Math.Max(0, Math.Min(1, u));
                double gaze = Math.Max(0, Math.Min(1, (value - .12) / .6));
                gaze = gaze * gaze * gaze * (10 + gaze * (-15 + 6 * gaze));
                gaze = Math.Max(0, Math.Min(1, gaze));
                SkateRiderPose settled;
                if (!impact.Board.Initialized || !SkateCaptureFootSupport.TryGroundRecovery(rig, impact.Pose, baseline, frame.Pose,
                    frame.RiderUp, u, gaze, out settled, out error,
                    active.SourceMovementKind == "kickflip" || active.SourceMovementKind == "heelflip" || active.SourceMovementKind == "360-flip" ||
                    active.SourceMovementKind == "nollie-heelflip" || active.SourceMovementKind == "fakie-kickflip" ||
                    active.SourceMovementKind == "nollie-backside-180-heelflip" || active.SourceMovementKind == "nollie-360-flip"))
                { Refuse(nextToken, error ?? "supported recovery frame unavailable"); return false; }
                frame = new SkateCaptureFrame(settled, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                    frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
            }
            SkateRiderPose absorbed; SkateVector absorptionShift;
            if (!SkateCaptureFootSupport.TryLandingAbsorption(rig, frame.Pose, frame.RiderUp, now - landedAt,
                out absorbed, out absorptionShift, out error, catchPreparationWeight, catchKneeDegrees, landingKneeDegrees, supportedKneePlanes))
            { Refuse(nextToken, error ?? "authored landing absorption unavailable"); return false; }
            frame = new SkateCaptureFrame(absorbed, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection + absorptionShift,
                frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
        }
        if (active.SourceMovementKind == "nollie-backside-180-heelflip" && (phase == SkateCapturePhase.Catch || landed))
        {
            SkateRiderPose supported;
            double sinceCatch = caughtAt >= 0 ? now - caughtAt - catchSourceLead : now - landedAt;
            if (!SkateCaptureFootSupport.TrySupportedTorso(frame.Pose, frame.RiderUp, sinceCatch, out supported, out error))
            { Refuse(nextToken, error); return false; }
            frame = new SkateCaptureFrame(supported, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, frame.RiderUp);
        }
        if (phase == SkateCapturePhase.Air) { previousAirSourceTime = active.FlightSourceTime(value); previousAirProgress = value; }
        previous = frame; previousCarrier = carrier; previousAt = now;
        displayed = board; return true;
    }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }
}
