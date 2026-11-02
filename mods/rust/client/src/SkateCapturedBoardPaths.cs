using System;
using Shortcut.RustMod;
using UnityEngine;

// Impossible keeps rear-foot ownership in Air. It must not enter the ordinary
// free-flip contract or inherit its sole pins, pitch and catch correction stack.
public static class SkateCapturedBoardPaths
{
    public const string RequiredClipId = "impossible-source-keyed-v1";
    public const string MovementKind = "impossible";
    public const string Provenance = "Source-led Griffin rear-foot Impossible: captured upper gesture, reconstructed full vertical winding and continuous rear-forefoot hook; actual physics carrier and landing remain authoritative. Hidden contact depth is authored, not measured mocap.";
    private static SkateCaptureClip approved;
    private static double pop, sourceAir, caught, land;
    private static bool active, landed, departing;
    private static int token = -1, refused = -1, lands, footVersion;
    private static uint epoch;
    private static Transform owner;
    private static SkateStance stance;
    private static double airAt, flight, landedAt, departedAt;
    private static SkateVector groundOrigin, entryRoot, entryVelocity, entryTarget, entryTargetVelocity;
    private static double entrySeconds; private static bool entry;
    private static SkateCaptureFrame previous;
    private static SkateBoardPose previousCarrier;
    public static double SourceSeconds { get; private set; }
    public static double ClearanceShift { get; private set; }
    public static bool HasFrame { get; private set; }
    public static string Status { get; private set; } = "Impossible capture unavailable";

