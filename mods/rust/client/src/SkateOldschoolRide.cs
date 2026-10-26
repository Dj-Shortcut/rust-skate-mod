using System;
using Shortcut.RustMod;
using UnityEngine;

// The real movement body jumps independently. This adapter observes the ground and sweeps the
// independent deck; it never moves the body to manufacture a pickup, a catch or missing support.
public static class SkateOldschoolRide
{
    private static SkateOldschoolState state;
    private static uint inputEpoch;
    private static Vector3 lastPosition;
    private static bool hasPosition, firstLaunchObservation, nameUsed, handLeft, releaseHand, landingConsumed;
    private static Vector3 releaseHandOffset;
    private static float releaseHandSeconds;
    private static int pendingFlip, pendingShove;
    private static float spinSide = 1f;
    private static string completedName;
    // Authored detailed mesh: wheel centres (+/- .082, .027, +/- .245), radius .027;
    // deck top .092 with kicks to .142. These queries enclose the actual rendered geometry.
    private const float Axle = 0.245f, WheelAcross = 0.082f, ProbeLift = 0.04f, ProbeReach = 3f;
    private const int SweepSteps = 4;
    public static bool Active { get { return state.Kind != SkateOldschoolKind.None && state.Phase != SkateOldschoolPhase.Failed; } }
    public static bool Owns { get { return state.Active && !landingConsumed; } }
    public static SkateOldschoolKind Kind { get { return state.Kind; } }
    public static SkateOldschoolState State { get { return state; } }
    public static SkateOldschoolPhase Phase { get { return state.Phase; } }
    public static SkateBoardPose Frame { get { return Active ? state.Deck : default(SkateBoardPose); } }
    public static float FootContact { get { return Active ? (float)state.FootContact : 1f; } }
    public static float HandContact { get { return Active ? (float)state.HandContact : 0f; } }
    public static float HandFollow { get { return HandContact > 0 ? 1f : releaseHand ? Math.Max(0f, 1f - releaseHandSeconds / 0.16f) : 0f; } }
    public static Vector3 HandTarget { get { return HandContact > 0 ? U(state.Grip) : releaseHand ? lastPosition + releaseHandOffset : Vector3.zero; } }
    public static bool HandLeft { get { return handLeft; } }
    public static bool LandingReady { get { return !Active || state.Caught && !state.Rotating; } }
    public static double CompletedFlip { get { return Active && !state.Rotating ? state.FlipDegrees : 0; } }
    // Shared scoring signs (FS positive), independent of this flight's world-space rotation.
    public static double CompletedShove { get { return Active && !state.Rotating ? -state.ShoveDegrees * spinSide : 0; } }
    public static Vector3 FinalForward { get { return Active ? U(state.Deck.Forward) : Vector3.forward; } }

    public static void Reset()
    {
        state = default(SkateOldschoolState); inputEpoch = 0; hasPosition = firstLaunchObservation = false;
        pendingFlip = pendingShove = 0; spinSide = 1f; completedName = null; nameUsed = landingConsumed = false;
        handLeft = releaseHand = false; releaseHandOffset = Vector3.zero; releaseHandSeconds = 0;
    }

    public static bool TryHippie(Vector3 pos, float dt, bool requested)
    {
        if (!requested || Owns) return false;
        if (Active) Reset();
        if (SkateRide.Mode != RideMode.Ground || !SkateRide.Grounded || SkateRide.GroundGrab.Active ||
            SkateRide.Manual || SkateRide.NoseManual || SkateRide.RevertLeft != 0 || !Controls() ||
            SkateRig.LocalT == null || !RiderRig.Bound)
        { SkateHud.Notice("Hippie Jump needs a supported skateboard and rider pose"); return false; }
        var shown = SkateBoard.PoseFor(SkateRig.LocalT);
        if (!SkateBoardPath.Valid(shown) || !shown.Initialized || !Point(pos)) return false;
        // The attached board's relative pose is canonical; anchor it to this actual physics
        // observation, rather than the independently interpolated presentation transform.
        var deck = new SkateBoardPose(shown.Origin + V(pos - SkateRig.LocalT.position), shown.Forward, shown.Up);
        var support = Floor(deck.Origin, deck.Forward, deck.Up);
        if (!support.Observed) { SkateHud.Notice("Hippie Jump needs ground under both axles"); return false; }
        if (Math.Abs(SkateVector.Dot(deck.Origin - support.Point, support.Normal) - SkateOldschool.DeckClearance) > 0.04)
        { SkateHud.Notice("Let the wheels settle before a Hippie Jump"); return false; }
        var velocity = SkateRide.Tangent * SkateRide.Speed;
        var feet = Feet(pos); string error;
        if (!SkateOldschool.TryBegin(SkateOldschoolKind.HippieJump, deck, V(velocity), V(feet), V(velocity), V(CarryHand(pos)),
            true, out state, out error))
        { Reset(); SkateHud.Notice("Hippie Jump could not begin"); return false; }
        BeginOwner(pos);
        pendingFlip = Sign(SkateKeys.PopFlip);
        pendingShove = Shove(SkateKeys.PopShove);
        Jump(pos, dt);
        firstLaunchObservation = true;
        return SkateRide.Mode == RideMode.Air && Active;
    }

