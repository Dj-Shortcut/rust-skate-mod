using System;
using Shortcut.RustMod;
using UnityEngine;

// Physical surface observations and the optional wall/transition flight. SkateRide remains the
// only movement-body writer; the rendered board and rider consume these same measured frames.
public static class SkateSurfaceRide
{
    private static SkateWallRideState wall;
    private static SkateBodyFlipState flip;
    private static SkateBodyFlipKind heldFlipRequest;
    private static SkateBoardPose wallFrame, recordedFrame;
    private static Collider wallCollider, dropCollider;
    private static int wallColliderId, dropColliderId;
    private static Vector3 wallPoint, wallNormal, flipOffset, flipAxis, recordedPosition, dropPoint, dropNormal;
    private static SkateRiderPose recordedPose;
    private static SkateGrabKind recordedGrab;
    private static float recordedAt = -99f, wallieAt = -99f, dropAt = -99f, dropPresentationAt = -99f, noticeAt = -99f;
    private static uint wallEpoch, flipEpoch, recordedEpoch;
    private static bool wallie, wallQualified, wallFrontside, dropPending, dropVerified, flipFailed, named;
    private static double wallSeconds;
    private static SkateBoardPose releaseFrame;
    private static Vector3 releasePosition, lastWallPosition;
    private static double releaseSeconds = .2;
    private static readonly Vector3[] shownFlipPoints = new Vector3[15];
    private static bool haveShownFlipPoints;
    public static bool Active { get { return wall.Active; } }
    public static bool DisplayWall { get { return wall.Active || releaseSeconds < .2; } }
    public static bool WallPopped { get; private set; }
    public static SkateBoardPose WallFrame { get { return wallFrame; } }
    public static Vector3 WallContactPoint { get { return wallPoint; } }
    public static Vector3 WallContactNormal { get { return wallNormal; } }
    public static SkateBodyFlipState BodyFlip { get { return flip; } }
    public static bool LandingReady { get { return !flipFailed && (!flip.Known || flip.Qualified); } }
    public static bool DropInVerified { get { return dropVerified; } }
    public static SkateDropInFrame DropInMotion { get; private set; }
    public static long LandingBonus
    { get { return named ? 0 : (flip.Qualified ? 250 : 0) + (wallQualified ? 100 + (long)Math.Round(Math.Min(SkateWallTricks.RideMaximumSeconds, wallSeconds) * 80) : 0) + (wallie ? 100 : 0); } }
    public static float WallieWeight { get { return wallie ? SkateRide.Clamp(1 - (Time.realtimeSinceStartup - wallieAt) / .18f, 0, 1) : 0; } }

    public static void Reset()
    {
        wall = default(SkateWallRideState); flip = default(SkateBodyFlipState); heldFlipRequest = SkateBodyFlipKind.None;
        wallFrame = recordedFrame = default(SkateBoardPose); wallCollider = dropCollider = null; wallColliderId = dropColliderId = 0;
        wallPoint = wallNormal = flipOffset = flipAxis = recordedPosition = dropPoint = dropNormal = default(Vector3);
        recordedPose = default(SkateRiderPose); recordedGrab = SkateGrabKind.None;
        recordedAt = wallieAt = dropAt = dropPresentationAt = -99f; wallie = wallQualified = wallFrontside = dropPending = dropVerified = flipFailed = haveShownFlipPoints = named = false; wallSeconds = 0;
        DropInMotion = default(SkateDropInFrame);
        WallPopped = false;
        releaseFrame = default(SkateBoardPose); releasePosition = lastWallPosition = default(Vector3); releaseSeconds = .2;
    }

    private static bool Controls { get { return SkateKeys.Scripted || SkateKeys.ControlsActive; } }
    private static bool OtherAction { get { return SkateActions.Owns || SkateOldschoolRide.Active || SkateRide.BoardPath.Known || SkateRide.FootPlanted || SkateRide.Handplant || SkateRide.GroundGrab.Active; } }
    private static SkateVector V(Vector3 value) { return new SkateVector(value.x, value.y, value.z); }
    private static Vector3 V(SkateVector value) { return new Vector3((float)value.X, (float)value.Y, (float)value.Z); }
    private static double Seconds(float dt) { return dt == .05f ? .05 : dt; }
    private static SkateSurfaceContact Contact(RaycastHit hit)
    { return new SkateSurfaceContact(true, V(hit.m_Point), V(hit.m_Normal), SkateParks.IsRamp(hit), SkateParks.IsCoping(hit)); }
    private static bool Same(Collider collider, Collider captured, int id)
    { return collider != null && captured != null && collider == captured && collider.GetInstanceID() == id; }
    private static void Notice(string message)
    { var now = Time.realtimeSinceStartup; if (now - noticeAt >= .5f) { noticeAt = now; SkateHud.Notice(message); } }

