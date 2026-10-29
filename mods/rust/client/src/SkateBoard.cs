using Shortcut.RustMod;
// Rust/Standard is the game's own lit shader: a plugin-made mesh drawn with it takes the world's
// light, casts a shadow and goes dark at night. It reads colours from a texture, not from the
// vertices. Hidden/Internal-Colored draws vertex colours without light and is the fallback.
// The mesh setters can be missing from a client build; primitives are the fallback for those.
using System;
using Il2CppInterop.Runtime;
using UnityEngine;

public static class SkateBoard
{
    public const float TipDegrees = 15f, Axle = 0.23f;
    public const string LitShader = "Rust/Standard", PlainShader = "Hidden/Internal-Colored";
    public const float Gloss = 0.2f, BailTurn = 0.5f, BailBehind = 0.9f, BailRest = 0.045f, PopMost = 0.22f, LieSeconds = 6f;
    // Shared wheel radius and the retained legacy truck-turn API constant.
    public const float WheelRadius = 0.027f, TruckTurn = (float)SkateBoardTruckMotion.SteeringRatio;
    public static string Drawn = "";
    public static float ShowLift, Tip, Pop, WheelSpin;
    // Held: the board is where the rider's feet hold it (a handplant), at HeldAt with its grip toward HeldUp.
    public static bool Held;
    public static SkateBoardPose HeldFrame;
    public static Vector3 HeldAt, HeldNose = Vector3.forward, HeldUp = Vector3.up;
    private static Transform[] trucks = new Transform[0], wheels = new Transform[0], bushings = new Transform[0];
    private static Vector3[] wheelAxes = new Vector3[0];
    private static Vector3[] truckOrigins = new Vector3[0];
    private static SkateBoardBushing[] bushingShapes = new SkateBoardBushing[0];
    private static Mesh[] bushingMeshes = new Mesh[0];
    private static Vector3[][] bushingVertices = new Vector3[0][], bushingNormals = new Vector3[0][];
    private static float[] truckSides = new float[0];
    private static int[] bushingTrucks = new int[0];
    private static readonly SkateBoardTruckState[] truckMotion = new SkateBoardTruckState[2]; // rear, front
    private static bool truckRigRiding;
    private static float wheelVelocity; // degrees/second, carried through an airborne board rotation
    private static SkateBoardWheelContact wheelContact;
    private static Vector3 contactOffset, observedPoint, observedNormal;
    private static bool contactCached, observedPlane;
    private static float motionAt = -1f, motionSeconds;
    private static int supportStep;
    private static bool displayTruckRig;
    private static float displayAhead = 1;
    public static bool TruckPlaneSupported { get; private set; }
    public static Vector3 TruckPlaneOffset { get { return contactOffset; } }
    public static SkateBoardTruckState RearTruck { get { return truckMotion[0]; } }
    public static SkateBoardTruckState FrontTruck { get { return truckMotion[1]; } }
    public static string TruckContactStatus { get; private set; } = "Truck plane not prepared";
    private static float followedAt = -1f;
    private static Vector3 bailFrom, bailTravel, bailNose, bailUp;
    private static float bailStamp = -1f, bailRoll, lieUntil = -1f;
    private static bool lying;
    private static void Say(string m) { Out.Say("SKATE " + m); }

    public static GameObject Build()
    {
        trucks = new Transform[0]; wheels = new Transform[0];
        bushings = new Transform[0]; bushingTrucks = new int[0];
        bushingShapes = new SkateBoardBushing[0]; bushingMeshes = new Mesh[0]; bushingVertices = bushingNormals = new Vector3[0][];
        truckMotion[0] = truckMotion[1] = default(SkateBoardTruckState); truckRigRiding = false;
        followedAt = -1f; WheelSpin = wheelVelocity = 0;
        wheelContact = null; contactOffset = Vector3.zero; contactCached = observedPlane = false; motionAt = -1f;
        SkateFootContacts.SetBoard(null, RiderRig.DeckTop);
        try { var made = FromParts(); Say("BOARD from the shared mesh in parts: " + trucks.Length + " trucks, " + wheels.Length + " wheels, " + bushings.Length + " bushings"); return made; }
        catch (Exception e) { Say("board in parts unavailable (" + e.GetType().Name + ": " + e.Message + "); using the one-piece mesh"); trucks = new Transform[0]; wheels = new Transform[0]; bushings = new Transform[0]; }
        try { var made = FromMesh(); Say("BOARD from the shared mesh"); return made; }
        catch (Exception e) { Say("shared mesh unavailable (" + e.GetType().Name + ": " + e.Message + "); using primitives"); return FromPrimitives(); }
    }

    private static Vector3 RawContact(Transform playerT) { return playerT.position + Vector3.up * (ShowLift + Pop); }
    public static Vector3 Contact(Transform playerT)
    {
        if (!contactCached)
        {
            contactCached = true; TruckPlaneSupported = false;
            SkateVector offset; string error = null;
            if (wheelContact != null && observedPlane && supportStep == SkateRide.Steps && OrdinaryPlaneEligible() &&
                (playerT.position - SkateRide.Position).sqrMagnitude <= .5625f &&
                wheelContact.TryOffset(PoseAt(playerT, false, RawContact(playerT)), RiderRig.DeckTop, truckMotion[0], truckMotion[1],
                    V(observedPoint), V(observedNormal), .04, out offset, out error))
            {
                contactOffset = V(offset); TruckPlaneSupported = true; TruckContactStatus = "Observed flat wheel plane";
            }
            else
            {
                contactOffset *= (float)Math.Exp(-14 * motionSeconds);
                TruckContactStatus = error ?? "Wheel plane unavailable outside ordinary flat support";
            }
        }
        return RawContact(playerT) + contactOffset;
    }