    // Call only after the existing mount preflight and a measured pickup grip solve. The loose
    // frame must be the actual nearby board, not PoseFor(LocalT) synthesized while on foot.
    public static bool BeginCaveman(SkateBoardPose actualLooseFrame, Vector3 actualLooseVelocity)
    {
        if (Owns || SkateRide.Mode != RideMode.Ground || !SkateRide.Grounded || !Controls() || !RiderRig.Bound ||
            !SkateBoardPath.Valid(actualLooseFrame) || !actualLooseFrame.Initialized) return false;
        var support = Floor(actualLooseFrame.Origin, actualLooseFrame.Forward, actualLooseFrame.Up);
        if (!support.Observed || Math.Abs(SkateVector.Dot(actualLooseFrame.Origin - support.Point, support.Normal) - SkateOldschool.DeckClearance) > 0.16)
        { SkateHud.Notice("Caveman needs a nearby skateboard resting on ground"); return false; }
        var pos = SkateRide.Position; var velocity = SkateRide.ActionVelocity; string error;
        if (!SkateOldschool.TryBegin(SkateOldschoolKind.Caveman, actualLooseFrame, V(actualLooseVelocity), V(Feet(pos)),
            V(velocity), V(CarryHand(pos)), true, out state, out error))
        { Reset(); SkateHud.Notice("The skateboard is outside Caveman pickup reach"); return false; }
        BeginOwner(pos); return true;
    }

    // While the hand picks the board up, preserve the real body's existing rolling velocity.
    // The native walk component remains enabled; ActionJump is the existing velocity launch.
    public static bool GroundStep(Vector3 pos, float dt, out Vector3 velocity)
    {
        velocity = SkateRide.ActionVelocity;
        if (state.Phase == SkateOldschoolPhase.Complete || landingConsumed) { Reset(); return false; }
        if (!Owns || state.Kind != SkateOldschoolKind.Caveman ||
            state.Phase != SkateOldschoolPhase.Pickup && state.Phase != SkateOldschoolPhase.Carrying) return false;
        if (!SkateRide.Grounded)
        {
            // Taking over pickup must not suspend the ordinary edge/fall transition or permit
            // an extra jump after the rider has already walked off the supporting floor.
            Fail("Caveman pickup lost the rider's ground");
            velocity.y = Math.Min(0f, velocity.y) - (float)SkateOldschool.Gravity * dt;
            return true;
        }
        var events = Tick(pos, dt, false);
        if ((events & SkateOldschoolEvents.ReadyToJump) != 0) Jump(pos, dt);
        if (SkateRide.Mode == RideMode.Air) velocity = SkateRide.Dir(SkateRide.Yaw) * SkateRide.Speed + Vector3.up * SkateRide.VSpeed;
        else if (SkateRide.Mode == RideMode.Bail) velocity = Vector3.zero;
        return true;
    }

    // Before generic air rotation, capture and landing. Root must stop generic AirStep if this
    // changes Mode to Bail; it must require LandingReady before accepting its own touchdown.
    public static void AirStep(Vector3 pos, float dt)
    {
        if (!Owns) return;
        if (firstLaunchObservation)
        {
            firstLaunchObservation = false;
            // GroundStep may invoke AirStep immediately after the launch in this very step.
            // Advancing the board now would put it one full step ahead of the real rider.
            if ((pos - lastPosition).sqrMagnitude < 0.0000000001f) return;
        }
        var contact = SkateRide.OnRamp ? SkateRide.BelowNormal : Vector3.up;
        var incoming = SkateRide.Dir(SkateRide.Yaw) * SkateRide.Speed + Vector3.up * SkateRide.VSpeed;
        var separation = Vector3.Dot(incoming, contact);
        var touchdown = SkateRide.AirTime > 0.12f && SkateRide.VSpeed <= 0f && separation <= 0f &&
            SkateRide.Clearance <= Math.Max(0.04f, -separation * dt + 0.02f);
        Tick(pos, dt, touchdown);
    }