    public static bool TryGround(Vector3 pos, float dt, bool wallRequest, bool dropRequest)
    {
        ConfirmDropIn(pos);
        if (SkateRide.Mode != RideMode.Ground || !SkateRide.Grounded || !Controls || OtherAction || DisplayWall || flip.Known || SkateRide.RevertLeft != 0) return false;
        try
        {
            var incoming = SkateRide.Tangent * SkateRide.Speed;
            var travel = new Vector3(incoming.x, 0, incoming.z);
            RaycastHit side;
            if (wallRequest && Time.realtimeSinceStartup - wallieAt > .5f && travel.sqrMagnitude >= 4 &&
                FindWall(pos, incoming, false, out side, true))
            {
                // Query just behind the actual face. The top must belong to that same obstacle.
                var inside = side.m_Point - side.m_Normal * .12f;
                RaycastHit top;
                if (SkateParks.Ray(new Vector3(inside.x, pos.y + 1, inside.z), Vector3.down, out top, 1.05f, SkateRide.GroundMask) &&
                    side.collider != null && top.collider != null && side.collider == top.collider && side.collider.GetInstanceID() == top.collider.GetInstanceID())
                {
                    SkateWallieResult launched; string error;
                    if (SkateWallTricks.TryWallie(V(pos), V(incoming), Contact(side), Contact(top), true, true, out launched, out error) && launched.Launched)
                    {
                        SkateRide.ActionLaunch(V(launched.Velocity), dt);
                        if (SkateRide.Mode != RideMode.Air) return false;
                        wallie = true; wallieAt = Time.realtimeSinceStartup; wallPoint = V(launched.ContactPoint); wallNormal = V(launched.Normal);
                        return true;
                    }
                }
            }
            if (dropRequest)
            {
                RaycastHit deck;
                // A rolling fakie entry commits toward its actual travel, not the physical
                // nose pointing away from that lip. At rest the visible nose selects the lip.
                var heading = travel.sqrMagnitude > .09f ? travel.normalized : SkateRide.Dir(SkateRide.DeckYaw);
                if (!SkateParks.Ray(pos + Vector3.up * .2f, Vector3.down, out deck, .55f, SkateRide.GroundMask)) return false;
                // Try nearest first: there must be a real descending transition beyond the lip.
                for (int i = 0; i < 3; i++)
                {
                    RaycastHit face;
                    var ahead = pos + heading * (.3f + .2f * i);
                    if (!SkateParks.Ray(ahead + Vector3.up * .2f, Vector3.down, out face, 1.4f, SkateRide.GroundMask)) continue;
                    SkateDropInResult entry; string error;
                    if (!SkateWallTricks.TryDropIn(V(pos), V(incoming), V(heading), Contact(deck), Contact(face), true, true, out entry, out error) || !entry.Began) continue;
                    var collider = face.collider;
                    if (collider == null) continue;
                    var id = collider.GetInstanceID();
                    // The body still stands on the top. A near-vertical face's tangent has
                    // almost no horizontal component; using it now would stop before the lip.
                    // Commit a bounded forward roll, retaining the observed descent. Ordinary
                    // ground contact owns the top until the body actually rolls beyond it.
                    var committed = heading * Math.Max(1.5f, Vector3.Dot(incoming, heading));
                    committed.y = Math.Min(0, (float)entry.Velocity.Y);
                    SkateRide.ActionLaunch(committed, dt, false);
                    if (SkateRide.Mode != RideMode.Air) return false;
                    dropPending = true; dropVerified = false; dropAt = Time.realtimeSinceStartup; dropCollider = collider; dropColliderId = id;
                    dropPoint = V(entry.SupportPoint); dropNormal = V(entry.Normal);
                    return true;
                }
                Notice("Drop-in needs a supported lip facing a descending transition");
            }
        }
        catch (Exception) { Notice("Surface contact could not be verified"); }
        return false;
    }

