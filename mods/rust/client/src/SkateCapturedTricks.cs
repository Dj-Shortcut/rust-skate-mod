using System;
using System.IO;
using System.Security.Cryptography;
using Shortcut.RustMod;
using UnityEngine;

// Normal riding consumes only owner-approved, reviewed captures. No runtime menu can approve data.
public static class SkateCapturedTricks
{
    private static readonly string[] Kinds = { "switch-heelflip", "nollie-heelflip", "fakie-kickflip", "nollie-backside-180-heelflip", "nollie-360-flip" };
    private static readonly SkateCapturePlayback[] clips = new SkateCapturePlayback[Kinds.Length];
    private static readonly SkateCaptureFootSupport[] supports = new SkateCaptureFootSupport[Kinds.Length * 2];
    private static readonly int[] supportVersions = new int[Kinds.Length * 2];
    private static readonly string[] supportErrors = new string[Kinds.Length * 2];
    private static bool loaded, airSeen, landed;
    private static double beganAt, landedAt, caughtAt = -1, previousAt;
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
    public static bool HasFrame { get { return displayed.Initialized; } }
    public static SkateBoardPose DisplayFrame { get { return displayed; } }
    public static string Status { get; private set; } = "Captured riding not loaded";
    public static void ClearFrame() { displayed = default(SkateBoardPose); }
    public static void Reset()
    { active = null; footSupport = null; token = refusedToken = -1; airSeen = landed = false; caughtAt = -1; previous = landingFrom = default(SkateCaptureFrame); ClearFrame(); }
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
            var pending = new SkateCapturePlayback[Kinds.Length]; int accepted = 0, unsupported = 0, rotationClocks = 0;
            for (int n = 0; n < approval.Count; n++)
            {
                SkateCaptureClip clip = null;
                for (int c = 0; c < library.Count; c++) if (library.GetClip(c).Id == approval.GetId(n)) clip = library.GetClip(c);
                if (clip == null || !clip.Reviewed) { Status = "Captured riding disabled: approved clip missing or unreviewed"; return; }
                int kind = Array.IndexOf(Kinds, clip.MovementKind);
                if (kind < 0) { unsupported++; continue; }
                if (pending[kind] != null) { Status = "Captured riding disabled: ambiguous approved trick variant"; return; }
                SkateCapturePlayback playback;
                if (!SkateCapturePlayback.TryCreate(clip, out playback, out error)) { Status = error; return; }
                if (playback.UsesSourceRotationClock) rotationClocks++;
                pending[kind] = playback; accepted++;
            }
            Array.Copy(pending, clips, clips.Length);
            Status = "Captured riding ready: " + accepted + " approved variants; source rotation clocks=" + rotationClocks +
                (unsupported > 0 ? "; " + unsupported + " unsupported approvals ignored" : "") + "; restart client after replacing pack";
        }
        catch (Exception e) { Status = "Captured riding disabled: " + e.GetType().Name; }
        finally { Out.Say("CAPTURE " + Status); }
    }
    private static void Refuse(int pop, string reason)
    {
        active = null; footSupport = null; refusedToken = pop; ClearFrame();
        if (reason != lastRefusal) { lastRefusal = reason; Out.Say("CAPTURE fallback: " + reason); }
    }
    private static bool Eligible()
    {
        // These source clips are flat-ground footage. Transition, wall and contact-action motion owns its own presentation.
        return SkateRide.On && !SkateRide.Frozen && !SkateRiggingShowcase.Running && SkateKeys.ControlsActive && !SkateKeys.Scripted && !Scenarios.Active &&
            (SkateRide.Mode == RideMode.Air || SkateRide.Mode == RideMode.Ground) &&
            (SkateRide.Mode != RideMode.Ground || !SkateRide.Rotating) &&
            !SkateRide.Manual && !SkateRide.NoseManual && !SkateRide.GroundGrab.Active &&
            !SkateRide.Grabbing && SkateKeys.Grab == SkateGrabKind.None && !SkateRide.Handplant && !SkateRide.FootPlanted &&
            !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateRide.BoardPath.Known &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && SkateSurfaceRide.WallieWeight <= 0 &&
            !SkateRide.LateTrick && Math.Abs(SkateRide.PresentationBodyTarget) < .01 &&
            SkateRide.ViewNormal.y >= .98f && SkateRide.Normal.y >= .98f && !SkateRide.OnRamp &&
            Math.Abs(SkateBoard.Tip) < .001f && Math.Abs(SkateBoard.GrindPitch) < .01f && Math.Abs(SkateBoard.GrindRoll) < .01f &&
            SkateRide.RevertLeft == 0 && SkateRide.Powerslide <= .001f && SkateRide.PushPhase <= .001f;
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
    public static bool TryFrame(SkateRiderRig rig, Transform player, SkateRiderPose baseline, SkateVector baselineLeftUp, SkateVector baselineRightUp, out SkateCaptureFrame frame)
    {
        frame = default(SkateCaptureFrame); ClearFrame();
        // Loading is done once by BeginRide, never by this per-frame path.
        if (!loaded || player == null) return false;
        bool airborne = SkateRide.Mode == RideMode.Air, queued = SkateRide.PresentationQueued;
        if (queued && !airborne) { active = null; return false; } // The flick is not known during most of the live load.
        int nextToken = SkateRide.Pops + (queued && !airborne ? 1 : 0);
        if (!Eligible()) { if (active != null) Refuse(nextToken, "another riding action owns presentation"); return false; }
        if (nextToken == refusedToken) return false;
        if (active != null && token != nextToken) active = null;
        if (active == null)
        {
            if (!airborne && !queued || airborne && (!SkateRide.Jumped || SkateRide.AirTime > .1f)) return false;
            var requested = SkateRide.PresentationSignature; string kind = requested.MovementKind;
            int index = Array.IndexOf(Kinds, kind);
            if (index < 0 || clips[index] == null) { if (airborne) refusedToken = nextToken; return false; }
            active = clips[index]; signature = requested; activeStance = SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular; token = nextToken; lands = SkateRide.Lands; airSeen = landed = false; beganAt = Time.realtimeSinceStartup; caughtAt = -1; previous = default(SkateCaptureFrame);
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
        { Refuse(nextToken, "the live trick variant changed"); return false; }
        SkateCapturePhase phase; double value;
        if (airborne)
        {
            airSeen = true;
            bool caught = !SkateRide.Rotating && SkateRide.AirTime > .03f;
            phase = caught ? SkateCapturePhase.Catch : SkateCapturePhase.Air;
            if (caught && caughtAt < 0) caughtAt = Time.realtimeSinceStartup;
            value = caught ? Math.Max(0, Time.realtimeSinceStartup - caughtAt) : signature.RotationProgress(SkateRide.FlipDeg, SkateRide.ShoveDeg, SkateRide.SpinDeg);
        }
        else if (queued) { phase = SkateCapturePhase.Load; value = SkateKeys.Charging ? SkateKeys.Charge : 1; }
        else
        {
            double since = Time.realtimeSinceStartup - SkateRide.LandAt;
            if (!airSeen || SkateRide.Lands <= lands || since < 0 || active.Finished(since)) { active = null; return false; }
            phase = SkateCapturePhase.Land; value = since;
            if (!landed) { landed = true; landedAt = Time.realtimeSinceStartup; landingFrom = previous; landingCarrier = previousCarrier; }
        }
        var up = SkateRide.ViewNormal; var heading = SkateBoard.OnFace(up, SkateRide.Dir(SkateRide.NoseYaw));
        var board = SkateBoard.PoseFor(player); string error;
        if (!active.TrySample(phase, value, rig, SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular,
            board, V(heading), V(up), out frame, out error, footSupport)) { Refuse(nextToken, error); return false; }
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
        if (landed && now - landedAt < .1 && landingFrom.Board.Initialized)
        {
            var start = SkateCapturePlayback.Transport(landingFrom, landingCarrier, carrier);
            if (!start.Board.Initialized || !SkateCapturePlayback.TryBlend(rig, start.Pose, start.LeftSoleUp, start.RightSoleUp,
                frame, Math.Max(0, (now - landedAt) / .1), out frame, out error, true))
            { Refuse(nextToken, error ?? "landing transition carrier unavailable"); return false; }
        }
        double weight = Math.Max(0, Math.Min(1, (now - beganAt) / .1));
        if (landed) weight = Math.Min(weight, Math.Max(0, Math.Min(1,
            (active.Clip.Duration - active.SourceTime(SkateCapturePhase.Land, value)) / .12)));
        if (weight <= 0) return false; // The first live pop frame keeps the exact procedural pose.
        if (!SkateCapturePlayback.TryBlend(rig, baseline, baselineLeftUp, baselineRightUp, frame, weight, out frame, out error))
        { Refuse(nextToken, error); return false; }
        if (!footSupport.TryConform(rig, frame, out frame, out error))
        { Refuse(nextToken, error ?? "the blended foot frame cannot follow the shaped deck"); return false; }
        previous = frame; previousCarrier = carrier; previousAt = now;
        displayed = board; return true;
    }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }
}