    private static void BeginOwner(Vector3 pos)
    { inputEpoch = SkateKeys.InputEpoch; lastPosition = pos; hasPosition = true; firstLaunchObservation = false; spinSide = SkateRide.SpinSide; completedName = null; nameUsed = landingConsumed = false;
      handLeft = SkateRide.Goofy; releaseHand = false; releaseHandOffset = Vector3.zero; releaseHandSeconds = 0; }

    private static void Jump(Vector3 pos, float dt)
    {
        // TakeOff may reset action modules. Restore this explicitly owned pickup/release state;
        // no stale request or another module's state survives through the mount boundary.
        var saved = state; var epoch = inputEpoch; var flip = pendingFlip; var shove = pendingShove; var side = spinSide;
        var left = handLeft;
        SkateRide.ActionJump(SkateRide.OlliePop, dt);
        state = saved; inputEpoch = epoch; pendingFlip = flip; pendingShove = shove; spinSide = side;
        handLeft = left;
        lastPosition = pos; hasPosition = true; firstLaunchObservation = true;
        if (SkateRide.Mode != RideMode.Air) Fail("The rider jump did not begin");
    }

    private static SkateOldschoolEvents Tick(Vector3 pos, float dt, bool touchdown)
    {
        if (!Owns) return SkateOldschoolEvents.None;
        if (!(dt >= 0.001f && dt <= 0.05f) || !Controls() || inputEpoch != SkateKeys.InputEpoch)
        { Fail("Oldschool action interrupted"); return SkateOldschoolEvents.Failed; }
        var observed = hasPosition ? (pos - lastPosition) / dt : SkateRide.Dir(SkateRide.Yaw) * SkateRide.Speed + Vector3.up * SkateRide.VSpeed;
        if (!Point(pos) || !Point(observed) || observed.sqrMagnitude > SkateOldschool.MaximumPace * SkateOldschool.MaximumPace)
        { Fail("Oldschool rider motion changed outside its capture"); return SkateOldschoolEvents.Failed; }
        lastPosition = pos; hasPosition = true;
        if (releaseHand) releaseHandSeconds += dt;
        // At the very first post-launch observation the body has not moved yet. Its known upward
        // launch must not be mistaken for a grounded rider or a downward throw.
        if (state.Phase == SkateOldschoolPhase.Carrying && SkateRide.Mode == RideMode.Air && observed.y < 0.3f && SkateRide.AirTime < 0.06f)
            observed.y = Math.Max(observed.y, SkateRide.VSpeed);
        int flip = pendingFlip, shove = pendingShove; pendingFlip = pendingShove = 0;
        if (state.Phase == SkateOldschoolPhase.Released)
        {
            if (flip == 0) flip = Sign(SkateKeys.LateFlip + SkateKeys.PopFlip);
            if (shove == 0) shove = Shove(SkateKeys.LateShove + SkateKeys.PopShove);
        }
        var feet = V(Feet(pos)); var hand = V(CarryHand(pos));
        // 0.05f is slightly greater than 0.05 after conversion to double. Preserve the supported
        // 20 Hz native step without weakening the pure model's strict maximum delta.
        var stepSeconds = Math.Min(0.05, (double)dt);
        var predicted = SkateOldschool.Predict(state, feet, V(observed), hand, flip, shove, stepSeconds);
        // A falling deck may cross the floor in this step. Start the downward observation at
        // least as high as the current underside, at the new footprint, so it cannot begin below
        // the very floor it must find. Rolling under a low bar still probes from its low underside.
        var floorProbe = predicted;
        if (state.Flying) floorProbe += state.Up * Math.Max(0, SkateVector.Dot(state.Position - predicted, state.Up));
        var floor = Floor(floorProbe, state.Deck.Forward, state.Up);
        bool release = !SkateKeys.Pad || (SkatePad.Buttons & SkatePad.LeftBumper) == 0;
        var input = new SkateOldschoolInput(feet, V(observed), hand, floor, true, touchdown, release, flip, shove);
        SkateOldschoolResult preview; string error;
        if (!SkateOldschool.TryStep(state, input, stepSeconds, out preview, out error))
        { Fail("Oldschool motion refused"); return SkateOldschoolEvents.Failed; }
        if ((preview.Events & SkateOldschoolEvents.Failed) != 0)
        { Fail("Oldschool action: " + preview.State.Failure); return SkateOldschoolEvents.Failed; }
        bool clear = Sweep(state.Deck, preview.State.Deck, floor);
        var observedFloor = new SkateOldschoolSupport(floor.Observed, floor.Point, floor.Normal, clear, clear);
        input = new SkateOldschoolInput(feet, V(observed), hand, observedFloor, true, touchdown, release, flip, shove);
        SkateOldschoolResult accepted;
        if (!SkateOldschool.TryStep(state, input, stepSeconds, out accepted, out error) || (accepted.Events & SkateOldschoolEvents.Failed) != 0)
        { Fail("The independent skateboard met an obstruction"); return SkateOldschoolEvents.Failed; }
        if ((accepted.Events & SkateOldschoolEvents.Released) != 0)
        { releaseHand = true; releaseHandSeconds = 0; releaseHandOffset = U(accepted.State.Grip) - pos; }
        state = accepted.State;
        if ((accepted.Events & SkateOldschoolEvents.RotationRefused) != 0) SkateHud.Notice("Flip during the release kick, while the deck is within reach");
        return accepted.Events;
    }