    public static void ConfirmDropIn(Vector3 pos)
    {
        UpdateDropInMotion(pos);
        if (!dropPending || SkateRide.Mode != RideMode.Ground) return;
        if (Time.realtimeSinceStartup - dropAt > 1.2f || dropCollider == null || !SkateRide.On || (pos - dropPoint).sqrMagnitude > 6.25f) { dropPending = false; return; }
        try
        {
            RaycastHit hit = default(RaycastHit);
            if (SkateRide.TryObservedRamp(pos, out hit) &&
                Same(hit.collider, dropCollider, dropColliderId) && SkateParks.IsRamp(hit) && !SkateParks.IsCoping(hit) &&
                hit.m_Normal.y > .015f && hit.m_Normal.y < .9f && Vector3.Dot(hit.m_Normal, dropNormal) >= .5f)
            {
                dropVerified = true; dropPending = false; dropPresentationAt = Time.realtimeSinceStartup;
                UpdateDropInMotion(pos); Notice("Drop-in");
            }
        }
        catch (Exception) { dropPending = false; }
    }

    private static void UpdateDropInMotion(Vector3 pos)
    {
        DropInMotion = default(SkateDropInFrame);
        if (dropPresentationAt < 0) return;
        var elapsed = Time.realtimeSinceStartup - dropPresentationAt;
        if (!SkateRide.On || SkateRide.Mode != RideMode.Ground || !SkateRide.Grounded || !SkateRide.OnRamp ||
            OtherAction || SkateRide.PushPhase > 0 || SkateRide.RevertLeft != 0 || DisplayWall || flip.Known ||
            elapsed < 0 || elapsed >= SkateDropInPresentation.Duration)
        { dropPresentationAt = -99f; return; }
        try
        {
            RaycastHit hit;
            SkateDropInFrame frame; string error;
            // Every displayed sample still requires current native ramp support. Adjacent ramp
            // segments may change collider; the presentation never attaches or moves the body.
            if (!SkateRide.TryObservedRamp(pos, out hit) ||
                !SkateDropInPresentation.TrySample(elapsed, Contact(hit), out frame, out error) || !frame.Initialized)
            { dropPresentationAt = -99f; return; }
            DropInMotion = frame;
        }
        catch (Exception) { dropPresentationAt = -99f; }
    }

    public static bool ConsumeDropIn()
    { var verified = dropVerified; dropVerified = false; return verified; }

    // Naming/award consumes the completed flight first. Recovery clears physical ownership so
    // a later ground action can start; the pending observed drop-in remains independently owned.
    public static void Landed()
    {
        flip = default(SkateBodyFlipState); flipFailed = haveShownFlipPoints = false;
        wall = default(SkateWallRideState); if (!DisplayWall) wallFrame = default(SkateBoardPose); wallCollider = null; wallColliderId = 0;
    }

    // Display recovery keeps no wall attachment or scoring. It follows the actual movement
    // body while the departed measured frame returns to the ordinary upright carrier frame.
    public static void AdvanceDisplay(Vector3 pos, float dt)
    {
        if (wall.Active || releaseSeconds >= .2) return;
        releaseSeconds = Math.Min(.2, releaseSeconds + Seconds(dt));
        if (releaseSeconds >= .2) { wallFrame = default(SkateBoardPose); return; }
        var share = releaseSeconds / .2; share = share * share * (3 - 2 * share);
        var up = (V(releaseFrame.Up) * (float)(1 - share) + Vector3.up * (float)share).normalized;
        var targetForward = SkateRide.Dir(SkateRide.DeckYaw);
        var forward = V(releaseFrame.Forward) * (float)(1 - share) + targetForward * (float)share;
        forward -= up * Vector3.Dot(forward, up);
        if (forward.sqrMagnitude < .0001f) forward = Vector3.Cross(up, SkateRide.Dir(SkateRide.DeckYaw + 90));
        forward = forward.normalized;
        var carried = V(releaseFrame.Origin) + pos - releasePosition;
        var target = pos + Vector3.up * (SkateBoard.ShowLift + SkateBoard.Pop + RiderRig.DeckTop);
        wallFrame = ClearReleaseGround(new SkateBoardPose(V(carried * (float)(1 - share) + target * (float)share), V(forward), V(up)), pos);
    }