    internal static bool TryApprove(SkateCaptureClip clip, out string error)
    {
        error = null;
        if (clip == null || !clip.Reviewed || clip.Id != RequiredClipId || clip.MovementKind != MovementKind)
        { error = "The exact owner-approved Impossible clip is required."; return false; }
        const SkateCaptureContacts both = SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot;
        double p = -1, a = -1, c = -1, l = -1, last = -1; int phase = 0;
        var seen = new bool[7];
        for (int n = 0; n < clip.EventCount; n++)
        {
            var e = clip.GetEvent(n); int next = (int)e.Phase;
            var expected = next == 2 || next == 3 ? SkateCaptureContacts.RightFoot : both;
            if (next < 1 || next > 6 || next < phase || e.Time <= last || e.Contacts != expected || n == 0 && (next != 1 || e.Time != 0))
            { error = "Impossible needs ordered phases and continuous canonical rear-foot ownership."; return false; }
            if (!seen[next])
            {
                if (next == 2) p = e.Time;
                if (next == 3) a = e.Time;
                if (next == 4) c = e.Time;
                if (next == 5) l = e.Time;
                seen[next] = true;
            }
            phase = next; last = e.Time;
        }
        for (int n = 1; n <= 6; n++) if (!seen[n])
        { error = "Impossible needs all six bounded presentation phases."; return false; }
        if (!(p < c && c < l && l < clip.Duration))
        { error = "Impossible source clock is invalid."; return false; }
        // A family name is not proof of a wrap. The bounded rows must make one
        // complete nose-up vertical winding, rather than a yaw or kickflip roll.
        double winding = 0, prior = 0; bool first = true;
        for (int n = 0; n < clip.SampleCount; n++)
        {
            var q = new SkateCaptureRotation(clip.Number(n, 4), clip.Number(n, 5), clip.Number(n, 6), clip.Number(n, 7));
            var forward = q.Rotate(new SkateVector(0, 0, 1));
            double angle = Math.Atan2(forward.Y, forward.Z) * 180 / Math.PI;
            if (!first) { double step = angle - prior; while (step > 180) step -= 360; while (step < -180) step += 360; winding += step; }
            first = false; prior = angle;
            if (Math.Abs(forward.X) > .05)
            { error = "Impossible wrap must retain its bounded vertical plane."; return false; }
        }
        if (Math.Abs(winding - 360) > 1)
        { error = "Impossible requires exactly one full rear-foot vertical winding."; return false; }
        approved = clip; pop = p; sourceAir = a; caught = c; land = l; Reset();
        Status = "Impossible approved; exact native rear-wrap action required"; return true;
    }
    internal static void ClearApproval() { approved = null; Reset(); }
    public static void Reset()
    { active = landed = departing = entry = HasFrame = false; token = refused = -1; owner = null; previous = default(SkateCaptureFrame); SourceSeconds = ClearanceShift = 0; }
    private static bool Stop(string why)
    {
        HasFrame = false;
        if (active) { refused = token; Status = "Impossible capture fallback: " + why; Out.Say("CAPTURE " + Status); }
        active = departing = entry = false; owner = null; previous = default(SkateCaptureFrame); return false;
    }
    private static bool Eligible()
    {
        return SkateRide.On && !SkateRide.Frozen && !SkateRiggingShowcase.Running && SkateKeys.ControlsActive && !SkateKeys.Scripted && !Scenarios.Active &&
            (SkateRide.Mode == RideMode.Air || SkateRide.Mode == RideMode.Ground) &&
            !SkateRide.Manual && !SkateRide.NoseManual && !SkateRide.GroundGrab.Active && !SkateRide.Grabbing && SkateKeys.Grab == SkateGrabKind.None &&
            !SkateRide.Handplant && !SkateRide.FootPlanted && !SkateActions.Owns && !SkateOldschoolRide.Active &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && SkateSurfaceRide.WallieWeight <= 0 &&
            !SkateRide.LateTrick && Math.Abs(SkateRide.PresentationBodyTarget) < .01 &&
            SkateRide.ViewNormal.y >= .98f && SkateRide.Normal.y >= .98f && !SkateRide.OnRamp &&
            Math.Abs(SkateBoard.Tip) < .001f && Math.Abs(SkateBoard.GrindPitch) < .01f && Math.Abs(SkateBoard.GrindRoll) < .01f &&
            SkateRide.RevertLeft == 0 && SkateRide.Powerslide <= .001f && SkateRide.PushPhase <= .001f;
    }
    private static bool Matches(SkateBoardPathSample path)
    {
        var s = SkateRide.PresentationSignature;
        return SkateBoardPath.Valid(path) && path.Kind == SkateBoardTrickKind.Impossible && path.Direction == 1 && !path.Nollie && !path.Reversed &&
            s.Pop == SkatePopKind.Ollie && !s.Switch && !s.TailLeads &&
            Math.Abs(s.Flip) < .01 && Math.Abs(s.Shove) < .01 && Math.Abs(s.Spin) < .01 && Math.Abs(s.Body) < .01;
    }
    public static bool OwnsNativePop
    { get { return approved != null && active && token == SkateRide.Pops && epoch == SkateKeys.InputEpoch && SkateRide.Mode == RideMode.Air &&
            stance == (SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular) && SkateFootContacts.Ready && footVersion == SkateFootContacts.Version &&
            refused != SkateRide.Pops && Eligible() && !SkateRide.PresentationQueued && Matches(SkateRide.BoardPath); } }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }
    private static double Ease(double u) { u = Math.Max(0, Math.Min(1, u)); return u * u * u * (10 + u * (-15 + 6 * u)); }
    private static SkateBoardPose Carrier(Transform player)
    {
        var up = SkateRide.ViewNormal;
        return new SkateBoardPose(V(SkateBoard.Contact(player) - Vector3.up * SkateBoard.Pop),
            V(SkateBoard.OnFace(up, SkateRide.Dir(SkateRide.NoseYaw))), V(up));
    }
    public static bool TryFrame(SkateRiderRig rig, Transform player, SkateRiderPose baseline,
        SkateVector baselineLeftUp, SkateVector baselineRightUp, out SkateCaptureFrame frame, SkateVector? observedGroundOrigin = null,
        SkateVector? observedLocalRoot = null, SkateVector? observedLocalVelocity = null)
    {
        frame = default(SkateCaptureFrame); HasFrame = false; ClearanceShift = 0;
        if (approved == null || player == null) return false;
        if (!Eligible() || SkateRide.PresentationQueued || active && SkateRide.Mode == RideMode.Ground && SkateKeys.Charging) return Stop("another action owns presentation");
        bool air = SkateRide.Mode == RideMode.Air; double now = Time.realtimeSinceStartup;
        var carrier = Carrier(player);
        if (!SkateBoardPath.Valid(carrier) || carrier.Up.Y < .98) return Stop("upright physical carrier unavailable");
        if (!active)
        {
            if (!air || !SkateRide.Jumped || SkateRide.AirTime > .1f || !Matches(SkateRide.BoardPath) || refused == SkateRide.Pops || !SkateFootContacts.Ready) return false;
            double predicted = SkateRide.FlatgroundFlightSeconds;
            if (!(predicted > 0 && predicted <= 10) || !observedGroundOrigin.HasValue) return false;
            active = true; landed = departing = false; token = SkateRide.Pops; lands = SkateRide.Lands;
            footVersion = SkateFootContacts.Version; stance = SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular;
            owner = player; epoch = SkateKeys.InputEpoch; flight = predicted; airAt = SkateRide.ModeAt;
            groundOrigin = observedGroundOrigin.Value; entry = false;
            if (observedLocalRoot.HasValue && observedLocalVelocity.HasValue)
            {
                // Same world-continuous quintic entry as the source-keyed route.
                // Source482.25 is on its own compact Air plateau; feet/board stay exact.
                double targetTime = pop + (land - pop) * (5.25 / 16), step = .005;
                SkateCaptureFrame target, before, after; string entryError;
                if (!SkateCaptureSampler.TrySample(approved, targetTime, rig, stance, out target, out entryError) ||
                    !SkateCaptureSampler.TrySample(approved, targetTime - step, rig, stance, out before, out entryError) ||
                    !SkateCaptureSampler.TrySample(approved, targetTime + step, rig, stance, out after, out entryError)) return Stop(entryError);
                entrySeconds = flight * (5.25 / 16); entryRoot = observedLocalRoot.Value;
                entryVelocity = observedLocalVelocity.Value - new SkateVector(0, SkateFlatgroundPop.Gravity * flight * .5, 0);
                entryTarget = target.Pose.Pelvis; entryTargetVelocity = (after.Pose.Pelvis - before.Pose.Pelvis) * ((land - pop) / (2 * step * flight));
                entry = true;
            }
            Status = "Impossible source-keyed rear hook active";
        }
        if (token != SkateRide.Pops || owner != player || epoch != SkateKeys.InputEpoch || !SkateFootContacts.Ready || footVersion != SkateFootContacts.Version ||
            stance != (SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular)) return Stop("pop, player, input, stance or foot geometry changed");
        if (air && !Matches(SkateRide.BoardPath)) return Stop("native Impossible action changed");
        double elapsed = Math.Max(0, now - airAt), sinceLand = -1;
        if (!air)
        {
            if (SkateRide.Lands <= lands || SkateRide.BoardPath.Known || SkateRide.Rotating || SkateRide.LandAt > now) return Stop("no physical Impossible landing");
            if (!landed) { landed = true; landedAt = SkateRide.LandAt; }
            sinceLand = Math.Max(0, now - landedAt);
        }
        double span = land - pop;
        SourceSeconds = landed ? Math.Min(approved.Duration, land + sinceLand / flight * span) : Math.Min(land - span * 1e-7, pop + elapsed / flight * span);
        string error = null;
        if (landed && SourceSeconds >= approved.Duration)
        {
            if (!departing) { departing = true; departedAt = now; }
            double weight = Ease((now - departedAt) / .15);
            if (weight >= 1) { active = false; owner = null; return false; }
            var incoming = SkateCapturePlayback.Transport(previous, previousCarrier, carrier);
            var target = new SkateCaptureFrame(baseline, SkateBoard.PoseFor(player), Rotation(SkateBoard.PoseFor(player)), baselineLeftUp, baselineRightUp,
                incoming.HasEvent, incoming.Observation, default(SkateVector), default(SkateVector), default(SkateVector), carrier.Up);
            if (!incoming.Board.Initialized || !SkateCapturePlayback.TryBlend(rig, incoming.Pose, incoming.LeftSoleUp, incoming.RightSoleUp, target, weight, out frame, out error, true))
                return Stop(error ?? "Impossible recovery handoff unavailable");
            HasFrame = true; return true;
        }
        SkateCaptureFrame sample;
        if (!SkateCaptureSampler.TrySample(approved, SourceSeconds, rig, stance, out sample, out error)) return Stop(error);
        var sourceCarrier = new SkateBoardPose(default(SkateVector), new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
        var transported = SkateCapturePlayback.Transport(sample, sourceCarrier, carrier);
        if (!transported.Board.Initialized) return Stop("Impossible source transport unavailable");
        var authoredBoard = new SkateBoardPose(carrier.Point(sample.Board.Origin), carrier.Rotate(sample.Board.Forward), carrier.Rotate(sample.Board.Up));
        frame = new SkateCaptureFrame(transported.Pose, authoredBoard, Rotation(authoredBoard), transported.LeftSoleUp, transported.RightSoleUp,
            transported.HasEvent, transported.Observation, transported.PelvisCorrection, transported.LeftHandCorrection, transported.RightHandCorrection, carrier.Up);
        // The same short reference-ground pressure correction as authored. The
        // rear hook thereafter follows its own source path, never a flat sole pin.
        double fraction = (SourceSeconds - pop) / span;
        double press = 1 - Ease((fraction - 1.25 / 16) / (1.75 / 16));
        if (!landed && press > 0)
        {
            double reference = .5 * SkateFlatgroundPop.Gravity * .6 * .6 * fraction * (1 - fraction);
            var delta = carrier.Up * ((reference - SkateVector.Dot(carrier.Origin - groundOrigin, carrier.Up)) * press);
            SkateRiderPose shifted;
            if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose, delta, delta, false, out shifted, out error)) return Stop(error);
            var b = new SkateBoardPose(frame.Board.Origin + delta, frame.Board.Forward, frame.Board.Up);
            frame = new SkateCaptureFrame(shifted, b, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp, frame.HasEvent, frame.Observation,
                frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        if (!landed && SourceSeconds > pop + span * 1.25 / 16)
        {
            // A lower real jump needs a slightly tighter rear-foot tuck to fit
            // the same vertical wrap above its real takeoff floor. Move only
            // the board and its hook foot; physics and source winding stay put.
            double minimum = double.MaxValue;
            double side = Math.Abs(SkateVector.Dot(frame.Board.Right, carrier.Up)) * .104;
            for (int n = 0; n < SupportYZ.GetLength(0); n++)
                minimum = Math.Min(minimum, SkateVector.Dot(frame.Board.Origin - groundOrigin, carrier.Up) +
                    SkateVector.Dot(frame.Board.Up, carrier.Up) * SupportYZ[n, 0] +
                    SkateVector.Dot(frame.Board.Forward, carrier.Up) * SupportYZ[n, 1] - side);
            double needed = (SourceSeconds >= sourceAir ? .005 : 0) - minimum;
            if (needed > 0)
            {
                if (needed > .12) return Stop("physical jump is too low for the bounded rear-foot wrap");
                var delta = carrier.Up * needed; var zero = default(SkateVector); SkateRiderPose tucked;
                bool rearLeft = stance == SkateStance.Goofy;
                if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose, rearLeft ? delta : zero, rearLeft ? zero : delta,
                    false, out tucked, out error)) return Stop(error ?? "low-pop Impossible hook exceeds measured reach");
                var board = new SkateBoardPose(frame.Board.Origin + delta, frame.Board.Forward, frame.Board.Up);
                frame = new SkateCaptureFrame(tucked, board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp,
                    frame.HasEvent, frame.Observation, frame.PelvisCorrection, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
                ClearanceShift = needed;
            }
        }
        if (entry && !landed && elapsed < entrySeconds)
        {
            double u = elapsed / entrySeconds, u3 = u * u * u;
            var a = entryVelocity * entrySeconds; var b = entryTargetVelocity * entrySeconds; var d = entryTarget - entryRoot;
            var root = entryRoot + a * u + (d * 10 - a * 6 - b * 4) * u3 +
                (d * -15 + a * 8 + b * 7) * u3 * u + (d * 6 - a * 3 - b * 3) * u3 * u * u;
            var delta = carrier.Point(root) - frame.Pose.Pelvis;
            var identity = new SkateBoardPose(default(SkateVector), new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
            var shiftedFrame = new SkateBoardPose(delta, identity.Forward, identity.Up); SkateRiderPose shifted, pinned;
            if (!SkateRider.TryTransformPose(frame.Pose, identity, shiftedFrame, out shifted) ||
                !SkateCaptureFootSupport.TrySetFeet(rig, shifted, frame.Pose.LeftAnkle, frame.Pose.LeftToe,
                    frame.Pose.RightAnkle, frame.Pose.RightToe, false, out pinned, out error)) return Stop(error ?? "Impossible entry root is unreachable");
            frame = new SkateCaptureFrame(pinned, frame.Board, frame.Rotation, frame.LeftSoleUp, frame.RightSoleUp, frame.HasEvent,
                frame.Observation, frame.PelvisCorrection + delta, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        previous = frame; previousCarrier = carrier; HasFrame = true; return true;
    }
    // Compact projection of the original 13-part model, not a convex riding collider.
    private static readonly double[,] SupportYZ = new double[,] {
        { 0.04549999, -0.40199998 },
        { 0.0423, -0.4014142 },
        { -0.084091883, -0.26409188 },
        { -0.0883826854, -0.2585 },
        { -0.0910799967, -0.2519881 },
        { -0.092, -0.245 },
        { -0.092, 0.245 },
        { -0.0910799967, 0.2519881 },
        { -0.0883826854, 0.2585 },
        { -0.084091883, 0.26409188 },
        { 0.0423, 0.4014142 },
        { 0.04549999, 0.40199998 },
        { 0.04869998, 0.4014142 },
        { 0.04999999, 0.39999998 },
        { 0.04999999, -0.39999998 },
        { 0.04869998, -0.4014142 }
    };
    private static SkateCaptureRotation Rotation(SkateBoardPose f)
    {
        var r = f.Right; var u = f.Up; var z = f.Forward; double x, y, v, w, s, trace = r.X + u.Y + z.Z;
        if (trace > 0) { s = Math.Sqrt(trace + 1) * 2; w = .25 * s; x = (u.Z - z.Y) / s; y = (z.X - r.Z) / s; v = (r.Y - u.X) / s; }
        else if (r.X > u.Y && r.X > z.Z) { s = Math.Sqrt(1 + r.X - u.Y - z.Z) * 2; w = (u.Z - z.Y) / s; x = .25 * s; y = (u.X + r.Y) / s; v = (z.X + r.Z) / s; }
        else if (u.Y > z.Z) { s = Math.Sqrt(1 + u.Y - r.X - z.Z) * 2; w = (z.X - r.Z) / s; x = (u.X + r.Y) / s; y = .25 * s; v = (z.Y + u.Z) / s; }
        else { s = Math.Sqrt(1 + z.Z - r.X - u.Y) * 2; w = (r.Y - u.X) / s; x = (z.X + r.Z) / s; y = (z.Y + u.Z) / s; v = .25 * s; }
        return new SkateCaptureRotation(x, y, v, w).Unit();
    }
}