    private static bool Controls() { return SkateKeys.Scripted || SkateKeys.ControlsActive; }
    private static Vector3 Feet(Vector3 pos) { return pos + Vector3.up * (float)SkateOldschool.DeckClearance; }
    private static Vector3 CarryHand(Vector3 pos)
    {
        var towardToes = Vector3.Cross(Vector3.up, SkateRide.Dir(SkateRide.NoseYaw)) * SkateRide.StanceSign;
        var height = (float)(RiderRig.Rig.PelvisHeight * 0.75 + RiderRig.Rig.SpineLength * 0.15);
        return Feet(pos) + Vector3.up * height + towardToes * 0.22f;
    }
    private static int Sign(int value) { return value < 0 ? -1 : value > 0 ? 1 : 0; }
    private static int Shove(float degrees)
    { return Math.Abs(degrees) < 90 ? 0 : (degrees > 0 ? -1 : 1) * (spinSide < 0 ? -1 : 1) * (Math.Abs(degrees) >= 270 ? 2 : 1); }

    private static SkateOldschoolSupport Floor(SkateVector position, SkateVector forward, SkateVector up)
    {
        var origin = U(position); var along = U(forward); var normal = U(up);
        var right = Vector3.Cross(normal, along).normalized;
        if (right.sqrMagnitude < 0.5f) return new SkateOldschoolSupport(false, default(SkateVector), default(SkateVector), true, true);
        var point = Vector3.zero; var sum = Vector3.zero; var firstPoint = Vector3.zero; var firstNormal = Vector3.zero;
        for (var axle = -1; axle <= 1; axle += 2)
        for (var side = -1; side <= 1; side += 2)
        {
            // Begin just above the underside. A ray lifted above a low rail would find its TOP
            // and incorrectly place the board on it during the very Hippie meant to pass under it.
            var from = origin + along * (axle * Axle) + right * (side * WheelAcross) - normal * ((float)SkateOldschool.DeckClearance - ProbeLift);
            RaycastHit hit;
            if (!SkateParks.Ray(from, -normal, out hit, ProbeReach, SkateRide.GroundMask) || hit.m_Normal.y < 0.65f)
                return new SkateOldschoolSupport(false, default(SkateVector), default(SkateVector), true, true);
            if (axle == -1 && side == -1) { firstPoint = hit.m_Point; firstNormal = hit.m_Normal; }
            if (Vector3.Dot(firstNormal, hit.m_Normal) < 0.95f || Math.Abs(Vector3.Dot(hit.m_Point - firstPoint, firstNormal)) > 0.06f)
                return new SkateOldschoolSupport(false, default(SkateVector), default(SkateVector), true, true);
            point += hit.m_Point; sum += hit.m_Normal;
        }
        return new SkateOldschoolSupport(true, V(point * 0.25f), V(sum.normalized), true, true);
    }