    // A manual lifts the end that leads: the board, and the rider's footing with it, pitches about
    // the ground under the trailing axle. A nose manual (Tip below zero) lifts the end that trails,
    // about the leading axle.
    // A grab brings the board up to the rider and tweaks it: each grab tilts it its own way, the
    // edge or end in the hand coming up (toe edge up for an indy, heel edge for a melon, the nose for
    // a nose grab), more as the grab is held. Degrees: toe edge up, nose up.
    public const float GrabRise = 0.3f;
    // A seatbelt takes the tail's toe corner (toe edge up, nose down), a crail the nose's (nose up);
    // a nosebone pushes the nose away with the front leg (nose down), a tailbone the tail (nose up).
    public static float GrabFold;
    // A no comply or a boneless: the front foot is down beside the board and the back foot kicks
    // the tail, so the nose comes up about the back foot and the board rises with it. Plant (0..1)
    // is set by the rider's pose each frame.
    public const float PlantPitch = 38f, PlantLift = 0.12f, PlantPivot = 0.2f;
    public static float Plant;
    // The board's own tweak in a grab, about the middle of the deck; the rider's frame stays upright
    // (the feet are near the middle, where the tweak moves the deck least).
    public static Quaternion GrabTweak(Vector3 up)
    {
        var kind = RiderRig.ShownGrab;
        if (SkateRide.Mode != RideMode.Air || kind == SkateGrabKind.None || !SkateGrabCatalog.Valid(kind) || GrabFold <= 0.001f) return Quaternion.identity;
        var grab = SkateGrabCatalog.Get(kind);
        var nose = SkateRide.Dir(SkateRide.DeckYaw); nose = nose - up * Vector3.Dot(nose, up);
        if (nose.sqrMagnitude < 0.0001f) return Quaternion.identity;
        nose = nose.normalized;
        var toe = Vector3.Cross(up, nose);
        return SkateRide.Turn(-(float)grab.Roll * GrabFold, nose) * SkateRide.Turn(-(float)grab.Pitch * GrabFold, toe) * SkateRide.Turn((float)grab.Yaw * GrabFold, up);
    }

    // A grind or a stall holds the deck its own way (SkateRider.GrindDeck): eased toward it while
    // grinding, back to flat after. Degrees, the leading end up and the toe edge up.
    public static float GrindPitch, GrindRoll;
    public static int SupportTruck { get; private set; }
    public static float SupportWeight { get { return SkateRide.Clamp(Math.Max(Math.Abs(GrindPitch), Math.Abs(GrindRoll)) / 20f, 0f, 1f); } }
    public static void PrepareSupport()
    {
        if (!SkateRide.On || SkateRide.Mode == RideMode.Bail) SupportTruck = 0;
        else if (SkateRide.Mode == RideMode.Grind) SupportTruck = SkateGrinds.Truck(SkateRide.GrindKind);
        else if (Math.Abs(GrindPitch) < 0.02f && Math.Abs(GrindRoll) < 0.02f) SupportTruck = 0;
        PrepareTrucks();
    }

    private static bool OrdinaryPlaneEligible()
    {
        return SkateRide.On && SkateRide.Mode == RideMode.Ground && !SkateRide.OnRamp && !SkateRide.Frozen &&
            !SkateRiggingShowcase.Running && !Scenarios.Active && !Held && !SkateRide.Handplant &&
            !SkateOldschoolRide.Active && !SkateActions.Owns && !SkateRide.BoardPath.Known &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && !SkateRide.GroundGrab.Active &&
            !SkateRide.Manual && !SkateRide.NoseManual && SkateRide.PushPhase <= .001f &&
            SkateRide.Powerslide <= .001f && SkateRide.RevertLeft == 0 &&
            Math.Abs(Vector3.Dot(SkateRide.Dir(SkateRide.Yaw), SkateRide.Dir(SkateRide.DeckYaw))) >= .9995f &&
            Math.Abs(Tip) < .001f && Math.Abs(GrindPitch) < .01f && Math.Abs(GrindRoll) < .01f && Math.Abs(ShowLift) < .000001f;
    }

