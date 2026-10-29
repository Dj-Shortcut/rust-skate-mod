using System;
using Shortcut.RustMod;
using UnityEngine;

// A special action has its own identity and foot ownership. It is never an
// ordinary zero-flip capture or an exception to the ordinary flip eligibility.
public static class SkateCapturedBoardPaths
{
    public const string MovementKind = "impossible";
    public const string Provenance = "Captured Impossible body and safely supported free-foot motion; native BoardPath owns wrap, pivot, authored hook-foot pickup, catch and landing. Authored free-front-foot tuck and shared supported knee prior through wrap, catch and landing; not measured hook contact.";
    private static SkateCapturePlayback approved;
    private static bool active, landed;
    private static int token = -1, refused = -1, lands, footVersion;
    private static SkateStance stance;
    private static double beganAt, caughtAt = -1, landedAt, catchPreparationWeight;
    private static SkateCaptureFrame previous, landingFrom;
    private static SkateBoardPose previousCarrier, landingCarrier;
    public static string Status { get; private set; } = "Impossible capture unavailable";

    // Called only after the shared digest, reviewed-bit and explicit-id gates.
    internal static void SetApproved(SkateCapturePlayback playback)
    {
        if (playback != null && (!playback.Clip.Reviewed || playback.Clip.MovementKind != MovementKind))
            throw new ArgumentException("Impossible capture must be reviewed and family-specific.");
        approved = playback; Reset(); Status = playback == null ? "Impossible capture unavailable" : "Impossible capture approved; native path required";
    }
    public static void Reset()
    { active = landed = false; token = refused = -1; caughtAt = -1; catchPreparationWeight = 0; previous = landingFrom = default(SkateCaptureFrame); }
    private static bool Stop(string why)
    {
        if (active) { refused = token; Status = "Impossible capture fallback: " + why; Out.Say("CAPTURE " + Status); }
        active = false; previous = default(SkateCaptureFrame); return false;
    }
    private static bool Eligible()
    {
        // This is an isolated special-action gate. Ordinary Eligible remains unchanged.
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
    private static double Ease(double value)
    { double u = Math.Max(0, Math.Min(1, value)); return Math.Max(0, Math.Min(1, u * u * u * (10 + u * (-15 + 6 * u)))); }
    private static SkateVector V(Vector3 value) { return new SkateVector(value.x, value.y, value.z); }

    private static SkateCaptureRotation Rotation(SkateBoardPose frame)
    {
        var r = frame.Right; var u = frame.Up; var f = frame.Forward; double x, y, z, w, n, trace = r.X + u.Y + f.Z;
        if (trace > 0) { n = Math.Sqrt(trace + 1) * 2; w = .25 * n; x = (u.Z - f.Y) / n; y = (f.X - r.Z) / n; z = (r.Y - u.X) / n; }
        else if (r.X > u.Y && r.X > f.Z) { n = Math.Sqrt(1 + r.X - u.Y - f.Z) * 2; w = (u.Z - f.Y) / n; x = .25 * n; y = (u.X + r.Y) / n; z = (f.X + r.Z) / n; }
        else if (u.Y > f.Z) { n = Math.Sqrt(1 + u.Y - r.X - f.Z) * 2; w = (f.X - r.Z) / n; x = (u.X + r.Y) / n; y = .25 * n; z = (f.Y + u.Z) / n; }
        else { n = Math.Sqrt(1 + f.Z - r.X - u.Y) * 2; w = (r.Y - u.X) / n; x = (f.X + r.Z) / n; y = (f.Y + u.Z) / n; z = .25 * n; }
        return new SkateCaptureRotation(x, y, z, w).Unit();
    }

    public static bool TryFrame(SkateRiderRig rig, Transform player, SkateRiderPose baseline,
        SkateVector baselineLeftUp, SkateVector baselineRightUp, out SkateCaptureFrame frame)
    {
        frame = default(SkateCaptureFrame);
        if (approved == null || player == null) return false;
        if (!Eligible() || SkateRide.PresentationQueued) return Stop("another riding action owns presentation");
        bool air = SkateRide.Mode == RideMode.Air;
        var path = SkateRide.BoardPath;
        if (active && token != SkateRide.Pops) active = false;
        if (air && !Matches(path)) return Stop("special board path changed");
        if (!active)
        {
            if (!air || !SkateRide.Jumped || SkateRide.AirTime > .1f || !Matches(path) || refused == SkateRide.Pops) return false;
            if (!SkateFootContacts.Ready) return false;
            active = true; landed = false; token = SkateRide.Pops; lands = SkateRide.Lands;
            stance = SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular;
            footVersion = SkateFootContacts.Version; beganAt = Time.realtimeSinceStartup; caughtAt = -1; catchPreparationWeight = 0;
            previous = default(SkateCaptureFrame); Status = "Impossible capture active; native hook foot retained";
        }
        if (!SkateFootContacts.Ready || footVersion != SkateFootContacts.Version ||
            stance != (SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular)) return Stop("visible foot geometry or stance changed");
        double now = Time.realtimeSinceStartup, sourceTime;
        if (air)
        {
            if (path.Complete && caughtAt < 0) caughtAt = now;
            double pop = approved.SourceTime(SkateCapturePhase.Pop, 0), caught = approved.SourceTime(SkateCapturePhase.Catch, 0);
            sourceTime = path.Active ? pop + (caught - pop) * path.Progress : approved.SourceTime(SkateCapturePhase.Catch, now - caughtAt);
        }
        else
        {
            if (path.Known || SkateRide.Rotating || SkateRide.Lands <= lands || SkateRide.LandAt > now) return Stop("no physical Impossible landing");
            double since = (now - SkateRide.LandAt) * 1.35;
            if (approved.Finished(since)) { active = false; return false; }
            if (!landed) { landed = true; landedAt = now; landingFrom = previous; landingCarrier = previousCarrier; }
            sourceTime = approved.SourceTime(SkateCapturePhase.Land, since);
        }
        var board = SkateBoard.PoseFor(player);
        var unwrapped = air ? board.WithoutPath(path) : board;
        var carrier = new SkateBoardPose(unwrapped.Origin,
            V(SkateBoard.OnFace(SkateRide.ViewNormal, SkateRide.Dir(SkateRide.NoseYaw))), V(SkateRide.ViewNormal));
        SkateCaptureFrame sample; string error;
        if (!SkateCaptureSampler.TrySample(approved.Clip, sourceTime, rig, stance, out sample, out error)) return Stop(error);
        // Remove the source authoring's original .045 m orbital datum,
        // independently of the native reference-shoe path. Transport only the
        // body to the physical unwrapped carrier; no source root drives physics.
        var pivot = new SkateVector(0, SkateBoardPath.FootRadius, -SkateBoardPath.Axle);
        var sourceCarrier = new SkateBoardPose(sample.Board.Point(pivot) - pivot, new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
        if (!SkateBoardPath.Valid(carrier)) return Stop("invalid Impossible carrier");
        var target = SkateCapturePlayback.Transport(sample, sourceCarrier, carrier);
        if (!target.Board.Initialized) return Stop("invalid Impossible source frame");
        var body = target.Pose;
        var la = baseline.LeftAnkle; var lt = baseline.LeftToe; var ra = baseline.RightAnkle; var rt = baseline.RightToe;
        var lu = baselineLeftUp; var ru = baselineRightUp;
        bool frontLeft = stance == SkateStance.Regular;
        var freeBit = frontLeft ? SkateCaptureContacts.LeftFoot : SkateCaptureContacts.RightFoot;
        bool freeApplied = false;
        if (air && path.Active && (sample.Observation.Contacts & freeBit) == 0)
        {
            // Only use a free trajectory where the existing measured support
            // projection accepts it. Steep/inverted projections retain the
            // native free-foot clearance instead of relaxing its sole guard.
            var a = frontLeft ? body.LeftAnkle : body.RightAnkle;
            var nativeAnkle = frontLeft ? la : ra;
            var nativeToe = frontLeft ? lt : rt;
            var t = a + nativeToe - nativeAnkle;
            var u = frontLeft ? lu : ru;
            SkateBoardPose foot; double offset;
            double alignment = SkateVector.Dot(u, board.Up);
            if (alignment > .2 && SkateCaptureFootSupport.TryFootFrame(a, t, u, out foot) &&
                (frontLeft ? SkateFootContacts.LeftFitter : SkateFootContacts.RightFitter).TryOffset(board, foot,
                    SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out offset, out error))
            {
                double end = approved.SourceTime(SkateCapturePhase.Catch, 0);
                double weight = Ease((sourceTime - approved.SourceTime(SkateCapturePhase.Pop, 0)) / .08) *
                    Ease((end - sourceTime) / .08) * Ease((alignment - .2) / .3);
                var shift = (a + board.Up * Math.Max(0, offset + SkateCaptureFootSupport.ContactBand) - nativeAnkle) * weight;
                if (frontLeft) { la += shift; lt += shift; }
                else { ra += shift; rt += shift; }
                freeApplied = weight > 0;
                // Use the native upright sole throughout this hook action.
            }
        }
        if (!SkateCaptureFootSupport.TrySetFeet(rig, body, la, lt, ra, rt, true, out body, out error)) return Stop(error);
        var captured = new SkateCaptureFrame(body, board, Rotation(board), lu, ru, true, sample.Observation,
            target.PelvisCorrection, target.LeftHandCorrection, target.RightHandCorrection, carrier.Up);
        double blend = Ease((now - beganAt) / .1);
        if (landed) blend = Math.Min(blend, approved.RecoveryWeight((now - SkateRide.LandAt) * 1.35));
        if (blend <= 0) return false;
        // The native hook remains fixed even during entry; after completion
        // both native feet own catch/landing while only the body fades out.
        var pins = !air || path.Complete ? SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot :
            frontLeft ? SkateCaptureContacts.RightFoot : SkateCaptureContacts.LeftFoot;
        if (!SkateCapturePlayback.TryBlend(rig, baseline, lu, ru, captured, blend, out frame, out error, false, pins)) return Stop(error);
        if (freeApplied)
        {
            SkateBoardPose freeFrame; double offset = 0;
            bool safe = SkateCaptureFootSupport.TryFootFrame(frontLeft ? frame.Pose.LeftAnkle : frame.Pose.RightAnkle,
                    frontLeft ? frame.Pose.LeftToe : frame.Pose.RightToe, frontLeft ? lu : ru, out freeFrame) &&
                (frontLeft ? SkateFootContacts.LeftFitter : SkateFootContacts.RightFitter).TryOffset(board, freeFrame,
                    SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out offset, out error);
            SkateRiderPose fitted;
            if (safe)
            {
                var shift = board.Up * Math.Max(0, offset + SkateCaptureFootSupport.ContactBand);
                if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose, frontLeft ? shift : default(SkateVector),
                    frontLeft ? default(SkateVector) : shift, true, out fitted, out error)) return Stop(error);
            }
            else if (!SkateCaptureFootSupport.TrySetFeet(rig, frame.Pose,
                baseline.LeftAnkle, baseline.LeftToe, baseline.RightAnkle, baseline.RightToe, true, out fitted, out error))
                return Stop(error ?? "native Impossible foot fallback exceeds measured reach");
            frame = new SkateCaptureFrame(fitted, board, frame.Rotation, lu, ru, true, sample.Observation,
                frame.PelvisCorrection + fitted.Pelvis - frame.Pose.Pelvis, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        if (air && path.Active && (sample.Observation.Contacts & freeBit) == 0)
        {
            // Authored free-front-foot tuck. The rear hook remains native and
            // neither foot inherits the wrapping deck's inverted normal.
            double tuck = Ease(path.Progress / .12) * Ease((1 - path.Progress) / .16);
            var lift = carrier.Up * (.26 * tuck * blend);
            SkateRiderPose tucked;
            if (!SkateCaptureFootSupport.TryShiftFeet(rig, frame.Pose,
                frontLeft ? lift : default(SkateVector), frontLeft ? default(SkateVector) : lift,
                true, out tucked, out error)) return Stop(error);
            frame = new SkateCaptureFrame(tucked, board, frame.Rotation, lu, ru, true, sample.Observation,
                frame.PelvisCorrection + tucked.Pelvis - frame.Pose.Pelvis, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        if (air && (path.Active || path.Complete &&
            (sample.Observation.Contacts & (SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot)) ==
            (SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot)))
        {
            double requested = Ease(path.Progress / .12) * blend;
            SkateRiderPose prepared; SkateVector shift;
            if (!SkateCaptureFootSupport.TryCatchPreparation(rig, frame.Pose, carrier.Up, requested,
                out prepared, out shift, out error, 100, true)) return Stop(error);
            catchPreparationWeight = SkateCaptureFootSupport.ReachedCatchPreparationWeight(rig, prepared, requested, 100);
            frame = new SkateCaptureFrame(prepared, board, frame.Rotation, lu, ru, true, sample.Observation,
                frame.PelvisCorrection + shift, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        if (!air && now - landedAt < .1 && landingFrom.Board.Initialized)
        {
            var start = SkateCapturePlayback.Transport(landingFrom, landingCarrier, carrier);
            if (!start.Board.Initialized || !SkateCapturePlayback.TryBlend(rig, start.Pose, start.LeftSoleUp, start.RightSoleUp,
                frame, Ease((now - landedAt) / .1), out frame, out error, true)) return Stop(error ?? "Impossible landing carrier unavailable");
        }
        if (!air)
        {
            SkateRiderPose absorbed; SkateVector shift;
            if (!SkateCaptureFootSupport.TryLandingAbsorption(rig, frame.Pose, carrier.Up, now - landedAt,
                out absorbed, out shift, out error, catchPreparationWeight, 100, 100, true)) return Stop(error);
            frame = new SkateCaptureFrame(absorbed, board, frame.Rotation, lu, ru, true, sample.Observation,
                frame.PelvisCorrection + shift, frame.LeftHandCorrection, frame.RightHandCorrection, carrier.Up);
        }
        double height = SkateVector.Dot(frame.Pose.Pelvis - carrier.Origin, carrier.Up);
        if (SkateVector.Dot(frame.Pose.PelvisUp, carrier.Up) < .25 || height < .15 || height > 1.5)
            return Stop("Impossible capture does not fit the current upright support");
        previous = frame; previousCarrier = carrier;
        return true;
    }
}