    private static SkateBoardPose ClearReleaseGround(SkateBoardPose frame, Vector3 pos)
    {
        RaycastHit ground;
        if (!SkateRide.TryObservedGround(pos, out ground)) return frame;
        var normal = V(ground.m_Normal); var point = V(ground.m_Point); double minimum = double.MaxValue;
        // Bounds of the detailed authored deck slab/kicks and four wheel spheres. A common
        // display-only lift keeps rider and deck together; the movement body stays untouched.
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            minimum = Math.Min(minimum, SkateVector.Dot(frame.Point(new SkateVector(x * .11, -.04, z * .4)) - point, normal));
            minimum = Math.Min(minimum, SkateVector.Dot(frame.Point(new SkateVector(x * .11, .06, z * .4)) - point, normal));
            minimum = Math.Min(minimum, SkateVector.Dot(frame.Point(new SkateVector(x * .082, -.065, z * .245)) - point, normal) - .027);
        }
        return minimum >= .003 ? frame : new SkateBoardPose(frame.Origin + normal * (.003 - minimum), frame.Forward, frame.Up);
    }

    // Returns true for a continuing wallride or its exit step. The pure step has already applied
    // gravity while attached; the caller must not apply ordinary Air gravity a second time.
    public static bool StepWall(Vector3 pos, Vector3 incoming, float dt, bool requested, bool pop, out Vector3 outgoing)
    {
        outgoing = incoming; var previousFrame = wallFrame; if (!DisplayWall) wallFrame = default(SkateBoardPose); WallPopped = false;
        var wasActive = wall.Active;
        var controls = Controls && SkateRide.Mode == RideMode.Air && (!wasActive || wallEpoch == SkateKeys.InputEpoch);
        if (OtherAction || flip.Known || !wasActive && SkateRide.Rotating) requested = false;
        try
        {
            RaycastHit hit = default(RaycastHit);
            var found = (requested || wasActive) && FindWall(pos, incoming, wasActive, out hit);
            // A wall cannot silently change into another coplanar object while bearing weight.
            if (found && wasActive && !Same(hit.collider, wallCollider, wallColliderId)) found = false;
            var observed = found ? Contact(hit) : default(SkateSurfaceContact);
            var toe = SkateRide.Dir(SkateRide.NoseYaw + 90 * SkateRide.StanceSign);
            var frontside = found && Vector3.Dot(hit.m_Point - pos, toe) > 0;
            SkateWallRideResult next; string error;
            if (!SkateWallTricks.TryRide(wall, V(pos), V(incoming), observed, frontside, requested, pop, controls,
                SkateRide.Grounded, Seconds(dt), out next, out error))
            { if (wasActive) SkateRide.Bail(); wall = default(SkateWallRideState); return wasActive; }
            if (wasActive) wallSeconds = Math.Max(wallSeconds, wall.Seconds);
            wall = next.State;
            if (next.Began)
            {
                wallCollider = hit.collider; wallColliderId = wallCollider.GetInstanceID(); wallEpoch = SkateKeys.InputEpoch;
                wallFrontside = wall.Frontside; wallSeconds = wall.Seconds;
                SkateRide.TrickAir = true; SkateRide.Jumped = true;
            }
            if (wall.Active)
            {
                wallSeconds = wall.Seconds; wallQualified |= wallSeconds >= .12;
                outgoing = V(next.Velocity); wallPoint = V(next.ContactOrigin); wallNormal = V(wall.Normal);
                // The body's actual measured capsule may be wider than the pure default. Keep
                // the same bounded attachment speed without continually pushing it into a wall.
                var gap = Vector3.Dot(pos - wallPoint, wallNormal);
                var correction = SkateRide.Clamp((SkateRide.SurfaceBodyGap - gap) / dt,
                    -(float)SkateWallTricks.RideMaximumCorrection, (float)SkateWallTricks.RideMaximumCorrection);
                outgoing += wallNormal * (correction - Vector3.Dot(outgoing, wallNormal));
                var nose = wall.Travel;
                if (SkateVector.Dot(nose, V(SkateRide.Dir(SkateRide.DeckYaw))) < 0) nose *= -1;
                wallFrame = new SkateBoardPose(next.ContactOrigin + wall.Normal * RiderRig.DeckTop, nose, wall.Normal);
                lastWallPosition = pos; releaseSeconds = .2;
                SkateRide.ViewNormal = wallNormal;
                return true;
            }
            if (next.Ended)
            {
                WallPopped = next.Popped;
                if (previousFrame.Initialized)
                {
                    releaseFrame = new SkateBoardPose(previousFrame.Origin + V(pos - lastWallPosition), previousFrame.Forward, previousFrame.Up);
                    releasePosition = pos; releaseSeconds = 0; wallFrame = ClearReleaseGround(releaseFrame, pos);
                }
                outgoing = V(next.Velocity);
                // An exit consumes this step too; the attached step didn't apply gravity to it.
                outgoing.y -= SkateRide.AirGravity * dt;
                wallCollider = null; wallColliderId = 0;
                return true;
            }
        }
        catch (Exception) { if (wasActive) SkateRide.Bail(); wall = default(SkateWallRideState); return wasActive; }
        return false;
    }

    private static bool FindWall(Vector3 pos, Vector3 velocity, bool continuing, out RaycastHit nearest, bool forwardOnly = false)
    {
        nearest = default(RaycastHit); var best = float.MaxValue; var found = false;
        var forward = new Vector3(velocity.x, 0, velocity.z);
        if (forward.sqrMagnitude < .001f) forward = SkateRide.Dir(SkateRide.Yaw); else forward = forward.normalized;
        var side = Vector3.Cross(Vector3.up, forward);
        for (int direction = 0; direction < (continuing || forwardOnly ? 1 : 3); direction++)
        {
            var toward = continuing ? -wallNormal : direction == 0 ? forward : direction == 1 ? side : -side;
            for (int height = 0; height < 2; height++)
            {
                RaycastHit hit;
                if (!SkateParks.Ray(pos + Vector3.up * (height == 0 ? .04f : .25f), toward, out hit, .6f, SkateRide.GroundMask) ||
                    hit.collider == null || SkateParks.IsRamp(hit) || SkateParks.IsCoping(hit) || Math.Abs(hit.m_Normal.y) > .3f || hit.m_Distance >= best) continue;
                nearest = hit; best = hit.m_Distance; found = true;
            }
        }
        return found;
    }

    // Called only after a successful measured pose, before the optional rigid body flip map.
    public static void Record(SkateRiderPose pose, SkateBoardPose frame, SkateGrabKind grab)
    {
        if (!frame.Initialized || !SkateBoardPath.Valid(frame)) return;
        recordedPose = pose; recordedFrame = frame; recordedGrab = grab; recordedPosition = SkateRide.Position;
        recordedAt = Time.realtimeSinceStartup; recordedEpoch = SkateKeys.InputEpoch;
        if (flip.Known)
        {
            for (int i = 0; i < shownFlipPoints.Length; i++) shownFlipPoints[i] = FlipPoint(BodyPoint(pose, frame, i));
            haveShownFlipPoints = true;
        }
    }

    public static void TickFlip(Vector3 pos, float dt, float remaining, SkateBodyFlipKind requested)
    {
        var fresh = requested != SkateBodyFlipKind.None && heldFlipRequest == SkateBodyFlipKind.None;
        heldFlipRequest = requested;
        var controls = Controls && (!flip.Known || flipEpoch == SkateKeys.InputEpoch);
        var holding = controls && recordedEpoch == SkateKeys.InputEpoch && Time.realtimeSinceStartup - recordedAt < .1f &&
            recordedGrab != SkateGrabKind.None && recordedGrab == SkateRide.Grab && RiderRig.Contact.GrabWeight >= .99;
        bool left, right;
        SkateRider.GrabHands(SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular, SkateRide.TailFirst, recordedGrab, out left, out right);
        holding &= left || right;
        if (!flip.Known && (!fresh || OtherAction || wall.Active || SkateRide.Rotating || !SkateRide.Jumped)) requested = SkateBodyFlipKind.None;
        if (!flip.Known && fresh && requested != SkateBodyFlipKind.None && !holding) Notice("Body Flip needs a held grab with actual hand contact");
        var before = flip; SkateBodyFlipResult next; string error;
        if (!SkateWallTricks.TryBodyFlip(before, requested, SkateRide.Mode == RideMode.Air, holding, controls,
            Math.Max(0, Math.Min(60, remaining)), Seconds(dt), out next, out error)) { flipFailed = true; SkateRide.Bail(); return; }
        if (next.Began)
        {
            flipOffset = V(recordedPose.Pelvis) - recordedPosition;
            flipAxis = Vector3.Cross(Vector3.up, SkateRide.Dir(SkateRide.NoseYaw)).normalized;
            flipEpoch = SkateKeys.InputEpoch;
            if (!ClearTurn(pos, next.State, before, true)) { Notice("Not enough room for a body flip"); return; }
            if (next.TooLate) Notice("Too late to finish the body flip");
        }
        else if (before.Active && !ClearTurn(pos, next.State, before, false))
        { flipFailed = true; SkateRide.Bail(); return; }
        flip = next.State;
    }

    private static bool ClearTurn(Vector3 pos, SkateBodyFlipState next, SkateBodyFlipState before, bool fullTurn)
    {
        try
        {
            var samples = fullTurn ? 12 : 1;
            var duration = fullTurn ? SkateWallTricks.BodyFlipDuration : (next.Progress - before.Progress) * SkateWallTricks.BodyFlipDuration;
            for (int point = 0; point < shownFlipPoints.Length; point++)
            {
                var basePoint = BodyPoint(recordedPose, recordedFrame, point) + pos - recordedPosition;
                var radius = point == 0 ? .18f : point < 3 ? .2f : .1f;
                var previous = fullTurn ? basePoint : haveShownFlipPoints ? shownFlipPoints[point] : TurnPoint(basePoint, pos + flipOffset, (float)before.Degrees);
                for (int sample = 1; sample <= samples; sample++)
                {
                    var fraction = (double)sample / samples;
                    var seconds = duration * fraction;
                    var translation = fullTurn ? SkateRide.Dir(SkateRide.Yaw) * (SkateRide.Speed * (float)seconds) + Vector3.up * (SkateRide.VSpeed * (float)seconds - .5f * SkateRide.CurrentAirGravity * (float)(seconds * seconds)) : Vector3.zero;
                    var angle = fullTurn ? (next.Kind == SkateBodyFlipKind.Back ? -360 : 360) * fraction : next.Degrees;
                    var target = TurnPoint(basePoint, pos + flipOffset, (float)angle) + translation;
                    var path = target - previous; var length = path.magnitude;
                    RaycastHit hit;
                    // Padding covers the short rotation arc beyond this chord. These actual
                    // swept body samples are refreshed every physics step during inversion.
                    if (length > .0001f && SkateParks.Sphere(previous, radius + .05f, path / length, out hit, length, SkateRide.GroundMask)) return false;
                    previous = target;
                }
            }
            return true;
        }
        catch (Exception) { return false; }
    }

    private static Vector3 BodyPoint(SkateRiderPose pose, SkateBoardPose frame, int index)
    {
        switch (index)
        {
            case 0: return V(pose.Head);
            case 1: return V(pose.Chest);
            case 2: return V(pose.Pelvis);
            case 3: return V(pose.LeftHand);
            case 4: return V(pose.RightHand);
            case 5: return V(pose.LeftAnkle);
            case 6: return V(pose.RightAnkle);
            case 7: return V(pose.LeftKnee);
            case 8: return V(pose.RightKnee);
            case 9: return V(pose.LeftElbow);
            case 10: return V(pose.RightElbow);
            // Conservative corners cover the detailed authored 0.8m x0.204m deck, its kick
            // and the trucks/wheels below it, rather than checking only the rider's ankles.
            default: return V(frame.Point(new SkateVector((index & 1) == 0 ? -.102 : .102, .05, index < 13 ? -.4 : .4)));
        }
    }
    private static Vector3 TurnPoint(Vector3 point, Vector3 pivot, float degrees)
    { return pivot + SkateRide.Turn(degrees, flipAxis) * (point - pivot); }
    public static Vector3 FlipPoint(Vector3 point)
    { return !flip.Known ? point : TurnPoint(point, SkateRide.Position + flipOffset, (float)flip.Degrees); }
    public static Vector3 FlipDirection(Vector3 direction)
    { return !flip.Known ? direction : SkateRide.Turn((float)flip.Degrees, flipAxis) * direction; }
    public static SkateBoardPose FlipFrame(SkateBoardPose frame)
    {
        if (!flip.Known || !frame.Initialized) return frame;
        return new SkateBoardPose(V(FlipPoint(V(frame.Origin))), V(FlipDirection(V(frame.Forward))), V(FlipDirection(V(frame.Up))));
    }
    public static string Name(string original, bool landed)
    {
        if (!landed || named) return original;
        // TryAdd/reverts also emit Landed; one flight's label belongs to its first landing only.
        named = true;
        var name = original ?? "";
        if (flip.Qualified) name = (flip.Kind == SkateBodyFlipKind.Front ? "Front Flip" : "Back Flip") + (name.Length > 0 ? " + " + name : "");
        if (wallQualified) name = (wallFrontside ? "Frontside Wallride" : "Backside Wallride") + (name.Length > 0 ? " + " + name : "");
        if (wallie) name = "Wallie" + (name.Length > 0 && name != "Ollie" ? " + " + name : "");
        if (dropVerified) name = "Drop-in" + (name.Length > 0 && name != "Ollie" ? " + " + name : "");
        return name;
    }
}