    // Existing frame hook runs before RiderRig.Frame and Follow. State advances
    // here only; all later Contact/PoseFor queries share one fitted translation.
    private static void PrepareTrucks()
    {
        contactCached = false; observedPlane = false; TruckPlaneSupported = false;
        if (!SkateRide.On || SkateRide.Mode == RideMode.Bail)
        { if (truckRigRiding) ResetTruckMotion(); truckRigRiding = false; return; }
        if (!truckRigRiding) { ResetTruckMotion(); truckRigRiding = true; }
        float now = Time.realtimeSinceStartup;
        motionSeconds = motionAt < 0 ? 0 : SkateRide.Clamp(now - motionAt, 0, .1f); motionAt = now;
        RaycastHit hit;
        if (wheelContact != null && OrdinaryPlaneEligible() && SkateRide.TryObservedGround(SkateRide.Position, out hit) &&
            hit.m_Normal.y >= .9995f && Vector3.Dot(hit.m_Normal, SkateRide.ViewNormal) >= .9995f)
        { observedPlane = true; observedPoint = hit.m_Point; observedNormal = hit.m_Normal; supportStep = SkateRide.Steps; }
        if (observedPlane)
        {
            displayTruckRig = true;
            // These are air-only display offsets. On confirmed ordinary ground,
            // retain neither a floating board nor an equal/opposite camera offset.
            Pop = AirPitch = 0;
            displayAhead = Vector3.Dot(SkateRide.Dir(SkateRide.Yaw), SkateRide.Dir(SkateRide.DeckYaw)) < 0 ? -1 : 1;
        }
        else if (SkateRide.Mode != RideMode.Air || Held || SkateOldschoolRide.Active || SkateActions.Owns) displayTruckRig = false;
        bool steering = SkateRide.Mode == RideMode.Ground && !Held && !SkateOldschoolRide.Active && !SkateActions.Owns;
        float since = now - SkateRide.LandAt;
        double settle = observedPlane && since >= 0 && since < .3f ? 1 - since / .3 : 0;
        double landing = settle * settle * SkateRide.Clamp(SkateRide.LandImpact / 8, 0, 1);
        var rear = truckMotion[0]; var front = truckMotion[1];
        for (int side = 0; side < 2; side++)
        {
            SkateBoardTruckState next; string error;
            if (SkateBoardTruckMotion.TryStep(truckMotion[side], SkateRide.Clamp(SkateRide.Lean * displayAhead, -20, 20), steering,
                observedPlane ? 1 : 0, landing, motionSeconds, out next, out error)) truckMotion[side] = next;
            else { truckMotion[side] = default(SkateBoardTruckState); TruckContactStatus = error; }
        }
        for (int i = 0; i < bushingShapes.Length; i++)
        {
            SkateTruckPose pose; string error;
            if (!SkateBoardTruckKinematics.TryPose(bushingShapes[i].Side, truckMotion[bushingTrucks[i]], out pose, out error) ||
                !bushingShapes[i].TryUpdate(pose, out error))
            {
                truckMotion[0] = rear; truckMotion[1] = front; TruckContactStatus = error;
                // Restore previously accepted geometry before any body/board query.
                for (int j = 0; j < bushingShapes.Length; j++)
                    if (SkateBoardTruckKinematics.TryPose(bushingShapes[j].Side, truckMotion[bushingTrucks[j]], out pose, out error)) bushingShapes[j].TryUpdate(pose, out error);
                break;
            }
        }
    }
    private static SkateBoardPose detached;
    private static Vector3 detachedVelocity;
    private static float detachedAt, detachedUntil;
    public static void ReleaseIndependent(SkateBoardPose frame, Vector3 motion)
    {
        if (!frame.Initialized || !SkateBoardPath.Valid(frame)) return;
        detached = frame; detachedVelocity = motion; detachedAt = Time.realtimeSinceStartup;
        detachedUntil = detachedAt + LieSeconds; rollingOut = false; lieUntil = detachedUntil;
    }
    private static bool FollowIndependent(GameObject board, float now)
    {
        if (!detached.Initialized || now >= detachedUntil) { detached = default(SkateBoardPose); return false; }
        var dt = SkateRide.Clamp(now - detachedAt, 0, .05f); detachedAt = now;
        var from = V(detached.Origin); var to = from + detachedVelocity * dt - Vector3.up * (SkateRide.AirGravity * dt * dt * .5f);
        detachedVelocity.y -= SkateRide.AirGravity * dt;
        try
        {
            RaycastHit hit; var travel = to - from; var length = travel.magnitude;
            if (length > .00001f && SkateParks.Sphere(from, .04f, travel / length, out hit, length, SkateRide.GroundMask) && hit.m_Normal.y < .5f)
            { to = from; detachedVelocity = Vector3.zero; }
            var top = Math.Max(from.y, to.y) + .15f;
            if (SkateParks.Ray(new Vector3(to.x, top, to.z), Vector3.down, out hit, top - to.y + .35f, SkateRide.GroundMask) && hit.m_Normal.y >= .5f &&
                to.y <= hit.m_Point.y + RiderRig.DeckTop)
            {
                to = hit.m_Point + hit.m_Normal * RiderRig.DeckTop;
                detachedVelocity -= hit.m_Normal * Math.Min(0, Vector3.Dot(detachedVelocity, hit.m_Normal));
                detachedVelocity *= Math.Max(0, 1 - dt * 2);
                var forward = OnFace(hit.m_Normal, V(detached.Forward));
                detached = new SkateBoardPose(V(to), V(forward), V(hit.m_Normal));
            }
            else detached = new SkateBoardPose(V(to), detached.Forward, detached.Up);
        }
        catch { detachedVelocity = Vector3.zero; }
        board.transform.rotation = Quaternion.LookRotation(V(detached.Forward), V(detached.Up));
        board.transform.position = V(detached.Origin) - V(detached.Up) * RiderRig.DeckTop;
        if (!board.activeSelf) board.SetActive(true);
        return true;
    }
    public static void BeginRide()
    {
        ResetTruckMotion(); truckRigRiding = false;
        WheelSpin = wheelVelocity = 0;
        for (var i = 0; i < wheels.Length; i++) if (wheels[i] != null) wheels[i].localRotation = Quaternion.identity;
        detached = default(SkateBoardPose); detachedVelocity = Vector3.zero;
        Tip = Pop = GrindPitch = GrindRoll = AirPitch = GrabFold = Plant = 0f;
        SupportTruck = 0; Held = false; bailStamp = -1f;
    }
    // An ollie's pitch, degrees, the nose up: the tail down at the pop, the board level at the
    // top, the nose a little down on the way to the landing (SkateRig sets it from the climb).
    public static float AirPitch;
    public const float OllieNoseUp = 26f, OllieNoseDown = 9f;

    private static bool RearWrapOwnsPitch
    {
        get
        {
            var path = SkateRide.BoardPath;
            return SkateRide.Mode == RideMode.Air && path.Kind == SkateBoardTrickKind.Impossible && path.Direction == 1 && !path.Nollie && !path.Reversed;
        }
    }
    // The wrap already owns a full vertical rotation around the upright rear foot.
    // Layering the ordinary ollie pitch tilts that foot's clearance frame again.
    // The deck's pitch, degrees, the leading end up: a manual's, a grind's and an ollie's together.
    public static float PitchDegrees { get { return Tip * TipDegrees + GrindPitch + (observedPlane && displayTruckRig || RearWrapOwnsPitch ? 0 : AirPitch) *
        (SkateRide.Mode == RideMode.Air ? (float)RiderRig.Contact.PopPitchSign : 1f); } }

    // A heading laid on the face whose up is that normal: the part along the face stays, the part
    // into it goes up it (as SkateRide's tangent does), so a nose pointed straight into a vertical
    // wall points up the wall instead of into it. On flat ground it is the heading itself.
    public static Vector3 OnFace(Vector3 normal, Vector3 heading)
    {
        return SkateRide.SurfaceTangent(normal, heading);
    }