    private static bool Sweep(SkateBoardPose from, SkateBoardPose to, SkateOldschoolSupport floor)
    {
        var previous = from;
        for (var step = 1; step <= SweepSteps; step++)
        {
            var next = Interpolate(from, to, (double)step / SweepSteps);
            // Eight deck corners plus the centres of all four wheels. Cover each finite sweep,
            // not just the centre trajectory; a rotating nose can strike a rail beside the body.
            for (var x = -1; x <= 1; x += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                if (!Segment(previous.Point(new SkateVector(x * .11, -.04, z * .4)), next.Point(new SkateVector(x * .11, -.04, z * .4)), .012f, floor) ||
                    !Segment(previous.Point(new SkateVector(x * .11, .05, z * .4)), next.Point(new SkateVector(x * .11, .05, z * .4)), .012f, floor) ||
                    !Segment(previous.Point(new SkateVector(x * WheelAcross, -.065, z * Axle)), next.Point(new SkateVector(x * WheelAcross, -.065, z * Axle)), .027f, floor)) return false;
            }
            // Five overlapping longitudinal capsules cover the deck slab between the corners.
            // This also sees a thin post through the middle of a deck whose four corners miss it.
            for (var lane = -2; lane <= 2; lane++)
                if (!Segment(next.Point(new SkateVector(lane * .055, -.02, -.4)), next.Point(new SkateVector(lane * .055, -.02, .4)), .03f, floor)) return false;
            previous = next;
        }
        return true;
    }

    private static bool Segment(SkateVector from, SkateVector to, float radius, SkateOldschoolSupport floor)
    {
        // A queried supporting floor is allowed at wheel contact, never below the deck volume.
        if (floor.Observed && SkateVector.Dot(to - floor.Point, floor.Normal) < radius - 0.009) return false;
        var delta = U(to - from); var distance = delta.magnitude;
        if (distance <= 0.0001f) return true;
        RaycastHit hit;
        if (!SkateParks.Sphere(U(from), radius, delta / distance, out hit, distance, SkateRide.GroundMask)) return true;
        return floor.Observed && Vector3.Dot(hit.m_Normal, U(floor.Normal)) >= 0.98f &&
            Math.Abs(SkateVector.Dot(V(hit.m_Point) - floor.Point, floor.Normal)) <= 0.015;
    }

    private static SkateBoardPose Interpolate(SkateBoardPose from, SkateBoardPose to, double weight)
    {
        var forward = Unit(from.Forward * (1 - weight) + to.Forward * weight);
        var up = from.Up * (1 - weight) + to.Up * weight;
        up = Unit(up - forward * SkateVector.Dot(up, forward));
        return new SkateBoardPose(from.Origin * (1 - weight) + to.Origin * weight, forward, up);
    }

    public static string Name(string original, bool landed)
    {
        if (!Active || nameUsed) return original;
        var basic = state.Kind == SkateOldschoolKind.Caveman ? "Caveman" : "Hippie Jump";
        if (!landed || !LandingReady) return basic;
        if (completedName != null) { nameUsed = landingConsumed = true; return completedName; }
        var name = basic;
        if (Math.Abs(CompletedFlip) >= 359) name += CompletedFlip > 0 ? " Kickflip" : " Heelflip";
        if (Math.Abs(CompletedShove) >= 179)
            name += (CompletedShove > 0 ? " Frontside " : " Backside ") + (Math.Abs(CompletedShove) >= 359 ? "360" : "180") + " Shove-it";
        if (!string.IsNullOrEmpty(original) && original != "Ollie")
        {
            var prefix = original.EndsWith(" Ollie", StringComparison.Ordinal) ? original.Substring(0, original.Length - 6) : original;
            name = prefix + " " + name;
        }
        completedName = name; nameUsed = landingConsumed = true; return name;
    }

    private static void Fail(string message)
    { SkateHud.Notice(message); Out.Say("SKATE " + message); SkateRide.Bail(); Reset(); }
    private static SkateVector Unit(SkateVector vector) { return vector.LengthSquared > 1e-12 ? vector * (1 / vector.Length) : default(SkateVector); }
    private static bool Point(Vector3 point)
    { return !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z) &&
        !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z) &&
        Math.Abs(point.x) <= 100000f && Math.Abs(point.y) <= 100000f && Math.Abs(point.z) <= 100000f; }
    private static SkateVector V(Vector3 value) { return new SkateVector(value.x, value.y, value.z); }
    private static Vector3 U(SkateVector value) { return new Vector3((float)value.X, (float)value.Y, (float)value.Z); }
}