    public static void Tilt(ref Vector3 at, ref Vector3 up)
    {
        var pitch = PitchDegrees;
        if (Math.Abs(pitch) > 0.01f)
        {
            var truck = SupportTruck;
            var lead = OnFace(up, SkateRide.Dir(truck != 0 ? SkateRide.DeckYaw : SkateRide.Yaw));
            if (lead.sqrMagnitude > 0.0001f)
            {
                lead = truck != 0 || SkateRide.Speed >= 0f ? lead.normalized : -lead.normalized;
                var turn = SkateRide.Turn(pitch, Vector3.Cross(lead, up));
                // A manual and a stall on one end pitch about the axle that stays down; a grind on a
                // truck about that truck, which is where the edge is.
                var pivot = at + lead * (truck != 0 ? truck * Axle : pitch > 0f ? -Axle : Axle);
                at = pivot + turn * (at - pivot);
                up = turn * up;
            }
        }
        if (Math.Abs(GrindRoll) > 0.01f)
        {
            var nose = SkateRide.Dir(SupportTruck != 0 ? SkateRide.DeckYaw : SkateRide.NoseYaw); nose = nose - up * Vector3.Dot(nose, up);
            if (nose.sqrMagnitude > 0.0001f) up = SkateRide.Turn(-GrindRoll, nose.normalized) * up;
        }
    }

    // Stepped off while rolling: the board rolls on by itself, slowing at RollOutDecel, and then
    // lies where it stopped for a while, as after a bail.
    public const float RollOutDecel = 2.5f, RollOutFrom = 0.8f;
    private static bool rollingOut; private static Vector3 rollAt, rollDir, rollUp = Vector3.up, rollNose; private static float rollSpeed, rolledAt;
    public static bool RollingOut { get { return rollingOut; } }
    public static Vector3 RollOutAt { get { return rollAt; } }

    // Called after Prepare/Pop, by both the rider and renderer in the same frame.
    // No previously rendered Transform participates in the contact calculation.
    // A Caveman begins with this actually visible nearby loose board, never a synthetic frame
    // under an on-foot player. The oldschool adapter independently verifies ground and pickup.
    public static bool TryLoosePose(GameObject board, out SkateBoardPose frame, out Vector3 velocity)
    {
        frame = default(SkateBoardPose); velocity = Vector3.zero;
        if (SkateRide.On || board == null || !board.activeSelf || (!detached.Initialized && !rollingOut && Time.realtimeSinceStartup >= lieUntil)) return false;
        var t = board.transform; frame = new SkateBoardPose(V(t.position + t.up * RiderRig.DeckTop), V(t.forward), V(t.up));
        if (!SkateBoardPath.Valid(frame)) return false;
        velocity = detached.Initialized ? detachedVelocity : rollingOut ? rollDir * rollSpeed : Vector3.zero; return true;
    }

    public static SkateBoardPose PoseFor(Transform playerT)
    {
        if (SkateRiggingShowcase.Running && SkateRiggingShowcase.CurrentFrame.Initialized)
            return SkateRiggingShowcase.CurrentFrame;
        return SkateSurfaceRide.FlipFrame(UnflippedFor(playerT));
    }
    public static SkateBoardPose CarrierFor(Transform playerT) { return PoseFor(playerT, true); }
    public static SkateBoardPose UnflippedFor(Transform playerT)
    {
        if (SkateSurfaceRide.DisplayWall && SkateSurfaceRide.WallFrame.Initialized)
        {
            var wall = SkateSurfaceRide.WallFrame; var n = V(wall.Up);
            var delta = playerT.position - SkateRide.Position; if (SkateSurfaceRide.Active) delta -= n * Vector3.Dot(delta, n);
            return new SkateBoardPose(wall.Origin + V(delta), wall.Forward, wall.Up);
        }
        var frame = PoseFor(playerT, false);
        var bounce = SkateSurfaceRide.WallieWeight;
        if (bounce > 0 && SkateRide.Mode == RideMode.Air)
        {
            var sign = Vector3.Dot(V(frame.Forward), SkateRide.Dir(SkateRide.NoseYaw)) >= 0 ? 1 : -1;
            var tilt = SkateRide.Turn(-20 * bounce * sign, V(frame.Right));
            var pivot = V(frame.Origin) - V(frame.Forward) * (Axle * sign);
            frame = new SkateBoardPose(V(pivot + tilt * (V(frame.Origin) - pivot)), V(tilt * V(frame.Forward)), V(tilt * V(frame.Up)));
        }
        return frame;
    }
    // FlipDeg is a scoring/gesture convention (+ kickflip), independent of stance.
    // Every ordinary visual consumer shares this one physical roll convention.
    public static float OrdinaryFlipRoll
    {
        get { return SkateRide.BoardPath.Known || SkateActions.Finger.Initialized || SkateOldschoolRide.Active
            ? 0f : (float)SkateRide.FlipDeg * SkateRide.StanceSign; }
    }
    private static SkateBoardPose PoseFor(Transform playerT, bool carrier)
    { return PoseAt(playerT, carrier, Contact(playerT)); }
    private static SkateBoardPose PoseAt(Transform playerT, bool carrier, Vector3 at)
    {
        var now = Time.realtimeSinceStartup;
        var n = SkateRide.ViewNormal;
        var trucked = SupportTruck != 0;
        var deckNose = OnFace(n, SkateRide.Dir(SkateRide.DeckYaw));
        var deckTurn = trucked ? SkateRide.Turn(PitchDegrees, Vector3.Cross(deckNose, n)) : Quaternion.identity;
        Tilt(ref at, ref n);
        var travel = OnFace(n, SkateRide.Dir(SkateRide.Yaw));
        // The lean rolls the board about its direction of travel, right side down in a right turn.
        float lean = displayTruckRig ? (float)((truckMotion[0].Steering + truckMotion[1].Steering) * .5) * displayAhead : SkateRide.Lean;
        var up = SkateRide.Turn(-lean * (trucked ? 1f - SupportWeight : 1f), travel) * n;
        // The board's own nose: the rider's stance, and what the board has turned under his feet.
        var nose = trucked ? deckTurn * deckNose : OnFace(up, SkateRide.Dir(SkateRide.DeckYaw));
        nose = nose - up * Vector3.Dot(nose, up);
        nose = nose.sqrMagnitude > 0.0001f ? nose.normalized : travel;
        var flip = OrdinaryFlipRoll; var askew = 0f;
        if (SkateRide.Mode == RideMode.Bail)
        {
            // The rider goes on and down. The board is on its own from the moment of the bail: it
            // turns over once and comes to rest behind where the rider will stop, on its deck.
            if (bailStamp != SkateRide.ModeAt)
            {
                bailStamp = SkateRide.ModeAt; bailFrom = at; bailTravel = travel; bailNose = nose; bailUp = up;
                var speed = Math.Abs(SkateRide.Speed);
                bailRoll = (SkateRide.Speed < 0f ? -1f : 1f) * (speed * speed / (2f * SkateRide.BailDecel)) - BailBehind;
            }
            var t = now - SkateRide.ModeAt;
            var over = SkateRide.Clamp(t / BailTurn, 0f, 1f); over = over * over * (3f - 2f * over);
            flip += 180f * over; askew = 35f * over;
            travel = bailTravel; nose = bailNose; up = bailUp;
            at = bailFrom + travel * (bailRoll * over) + Vector3.up * (0.22f * (float)Math.Sin(over * Math.PI)) - up * ((RiderRig.DeckTop - BailRest) * over);
        }
        var rot = SkateRide.Turn(askew, up) * Quaternion.LookRotation(nose, up) * SkateRide.Turn(flip, Vector3.forward);
        if (!carrier) rot = GrabTweak(up) * rot;
        // A flip turns the board about the middle of the deck, not about the wheels.
        var centre = at + up * RiderRig.DeckTop - (rot * Vector3.up) * RiderRig.DeckTop;
        if (Plant > 0.001f && SkateRide.Mode != RideMode.Bail)
        {
            var lead = Vector3.Dot(travel, nose) >= 0f ? nose : -nose;
            var kick = SkateRide.Turn(-PlantPitch * Plant, Vector3.Cross(up, lead));
            var pivot = centre - lead * PlantPivot;
            centre = pivot + kick * (centre - pivot) + up * (PlantLift * Plant);
            rot = kick * rot;
        }
        var top = centre + (rot * Vector3.up) * RiderRig.DeckTop;
        var frame = new SkateBoardPose(V(top), V(rot * Vector3.forward), V(rot * Vector3.up));
        return SkateRide.Mode == RideMode.Air ? frame.WithPath(SkateRide.BoardPath) : frame;
    }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }
    private static Vector3 V(SkateVector v) { return new Vector3((float)v.X, (float)v.Y, (float)v.Z); }

    public static void Follow(GameObject board, Transform playerT)
    {
        var now = Time.realtimeSinceStartup;
        var riding = SkateRide.On && playerT != null;
        if ((!riding || SkateRide.Mode == RideMode.Bail) && truckRigRiding)
        { ResetTruckMotion(); truckRigRiding = false; }
        if ((!riding || SkateRide.Mode == RideMode.Bail) && FollowIndependent(board, now)) { wasRiding = riding; return; }
        // A bail ends on foot: the board stays where it came to rest for a while, or until the
        // rider gets on again. Stepped off while rolling, it rolls on and comes to rest.
        if (riding) { lying = SkateRide.Mode == RideMode.Bail; rollingOut = false; }
        else if (lying) { lying = false; lieUntil = now + LieSeconds; }
        else if (wasRiding && SkateRide.OffReason == "stepped off" )
        {
            rollingOut = true; rollAt = lastAt; rollDir = lastTravel * (lastSpeed < 0f ? -1f : 1f); rollUp = lastUp; rollNose = lastNose; rollSpeed = Math.Abs(lastSpeed); rolledAt = now; lieUntil = now + 999f;
        }
        wasRiding = riding;
        if (rollingOut)
        {
            var dt = SkateRide.Clamp(now - rolledAt, 0f, 0.1f); rolledAt = now;
            rollSpeed = Math.Max(0f, rollSpeed - RollOutDecel * dt);
            rollAt += rollDir * (rollSpeed * dt);
            if (rollSpeed <= 0.001f) { rollingOut = false; lieUntil = now + LieSeconds; }
            board.transform.rotation = Quaternion.LookRotation(rollNose, rollUp);
            board.transform.position = rollAt + rollUp * RiderRig.DeckTop - (board.transform.rotation * Vector3.up) * RiderRig.DeckTop;
            WheelSpin = (WheelSpin + rollSpeed / WheelRadius * 57.29578f * dt) % 360f;
            for (var i = 0; i < wheels.Length; i++) wheels[i].localRotation = Quaternion.AngleAxis(WheelSpin, wheelAxes[i]);
        }
        var show = riding || now < lieUntil;
        if (board.activeSelf != show) board.SetActive(show);
        if (!riding) return;
        lieUntil = -1f;
        // The rider and rendered board consume the same current live frame when a capture is shown.
        var frame = SkateCapturedTricks.HasFrame ? SkateCapturedTricks.DisplayFrame : PoseFor(playerT);
        var up = V(frame.Up); var nose = V(frame.Forward);
        var rot = Quaternion.LookRotation(nose, up);
        var centre = V(frame.Origin) - up * RiderRig.DeckTop;
        var at = Contact(playerT); var travel = OnFace(SkateRide.ViewNormal, SkateRide.Dir(SkateRide.Yaw));
        if (SkateOldschoolRide.Active)
        {
            var independent = SkateOldschoolRide.Frame;
            board.transform.rotation = Quaternion.LookRotation(V(independent.Forward), V(independent.Up));
            board.transform.position = V(independent.Origin) - V(independent.Up) * RiderRig.DeckTop;
            Parts(now, travel, V(independent.Forward)); return;
        }
        if (SkateActions.DisplayFrame.Initialized)
        {
            var action = SkateActions.DisplayFrame;
            board.transform.rotation = Quaternion.LookRotation(V(action.Forward), V(action.Up));
            board.transform.position = V(action.Origin) - V(action.Up) * RiderRig.DeckTop;
            Parts(now, travel, V(action.Forward)); return;
        }
        if (Held && SkateRide.Handplant)
        {
            if (HeldFrame.Initialized)
            {
                var frameUp = V(HeldFrame.Up); var frameForward = V(HeldFrame.Forward);
                board.transform.rotation = Quaternion.LookRotation(frameForward, frameUp);
                board.transform.position = V(HeldFrame.Origin) - frameUp * RiderRig.DeckTop;
                Parts(now, travel, frameForward); return;
            }
            // Against the feet through the handplant: from where it rode as the body goes up, and
            // back to that as the body comes down, by how far the pose has gone over (the rig's blend).
            var heldUp = HeldUp.sqrMagnitude > 0.5f ? HeldUp : Vector3.up;
            var heldNose = HeldNose - heldUp * Vector3.Dot(HeldNose, heldUp);
            if (heldNose.sqrMagnitude < 0.0001f) heldNose = Vector3.Cross(heldUp, Vector3.right);
            var over = SkateRide.Clamp(RiderRig.PlantBlend, 0f, 1f);
            var heldRot = Quaternion.LookRotation(heldNose.normalized, heldUp);
            board.transform.rotation = Quaternion.Slerp(rot, heldRot, over);
            board.transform.position = Vector3.Lerp(centre, HeldAt, over);
            Parts(now, travel, heldNose.normalized);
            return;
        }
        board.transform.rotation = rot;
        board.transform.position = centre;
        Parts(now, travel, nose);
        lastAt = at; lastTravel = travel; lastUp = up; lastNose = nose; lastSpeed = SkateRide.Speed;
    }
    private static bool wasRiding; private static Vector3 lastAt, lastTravel, lastUp = Vector3.up, lastNose = Vector3.forward; private static float lastSpeed;

    // The trucks steer with the lean, the front one into the turn and the rear one out of it, each
    // about its own kingpin; the wheels roll with the speed, the way the deck's nose is pointing.
    private static void Parts(float now, Vector3 travel, Vector3 nose)
    {
        var dt = followedAt < 0f ? 0f : SkateRide.Clamp(now - followedAt, 0f, 0.1f);
        followedAt = now;
        // An independently held/action-driven board does not prove wheel support.
        bool ground = SkateRide.Mode == RideMode.Ground && !Held && !SkateOldschoolRide.Active && !SkateActions.Owns;
        if (SkateRide.Mode != RideMode.Bail)
        {
            float turned;
            if (ground)
            {
                var ahead = Vector3.Dot(travel, nose) < 0f ? -1f : 1f;
                wheelVelocity = ahead * SkateRide.Speed / WheelRadius * 57.29578f;
                turned = wheelVelocity * dt;
            }
            else
            {
                double decay = Math.Exp(-.12 * dt);
                turned = (float)(wheelVelocity * (1 - decay) / .12);
                wheelVelocity *= (float)decay;
            }
            WheelSpin = (WheelSpin + turned) % 360f;
        }
        for (var i = 0; i < trucks.Length; i++)
        {
            SkateTruckPose hanger; string error;
            if (!SkateBoardTruckKinematics.TryPose(truckSides[i] < 0 ? -1 : 1, truckMotion[truckSides[i] < 0 ? 0 : 1], out hanger, out error)) continue;
            trucks[i].localPosition = V(hanger.Point(SkateBoardTruckKinematics.Eye(truckSides[i] < 0 ? -1 : 1))) - new Vector3(0, RiderRig.DeckTop, 0);
            trucks[i].localRotation = Quaternion.LookRotation(V(hanger.Forward), V(hanger.Up));
        }
        ApplyBushings();
        for (var i = 0; i < wheels.Length; i++) wheels[i].localRotation = Quaternion.AngleAxis(WheelSpin, wheelAxes[i]);
    }

    private static void ApplyBushings()
    {
        for (var i = 0; i < bushings.Length; i++)
        {
            var shape = bushingShapes[i]; var vertices = bushingVertices[i]; var normals = bushingNormals[i];
            bool changed = false;
            for (int k = 0; k < vertices.Length; k++)
                if (Math.Abs(vertices[k].x - shape.Positions[k * 3]) + Math.Abs(vertices[k].y - shape.Positions[k * 3 + 1]) + Math.Abs(vertices[k].z - shape.Positions[k * 3 + 2]) > 1e-7) { changed = true; break; }
            if (!changed) continue;
            for (int k = 0; k < vertices.Length; k++)
            {
                vertices[k] = new Vector3(shape.Positions[k * 3], shape.Positions[k * 3 + 1], shape.Positions[k * 3 + 2]);
                normals[k] = new Vector3(shape.Normals[k * 3], shape.Normals[k * 3 + 1], shape.Normals[k * 3 + 2]);
            }
            // Fixed rest quaternion, unit scale; actual closed wedge geometry changes.
            bushingMeshes[i].vertices = vertices; bushingMeshes[i].normals = normals; bushingMeshes[i].RecalculateBounds();
        }
    }

    private static void ResetTruckMotion()
    {
        truckMotion[0] = truckMotion[1] = default(SkateBoardTruckState); followedAt = -1f;
        motionAt = -1f; motionSeconds = 0; contactCached = observedPlane = false; contactOffset = Vector3.zero; TruckPlaneSupported = false;
        displayTruckRig = false; displayAhead = 1;
        for (var i = 0; i < trucks.Length; i++) if (trucks[i] != null) { trucks[i].localRotation = Quaternion.identity; trucks[i].localPosition = truckOrigins[i]; }
        for (var i = 0; i < bushings.Length; i++) if (bushings[i] != null) bushings[i].localScale = Vector3.one;
        for (int i = 0; i < bushingShapes.Length; i++)
        {
            SkateTruckPose pose; string error;
            if (SkateBoardTruckKinematics.TryPose(bushingShapes[i].Side, default(SkateBoardTruckState), out pose, out error)) bushingShapes[i].TryUpdate(pose, out error);
        }
        ApplyBushings();
    }

    // The detailed shared mesh: a deck, two trucks that hang from it and four wheels that hang
    // from the trucks, each its own object about its own pivot, all drawn with one texture.
    private static GameObject FromParts()
    {
        var data = Shortcut.RustMod.SkateBoardMesh.CreateDetailed();
        if (data.Parts.Length == 0 || data.TextureWidth == 0) throw new InvalidOperationException("the shared mesh has no parts");
        var tex = new Texture2D(data.TextureWidth, data.TextureHeight);
        for (var y = 0; y < data.TextureHeight; y++)
            for (var x = 0; x < data.TextureWidth; x++)
            {
                var o = (y * data.TextureWidth + x) * 4;
                tex.SetPixel(x, y, new Color(data.TextureRgba[o] / 255f, data.TextureRgba[o + 1] / 255f, data.TextureRgba[o + 2] / 255f, data.TextureRgba[o + 3] / 255f));
            }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        string how; var lit = Find(LitShader, out how); Material mat;
        if (lit != null)
        {
            mat = new Material(lit); mat.mainTexture = tex; mat.color = Color.white;
            mat.SetFloat("_Glossiness", Gloss); mat.SetFloat("_Metallic", 0f);
            Drawn = LitShader; Say("BOARD lit by " + LitShader + " (found " + how + "), textured");
        }
        else
        {
            var sh = Shader.Find(PlainShader);
            if (sh == null) throw new InvalidOperationException("no vertex-colour shader");
            mat = new Material(sh); mat.color = Color.white; Drawn = PlainShader;
            Say("no " + LitShader + " shader in this client; the board is drawn without light");
        }
        var root = new GameObject("skate_board");
        var objects = new Transform[data.Parts.Length];
        var pivots = new Vector3[data.Parts.Length];
        var truckList = new System.Collections.Generic.List<Transform>(); var truckSide = new System.Collections.Generic.List<float>();
        var wheelList = new System.Collections.Generic.List<Transform>(); var wheelAxis = new System.Collections.Generic.List<Vector3>();
        var bushList = new System.Collections.Generic.List<Transform>(); var bushTruck = new System.Collections.Generic.List<int>();
        var shapes = new System.Collections.Generic.List<SkateBoardBushing>(); var meshes = new System.Collections.Generic.List<Mesh>();
        var vertexBuffers = new System.Collections.Generic.List<Vector3[]>(); var normalBuffers = new System.Collections.Generic.List<Vector3[]>();
        var rubber = new Material(lit != null ? lit : Shader.Find(PlainShader));
        rubber.color = new Color(.95f, .29f, .028f); rubber.mainTexture = tex;
        for (var p = 0; p < data.Parts.Length; p++)
        {
            var part = data.Parts[p];
            var pivot = new Vector3(part.PivotX, part.PivotY, part.PivotZ);
            var axis = new Vector3(part.AxisX, part.AxisY, part.AxisZ);
            var rest = new Quaternion(part.RestQX, part.RestQY, part.RestQZ, part.RestQW);
            var inverseRest = Quaternion.Inverse(rest);
            var verts = new Vector3[part.VertexCount]; var norms = new Vector3[part.VertexCount]; var cols = new Color[part.VertexCount]; var uv = new Vector2[part.VertexCount];
            for (var i = 0; i < part.VertexCount; i++)
            {
                var v = part.VertexStart + i;
                verts[i] = inverseRest * (new Vector3(data.Positions[v * 3], data.Positions[v * 3 + 1], data.Positions[v * 3 + 2]) - pivot);
                norms[i] = inverseRest * new Vector3(data.Normals[v * 3], data.Normals[v * 3 + 1], data.Normals[v * 3 + 2]);
                cols[i] = new Color(data.Colours[v * 4], data.Colours[v * 4 + 1], data.Colours[v * 4 + 2], data.Colours[v * 4 + 3]);
                uv[i] = new Vector2(data.UVs[v * 2], data.UVs[v * 2 + 1]);
            }
            var tris = new int[part.IndexCount];
            for (var i = 0; i < part.IndexCount; i++) tris[i] = data.Triangles[part.IndexStart + i] - part.VertexStart;
            var mesh = new Mesh();
            mesh.vertices = verts; mesh.normals = norms; mesh.colors = cols; mesh.uv = uv; mesh.triangles = tris;
            mesh.RecalculateBounds();
            var go = new GameObject(part.Kind.ToString().ToLowerInvariant());
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().material = part.Kind == SkateBoardPartKind.Bushing ? rubber : mat;
            var parent = part.ParentIndex >= 0 && part.ParentIndex < p ? objects[part.ParentIndex] : root.transform;
            var parentPivot = part.ParentIndex >= 0 && part.ParentIndex < p ? pivots[part.ParentIndex] : Vector3.zero;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pivot - parentPivot; go.transform.localRotation = rest;
            objects[p] = go.transform; pivots[p] = pivot;
            if (part.Kind == Shortcut.RustMod.SkateBoardPartKind.Truck) { truckList.Add(go.transform); truckSide.Add(part.PivotZ < 0f ? -1f : 1f); }
            if (part.Kind == Shortcut.RustMod.SkateBoardPartKind.Wheel) { wheelList.Add(go.transform); wheelAxis.Add(axis); }
            if (part.Kind == SkateBoardPartKind.Bushing)
            {
                SkateBoardBushing shape; string error;
                if (!SkateBoardBushing.TryCreate(data, p, out shape, out error)) throw new InvalidOperationException(error);
                bushList.Add(go.transform); bushTruck.Add(part.PivotZ < 0 ? 0 : 1);
                shapes.Add(shape); meshes.Add(mesh); vertexBuffers.Add(verts); normalBuffers.Add(norms);
            }
        }
        trucks = truckList.ToArray(); truckSides = truckSide.ToArray();
        truckOrigins = new Vector3[trucks.Length];
        for (int i = 0; i < trucks.Length; i++) truckOrigins[i] = trucks[i].localPosition;
        wheels = wheelList.ToArray(); wheelAxes = wheelAxis.ToArray();
        bushings = bushList.ToArray(); bushingTrucks = bushTruck.ToArray();
        bushingShapes = shapes.ToArray(); bushingMeshes = meshes.ToArray(); bushingVertices = vertexBuffers.ToArray(); bushingNormals = normalBuffers.ToArray();
        UnityEngine.Object.DontDestroyOnLoad(root);
        SkateFootContacts.SetBoard(data, RiderRig.DeckTop);
        string contactError;
        if (!SkateBoardWheelContact.TryCreate(data, out wheelContact, out contactError)) TruckContactStatus = contactError;
        return root;
    }

    private static GameObject FromMesh()
    {
        var data = Shortcut.RustMod.SkateBoardMesh.Create();
        var n = data.Positions.Length / 3;
        var verts = new Vector3[n]; var norms = new Vector3[n]; var cols = new Color[n];
        for (var i = 0; i < n; i++)
        {
            verts[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
            norms[i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
            cols[i] = new Color(data.Colours[i * 4], data.Colours[i * 4 + 1], data.Colours[i * 4 + 2], data.Colours[i * 4 + 3]);
        }
        var mesh = new Mesh();
        mesh.vertices = verts; mesh.normals = norms; mesh.colors = cols; mesh.triangles = data.Triangles;
        mesh.RecalculateBounds();
        var mat = Lit(mesh, cols);
        if (mat == null)
        {
            var sh = Shader.Find(PlainShader);
            if (sh == null) throw new InvalidOperationException("no vertex-colour shader");
            mat = new Material(sh); mat.color = Color.white; Drawn = PlainShader;
        }
        var root = new GameObject("skate_board");
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().material = mat;
        UnityEngine.Object.DontDestroyOnLoad(root);
        return root;
    }

    private static Shader Find(string name, out string how)
    {
        how = "by name";
        var sh = Shader.Find(name);
        if (sh != null) return sh;
        // A shader that came with an asset bundle is loaded without being findable by name.
        how = "among the loaded shaders";
        var all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<Shader>());
        for (var i = 0; i < all.Length; i++)
        {
            var one = all[i] == null ? null : all[i].TryCast<Shader>();
            if (one != null && one.name == name) return one;
        }
        return null;
    }

    // One block of texels per vertex colour; every vertex looks its own colour up.
    private static Material Lit(Mesh mesh, Color[] cols)
    {
        try
        {
            string how;
            var sh = Find(LitShader, out how);
            if (sh == null) { Say("no " + LitShader + " shader in this client; the board is drawn without light"); return null; }
            var unique = new System.Collections.Generic.List<Color>(); var index = new int[cols.Length];
            for (var i = 0; i < cols.Length; i++)
            {
                var c = cols[i]; var found = -1;
                for (var u = 0; u < unique.Count && found < 0; u++)
                    if (Math.Abs(unique[u].r - c.r) + Math.Abs(unique[u].g - c.g) + Math.Abs(unique[u].b - c.b) < 0.002f) found = u;
                if (found < 0) { found = unique.Count; unique.Add(c); }
                index[i] = found;
            }
            const int cell = 4;
            var across = 1; while (across * across < unique.Count) across++;
            var tex = new Texture2D(cell * across, cell * across);
            for (var u = 0; u < across * across; u++)
            {
                var c = u < unique.Count ? unique[u] : Color.white;
                for (var y = 0; y < cell; y++) for (var x = 0; x < cell; x++) tex.SetPixel((u % across) * cell + x, (u / across) * cell + y, c);
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            var uv = new Vector2[cols.Length];
            for (var i = 0; i < uv.Length; i++) uv[i] = new Vector2(((index[i] % across) + 0.5f) / across, ((index[i] / across) + 0.5f) / across);
            mesh.uv = uv;
            var m = new Material(sh);
            m.mainTexture = tex; m.color = Color.white;
            m.SetFloat("_Glossiness", Gloss); m.SetFloat("_Metallic", 0f);
            Drawn = LitShader;
            Say("BOARD lit by " + LitShader + " (found " + how + "), " + unique.Count + " colours");
            return m;
        }
        catch (Exception e) { Say("lit board unavailable (" + e.GetType().Name + ": " + e.Message + "); it is drawn without light"); return null; }
    }

    private static GameObject FromPrimitives()
    {
        var root = new GameObject("skate_board");
        var sh = Shader.Find(PlainShader); Drawn = PlainShader + ", primitives";
        var grip = new Color(0.07f, 0.07f, 0.08f); var kick = new Color(0.16f, 0.16f, 0.18f); var under = new Color(1f, 0.1f, 0.6f);
        var metal = new Color(0.55f, 0.57f, 0.6f); var wheel = new Color(0.95f, 0.93f, 0.85f);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.078f, 0f), new Vector3(0.20f, 0.010f, 0.60f), Quaternion.identity, grip, sh);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.069f, 0f), new Vector3(0.20f, 0.008f, 0.60f), Quaternion.identity, under, sh);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.084f, 0f), new Vector3(0.025f, 0.003f, 0.56f), Quaternion.identity, under, sh);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.097f, 0.365f), new Vector3(0.20f, 0.012f, 0.15f), Quaternion.Euler(-17f, 0f, 0f), kick, sh);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.097f, -0.365f), new Vector3(0.20f, 0.012f, 0.15f), Quaternion.Euler(17f, 0f, 0f), kick, sh);
        for (var z = -1; z <= 1; z += 2)
        {
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.046f, z * 0.23f), new Vector3(0.15f, 0.034f, 0.05f), Quaternion.identity, metal, sh);
            for (var x = -1; x <= 1; x += 2)
                Part(root, PrimitiveType.Cylinder, new Vector3(x * 0.097f, 0.0275f, z * 0.23f), new Vector3(0.055f, 0.017f, 0.055f), Quaternion.Euler(0f, 0f, 90f), wheel, sh);
        }
        UnityEngine.Object.DontDestroyOnLoad(root);
        return root;
    }

    private static void Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Color color, Shader sh)
    {
        var go = GameObject.CreatePrimitive(type);
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.Destroy(col);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
        if (sh != null) { var m = new Material(sh); m.color = color; go.GetComponent<Renderer>().material = m; }
    }
}
