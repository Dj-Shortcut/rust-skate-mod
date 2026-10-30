// While mounted the plugin owns the whole velocity of the movement body.
//   - The game's fixed step cancels any velocity written before it, so Step() must run from a
//     component created after the game's walk component (the late driver).
//   - The walk component must stay enabled: with it off the server pulls the player back.
//   - Engine methods the game itself never calls can be missing from the build, hence plain
//     arithmetic instead of UnityEngine.Mathf and a rotation built from its fields.
using System;
using Shortcut.RustMod;
using UnityEngine;

public enum RideMode { Off, Ground, Air, Grind, Bail }

public static class SkateRide
{
    // Ground speed remains tangent speed across changing slopes; server feedback and caps are X/Z.
    // RollDecel: what plain rolling loses, m/s each second. At 0.45 a board pushed to 20 km/h stood
    // still on a flat floor within seconds (owner, 15 October: the skate. games coast far longer);
    // 0.2 is about a real board on smooth concrete, 20 km/h rolling on for some 28 s.
    public const float PushAccel = 7f, PushSteady = 0.4f, PushKick = 2.7f, MaxPush = 11f, BrakeDecel = 12f, RollDecel = 0.2f, MaxSpeed = 25f, TurnRate = 150f, SlopeGain = 1.6f;
    // A powerslide: the brake with a turn held while rolling faster than PowerslideFrom swings the
    // board across the way of travel (PowerslideDegrees) and scrubs speed at PowerslideDecel; the
    // board keeps its line while it slides and swings back when the brake or the turn is let go.
    public const float PowerslideFrom = 2.5f, PowerslideDegrees = 85f, PowerslideDecel = 9f, PowerslideIn = 12f, PowerslideOut = 7f;
    public static float Powerslide, PowerslideSide;
    // Steering with the stick has weight, as in the skate. games: the turn builds over a moment as the
    // rider leans into it and eases out when let go, rather than the heading jumping with the stick;
    // and a board barely rolling turns only a little (a stopped board is turned by a kickturn, not a carve).
    public const float SteerIn = 7f, SteerOut = 10f, SteerStill = 0.35f, SteerFull = 2f;
    public static float SteerRate;
    // Low-speed axle pivots own a real movement step, never the manual score flag.
    public const float KickturnSpeed = .95f, KickturnRate = 120f, KickturnLimit = 180f, KickturnLiftSeconds = .16f, KickturnSetupSeconds = .22f;
    private static float kickturnLift, kickturnTurned, kickturnSetup, kickturnPoseAt = -99f;
    private static Transform kickturnPosePlayer;
    private static int kickturnFootVersion = -1;
    private static int kickturnSide;
    private static uint kickturnEpoch;
    private static bool kickturnRelease, kickturnRearm, kickturnNose;
    public static int Kickturns;
    public static bool Kickturning { get { return Mode == RideMode.Ground && kickturnSide != 0; } }
    public static int KickturnSide { get { return kickturnSide; } }
    public static float KickturnSetup { get { return KickturnCurve(kickturnSetup); } }
    public static float KickturnTip
    { get { return -kickturnSide * KickturnCurve(kickturnLift); } }
    private static bool strokeBack;
    public const float AirGravity = 16f, OlliePop = 6f, MaxFall = 30f, BailImpact = 12f, BailSeconds = 1.3f, BailDecel = 10f, PushPeriod = 0.8f, SeaLevel = -1.1f, LowestCap = 2f, LagStep = 0.1f, RaiseWait = 3f, CapForget = 30f, ManualPace = 1f, ManualHold = 0.3f, HopGrace = 0.3f;
    // These are frozen when the flight starts, not recomputed from a later ray/held grab.
    private static bool flatgroundFlight;
    private static float flatgroundFlightSeconds;
    public static float FlatgroundFlightSeconds { get { return flatgroundFlight ? flatgroundFlightSeconds : 0f; } }
    private static bool initialFlipFlick;
    private static float flightGravity = AirGravity;
    public static float CurrentAirGravity { get { return Mode == RideMode.Air ? flightGravity : AirGravity; } }
    public static float LaunchPopImpulse { get { return Mode == RideMode.Air && flatgroundFlight ? (float)SkateFlatgroundPop.ReferencePop : OlliePop; } }
    public static float CurrentRotationRateScale { get { return Mode == RideMode.Air && flatgroundFlight ? (float)SkateFlatgroundPop.RotationRateScale : 1f; } }
    public static float AirPopLift { get { return Mode == RideMode.Air && flatgroundFlight ? (float)SkateFlatgroundPop.VisualLift : SkateBoard.PopMost; } }
    public static bool FlipFlickKnown { get { return Mode == RideMode.Air && initialFlipFlick; } }
    public static float FlipFlickElapsed { get { return FlipFlickKnown ? Math.Max(0f, AirTime) : 0f; } }
    public static bool FlipFlickActive { get { return FlipFlickKnown && SkateFlipFlick.RemainingDelay(FlipFlickElapsed) > 0; } }
    // Pumping: the shared SkateRidePhysics judges it (a crouch held down a transition, let go
    // through its bottom); PumpCharge only shapes the rider's crouch. Landing: the fall becomes
    // roll on a slope by LandRoll (the shared landing rule); a revert within RevertWindow of a
    // landed trick turns the board round at RevertRate and keeps the combo.
    public const float PumpCharge = 0.4f, LandRoll = 0.8f, RevertWindow = 0.6f, RevertRate = 600f;
    public const long RevertPoints = 50;
    public const float RevertStick = 0.85f, RevertSweep = 180f;
    private static float spinAtLand;
    private static Vector3 landRoll; private static bool hasLandRoll;
    private const float RayLift = 0.6f, FeetProbe = 0.3f, AirProbe = 3f;
    // A lip ahead up to StepUp high (the seam between two foundations, a kerb) is stepped over:
    // the board is lifted onto it and the loss of way it costs is not read as an obstacle.
    public const float StepUp = 0.25f, StepLook = 0.35f, StepGrace = 0.25f;
    public static int StepUps;
    private static float steppedAt = -99f;
    private static Vector3 airViewFrom = Vector3.up;
    private static float airViewAt, airViewRise;
    private static SkateRiderAirFrameState airView;
    private static int airViewTrace;
    private static bool airViewFromGrind;
    private static bool airViewVertDeparture;
    private static int groundedRampStep = -2;
    private static float groundedRampAt;
    private static Vector3 groundedRampUp;
    public static readonly int GroundMask = ~((1 << 12) | (1 << 17) | (1 << 18) | (1 << 4) | (1 << 10) | (1 << 9) | (1 << 2));

    public static RideMode Mode = RideMode.Off;
    public static bool On { get { return Mode != RideMode.Off; } }
    public static bool Grounded, Braking, Jumped, TrickAir, Manual, NoseManual;
    public static SkateGrabKind Grab;
    public static SkateGroundGrabState GroundGrab;
    public static bool Grabbing { get { return Grab != SkateGrabKind.None; } }
    public static bool Frozen;
    public static float Speed, Yaw, Lean, VSpeed, AirTime, AirPeak, Clearance = 99f, Actual, ActualV, Top, Cap = MaxSpeed, ModeAt, LandAt = -99f, LandImpact, PushPhase, CapLoweredAt = -99f;
    public static Vector3 Normal = Vector3.up, Tangent = Vector3.forward, ViewNormal = Vector3.up, Position;
    public static Vector3 RiderViewNormal { get { return ViewNormal; } }
    public static float TransitionLandingApproach
    {
        get
        {
            if (Mode != RideMode.Air || airViewFrom.y >= .85f || !airView.Initialized || FootPlanted || Handplant ||
                SkateSurfaceRide.DisplayWall || SkateSurfaceRide.BodyFlip.Known || SkateActions.Owns ||
                SkateOldschoolRide.Active || SkateRiggingShowcase.Running || Scenarios.Active) return 0f;
            return (float)airView.LandingShare;
        }
    }
    public static float TransitionAirTuck
    {
        get
        {
            if (Mode != RideMode.Air || Jumped || airViewFrom.y >= .85f || airViewRise <= 0f ||
                FootPlanted || Handplant || Grabbing || SkateBoard.GrabFold > .001f ||
                SkateSurfaceRide.DisplayWall || SkateSurfaceRide.BodyFlip.Known ||
                SkateActions.Owns || SkateOldschoolRide.Active || SkateRiggingShowcase.Running || Scenarios.Active) return 0f;
            var apex = Math.Max(.18f, airViewRise / CurrentAirGravity);
            var phase = Clamp(1f - Math.Abs((AirTime - airViewAt) / apex - 1f), 0f, 1f);
            return phase * phase * (3f - 2f * phase) * (1f - (float)airView.LandingShare);
        }
    }
    // In the shared trick module's signs: positive spin and shove are frontside, positive flip is a
    // kickflip. Spin turns rider and board, shove the board alone under his feet.
    public static double SpinDeg, FlipDeg, ShoveDeg;
    // A half shove that was landed leaves the board the other way round under the same rider.
    public static bool DeckReversed;
    public static SkateTrickState Trick;
    public static string TrickName = "", LastAir = "", OffReason = "";
    public static long TrickPoints, Banked, ComboPoints;
    public static float TrickAt = -99f, BankedAt = -99f;
    public static int ResetsAll;
    public static int Steps, Resets, Pops, Lands, Bails, Pushes, GrindStarts, GrindEnds, Refused, Pumps, Reverts;
    public static float PumpHeld, RevertLeft;
    // The shared ride physics: the slope along the actual travel, the pump's pass, and the normal of
    // the ground last hit under the board, which is what a landing meets.
    public static float SlopeDegrees;
    public static SkatePumpState Pump = default(SkatePumpState);
    private static uint pumpInputEpoch;
    private static uint jumpInputEpoch; private static SkateOllieStyle wantStyle;
    public static Vector3 ActionVelocity { get { return velocity; } }
    public static Vector3 ContactNormal = Vector3.up;
    public static string PhysicsError = "";
    public static SkateGrindKind GrindKind;
    public static bool GrindFrontside, GrindAlong;
    // A stall holds the board still on an edge (Mode is Grind, GrindKind a stall kind); a handplant
    // is the stall with a hand on the edge. StallMax: how long one is held before rolling off.
    public const float StallSpeed = 7f, StallMax = 2.5f, StallOut = 2.5f;
    // How near a half flip the board must be for the edge to take it upside down (the scorer's tolerance).
    public const double DarkCatch = 25.0;
    private static bool spinLate;
    // How much higher than the take-off the ground under the board must be for that.
    public const float DarkOver = 0.15f;
    private static float Over() { return Clearance > 50f ? -99f : Position.y - Clearance - takeOffY; }
    // A handplant comes down before it leaves the edge: the body swings back onto the board over
    // HandDownSeconds, held still on the edge meanwhile.
    public const float HandDownSeconds = 0.4f;
    private static float handDownAt = -99f; private static bool handDownPop; private static uint handDownInputEpoch;
    public static bool HandplantDown { get { return Stalling && handDownAt > 0f; } }
    public static bool Stalling;
    public static SkateHandplantKind HandplantVariantUsed;
    public static int Stalls;
    private static float stallAt, stallSign = 1f;
    public static bool Handplant { get { return Stalling && GrindKind == SkateGrindKind.Handplant; } }
    // A no comply or a boneless: the front foot is down beside the board for a moment after the pop.
    public const float PlantSeconds = 0.25f;
    public static float PlantedUntil = -99f;
    public static bool FootPlanted { get { return Mode == RideMode.Air && now < PlantedUntil; } }
    // The push with the front foot (mongo), as the push began.
    public static int Powerslides;
    public static bool PushMongo;
    // Which foot a push uses: the rider's back foot (the right one for a regular rider), or the
    // front one in a mongo push, whichever end of the board leads. Rolling tail first, after a
    // rollback, a revert or a 180 alike, the back foot is the one at the leading end, and it pushes
    // from there, as a skater pushing fakie does; the front foot pushing there would be a mongo push
    // the rider never asked for. PushLead: the foot that leads in the way of travel pushes.
    public static bool PushLead;
    // The deck's angle to the edge, taken when the grind begins and kept through it.
    public static float GrindDeck;
    private static float grindCross, grindNoseFar, grindNose, airNoseOffset, airDeckOffset, grindAcross, grindCrossing;
    private static bool grindClassified, airFromValid;
    private static Vector3 airFrom;
    // Off an edge the board comes round to the way of travel over a moment, not in a jump: the pose
    // it had on the edge is kept as an air offset that runs out at ComeRoundRate.
    private const float ComeRoundRate = 540f;
    private static bool comingRound;
    private static Vector3 revertDir;
    private static float revertSign = 1f;
    private static float revertEndAt = -99f;
    // The revert's turn so far: the rider and the board pivot together on the ground, the way of
    // travel unchanged, and at the end the half turn is the stance's (the scorer's Switch) instead.
    private static float revertTurned, revertStep;
    private static bool landedTrick;
    private static Rigidbody body;
    private static Vector3 lastPos, lastDir, velocity;
    private static float lastSpeed, lastPull = -999f, raisedAt = -999f, putBackAt = -999f, raiseWait = RaiseWait, now, mountAt, takeOffY, flipLeft, shoveLeft, jumpWanted = -99f;
    private static bool hasLast, refusedSinceRaise, spinning;
    private static int stuck;
    // Held back: for HeldSteps physics steps in a row the board got less than HeldShare of what it
    // rode. The speed follows what it got, plus HeldLeft.
    public static bool Held;
    public const int HeldSteps = 3;
    public const float HeldShare = 0.35f, HeldLeft = 0.3f;
    private static int heldSteps;
    // The pop that was asked for, kept with the press for the moment the press itself is kept; what
    // of it is still to be started in the air; and which way a frontside turn goes in this jump.
    private static SkatePopKind wantKind = SkatePopKind.Ollie, popKind;
    private static int wantFlip, pendFlip;
    private static SkateBoardTrickKind wantSpecial;
    private static int wantSpecialDirection;
    public static SkateBoardPathSample BoardPath { get; private set; }
    // In the air: degrees of body varial still to turn (the rider only; the board keeps its yaw),
    // the rider's turn so far, and whether the flip or shove under way was asked for in the air.
    private static float bodyLeft;
    public static double BodyDeg;
    public static bool LateTrick;
    // A flip or shove asked for this long after take-off or later is late: part of the pop before that.
    public const float LateAfter = 0.25f;
    private static float wantShove, wantScale = 1f, pendShove, spinSide = 1f;
    private const float SpinSlack = 15f;
    // The chest ahead of a spin (degrees, positive frontside): turned into it while it goes on and
    // brought back as the half turn completes; on the ground, wound up against the one being asked for.
    public static float SpinLead;
    public const float SpinLeadMost = 32f, SpinWindUp = 18f;

    public static Vector3 Dir(float yaw) { return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; }
    public static float Toward(float a, float b, float step) { return Math.Abs(b - a) <= step ? b : a + Math.Sign(b - a) * step; }
    public static float Delta(float from, float to) { var d = (to - from) % 360f; if (d > 180f) d -= 360f; if (d < -180f) d += 360f; return d; }
    public static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }
    public static Quaternion Turn(float degrees, Vector3 axis)
    {
        var n = axis.normalized; var half = degrees * (float)Math.PI / 360f; var s = (float)Math.Sin(half);
        var q = default(Quaternion); q.x = n.x * s; q.y = n.y * s; q.z = n.z * s; q.w = (float)Math.Cos(half);
        return q;
    }

    // Frontside is the turn that brings the rider's chest round to the way he travels: to the left
    // for a regular rider going nose first, to the right when the tail leads, and the other way
    // round for a goofy rider. NoseYaw is where the
    // rider's stance points (spin included), DeckYaw where the board's own nose points.
    public static float SpinYaw { get { return -(float)SpinDeg * spinSide; } }
    // A body varial turns the rider's stance and nothing else: the deck keeps its own yaw.
    public static float BodyYaw { get { return -(float)BodyDeg * spinSide; } }
    public static float SlideYaw { get { return Mode == RideMode.Ground ? PowerslideDegrees * Powerslide * PowerslideSide : 0f; } }
    public static float NoseYaw { get { return Mode == RideMode.Grind ? SkateGrind.Heading + grindNose : Yaw + (Trick.Switch ? 180f : 0f) + SpinYaw + BodyYaw + SlideYaw + revertTurned + airNoseOffset; } }
    public static float DeckYaw { get { return Mode == RideMode.Grind ? SkateGrind.Heading + GrindDeck : NoseYaw - BodyYaw - (float)ShoveDeg * spinSide + (DeckReversed ? 180f : 0f) + airDeckOffset; } }
    private static float HorizontalFactor { get { return Mode == RideMode.Ground ? Flat(Tangent).magnitude : 1f; } }
    public static float HorizontalSpeed { get { return Speed * HorizontalFactor; } }
    private static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
    // The way along the ground that goes the heading's way. The shared frame (its ground is at most
    // 72 degrees, normal y over 0.3) keeps the heading as the horizontal part and gives it the rise
    // the normal asks for: the heading's part into the slope, up the slope, by 1/y. Up a transition
    // the normal tilts on to level at the lip (SteepSlope), where that rise would be unbounded and
    // the heading's part along the face would vanish with it, so a diagonal line up the wall became
    // a straight vertical launch. Past the shared limit the factor is held at its value there: the
    // part into the face goes up it, the part along the face (along the coping) stays, and the two
    // agree with the shared frame at 72 degrees.
    private const float SharedNormalLeast = 0.3f;
    public static Vector3 SurfaceTangent(Vector3 normal, Vector3 heading)
    {
        var level = new Vector3(normal.x, 0f, normal.z); var lean = level.magnitude;
        if (lean < 0.0001f) return heading;
        level /= lean;
        var into = -(heading.x * level.x + heading.z * level.z);
        var along = heading + level * into;
        var up = new Vector3(-level.x * normal.y, lean, -level.z * normal.y);
        var v = along + up * (into / Math.Max(normal.y, SharedNormalLeast));
        return v.sqrMagnitude > 0.000001f ? v.normalized : heading;
    }
    private static void LimitSpeed(float forward, float reverse)
    {
        var h = HorizontalFactor;
        // Straight up a lip there is no horizontal pace to cap.
        if (h <= 0.0001f) return;
        Speed = Clamp(Speed, -reverse / h, forward / h);
        if (Mode == RideMode.Grind) SkateGrind.Speed = Speed;
    }
    // Which end leads: the stance's half turns, and whether the board rolls against its heading. The
    // latter is read with a band (from -0.5 m/s back, to -0.1 m/s forward again), so a board rocking
    // about a standstill does not turn the rider's chest back and forth.
    public static bool TailFirst { get { return Trick.Switch != rollingBack; } }
    private static bool rollingBack;
    // What stands in the way, for the log: a ray just over the ground and one at the knee, ahead.
    // A lip stops only the low one; a wall both.
    private static string HeldAhead(Vector3 pos)
    {
        var ahead = Dir(Yaw) * (Speed < 0f ? -1f : 1f);
        var line = "";
        foreach (var h in new[] { 0.08f, 0.5f })
        {
            RaycastHit hit;
            if (SkateParks.Ray(pos + Vector3.up * h, ahead, out hit, 1.5f, GroundMask))
                line += " | " + h.ToString("F2") + " m up: hit at " + hit.distance.ToString("F2") + " m, normal y " + hit.normal.y.ToString("F2");
            else line += " | " + h.ToString("F2") + " m up: clear";
        }
        return line;
    }

    public static void SenseRollingBack() { if (Speed < -0.5f) rollingBack = true; else if (Speed > -0.1f) rollingBack = false; }
    // The rider's stance: regular (left foot forward, chest to the right of the nose) or goofy, the
    // mirror image. Read from the stance file at start, switched on foot with J or Back+Y, and kept.
    public static bool Goofy;
    public static float StanceSign { get { return Goofy ? -1f : 1f; } }
    public const string StanceFile = "skate-stance.txt";
    public static string StanceName { get { return Goofy ? "goofy" : "regular"; } }
    public static string StancePath()
    {
        try { return System.IO.Path.Combine(Application.persistentDataPath, StanceFile); } catch (Exception) { return null; }
    }
    public static void LoadStance()
    {
        try { var path = StancePath(); Goofy = path != null && System.IO.File.Exists(path) && System.IO.File.ReadAllText(path).Trim().ToLowerInvariant() == "goofy"; }
        catch (Exception e) { Goofy = false; Out.Say("STANCE could not be read: " + e.GetType().Name + ": " + e.Message); }
    }
    // A tap never changes the stance: F2 (J is the game's clan key, which frees the cursor) or Y in
    // the controller's menu has to be held this long. A stance changed by a stray press was the rider pushing with the wrong foot.
    public const float StanceHoldSeconds = 1f;
    // Called each frame with whether the key is held; switches once per hold, after StanceHoldSeconds.
    public static void StanceHold(ref float since, bool held, float now)
    {
        if (!held) { since = -1f; return; }
        if (since < 0f) { since = now; SkatePieces.Said = "Stance: " + StanceName + " - hold to switch"; SkatePieces.SaidAt = now; return; }
        if (since > 0f && now - since >= StanceHoldSeconds) { ToggleStance(); since = 0f; }
    }
    // Only off the board: the feet are placed on getting on.
    public static bool ToggleStance()
    {
        if (On) return false;
        Goofy = !Goofy;
        try { var path = StancePath(); if (path != null) System.IO.File.WriteAllText(path, StanceName); }
        catch (Exception e) { Out.Say("STANCE could not be kept: " + e.GetType().Name + ": " + e.Message); }
        Out.Say("STANCE " + StanceName);
        SkatePieces.Said = "Stance: " + StanceName + " (" + (Goofy ? "right" : "left") + " foot forward)"; SkatePieces.SaidAt = Time.realtimeSinceStartup;
        return true;
    }
    // Which way round a frontside turn goes from here: the sign a left-stick turn gets in the trick module.
    public static float SpinSide { get { return (TailFirst ? -1f : 1f) * StanceSign; } }
    public static bool Rotating { get { return BoardPath.Active || spinning || Math.Abs(flipLeft) > 0.001f || Math.Abs(shoveLeft) > 0.001f || Math.Abs(bodyLeft) > 0.001f; } }
    // Read-only presentation observations. These do not request or advance a trick.
    public static bool PresentationQueued { get { return Mode == RideMode.Ground && now - jumpWanted >= 0 && now - jumpWanted < .12f && jumpInputEpoch == SkateKeys.InputEpoch; } }
    public static float PresentationStepSeconds
    { get { var step = Time.fixedDeltaTime; return step > .001f && step <= .05f ? step : .03125f; } }
    public static double PresentationBodyTarget { get { return BodyDeg + bodyLeft; } }
    public static SkateCaptureTrickSignature PresentationSignature
    {
        get
        {
            bool queued = PresentationQueued;
            double flip = queued ? wantFlip * 360.0 : FlipDeg + flipLeft + pendFlip * 360.0;
            double shove = queued ? wantShove : ShoveDeg + shoveLeft + pendShove;
            double spin = SpinDeg;
            double asked = -SkateKeys.Spin * (queued ? SpinSide : spinSide);
            if (Math.Abs(asked) > .001) spin = Math.Sign(asked) * Math.Ceiling(Math.Abs(spin) / 180.0 + .000001) * 180.0;
            else spin = Math.Round(spin / 180.0, MidpointRounding.AwayFromZero) * 180.0;
            return new SkateCaptureTrickSignature(queued ? wantKind : popKind,
                Trick.Airborne ? Trick.PopSwitch : Trick.Switch, Trick.Airborne ? Trick.PopTailLeads : TailFirst,
                flip, shove, spin, PresentationBodyTarget);
        }
    }

    public static void ForgetServer() { Cap = MaxSpeed; raiseWait = RaiseWait; refusedSinceRaise = false; raisedAt = putBackAt = -999f; }

    public static void Mount(Rigidbody b, float yaw)
    {
        observedSupportValid = false;
        airViewVertDeparture = false; groundedRampStep = -2;
        body = b; Yaw = yaw; flatgroundFlight = initialFlipFlick = false; flightGravity = AirGravity;
        try
        {
            var cap = b.gameObject.GetComponent<CapsuleCollider>();
            CapsuleReach = cap != null ? Math.Max(0f, cap.radius - WallProbe) : 0.2f;
            CapsuleRadius = cap != null ? Math.Max(0f, Math.Min(1f, cap.radius)) : 0.5f;
            if (!capsuleSaid) { capsuleSaid = true; Out.Say("SKATE capsule " + (cap != null ? "radius " + cap.radius.ToString("F2") + " height " + cap.height.ToString("F2") : "not found") + "; walls are swept " + CapsuleReach.ToString("F2") + " m past the probe"); }
        }
        catch (Exception e) { CapsuleReach = 0.2f; if (!capsuleSaid) { capsuleSaid = true; Out.Say("SKATE capsule unreadable: " + e.GetType().Name + ": " + e.Message); } }
        var v = b.linearVelocity; velocity = v; var rise = v.y; v.y = 0f;
        Speed = Vector3.Dot(v, Dir(yaw));
        if (SkateKeys.Scripted) SkateKeys.EndStep(); else SkateKeys.Reset();
        SkateBoard.BeginRide();
        RiderRig.BeginRide(); SkateActions.Reset(); SkateOldschoolRide.Reset(); SkateSurfaceRide.Reset(); wantStyle = SkateOllieStyle.None; HandplantVariantUsed = SkateHandplantKind.None;
        hasLast = false; pastCount = 0; Steps = 0; Resets = 0; Top = 0f; Lean = 0f; SteerRate = 0f; Powerslide = 0f; PowerslideSide = 1f; lastPull = mountAt = Time.realtimeSinceStartup;
        // A cap from a ride a while ago says nothing about this one.
        if (mountAt - putBackAt > CapForget) ForgetServer();
        VSpeed = 0f; AirTime = 0f; flipLeft = shoveLeft = pendShove = bodyLeft = 0f; pendFlip = 0; SpinDeg = FlipDeg = ShoveDeg = BodyDeg = 0; LateTrick = false; Jumped = TrickAir = Braking = Manual = NoseManual = spinning = DeckReversed = false; PushPhase = 0f; stuck = 0; heldSteps = 0; Held = false; ResetKickturn();
        Grab = SkateGrabKind.None; GroundGrab = default(SkateGroundGrabState); BoardPath = default(SkateBoardPathSample); wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0; spinSide = 1f; wantKind = SkatePopKind.Ollie; wantFlip = 0; wantShove = 0f; wantScale = 1f;
        PumpHeld = RevertLeft = revertTurned = 0f; landedTrick = false; steppedAt = -99f; StepUps = 0; Pump = default(SkatePumpState); SlopeDegrees = 0f; ContactNormal = Vector3.up; OnRamp = false; GrindKind = SkateGrindKind.None; GrindFrontside = GrindAlong = false; GrindDeck = 0f; grindClassified = airFromValid = false; Stalling = false; Stalls = 0; PlantedUntil = -99f; PushMongo = PushLead = false; handDownAt = -99f; rollingBack = false;
        Trick = new SkateTrickState(false); TrickName = ""; ComboPoints = 0; TrickAt = BankedAt = LandAt = jumpWanted = -99f; OffReason = "";
        SkateHud.Clear();
        Normal = ViewNormal = Vector3.up; Position = b.position; Tangent = Dir(yaw);
        airViewFrom = Vector3.up; airViewAt = airViewRise = 0f; ResetAirDisplay();
        airNoseOffset = airDeckOffset = 0f; comingRound = false;
        SkateWatch.Reset();
        SkateGrind.Reset();
        // Getting on while going up (the second press of a double jump) finishes that jump on the
        // board; the ground is still within reach of the feet then, and ground movement would cut it.
        if (rise > 1f) TakeOff(false, rise);
        else
        {
            Enter(RideMode.Ground); Sense(Position); Tangent = SurfaceTangent(Normal, Dir(Yaw));
            Speed = Vector3.Dot(velocity, Tangent);
            LimitSpeed(Cap, Cap);
        }
        SkatePhysicsDiagnostic.NewRide();
    }

    public static void Dismount(string reason)
    {
        observedSupportValid = false;
        airViewVertDeparture = false; groundedRampStep = -2;
        SkateRig.ReleaseRootProxy();
        if (Mode == RideMode.Off) return;
        if (SkateOldschoolRide.Active) SkateBoard.ReleaseIndependent(SkateOldschoolRide.Frame, V(SkateOldschoolRide.State.Velocity));
        airViewFrom = Vector3.up; airViewAt = airViewRise = 0f; ResetAirDisplay();
        Mode = RideMode.Off; SkateActions.Reset(); SkateOldschoolRide.Reset(); SkateSurfaceRide.Reset(); HandplantVariantUsed = SkateHandplantKind.None; GroundGrab = default(SkateGroundGrabState); BoardPath = default(SkateBoardPathSample); wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0; Pump = default(SkatePumpState); PumpHeld = RevertLeft = 0f; Stalling = false; grindClassified = airFromValid = false; OffReason = reason; Grab = SkateGrabKind.None; Manual = NoseManual = false; ResetKickturn();
        if (SkateKeys.Scripted) SkateKeys.EndStep(); else SkateKeys.Reset();
    }

    private static void Enter(RideMode m)
    {
        if (m != RideMode.Ground) ResetKickturn();
        if (m != RideMode.Air) airViewVertDeparture = false;
        if (m != RideMode.Ground) groundedRampStep = -2;
        // A pump's pass lives on the ground: leaving it (air, grind, bail) forgets the charge, so a
        // crouch let go in the air cannot be paid out on the next ground step.
        if (m != RideMode.Ground && Mode == RideMode.Ground) { Pump = default(SkatePumpState); PumpHeld = 0f; RevertLeft = revertTurned = 0f; }
        if (m != RideMode.Air) { flatgroundFlight = initialFlipFlick = false; flightGravity = AirGravity; }
        Mode = m; ModeAt = Time.realtimeSinceStartup;
    }

    public static string Status()
    {
        return Mode + " speed=" + HorizontalSpeed.ToString("F1") + " actual=" + Actual.ToString("F1") + " top=" + Top.ToString("F1") + " cap=" + Cap.ToString("F1") + " yaw=" + Yaw.ToString("F0")
            + " grounded=" + Grounded + " pull-backs=" + Resets + " total=" + Trick.TotalPoints + (body != null ? " pos=" + Out.V1(body.position) : "");
    }

    public static void Step()
    {
        observedSupportValid = false;
        if (Mode == RideMode.Off) return;
        var supportStep = Steps;
        var completed = false;
        try
        {
            if (body == null) { Dismount("the movement body is gone"); return; }
            if (Frozen)
            {
                // The preview stages the drawn rig only. Keep the actual stationary body still;
                // a moved/changed player cancels the preview instead of being teleported back.
                if (SkateRiggingShowcase.Running) body.linearVelocity = Vector3.zero;
                hasLast = false; pastCount = 0; return;
            }
            var dt = Time.fixedDeltaTime;
            if (!(dt > 0.001f && dt <= 0.05f)) dt = 0.03125f;
            now = Time.realtimeSinceStartup;
            var pos = body.position; var game = body.linearVelocity;
            SkatePhysicsDiagnostic.BeginStep(pos, game, velocity, Time.fixedDeltaTime, dt);
            Position = pos;
            if (pos.y < SeaLevel && OpenSky(pos)) { Dismount("in the water"); return; }
            SenseRollingBack();
            Measure(pos, dt);
            Sense(pos);
            SkateSurfaceRide.AdvanceDisplay(pos, dt);
            SkateSurfaceRide.ConfirmDropIn(pos);
            if (Mode != RideMode.Grind) SkateGrind.Tick(dt);
            if (SkateKeys.JumpPressed) { jumpWanted = now; jumpInputEpoch = SkateKeys.InputEpoch; wantKind = SkateKeys.PopKind; wantStyle = SkateKeys.PopStyle; wantFlip = SkateKeys.PopFlip; wantShove = SkateKeys.PopShove; wantScale = SkateKeys.PopScale; wantSpecial = SkateKeys.PopSpecial; wantSpecialDirection = SkateKeys.PopSpecialDirection; }
            if (Mode == RideMode.Ground && now - jumpWanted >= 0 && now - jumpWanted < .12f &&
                (jumpInputEpoch != SkateKeys.InputEpoch || !(SkateKeys.Scripted || SkateKeys.ControlsActive))) jumpWanted = -99f;
            if (Mode == RideMode.Ground && SkateSurfaceRide.DisplayWall && now - jumpWanted >= 0 && now - jumpWanted < .12f)
                jumpWanted = Grounded && SkateKeys.GroundGrab == SkateGroundGrabKind.None && jumpInputEpoch == SkateKeys.InputEpoch &&
                    (SkateKeys.Scripted || SkateKeys.ControlsActive) ? now : -99f;
            GroundGrabTick(dt);
            if (Mode == RideMode.Ground) GroundStep(dt);
            else if (Mode == RideMode.Air) AirStep(pos, dt);
            else if (Mode == RideMode.Grind) GrindStep(pos, dt);
            else BailStep(game, dt);
            if (Mode == RideMode.Off) return;
            SurfaceGroundContact(pos);
            if (Cap < MaxSpeed && now - lastPull > raiseWait)
            {
                Cap = Math.Min(MaxSpeed, Cap + (refusedSinceRaise ? 0.5f : 1f));
                if (!refusedSinceRaise) raiseWait = RaiseWait;
                refusedSinceRaise = false; lastPull = raisedAt = now;
            }
            var flat = Flat(velocity); var pace = flat.magnitude;
            if (pace > Cap && pace > 0f)
            {
                var scale = Cap / pace; velocity.x *= scale; velocity.z *= scale;
                if (Mode == RideMode.Ground) velocity.y += Tangent.y * Speed * (scale - 1f);
                Speed *= scale; if (Mode == RideMode.Grind) SkateGrind.Speed = Speed;
                flat = Flat(velocity); pace = flat.magnitude;
            }
            if (Mode == RideMode.Ground || Mode == RideMode.Air)
            {
                velocity = WallAhead(pos, velocity, dt);
                flat = Flat(velocity); pace = flat.magnitude;
            }
            float boundedX, boundedZ;
            if (!SkateRidePhysics.TryBoundHorizontal(pos.x, pos.z, velocity.x, velocity.z, dt, Cap, out boundedX, out boundedZ))
            { Dismount("invalid movement speed budget"); return; }
            if (boundedX != velocity.x || boundedZ != velocity.z)
            {
                var previousPace = Math.Sqrt((double)velocity.x * velocity.x + (double)velocity.z * velocity.z);
                var scale = previousPace > 0 ? (float)(Math.Sqrt((double)boundedX * boundedX + (double)boundedZ * boundedZ) / previousPace) : 0f;
                if (Mode == RideMode.Ground) velocity.y += Tangent.y * Speed * (scale - 1f);
                velocity.x = boundedX; velocity.z = boundedZ;
                Speed *= scale; if (Mode == RideMode.Grind) SkateGrind.Speed = Speed;
                flat = Flat(velocity); pace = flat.magnitude;
            }
            if (Mode == RideMode.Air && SkateSurfaceRide.Active)
            {
                // The forward sweep/budget can shorten an attached wall step (an end wall or
                // corner). Carry the actual accepted velocity into the next support decision.
                var nose = NoseYaw; var deck = DeckYaw; var direction = Speed < 0 ? -1f : 1f;
                if (flat.sqrMagnitude > .0001f) Yaw = (float)(Math.Atan2(flat.x * direction, flat.z * direction) * 180 / Math.PI);
                Speed = flat.magnitude * direction; VSpeed = velocity.y; KeepAirPose(nose, deck);
            }
            if (pace > Top) Top = pace;
            SkatePhysicsDiagnostic.EndStep(velocity);
            body.linearVelocity = velocity;
            var sign = Speed < 0f ? -1f : 1f;
            lastPos = pos; lastDir = pace > 0.0001f ? flat / pace * sign : Dir(Yaw); lastSpeed = pace * sign; hasLast = true; Steps++;
            pastPos[pastNext] = pos; pastAt[pastNext] = now; pastNext = (pastNext + 1) % PastSteps; if (pastCount < PastSteps) pastCount++;
            if ((Mode == RideMode.Air || Mode == RideMode.Ground) && SkateSurfaceRide.DisplayWall && SkateSurfaceRide.WallFrame.Initialized)
            {
                ViewNormal = V(SkateSurfaceRide.WallFrame.Up);
                airViewVertDeparture = false; airViewFrom = ViewNormal; airViewAt = AirTime; airViewRise = Math.Max(0, VSpeed); ResetAirDisplay();
            }
            else if (Mode == RideMode.Air) AirView(dt);
            else
            {
                var target = Mode == RideMode.Ground ? Normal : Vector3.up;
                ViewNormal = (ViewNormal + (target - ViewNormal) * 0.3f).normalized;
            }
            SkateKeys.EndStep();
            // Sense stamped the step being executed. Publish only that hit to
            // the completed counter consumed by the following render frames.
            if (observedSupportValid && observedSupportStep == supportStep && Steps == supportStep + 1)
                observedSupportStep = Steps;
            else observedSupportValid = false;
            completed = true;
        }
        catch (Exception e) { Dismount("the step threw"); Out.Say("SKATE step threw " + e.GetType().Name + ": " + e.Message); }
        finally { if (!completed) observedSupportValid = false; }
    }

    // A wall the next step would reach. The physics engine moves the player's capsule one step at
    // a time without sweeping it, and a step at riding speed (0.8 m at 25 m/s) carries the capsule
    // into a thin wall, where it stays wedged: the game's collision then holds it wherever the
    // board points. An owner's ride into a thatch wall at 26 m/s was held on that spot for more
    // than 500 steps, with nothing ahead of it. So the step is swept first: what would go into a
    // steep face is taken out (a glancing hit slides along it), and a hard hit is a bail.
    public const float WallProbe = 0.3f, WallSkin = 0.1f, WallSlam = 8f, WallFrom = 3f;
    private static readonly float[] wallHeights = { 0.6f, 1.3f };
    public static int WallStops, WallSlams;
    // A transition's riding face is ground to roll up or come down onto, including contact while
    // rising through the air. Its undersides, side edges and coping stay walls. A sphere sweep's
    // normal points at the sphere, not out of the face (round on an edge
    // or the coping), so the face is read again with a ray to the contact. On the face itself the
    // two agree; on a transition's open side edge (the drawn surface has no side face) the ray
    // still finds the upward face, but the sweep's normal turns out sideways, and that stays a wall.
    // The face is ridden as far as the vertical lip, so a face is one up to level.
    private static bool RampFaceAhead(Vector3 from, RaycastHit hit)
    {
        if (Mode != RideMode.Ground && Mode != RideMode.Air) return false;
        if (!SkateParks.IsRamp(hit) || SkateParks.IsCoping(hit)) return false;
        var to = hit.m_Point - from; var reach = to.magnitude;
        RaycastHit face;
        if (reach < 0.001f || !SkateParks.Ray(from, to / reach, out face, reach + WallProbe, GroundMask) || face.collider != hit.collider) return false;
        return face.m_Normal.y > -0.05f && Vector3.Dot(face.m_Normal, hit.m_Normal) > 0.9f;
    }
    private static bool RidingWallFace(Vector3 from, Vector3 dir, float distance, RaycastHit hit)
    {
        return RampFaceAhead(from, hit) || ((Mode == RideMode.Ground || Mode == RideMode.Air) &&
            SkateParks.MegaLipFace(from, dir, WallProbe, distance, hit));
    }
    // The whole measured pace of the last step, along the path the body took (for the checks' logs).
    public static float PathSpeed;
    private static Vector3 WallAhead(Vector3 pos, Vector3 v, float dt)
    {
        var pace = v.magnitude;
        if (pace < WallFrom) return v;
        var dir = v / pace; var reach = pace * dt + CapsuleReach + WallSkin;
        var found = false; var nearest = float.MaxValue; var normal = Vector3.zero;
        for (var i = 0; i < wallHeights.Length; i++)
        {
            RaycastHit hit;
            var from = pos + Vector3.up * wallHeights[i];
            if (SkateParks.SphereWall(from, WallProbe, dir, out hit, reach, GroundMask, RidingWallFace)
                && hit.distance < nearest) { found = true; nearest = hit.distance; normal = hit.m_Normal; }
        }
        if (!found) return v;
        var into = -Vector3.Dot(v, normal);
        if (into <= 0f) return v;
        var kept = v + normal * into;
        WallStops++;
        if (into > WallSlam && Mode == RideMode.Ground)
        {
            WallSlams++;
            Out.Say("SKATE wall: hit at " + into.ToString("F1") + " m/s into it, " + nearest.ToString("F2") + " m ahead at " + Out.V3(pos) + "; bail");
            Bail(); Speed = 0f;
            return new Vector3(0f, Math.Min(0f, v.y), 0f);
        }
        // Along the ground the speed follows what is left of the step, so the next step does not
        // ride on into the wall.
        if (Mode == RideMode.Ground)
        {
            var left = Flat(kept).magnitude; var had = Flat(v).magnitude;
            if (had > 0.0001f) Speed *= left / had;
        }
        if (WallStops <= 20 || WallStops % 50 == 0) Out.Say("SKATE wall: " + into.ToString("F1") + " m/s into it taken out (" + WallStops + ") at " + Out.V3(pos));
        return kept;
    }
    // How far the player's capsule reaches past the probe's sphere, read from its collider.
    private static float CapsuleReach = 0.2f;
    // The capsule's radius, for its rest on a slope (Sense); a slope over 78 degrees counts as 78.
    private static float CapsuleRadius = 0.5f;
    public static float SurfaceBodyGap { get { return Math.Max((float)SkateWallTricks.RideBodyGap, CapsuleRadius); } }
    // Rotate the ordinary display about the capsule sphere centre. Independent
    // wall, plant, detached-board and showcase routes retain their own anchors.
    public static Vector3 DisplaySupportOffset
    {
        get { return (Mode == RideMode.Ground || Mode == RideMode.Air) && !Handplant &&
            !SkateSurfaceRide.DisplayWall && !SkateOldschoolRide.Active && !SkateActions.Owns &&
            !SkateRiggingShowcase.Running && !Scenarios.Active
                ? (Vector3.up - ViewNormal) * CapsuleRadius : Vector3.zero; }
    }
    private const float CapsuleSlackMost = 2f;
    private static bool capsuleSaid;

    // Where the body was over the last steps (#487). A server that puts a player back sets it on a
    // position it had before, so a put-back landing on one of these, a few steps old, is the
    // server's; one landing nowhere the body has been is the client's own physics.
    private const int PastSteps = 64;
    private static readonly Vector3[] pastPos = new Vector3[PastSteps];
    private static readonly float[] pastAt = new float[PastSteps];
    private static int pastNext, pastCount;

    private static string NearestPast(Vector3 pos)
    {
        if (pastCount == 0) return "no earlier steps";
        // Along the path between two kept steps, so the spacing of the steps (0.3 m at 9 m/s) does
        // not count as a miss.
        var best = float.MaxValue; var age = 0f;
        var newest = (pastNext - 1 + PastSteps) % PastSteps;
        for (var n = 0; n < pastCount; n++)
        {
            var i = (newest - n + PastSteps) % PastSteps;
            var a = pastPos[i]; var d = (a - pos).magnitude; var t = pastAt[i];
            if (n + 1 < pastCount)
            {
                var j = (i - 1 + PastSteps) % PastSteps; var ab = pastPos[j] - a; var len = ab.sqrMagnitude;
                if (len > 0.000001f)
                {
                    var f = Clamp(Vector3.Dot(pos - a, ab) / len, 0f, 1f);
                    d = (a + ab * f - pos).magnitude; t = pastAt[i] + (pastAt[j] - pastAt[i]) * f;
                }
            }
            if (d < best) { best = d; age = now - t; }
        }
        return "nearest earlier path " + best.ToString("F2") + " m away, " + age.ToString("F2") + " s old" + (best < 0.1f ? " (back where it was)" : " (nowhere it was)");
    }

    private static void Measure(Vector3 pos, float dt)
    {
        if (!hasLast) return;
        var moved = pos - lastPos;
        // The whole path, up a transition as much as along the flat: on a steep bank the
        // horizontal part of a step is small while the board rides the full step along the slope.
        var path = moved.magnitude / dt;
        PathSpeed = path;
        ActualV = moved.y / dt; moved.y = 0f;
        Actual = Vector3.Dot(moved, lastDir) / dt;
        // Through a revert the board's heading and the way of travel part: no pull-back can be read.
        if (RevertLeft != 0f || now - revertEndAt < 0.15f) return;
        if ((lastSpeed > 1f && Actual < -3f) || (lastSpeed < -1f && Actual > 3f))
        {
            // The server put the player back. That is logged and nothing more: the board never
            // slows itself down for the server. A server that refuses skate speed has to allow it
            // (the ShortcutSkate plugin, or its own speed checks turned off by the admin).
            Resets++; ResetsAll++;
            Out.Say("SKATE put back #" + Resets + ", " + (now - mountAt).ToString("F2") + " s after getting on: moved " + Out.V3(pos - lastPos) + " in a step at speed " + lastSpeed.ToString("F1") + "; " + NearestPast(pos));
            return;
        }
        if (now - steppedAt < StepGrace) { heldSteps = 0; return; }
        // Held back by anything on the ground or along an edge, a wall, a post, a fence: the board
        // did not get as far as it rode for a few steps in a row. Its speed then comes down to what
        // it actually does, so the meter and the tricks do not run on against the wall; a little is
        // left so that a push away from it starts again. A single short step is not that: a lip
        // stepped over, a slope, a frame of the server's. Measured along the whole path, so that a
        // steep transition, where little of a step is horizontal, is not taken for a wall.
        var rode = Math.Abs(Speed); var got = path;
        if ((Mode == RideMode.Ground || Mode == RideMode.Grind) && rode > 1f && got < rode * HeldShare) heldSteps++; else heldSteps = 0;
        Held = heldSteps >= HeldSteps;
        if (Held)
        {
            if (heldSteps == HeldSteps) Out.Say("SKATE held back: rode " + rode.ToString("F1") + " m/s, got " + got.ToString("F1") + (Mode == RideMode.Grind ? " on the grind" : "") + " at " + Out.V3(pos) + HeldAhead(pos));
            if (Mode != RideMode.Grind) { var sign = Speed < 0f ? -1f : 1f; Speed = sign * Math.Min(rode, got + HeldLeft); }
            return;
        }
        // A wall right in front stops the board at once: it did not get as far as it rode and
        // something steep stands right in front of it.
        if (rode < 0.5f || got > rode * 0.5f) return;
        RaycastHit wall;
        var ahead = new Vector3(lastDir.x, 0f, lastDir.z) * Math.Sign(Speed);
        var from = pos + Vector3.up * 0.3f;
        if (ahead.sqrMagnitude < 0.0001f || !SkateParks.Ray(from, ahead.normalized, out wall, 0.6f, GroundMask) || wall.m_Normal.y > 0.5f || RampFaceAhead(from, wall)) return;
        if (Speed > 0f) LimitSpeed(Math.Max(0f, Actual) + 2f, Cap);
        else LimitSpeed(Cap, Math.Max(0f, -Actual) + 2f);
    }

    // Land a metre below the water's surface is for swimming, unless something is overhead (tunnels,
    // basements); the shallows along the shore are rideable.
    private static bool OpenSky(Vector3 pos)
    {
        RaycastHit hit;
        return !SkateParks.Ray(pos + Vector3.up * 2f, Vector3.up, out hit, 60f, GroundMask);
    }

    private static void Sense(Vector3 pos)
    {
        observedSupportValid = false;
        RaycastHit hit;
        // Keep the last verified face's normal probe in the air too: a rising capsule can
        // meet the curve again where the straight-down probe no longer reaches it.
        if ((Mode == RideMode.Ground || Mode == RideMode.Air) && OnRamp && RampContact(pos, out hit))
        { ObserveSupport(pos, hit); return; }
        var reach = Mode == RideMode.Air ? AirProbe : FeetProbe;
        var got = SkateParks.Ray(pos + Vector3.up * RayLift, Vector3.down, out hit, RayLift + reach, GroundMask);
        // The player's capsule ends in a half sphere: on a slope it rests on its side of that sphere, so
        // its lowest point stands radius * (1/cos - 1) over the ground straight below (0.19 m at 43
        // degrees, 0.5 m at 60, measured on a MEGA transition). That is contact, not a gap: read as a
        // gap, the gap closer pressed the body into every transition and the ground was lost past 55
        // degrees.
        var slack = got && hit.m_Normal.y > 0.2f && hit.m_Normal.y < 0.999f ? Math.Min(CapsuleSlackMost, CapsuleRadius * (1f / hit.m_Normal.y - 1f)) : 0f;
        Clearance = got ? hit.m_Distance - RayLift - slack : 99f;
        OnRamp = got && SkateParks.IsRamp(hit) && !SkateParks.IsCoping(hit);
        // Over a transition's face (coming down onto it from the air, or the first step onto it)
        // the gap is the half sphere's distance to the face along the face's normal, exact rather
        // than the rest's estimate, and the face keeps its normal however steep: a fall back onto
        // the wall is landed on the wall (Land), not on level ground.
        if (OnRamp && hit.m_Normal.y > 0.001f) Clearance = Vector3.Dot(pos + Vector3.up * CapsuleRadius - hit.m_Point, hit.m_Normal) - CapsuleRadius;
        Grounded = got && Clearance <= FeetProbe;
        Normal = Grounded && (hit.m_Normal.y > SharedNormalLeast || (OnRamp && hit.m_Normal.y > 0.001f)) ? hit.m_Normal : Vector3.up;
        BelowNormal = got && hit.m_Normal.y > 0.001f ? hit.m_Normal : Vector3.up;
        if (got && hit.m_Normal.y > SharedNormalLeast) ContactNormal = hit.m_Normal;
        if (got) ObserveSupport(pos, hit);
        SkatePhysicsDiagnostic.Contact(hit, got);
    }
    private static RaycastHit observedSupport;
    private static Vector3 observedSupportAt;
    private static int observedSupportStep;
    private static bool observedSupportValid;
    private static void ObserveSupport(Vector3 pos, RaycastHit hit)
    {
        observedSupport = hit; observedSupportAt = pos; observedSupportStep = Steps; observedSupportValid = hit.m_Normal.y > .001f;
        if (Mode == RideMode.Ground && Grounded)
        {
            // Only the immediately preceding, actually supported ramp face can
            // classify a natural lip departure when the next ray misses it.
            groundedRampStep = OnRamp && SkateParks.IsRamp(hit) && !SkateParks.IsCoping(hit) ? Steps : -2;
            groundedRampAt = Time.realtimeSinceStartup; groundedRampUp = hit.m_Normal;
        }
    }
    // Reuse the current physics observation, including RampContact's along-normal hit. A later
    // render frame, another position or a missing query cannot claim remembered world support.
    public static bool TryObservedGround(Vector3 pos, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        if (!observedSupportValid || observedSupportStep != Steps || !Grounded ||
            Mode != RideMode.Ground && Mode != RideMode.Air || (pos - observedSupportAt).sqrMagnitude > .00000001f) return false;
        hit = observedSupport; return hit.collider != null;
    }
    public static bool TryObservedRamp(Vector3 pos, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        return Mode == RideMode.Ground && OnRamp && TryObservedGround(pos, out hit) && SkateParks.IsRamp(hit) && !SkateParks.IsCoping(hit);
    }
    // The normal of what Sense last found under the body, grounded or not (the air step's landing
    // reads the gap to a transition's face along it).
    public static Vector3 BelowNormal = Vector3.up;

    // On a transition's drawn surface (Sense): its faces are ground, not walls (WallAhead), and the
    // contact is read along the face's normal (RampContact).
    public static bool OnRamp;
    // Rides off the lip of a transition (the log's count): the board leaves the face with the whole
    // pace it had along it, up as much as the face stood up, and comes back down onto it.
    public static int Verts;
    public const float RampReach = 0.3f;

    // On a transition the body rests against the face, not on the ground straight below it: the
    // capsule's bottom half sphere touches the face along the face's normal. Read straight down,
    // the ground under the body is lost from about 53 degrees on (the gap below grows past the
    // probe, the capsule's rest on the slope counted in), and the ride flapped between ground and
    // air up every transition (#487). So on a face the ride is on, the contact is read along the
    // last normal from the half sphere's centre: the distance to the face less the radius is the
    // gap. The ground is kept as far as the lip, where the face stands vertical; a face that leans
    // over is none, and so is anything but the transition's own collider. The ride's frame on a
    // face past the shared physics' 72 degrees is the client's own (SteepSlope).
    private static bool RampContact(Vector3 pos, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        var along = -Normal;
        if (along.sqrMagnitude < 0.5f) return false;
        along = along.normalized;
        if (!SkateParks.Ray(pos + Vector3.up * CapsuleRadius, along, out hit, CapsuleRadius + FeetProbe + RampReach, GroundMask)) return false;
        // The coping on the lip is not the face: met by this ray, the face has ended there.
        if (!SkateParks.IsRamp(hit) || SkateParks.IsCoping(hit) || hit.m_Normal.y < -0.05f) return false;
        Clearance = hit.m_Distance - CapsuleRadius;
        Grounded = Clearance <= FeetProbe;
        OnRamp = true;
        Normal = hit.m_Normal.y > 0.001f ? hit.m_Normal : new Vector3(hit.m_Normal.x, 0.001f, hit.m_Normal.z).normalized;
        BelowNormal = Normal;
        if (hit.m_Normal.y > SharedNormalLeast) ContactNormal = hit.m_Normal;
        SkatePhysicsDiagnostic.Contact(hit, true);
        return true;
    }

    // Past the shared physics' limit (its frame takes no ground over 72 degrees) the same rule is
    // kept by the client up to the lip: gravity along the tangent, the slope signed by the way of travel.
    private static void SteepSlope(Vector3 tangent, float dt)
    {
        Speed += -AirGravity * tangent.y * dt;
        var horizontal = Flat(tangent).magnitude;
        SlopeDegrees = Speed == 0f ? 0f : (float)(Math.Atan2(tangent.y, horizontal) * 180.0 / Math.PI) * Math.Sign(Speed);
    }

    private static void GroundGrabTick(float dt)
    {
        // Keep a fresh pop/plant alive only through this bounded release, under the same input owner.
        // Regripping, opening a menu or losing support cancels it instead of replaying it later.
        if (GroundGrab.Active && now - jumpWanted >= 0 && now - jumpWanted < 0.12f)
            jumpWanted = Mode == RideMode.Ground && Grounded && (SkateKeys.Scripted || SkateKeys.ControlsActive) &&
                jumpInputEpoch == SkateKeys.InputEpoch && SkateKeys.GroundGrab == SkateGroundGrabKind.None ? now : -99f;
        if (Mode != RideMode.Ground)
        {
            SkateGroundGrabResult release; string releaseError;
            if (Mode == RideMode.Air && SkateGroundGrabs.TryStep(GroundGrab, SkateGroundGrabKind.None, false, dt, out release, out releaseError)) GroundGrab = release.State;
            else GroundGrab = default(SkateGroundGrabState);
            return;
        }
        var eligible = !SkateOldschoolRide.Active && !SkateSurfaceRide.DisplayWall && (SkateKeys.Scripted || SkateKeys.ControlsActive) && Grounded && Math.Abs(Speed) >= 0.5f &&
            PushPhase <= 0 && RevertLeft == 0 && !Manual && !NoseManual && !SkateKeys.Charging && now - jumpWanted >= 0.12f;
        SkateGroundGrabResult r; string error;
        if (!SkateGroundGrabs.TryStep(GroundGrab, SkateKeys.GroundGrab, eligible, dt, out r, out error))
        { GroundGrab = default(SkateGroundGrabState); Out.Say("SKATE ground grab refused: " + error); return; }
        GroundGrab = r.State;
        if (r.Ended != SkateGroundGrabKind.None && r.EndedSeconds >= SkateGroundGrabs.MinimumScoreSeconds)
        {
            SkateTrickResult score;
            if (SkateTricks.TryAdd(Trick, SkateGroundGrabs.Name(r.Ended), Math.Max(1, (long)Math.Round(Math.Min(60, r.EndedSeconds) * 80)), false, out score, out error)) Take(score);
        }
    }

    private static void GroundStep(float dt)
    {
        UpdateKickturn(dt);
        if (!SkateSurfaceRide.DisplayWall && SkateOldschoolRide.TryHippie(Position, dt, SkateKeys.HippieTap)) { jumpWanted = -99f; AirStep(Position, dt); return; }
        Vector3 oldschoolVelocity;
        if (SkateOldschoolRide.GroundStep(Position, dt, out oldschoolVelocity))
        { velocity = oldschoolVelocity; if (Mode == RideMode.Air) AirStep(Position, dt); return; }
        if (SkateSurfaceRide.TryGround(Position, dt, SkateKeys.WallHeld, SkateKeys.DropTap))
        { if (Mode == RideMode.Air) AirStep(Position, dt); return; }
        if (!Grounded)
        {
            // Off the lip of a transition (or any edge): the pace along the ground goes on as it
            // was, and a pop asked for in the last moment on it is taken here, as it would have
            // been a step earlier on the ground (TakeOff forgets the request otherwise). The air
            // brings the board back down onto the face, rolling back fakie, or onto the deck when
            // the way led over it.
            var popped = !GroundGrab.Active && !SkateSurfaceRide.DisplayWall && now - jumpWanted < 0.12f;
            var pop = 0f;
            if (popped)
            {
                Pops++; jumpWanted = -99f;
                if (SkateTricks.IsPlant(wantKind)) PlantedUntil = now + PlantSeconds;
                pop = OlliePop * wantScale * PopHeight(wantKind);
            }
            var rise = Tangent.y * Speed;
            if (rise > 0.5f && Math.Abs(SlopeDegrees) >= 45f)
            {
                Verts++;
                Out.Say("SKATE vert: off the lip at " + Math.Abs(SlopeDegrees).ToString("F0") + " deg, " + rise.ToString("F1") + " m/s up, " + Flat(Tangent * Speed).magnitude.ToString("F1") + " along" + (popped ? ", popped" : "") + ", at " + Out.V3(Position));
            }
            Launch(Tangent, pop, dt); Sense(Position); AirStep(Position, dt); return;
        }
        var before = Yaw;
        var maxTurn = TurnRate / (1f + Math.Abs(Speed) / 8f) * dt;
        // The brake with a turn held at speed is a powerslide: the board swings across the line and
        // scrubs speed; let go, or slowed to a walk, it swings back. The side is the side the turn
        // was held to, read as the slide starts.
        var turnHeld = SkateKeys.Analog ? Math.Abs(SkateKeys.Steer) > 0.5f : (SkateKeys.Left || SkateKeys.Right);
        var wantSlide = !SkateSurfaceRide.DisplayWall && SkateKeys.Brake && turnHeld && RevertLeft == 0f && (Powerslide > 0.01f ? Math.Abs(Speed) > 0.8f : Math.Abs(Speed) > PowerslideFrom);
        if (wantSlide && Powerslide < 0.01f)
        {
            var toRight = SkateKeys.Analog ? SkateKeys.Steer > 0f : SkateKeys.Right;
            PowerslideSide = (toRight ? 1f : -1f) * (Speed < 0f ? -1f : 1f);
            Powerslides++;
        }
        Powerslide += ((wantSlide ? 1f : 0f) - Powerslide) * Clamp(dt * (wantSlide ? PowerslideIn : PowerslideOut), 0f, 1f);
        if (Powerslide < 0.01f && !wantSlide) Powerslide = 0f;
        // A stick steers by how far it is pushed; keys and the mouse turn the board toward where the rider looks.
        if (Kickturning || RevertLeft != 0f || Powerslide > 0.3f) SteerRate = 0f;
        else if (SkateKeys.Analog)
        {
            var rolling = Clamp(SteerStill + (1f - SteerStill) * Math.Abs(Speed) / SteerFull, SteerStill, 1f);
            var want = Clamp(SkateKeys.Steer, -1f, 1f) * TurnRate / (1f + Math.Abs(Speed) / 8f) * rolling;
            var easing = Math.Abs(want) > Math.Abs(SteerRate) && want * SteerRate >= 0f ? SteerIn : SteerOut;
            SteerRate += (want - SteerRate) * (1f - (float)Math.Exp(-easing * dt));
            Yaw += SteerRate * dt;
        }
        else Yaw += Clamp(Delta(Yaw, SkateKeys.LookYaw + ((SkateKeys.Left ? -1f : 0f) + (SkateKeys.Right ? 1f : 0f)) * 40f), -maxTurn, maxTurn);
        if (!Kickturning && (RevertLeft != 0 || !SkateSurfaceRide.DisplayWall && !GroundGrab.Active && now - jumpWanted >= 0.12f)) Revert(dt);
        var dir = RevertLeft != 0f ? revertDir * revertSign : Dir(Yaw);
        var tangent = SurfaceTangent(Normal, dir);
        // The shared physics: gravity along the tangent of the ground, the slope signed by the way of
        // travel. Up a transition past its limit (a face over 72 degrees, only ever a ramp's), the
        // client keeps the same rule to the lip.
        SkateSlopeResult slope; string error;
        if (Normal.y <= SharedNormalLeast) SteepSlope(tangent, dt);
        else if (SkateRidePhysics.TrySlope(new SkateSlopeInput(V(Normal), V(dir), Speed, AirGravity), dt, out slope, out error))
        {
            tangent = V(slope.Tangent); Speed = (float)slope.SignedTangentSpeed; SlopeDegrees = (float)slope.SignedSlopeDegrees;
        }
        else { Speed += Vector3.Dot(Physics.gravity, tangent) * SlopeGain * dt; Physics_(error); }
        Tangent = tangent; Braking = SkateKeys.Brake;
        // A press up to a moment before touch-down still counts, so tricks can be chained.
        if (!GroundGrab.Active && !SkateSurfaceRide.DisplayWall && now - jumpWanted < 0.12f)
        {
            Pops++; jumpWanted = -99f;
            if (SkateTricks.IsPlant(wantKind)) PlantedUntil = now + PlantSeconds;
            Launch(tangent, OlliePop * wantScale * PopHeight(wantKind), dt, true);
            AirStep(Position, dt);
            return;
        }
        if (!SkateSurfaceRide.DisplayWall) PumpStep(dt);
        else { Pump = default(SkatePumpState); PumpHeld = 0; }
        // A manual is held on two wheels: it needs some speed and leaves no foot free to push. One
        // that has begun lasts down to a crawl: ending it at the pace it needs to begin would let a
        // held push in and out every few tenths of a second, each time ending and scoring it.
        var pace = Math.Abs(Speed);
        Manual = !Kickturning && !SkateSurfaceRide.DisplayWall && !GroundGrab.Active && SkateKeys.Manual && !SkateKeys.Brake && pace > (Manual ? ManualHold : ManualPace);
        NoseManual = !Kickturning && !SkateSurfaceRide.DisplayWall && !GroundGrab.Active && !Manual && SkateKeys.NoseManual && !SkateKeys.Brake && pace > (NoseManual ? ManualHold : ManualPace);
        var h = HorizontalFactor;
        var pushLimit = h > 0.0001f ? MaxPush / h : 0f;
        // Rolling tail first, a push drives the board on the way it rolls (a fakie push), not to a stop.
        var fakiePush = Speed < -0.3f;
        // A controller pump interrupts a foot stroke too; the existing rider ease returns it to the deck.
        var pumpCrouch = SkateKeys.Pad && SkateKeys.Crouch;
        if (pumpCrouch) PushPhase = 0f;
        var pushing = !SkateSurfaceRide.DisplayWall && !GroundGrab.Active && SkateKeys.Push && !pumpCrouch && !SkateKeys.Brake && !Manual && !NoseManual && (fakiePush ? Speed > -pushLimit : Speed < pushLimit);
        // Part of a push's speed comes as soon as it is asked for; the rest, the kick, comes while the
        // foot is on the ground driving the board on (below), so the surge is seen with the stroke.
        if (pushing && fakiePush) Speed = Math.Max(-pushLimit, Speed - PushAccel * PushSteady * dt);
        else if (pushing) Speed = Math.Min(pushLimit, Speed + PushAccel * PushSteady * dt);
        if (Powerslide > 0.01f) Speed = Toward(Speed, 0f, (BrakeDecel * (1f - Powerslide) + PowerslideDecel * Powerslide) * dt);
        else if (SkateKeys.Brake) Speed = Toward(Speed, 0f, BrakeDecel * dt);
        Speed = Toward(Speed, 0f, RollDecel * dt);
        // Rolling back down a transition keeps its pace both ways (#409): only friction, the brake,
        // a powerslide and the server's horizontal cap slow it, never which end leads.
        LimitSpeed(Cap, Cap);
        // A stroke begun before a rollback must not kick against the new way of travel.
        if (PushPhase > 0f && ((strokeBack && Speed > 0.3f) || (!strokeBack && Speed < -0.3f)))
            PushPhase = 0f;
        if (pushing || PushPhase > 0f)
        {
            if (PushPhase <= 0f) { Pushes++; PushMongo = SkateKeys.Mongo; PushLead = PushMongo != TailFirst; strokeBack = fakiePush; }
            // While the foot is on the ground it stays where it came down and the board rolls on
            // past it: the stroke runs by the distance the board moves, not by the clock (a board
            // that cannot move gets the slow clock), so the foot never slides along the ground.
            var stroke = (float)(SkateRider.PushPlant + SkateRider.PushSweep);
            var span = (float)(SkateRider.PushUp - SkateRider.PushDown);
            var down = PushPhase > (float)SkateRider.PushDown && PushPhase < (float)SkateRider.PushUp;
            // The kick: what the stroke covers of the foot's time on the ground this step drives the
            // board on; the stroke then runs by the board's new pace, so the foot stays put.
            var from = PushPhase;
            var guess = from + (down ? Math.Max(dt / (PushPeriod * 3f), Math.Abs(Speed) * dt / stroke * span) : dt / PushPeriod);
            var driven = Math.Min(guess, (float)SkateRider.PushUp) - Math.Max(from, (float)SkateRider.PushDown);
            if (driven > 0f && SkateKeys.Push && !SkateKeys.Brake && !Manual && !NoseManual)
            {
                var gain = PushKick * driven / span;
                if (strokeBack) { if (Speed > -pushLimit) Speed = Math.Max(-pushLimit, Speed - gain); }
                else if (Speed < pushLimit) Speed = Math.Min(pushLimit, Speed + gain);
            }
            PushPhase += down ? Math.Max(dt / (PushPeriod * 3f), Math.Abs(Speed) * dt / stroke * span) : dt / PushPeriod;
            if (PushPhase >= 1f) PushPhase = 0f;
        }
        // The velocity runs along the ground's tangent; a gap under the wheels is closed on top of it:
        // down, and on a transition's face toward the face, which is where the gap was read.
        velocity = tangent * Speed;
        MoveKickturn(dt);
        if (Kickturning) dir = Dir(Yaw);
        var close = Clamp(Clearance * 8f, 0f, 3f);
        if (OnRamp) velocity -= Normal * close; else velocity.y -= close;
        StepOver(dir, dt);
        // In a slide the rider leans back from the way he goes, into the turn that started it.
        var leanWant = Kickturning ? 0f : Powerslide > 0.01f ? -PowerslideSide * 18f * Powerslide : Clamp((Delta(before, Yaw) + revertStep) / dt * 0.12f, -25f, 25f);
        revertStep = 0f;
        Lean += (leanWant - Lean) * (1f - (float)Math.Exp(-12f * dt));
        if (!GroundGrab.Active) TrickTick(false, false, 0, 0, 0, 0, dt);
    }

    private static void ResetKickturn()
    { kickturnLift = kickturnTurned = kickturnSetup = 0f; kickturnSide = 0; kickturnRelease = kickturnRearm = false; kickturnPoseAt = -99f; kickturnPosePlayer = null; kickturnFootVersion = -1; }
    // Only the current measured rider/sole solve can release the lift. A timer
    // reaching its end does not prove that the rear foot and body arrived.
    public static void ConfirmKickturnPose(Transform player, int side, uint epoch, int footVersion)
    {
        if (!Kickturning || kickturnSetup < 1 || side != kickturnSide || epoch != kickturnEpoch ||
            player == null || player != SkateRig.LocalT || !SkateFootContacts.Ready || footVersion != SkateFootContacts.Version) return;
        kickturnPosePlayer = player; kickturnFootVersion = footVersion; kickturnPoseAt = Time.realtimeSinceStartup;
    }
    public static void RefuseKickturnPose()
    { if (Kickturning) { kickturnPoseAt = -99f; kickturnRelease = true; } }
    private static bool KickturnPoseFresh()
    {
        var age = Time.realtimeSinceStartup - kickturnPoseAt;
        return kickturnPosePlayer != null && kickturnPosePlayer == SkateRig.LocalT &&
            SkateFootContacts.Ready && kickturnFootVersion == SkateFootContacts.Version && age >= 0 && age <= .12f;
    }
    private static float KickturnCurve(float f) { return f * f * f * (10f + f * (-15f + 6f * f)); }
    private static float KickturnPhase(float visible)
    {
        if (visible <= 0 || visible >= 1) return Clamp(visible, 0, 1);
        float lo = 0, hi = 1;
        // Tip is an already rendered weight. Recover its phase without applying
        // the lift curve twice at the ordinary-manual ownership handoff.
        for (int i = 0; i < 24; i++)
        { var mid = (lo + hi) * .5f; if (KickturnCurve(mid) < visible) lo = mid; else hi = mid; }
        return (lo + hi) * .5f;
    }
    private static bool KickturnEligible()
    {
        return On && Mode == RideMode.Ground && Grounded && !OnRamp && !Frozen &&
            (SkateKeys.Scripted || SkateKeys.ControlsActive) && !SkateRiggingShowcase.Running && !Scenarios.Active &&
            !SkateBoard.Held && !Handplant && !SkateActions.Owns && !SkateOldschoolRide.Active &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && !BoardPath.Known && !GroundGrab.Active &&
            PushPhase <= .001f && Powerslide <= .001f && RevertLeft == 0 && !Rotating &&
            !SkateKeys.Brake && !SkateKeys.Push && !SkateKeys.Charging && !SkateKeys.Crouch &&
            now - jumpWanted >= .12f && Math.Abs(Speed) <= KickturnSpeed;
    }
    private static float KickturnSteer()
    { return SkateKeys.Analog ? Clamp(SkateKeys.Steer, -1f, 1f) : (SkateKeys.Left ? -1f : 0f) + (SkateKeys.Right ? 1f : 0f); }
    private static bool KickturnSupport(int side, out Vector3 axle)
    {
        axle = Vector3.zero; RaycastHit plane;
        if (!TryObservedGround(Position, out plane) || plane.m_Normal.y < .9995f ||
            Math.Abs(Clearance) > .04f || !SkateBoard.TryKickturnAxle(side, out axle)) return false;
        var forward = Dir(DeckYaw); var right = Vector3.Cross(Vector3.up, forward);
        // Both tyres of the loaded axle must have fresh support, not just the body centre.
        for (int sign = -1; sign <= 1; sign += 2)
        {
            RaycastHit wheel; var point = Position + forward * axle.z + right * (.082f * sign);
            if (!SkateParks.Ray(point + Vector3.up * RayLift, Vector3.down, out wheel, RayLift + .04f, GroundMask) ||
                wheel.m_Normal.y < .9995f || Math.Abs(Vector3.Dot(wheel.m_Point - plane.m_Point, plane.m_Normal)) > .0005f) return false;
        }
        return true;
    }
    private static void UpdateKickturn(float dt)
    {
        var steer = KickturnSteer();
        var intent = SkateKeys.Manual != SkateKeys.NoseManual && Math.Abs(steer) > .5f;
        if (!intent) kickturnRearm = false;
        if (!KickturnEligible() || kickturnSide != 0 && kickturnEpoch != SkateKeys.InputEpoch)
        { ResetKickturn(); return; }
        Vector3 axle;
        if (kickturnSide == 0)
        {
            if (!intent || kickturnRearm) return;
            var ahead = Vector3.Dot(Dir(NoseYaw), Dir(DeckYaw)) >= 0 ? 1 : -1;
            var side = (SkateKeys.NoseManual ? 1 : -1) * ahead;
            var oldAhead = Vector3.Dot(Dir(Yaw), Dir(DeckYaw)) * (Speed < 0 ? -1 : 1) >= 0 ? 1 : -1;
            if (Math.Abs(SkateBoard.Tip) > .001f && -Math.Sign(SkateBoard.Tip) * oldAhead != side) return;
            if (!KickturnSupport(side, out axle)) return;
            kickturnSide = side; kickturnEpoch = SkateKeys.InputEpoch; kickturnNose = SkateKeys.NoseManual;
            kickturnLift = KickturnPhase(Math.Abs(SkateBoard.Tip));
            kickturnSetup = 0; kickturnPoseAt = -99f; kickturnPosePlayer = null; kickturnFootVersion = -1;
            kickturnTurned = 0; kickturnRelease = false; kickturnRearm = true; Kickturns++;
        }
        if (!KickturnSupport(kickturnSide, out axle)) { ResetKickturn(); return; }
        if (!intent || SkateKeys.NoseManual != kickturnNose || kickturnTurned >= KickturnLimit) kickturnRelease = true;
        if (kickturnRelease)
        {
            kickturnLift = Math.Max(0, kickturnLift - dt / KickturnLiftSeconds);
            // Keep the weight and tail foot until the front wheels are down.
            if (kickturnLift == 0) kickturnSetup = Math.Max(0, kickturnSetup - dt / KickturnSetupSeconds);
            if (kickturnSetup == 0) { kickturnSide = 0; kickturnTurned = 0; kickturnPoseAt = -99f; }
            return;
        }
        if (kickturnSetup < 1)
        {
            // A pre-existing partial manual first settles, without a new pivot.
            if (kickturnLift > 0) kickturnLift = Math.Max(0, kickturnLift - dt / KickturnLiftSeconds);
            else kickturnSetup = Math.Min(1, kickturnSetup + dt / KickturnSetupSeconds);
            return;
        }
        if (!KickturnPoseFresh()) { if (kickturnLift > 0) kickturnRelease = true; return; }
        kickturnLift = Math.Min(1, kickturnLift + dt / KickturnLiftSeconds);
    }
    private static void MoveKickturn(float dt)
    {
        if (!Kickturning || kickturnRelease || kickturnLift < 1f || !KickturnPoseFresh()) return;
        Vector3 axle; if (!KickturnSupport(kickturnSide, out axle)) { ResetKickturn(); return; }
        var step = Math.Min(KickturnLimit - kickturnTurned, KickturnRate * dt * Math.Abs(KickturnSteer())) * Math.Sign(KickturnSteer());
        var nextYaw = Yaw + step;
        var next = SurfaceTangent(Normal, Dir(nextYaw));
        // The centre goes around the loaded axle. This velocity still goes through
        // WallAhead, the horizontal budget and the Rigidbody's normal collision move.
        var pivotMove = (Dir(DeckYaw) - Dir(DeckYaw + step)) * (axle.z / dt);
        var wanted = next * Speed + pivotMove;
        // Refuse the pivot if its centre arc is obstructed. Do not call the
        // side-effecting WallAhead twice; the accepted step still uses it below.
        var pace = wanted.magnitude;
        if (pace > .0001f)
        {
            var direction = wanted / pace; var reach = pace * dt + CapsuleReach + WallSkin;
            foreach (var height in wallHeights)
            {
                RaycastHit wall;
                if (SkateParks.SphereWall(Position + Vector3.up * height, WallProbe, direction, out wall, reach, GroundMask, RidingWallFace) &&
                    -Vector3.Dot(wanted, wall.m_Normal) > 0f) { kickturnRelease = true; return; }
            }
        }
        Yaw = nextYaw; Tangent = next; velocity = wanted; kickturnTurned += Math.Abs(step);
    }

    // The ground a little ahead, in the way of travel: higher by a lip's worth, the board is lifted
    // over it within the step, which the game's own fixed step then carries.
    private static void StepOver(Vector3 dir, float dt)
    {
        if (Math.Abs(Speed) < 0.5f) return;
        var ahead = Position + dir * (Math.Sign(Speed) * StepLook);
        RaycastHit hit;
        if (!SkateParks.Ray(ahead + Vector3.up * (StepUp + 0.3f), Vector3.down, out hit, StepUp + 0.3f + FeetProbe, GroundMask)) return;
        var rise = hit.m_Point.y - Position.y;
        if (rise < 0.03f || rise > StepUp || hit.m_Normal.y < 0.7f) return;
        // A lip has a face: a ray along the ground from the wheels' height must meet something steep
        // before the spot ahead. A smooth bank rises as much but has no face, and is ridden up.
        RaycastHit face;
        var flat = new Vector3(dir.x, 0f, dir.z); if (flat.sqrMagnitude < 0.0001f) return; flat = flat.normalized;
        if (!SkateParks.Ray(Position + Vector3.up * 0.02f, flat * Math.Sign(Speed), out face, StepLook + 0.05f, GroundMask) || face.m_Normal.y > 0.5f) return;
        var lift = rise / Math.Max(dt, 0.001f) * 0.6f;
        if (velocity.y < lift) velocity.y = lift;
        if (now - steppedAt > StepGrace) StepUps++;
        steppedAt = now;
    }

    // Right after a landed trick a fresh turn of the stick turns the board round on the ground: the
    // rider rolls on the same way, the other end leading, and the combo stays open.
    private static void Revert(float dt)
    {
        if (RevertLeft == 0f)
        {
            // With the controller a revert is the left stick swept half round its rim, as in the
            // skate. games: a hard steer after a landing is a carve (the owner's ride had a dozen
            // reverts he never asked for). On the keys the turn must be asked for afresh.
            if (!landedTrick || now - LandAt > RevertWindow || Math.Abs(Speed) < 1f) return;
            float way;
            if (SkateKeys.Analog) { if (Math.Abs(SkatePad.LeftSweep) < RevertSweep) return; way = Math.Sign(SkatePad.LeftSweep); SkatePad.LeftSweep = 0f; }
            else { if (!SkateKeys.SpinPressed || SkateKeys.Spin == 0f || spinAtLand != 0f) return; way = SkateKeys.Spin > 0f ? 1f : -1f; }
            landedTrick = false; RevertLeft = 180f * way; Reverts++; revertSign = Math.Sign(Speed); revertDir = Dir(Yaw) * revertSign;
            Out.Say("SKATE revert begins");
        }
        var step = Math.Min(Math.Abs(RevertLeft), RevertRate * dt) * Math.Sign(RevertLeft);
        revertTurned += step; RevertLeft -= step; revertStep = step;
        if (Math.Abs(RevertLeft) > 0.001f) return;
        // Round: the rider and the board point the other way and roll on as before, the other end
        // leading. The half turn goes to the stance from here on, so the pose does not move at the
        // handover: the turn made so far comes off as the scorer's Switch comes on. The heading and
        // the speed stay as they were: the way of travel never changed.
        RevertLeft = 0f; revertEndAt = now;
        SkateTrickResult r; string error;
        if (SkateTricks.TryAdd(Trick, "Revert", RevertPoints, true, out r, out error)) { Take(r); revertTurned = 0f; }
        else { Out.Say("SKATE revert refused: " + error); Yaw += revertTurned; Speed = -Speed; revertTurned = 0f; }
    }

    // The crouch is held with a trigger; the shared pump judges the pass: charged going down a
    // transition, awarded when let go through its bottom, once per pass, by an energy budget.
    private static void PumpStep(float dt)
    {
        if (GroundGrab.Active || SkateKeys.GroundGrab != SkateGroundGrabKind.None) { Pump = default(SkatePumpState); PumpHeld = 0; return; }
        // Losing controls or changing their owner is not the rider extending his knees.
        var active = SkateKeys.Scripted || SkateKeys.ControlsActive;
        if (!active || pumpInputEpoch != SkateKeys.InputEpoch)
        {
            Pump = default(SkatePumpState); PumpHeld = 0f; pumpInputEpoch = SkateKeys.InputEpoch;
            if (!active) return;
        }
        PumpHeld = SkateKeys.Crouch ? Math.Min(PumpCharge, PumpHeld + dt) : 0f;
        var travel = Flat(Tangent); if (travel.sqrMagnitude < 0.0001f) travel = Dir(Yaw);
        travel = travel * (Speed >= 0f ? 1f : -1f);
        SkatePumpResult r; string error;
        // The shared pump takes a slope short of vertical; at the lip the face is as good as that.
        var input = new SkatePumpInput(Mode == RideMode.Ground, SkateKeys.Crouch, Clamp(SlopeDegrees, -89.9f, 89.9f), Math.Abs(Speed), new SkateVector(travel.x, 0, travel.z));
        if (!SkateRidePhysics.TryPump(Pump, input, dt, out r, out error)) { Physics_(error); return; }
        Pump = r.State;
        if (!r.Awarded) return;
        Speed += Math.Sign(Speed) * (float)r.SpeedGain; Pumps++;
        Out.Say("SKATE pump +" + r.SpeedGain.ToString("F1") + " m/s (" + r.EnergyGain.ToString("F1") + " J/kg) through the bottom at " + SlopeDegrees.ToString("F0") + " degrees");
    }

    // Leaving the ground: the velocity along it is kept whole, the pop added straight up; the air
    // step takes its gravity off at once, which the launch gives back for that one step.
    private static void Launch(Vector3 tangent, float pop, float dt, bool ordinaryGroundPop = false)
    {
        var flatground = ordinaryGroundPop && Mode == RideMode.Ground && Grounded && !OnRamp &&
            Normal.y >= .9995f && Math.Abs(tangent.y * Speed) <= .05f &&
            (wantKind == SkatePopKind.Ollie || wantKind == SkatePopKind.Nollie) &&
            Grab == SkateGrabKind.None && SkateKeys.Grab == SkateGrabKind.None &&
            wantStyle == SkateOllieStyle.None && !SkateActions.Owns && !SkateOldschoolRide.Owns && !SkateSurfaceRide.DisplayWall;
        var gravity = flatground ? (float)SkateFlatgroundPop.Gravity : AirGravity;
        if (flatground) pop = (float)SkateFlatgroundPop.ScaleImpulse(pop);
        SkateLaunchResult r; string error;
        var surface = tangent * Speed;
        Vector3 outgoing;
        if (SkateRidePhysics.TryLaunch(new SkateLaunchInput(V(surface), pop), out r, out error)) outgoing = V(r.Velocity);
        else { Physics_(error); outgoing = surface + Vector3.up * pop; }
        var nose = NoseYaw; var deck = DeckYaw;
        var flat = Flat(outgoing); var sign = Speed < 0f ? -1f : 1f;
        if (flat.sqrMagnitude > 0.0001f) Yaw = (float)(Math.Atan2(flat.x * sign, flat.z * sign) * 180.0 / Math.PI);
        Speed = flat.magnitude * sign;
        TakeOff(pop > 0f, outgoing.y + gravity * dt, flatground, outgoing.y);
        // Equal-height presentation span from the actual charged launch, before
        // the one-step gravity compensation. It never changes player motion.
        flatgroundFlightSeconds = flatground && outgoing.y > 0f ? 2f * outgoing.y / gravity : 0f;
        KeepAirPose(nose, deck);
    }

    // The shared edge finder takes speeds up to the shared ground limit: a faster board is taken
    // onto the edge at that speed.
    private static Vector3 EdgeVelocity(Vector3 v)
    {
        var flat = Flat(v); var most = (float)SkateMotion.MaximumGroundSpeed - 0.01f;
        if (flat.magnitude <= most) return v;
        flat = flat.normalized * most; return new Vector3(flat.x, v.y, flat.z);
    }

    private static void KeepAirPose(float nose, float deck)
    {
        airNoseOffset = Delta(Yaw + (Trick.Switch ? 180f : 0f), nose);
        airDeckOffset = Delta(nose + (DeckReversed ? 180f : 0f), deck);
    }

    // Off an edge (or out of a handplant, turned round): the pose is kept, and the air step
    // brings the board round to the heading at ComeRoundRate.
    private static void ComeRound(float nose, float deck)
    {
        KeepAirPose(nose, deck);
        comingRound = Math.Abs(airNoseOffset) > 0.5f || Math.Abs(airDeckOffset) > 0.5f;
        if (!comingRound) airNoseOffset = airDeckOffset = 0f;
    }

    private static int physicsSaid;
    private static void Physics_(string error) { PhysicsError = error; if (physicsSaid++ < 3) Out.Say("SKATE physics refused: " + error); }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }
    private static Vector3 V(SkateVector v) { return new Vector3((float)v.X, (float)v.Y, (float)v.Z); }

    // A no comply pops lower than an ollie (the front foot is off the board), a boneless higher
    // (the rider jumps off his planted foot).
    public static float PopHeight(SkatePopKind kind)
    {
        return kind == SkatePopKind.NoComply ? 0.85f : kind == SkatePopKind.Boneless ? 1.12f : kind == SkatePopKind.Fastplant ? 1.1f : kind == SkatePopKind.Beanplant ? 1.05f : 1f;
    }

    private static void SurfaceGroundContact(Vector3 pos)
    {
        if (Mode != RideMode.Ground || !Grounded || !OnRamp) return;
        SkateSurfaceRide.ConfirmDropIn(pos);
        if (!SkateSurfaceRide.ConsumeDropIn()) return;
        SkateTrickResult score; string error;
        if (SkateTricks.TryAdd(Trick, "Drop-in", 80, false, out score, out error)) Take(score);
        else Out.Say("SKATE drop-in award refused: " + error);
    }

    // Observed wall/transition actions supply a physical velocity, never a target position.
    // Their next AirStep consumes the one-step gravity compensation exactly as Launch does.
    public static void ActionLaunch(Vector3 outgoing, float dt, bool pop = true)
    {
        if (Mode != RideMode.Ground || !Grounded || !(dt > .001f && dt <= .05f) ||
            float.IsNaN(outgoing.x) || float.IsNaN(outgoing.y) || float.IsNaN(outgoing.z) ||
            float.IsInfinity(outgoing.x) || float.IsInfinity(outgoing.y) || float.IsInfinity(outgoing.z) ||
            outgoing.sqrMagnitude > 2500 || Flat(outgoing).magnitude > MaxSpeed || outgoing.y < -MaxFall || outgoing.y > OlliePop) return;
        var nose = NoseYaw; var deck = DeckYaw;
        wantKind = SkatePopKind.Ollie; wantStyle = SkateOllieStyle.None; wantFlip = 0; wantShove = 0;
        wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0; jumpWanted = -99f;
        var flat = Flat(outgoing); var sign = Vector3.Dot(flat, Dir(Yaw)) < 0 ? -1f : 1f;
        if (flat.sqrMagnitude > .0001f) Yaw = (float)(Math.Atan2(flat.x * sign, flat.z * sign) * 180 / Math.PI);
        Speed = flat.magnitude * sign; SenseRollingBack();
        if (pop) Pops++;
        TakeOff(pop, outgoing.y + AirGravity * dt);
        KeepAirPose(nose, deck); velocity = outgoing;
    }

    // Shared native launch path for an intentional action: keep momentum, clear old pop metadata.
    public static void ActionJump(float rise, float dt)
    {
        if (Mode != RideMode.Ground || !Grounded || float.IsNaN(rise) || float.IsInfinity(rise) || rise <= 0 || rise > OlliePop || !(dt > .001f && dt <= .05f)) return;
        wantKind = SkatePopKind.Ollie; wantStyle = SkateOllieStyle.None; wantFlip = 0; wantShove = 0;
        wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0; jumpWanted = -99f;
        var carry = SkateOldschoolRide.Owns && SkateOldschoolRide.Kind == SkateOldschoolKind.Caveman;
        var nose = NoseYaw; var deck = DeckYaw;
        if (carry)
        {
            var actual = Flat(velocity); var sign = Speed < 0 ? -1f : 1f;
            if (actual.sqrMagnitude > .0001f)
            {
                Yaw = (float)(Math.Atan2(actual.x * sign, actual.z * sign) * 180 / Math.PI);
                Tangent = SurfaceTangent(Normal, Dir(Yaw)); Speed = Vector3.Dot(velocity, Tangent);
            }
        }
        Pops++; Launch(Tangent, rise, dt);
        if (carry) KeepAirPose(nose, deck);
    }

    private static void TakeOff(bool jumped, float vertical, bool flatground = false, float? departureRise = null)
    {
        // Capture before resetting any action/wall owner. Speed was just set
        // from Flat(outgoing) by Launch, so it is the real horizontal departure.
        airViewVertDeparture = PreserveVertDeparture(departureRise ?? vertical);
        flatgroundFlight = flatground;
        flatgroundFlightSeconds = 0f;
        initialFlipFlick = flatground && jumped && wantFlip != 0 && wantSpecial == SkateBoardTrickKind.None;
        flightGravity = flatground ? (float)SkateFlatgroundPop.Gravity : AirGravity;
        airViewTrace = 0; airViewFromGrind = Mode == RideMode.Grind;
        SkateOldschoolRide.Reset();
        SkateSurfaceRide.Reset();
        SkateActions.Begin(jumped && (wantKind == SkatePopKind.Ollie || wantKind == SkatePopKind.Nollie) && wantSpecial == SkateBoardTrickKind.None && wantFlip == 0 && wantShove == 0 ? wantStyle : SkateOllieStyle.None); wantStyle = SkateOllieStyle.None;
        if (Mode == RideMode.Ground && Grounded) ViewNormal = Normal;
        airViewFrom = ViewNormal; airViewAt = 0f; airViewRise = Math.Max(0f, vertical); ResetAirDisplay();
        // Read while the board still rolls: which end leads decides which way is frontside.
        spinSide = (TailFirst ? -1f : 1f) * StanceSign;
        Enter(RideMode.Air);
        // A manual survives a hop over a bump; a jump ends it, and so does a longer fall below.
        airFrom = Position; airFromValid = jumped;
        Jumped = jumped; TrickAir = jumped; if (jumped) Manual = NoseManual = false; VSpeed = vertical; AirTime = 0f; AirPeak = 0f; takeOffY = Position.y;
        flipLeft = shoveLeft = bodyLeft = 0f; spinning = spinLate = false; PushPhase = 0f; Braking = false; stuck = 0; heldSteps = 0; Held = false; LateTrick = false;
        popKind = jumped ? wantKind : SkatePopKind.Ollie; pendFlip = jumped ? wantFlip : 0; pendShove = jumped ? wantShove : 0f;
        BoardPath = default(SkateBoardPathSample);
        SkateBoardPathSample path; string pathError;
        if (jumped && !Trick.DarkslideCaught && wantSpecial != SkateBoardTrickKind.None && SkateBoardPath.TrySample(wantSpecial, 0, wantSpecialDirection, popKind == SkatePopKind.Nollie, DeckReversed, out path, out pathError))
        { BoardPath = path; pendFlip = 0; pendShove = 0; }
        wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0;
        wantFlip = 0; wantShove = 0f; wantKind = SkatePopKind.Ollie;
    }

    // A planted foot retains its support frame until released.
    private static void AirView(float dt)
    {
        if (Handplant || SkateActions.Owns || SkateOldschoolRide.Active || SkateSurfaceRide.Active || SkateSurfaceRide.BodyFlip.Known || BoardPath.Known)
            airViewVertDeparture = false;
        if (FootPlanted)
        {
            airViewVertDeparture = false;
            airViewFrom = ViewNormal; airViewAt = AirTime; airViewRise = Math.Max(0f, VSpeed);
            ResetAirDisplay();
            return;
        }
        var contact = OnRamp ? BelowNormal : Vector3.up;
        var closing = Math.Max(0f, -Vector3.Dot(velocity, contact));
        // Align before the existing air-step contact threshold, not after a catch or landing snap.
        var gap = Clearance - Math.Max(0.04f, closing * dt + 0.02f);
        // A straight vert air keeps its departure basis until a landing is
        // actually observed. Reaching the ballistic apex is not a new surface.
        SkateVector shown; SkateRiderAirFrameState next; string error;
        if (SkateRiderMotion.TryAirFrame(airView, dt, V(airViewFrom), Math.Max(0f, AirTime - airViewAt), airViewRise,
            CurrentAirGravity, V(BelowNormal), gap, closing, airViewVertDeparture, out shown, out next, out error))
        { airView = next; ViewNormal = V(shown); }
        if (airViewFrom.y < .85f)
        {
            TraceAirFrame(1, "departure");
            if (airViewRise > 0 && VSpeed <= 0) TraceAirFrame(2, "descending");
            if (airView.Initialized && airView.LandingShare > 0) TraceAirFrame(4, "landing-approach");
        }
    }

    private static bool PreserveVertDeparture(float vertical)
    {
        var age = Time.realtimeSinceStartup - groundedRampAt;
        var ramp = groundedRampStep >= 0 && groundedRampStep <= Steps && Steps - groundedRampStep <= 1 && age >= 0 && age <= .1f;
        return Mode == RideMode.Ground && ramp && groundedRampUp.y > 0 && groundedRampUp.y < .15f &&
            vertical > 0 && Math.Abs(Speed) <= vertical * .25f && !FootPlanted && !Handplant && !GroundGrab.Active &&
            !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.Active &&
            !SkateSurfaceRide.BodyFlip.Known && !BoardPath.Known && wantSpecial == SkateBoardTrickKind.None;
    }

    private static void ResetAirDisplay()
    {
        airView = default(SkateRiderAirFrameState);
    }

    private static void TraceAirFrame(int bit, string phase)
    {
        if ((airViewTrace & bit) != 0) return;
        airViewTrace |= bit;
        Out.Say("AIRFRAME phase=" + phase + " mode=" + Mode + " from-grind=" + airViewFromGrind +
            " jumped=" + Jumped + " nose-yaw=" + NoseYaw.ToString("F0") + " deck-yaw=" + DeckYaw.ToString("F0") +
            " board-up=" + Out.V1(ViewNormal) + " body-up=" + Out.V1(RiderViewNormal) + " grind-pitch=" + SkateBoard.GrindPitch.ToString("F1") +
            " grind-roll=" + SkateBoard.GrindRoll.ToString("F1") + " air-pitch=" + SkateBoard.AirPitch.ToString("F1"));
    }

    private static float TimeToGround()
    {
        var h = Clearance;
        // Over a transition's face the gap is read along its normal: the fall to it is longer.
        if (OnRamp && h > 0f) h /= Math.Max(BelowNormal.y, 0.2f);
        h = h > AirProbe ? AirProbe : h < 0f ? 0f : h;
        var gravity = CurrentAirGravity;
        return (VSpeed + (float)Math.Sqrt(VSpeed * VSpeed + 2f * gravity * h)) / gravity;
    }

    private static void AirStep(Vector3 pos, float dt)
    {
        AirTime += dt;
        if (comingRound)
        {
            airNoseOffset = Toward(airNoseOffset, 0f, ComeRoundRate * dt); airDeckOffset = Toward(airDeckOffset, 0f, ComeRoundRate * dt);
            comingRound = airNoseOffset != 0f || airDeckOffset != 0f;
        }
        if (AirTime > HopGrace) Manual = NoseManual = false;
        // Wall contact owns this physical step before ordinary air gravity/rotation. Its fresh
        // side query never exempts the velocity from the outer forward sweep or speed budget.
        var wallWasActive = SkateSurfaceRide.Active;
        var wallPop = wallWasActive && (SkateKeys.WallPop || now - jumpWanted >= 0 && now - jumpWanted < .12f &&
            jumpInputEpoch == SkateKeys.InputEpoch && (SkateKeys.Scripted || SkateKeys.ControlsActive));
        Vector3 wallVelocity;
        if (SkateSurfaceRide.StepWall(pos, Dir(Yaw) * Speed + Vector3.up * VSpeed, dt, SkateKeys.WallHeld, wallPop, out wallVelocity))
        {
            if (Mode != RideMode.Air) return;
            // A newly acquired wall action owns its physical velocity/gravity from here.
            // A late grab alone does not replace a flight's frozen launch profile.
            flatgroundFlight = initialFlipFlick = false; flightGravity = AirGravity;
            var nose = NoseYaw; var deck = DeckYaw;
            var flat = Flat(wallVelocity); var sign = Speed < 0 ? -1f : 1f;
            if (flat.sqrMagnitude > .0001f) Yaw = (float)(Math.Atan2(flat.x * sign, flat.z * sign) * 180 / Math.PI);
            Speed = flat.magnitude * sign; VSpeed = Math.Max(-MaxFall, wallVelocity.y); wallVelocity.y = VSpeed; velocity = wallVelocity;
            KeepAirPose(nose, deck);
            if (SkateSurfaceRide.WallPopped) Pops++;
            if (wallPop) { jumpWanted = -99f; wantKind = SkatePopKind.Ollie; wantStyle = SkateOllieStyle.None; wantFlip = 0; wantShove = 0; wantSpecial = SkateBoardTrickKind.None; wantSpecialDirection = 0; }
            Manual = NoseManual = false; Grab = SkateGrabKind.None; SteerRate = Lean = SpinLead = 0;
            flipLeft = shoveLeft = bodyLeft = 0; pendFlip = 0; pendShove = 0; spinning = spinLate = false;
            Tangent = flat.sqrMagnitude > .0001f ? flat.normalized * sign : Dir(Yaw);
            if (!SkateSurfaceRide.Active)
            { airViewVertDeparture = false; airViewFrom = ViewNormal; airViewAt = AirTime; airViewRise = Math.Max(0, VSpeed); ResetAirDisplay(); }
            if (pos.y - takeOffY > AirPeak) AirPeak = pos.y - takeOffY;
            if (TrickAir) TrickTick(true, false, 0, 0, 0, 0, dt);
            return;
        }
        VSpeed -= CurrentAirGravity * dt; if (VSpeed < -MaxFall) VSpeed = -MaxFall;
        if (pos.y - takeOffY > AirPeak) AirPeak = pos.y - takeOffY;
        var dir = Dir(Yaw);
        SkateOldschoolRide.AirStep(pos, dt); if (Mode != RideMode.Air) return;
        SkateActions.Tick(dt, TimeToGround()); if (Mode != RideMode.Air) return;
        CancelFlipFlickForOwner();
        Tangent = dir; SteerRate = 0f; Lean += (0f - Lean) * (1f - (float)Math.Exp(-12f * dt));

        // A rotation is only started when the arc lasts long enough to finish it: an unfinished one is a bail.
        var left = (float)(TimeToGround() - 0.03);
        var flips = SkateActions.Owns || SkateOldschoolRide.Active || SkateSurfaceRide.BodyFlip.Known ? 0 : pendFlip + SkateKeys.LateFlip + SkateKeys.HeldFlip; var lateFlips = SkateKeys.LateFlip != 0 && Jumped && AirTime > LateAfter; pendFlip = 0;
        if (BoardPath.Known) { flips = 0; pendShove = 0; }
        // As in Skate, a flip and a grab never overlap: a flip or shove-it asked for during a grab,
        // or while the grab button is already held (also from before the pop), is not done; a grab
        // pressed during a flip or shove-it starts once the board has come round.
        var grabbing = Grab != SkateGrabKind.None || SkateKeys.Grab != SkateGrabKind.None;
        if (grabbing && Math.Abs(shoveLeft) < 0.001f) pendShove = 0f;
        if (flips != 0 && !grabbing && (Math.Abs(flipLeft) < 0.001f || Math.Sign(flipLeft) == Math.Sign(flips)))
        {
            // Keep every requested turn, including an initial flick's unspent edge release.
            // A request too late to finish still bails, unless an edge catches a darkslide.
            var flipTime = left - (initialFlipFlick ? (float)SkateFlipFlick.RemainingDelay(AirTime) : 0f);
            if (flipTime < (Math.Abs(flipLeft) + 360f * Math.Abs(flips)) / ((float)SkateMotion.FlipRate * CurrentRotationRateScale)) Late("a flip", flipTime);
            flipLeft += 360f * flips; TrickAir = true; if (lateFlips) LateTrick = true;
        }
        else if (flips != 0 && grabbing) { initialFlipFlick = false; SkateHud.Notice("No flip during a grab"); }
        // A shove asked for in the air goes on top of what is under way, the same way round.
        var lateShove = Jumped && !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateSurfaceRide.BodyFlip.Known && !BoardPath.Known ? SkateKeys.LateShove : 0f;
        if (lateShove != 0f && !grabbing && (Math.Abs(shoveLeft) < 0.001f || Math.Sign(shoveLeft) == Math.Sign(lateShove))) pendShove += lateShove;
        if (pendShove != 0f)
        {
            if (left < (Math.Abs(shoveLeft) + Math.Abs(pendShove)) / ((float)SkateMotion.ShoveRate * CurrentRotationRateScale)) Late("a shove-it", left);
            shoveLeft += pendShove; TrickAir = true; if (lateShove != 0f && AirTime > LateAfter) LateTrick = true;
            pendShove = 0f;
        }
        // A body varial: the rider turns a half round, the board keeps its yaw. Only when the arc
        // lasts long enough for it; one per flight.
        var carryingBoard = SkateOldschoolRide.Active && SkateOldschoolRide.Kind == SkateOldschoolKind.Caveman &&
            (SkateOldschoolRide.Phase == SkateOldschoolPhase.Pickup || SkateOldschoolRide.Phase == SkateOldschoolPhase.Carrying);
        var body = Jumped && !carryingBoard && !SkateSurfaceRide.BodyFlip.Known ? SkateKeys.BodyVarial : 0f;
        if (body != 0f && Math.Abs(bodyLeft) < 0.001f && Math.Abs(BodyDeg) < 0.001)
        {
            if (left < Math.Abs(body) / ((float)SkateMotion.RotationRate * CurrentRotationRateScale)) Late("a body varial", left);
            bodyLeft = body; TrickAir = true;
        }
        var spinAxis = carryingBoard || SkateSurfaceRide.BodyFlip.Known ? 0f : SpinAxis(left, dt);
        var grabReady = !SkateActions.Owns && !SkateOldschoolRide.Active && !BoardPath.Active && !FootPlanted && Math.Abs(flipLeft) < 0.001f && Math.Abs(shoveLeft) < 0.001f;
        // A real vert departure needs no pop to become a grab trick. Start the
        // existing scorer lifetime only on an eligible request; Jumped stays false.
        if (!TrickAir && airViewVertDeparture && grabReady && SkateKeys.Grab != SkateGrabKind.None &&
            !Handplant && !GroundGrab.Active && !SkateSurfaceRide.Active && !SkateSurfaceRide.BodyFlip.Known && !BoardPath.Known)
            TrickAir = true;
        Grab = TrickAir && grabReady ? SkateKeys.Grab : SkateGrabKind.None;
        SkateSurfaceRide.TickFlip(pos, dt, TimeToGround(), SkateKeys.BodyFlipRequest); if (Mode != RideMode.Air) return;
        CancelFlipFlickForOwner();

        var flipStep = (float)SkateMotion.FlipRate * CurrentRotationRateScale * dt; var shoveStep = (float)SkateMotion.ShoveRate * CurrentRotationRateScale * dt; var bodyStep = (float)SkateMotion.RotationRate * CurrentRotationRateScale * dt;
        var flipFraction = initialFlipFlick ? (float)(SkateFlipFlick.ActiveSeconds(AirTime, dt) / dt) : 1f;
        var flipAxis = flipFraction > 0 ? Clamp(flipLeft / (flipStep * flipFraction), -1f, 1f) * flipFraction : 0f;
        var shoveAxis = Clamp(shoveLeft / shoveStep, -1f, 1f); var bodyAxis = Clamp(bodyLeft / bodyStep, -1f, 1f);

        stuck = VSpeed < -2f && Math.Abs(ActualV) < 0.3f && AirTime > 0.2f ? stuck + 1 : 0;
        // Touching a curve is decided along its normal, not by world-up: the rider can
        // still be rising when the face catches up. A pop separating from it stays airborne.
        var incoming = dir * Speed + Vector3.up * VSpeed;
        var contactNormal = OnRamp ? BelowNormal : Vector3.up;
        var separation = Vector3.Dot(incoming, contactNormal);
        var closing = Math.Max(0f, -separation);
        if ((separation <= 0f && Clearance <= Math.Max(0.04f, closing * dt + 0.02f)) || stuck >= 3)
        {
            var recovery = stuck >= 3;
            var impact = recovery ? 0f : closing;
            Land(incoming, recovery, dt);
            if (Mode == RideMode.Ground)
            {
                velocity = hasLandRoll ? landRoll : dir * Speed;
                if (!hasLandRoll) velocity.y = 0f;
                velocity -= contactNormal * Clamp(Clearance / dt, 0f, impact);
            }
            else BailStep(incoming, dt);
            return;
        }
        velocity = dir * Speed + Vector3.up * VSpeed;
        // A board half way through a flip, upside down, may come down onto an edge: a darkslide.
        var halfFlipped = !spinning && Math.Abs(shoveLeft) < 0.001f && Math.Abs(bodyLeft) < 0.001f && Math.Abs(flipLeft) > 0.001f
            && Math.Abs(Math.Abs(FlipDeg) - 180.0) <= DarkCatch;
        if (!SkateActions.Owns && !SkateOldschoolRide.Active && !SkateSurfaceRide.BodyFlip.Known && VSpeed < 0f && Jumped && (!Rotating || halfFlipped) && SkateGrind.TryCapture(pos, EdgeVelocity(velocity), NoseYaw, dt))
        {
            LastAir = AirLine(halfFlipped ? "darkslide" : "grind");
            var impact = -VSpeed;
            ClassifyGrind(pos);
            if (halfFlipped)
            {
                // The rest of the flip is not done: the board stays over, the griptape on the edge.
                GrindKind = SkateGrindKind.Darkslide; flipLeft = 0f; SkateBoard.GrindRoll = 180f;
                Out.Say("GRIND kind " + SkateTricks.GrindName(GrindKind, GrindFrontside) + " (caught half way through the flip)");
            }
            Enter(RideMode.Grind); GrindStarts++;
            Yaw = SkateGrind.Heading; Speed = SkateGrind.Speed; VSpeed = 0f;
            LimitSpeed(Cap, Cap);
            TrickTick(false, true, 0, 0, 0, impact, dt);
            TrickAir = Jumped = false; AirTime = 0f; Grab = SkateGrabKind.None;
            if (Mode == RideMode.Grind) GrindVelocity(pos);
            return;
        }
        if (!SkateActions.Owns && !SkateOldschoolRide.Active && !SkateSurfaceRide.BodyFlip.Known && VSpeed < 0f && Jumped && !Rotating && SkateGrind.StallCandidate && Math.Abs(Speed) < StallSpeed)
        {
            BeginStall(pos, dt);
            return;
        }
        // A fall without a jump is no trick, and it is not rolling either: the trick module is not
        // stepped, so an open combo neither banks nor is lost until the rider is down.
        if (TrickAir)
        {
            if (BoardPath.Active)
            {
                var previous = BoardPath; SkateBoardPathSample next; string error;
                if (SkateBoardPath.TrySample(previous.Kind, Math.Min(1, previous.Progress + dt * CurrentRotationRateScale / previous.Duration), previous.Direction, previous.Nollie, previous.Reversed, out next, out error))
                { BoardPath = next; flipAxis = Clamp((float)((next.Roll - previous.Roll) / flipStep), -1f, 1f); }
            }
            // The finger owner reports a real angular delta, not a faster flatground request.
            if (SkateActions.Finger.Initialized) flipAxis = SkateActions.FlipAxis(dt) / CurrentRotationRateScale;
            TrickTick(true, false, spinAxis, flipAxis, shoveAxis, 0, dt, bodyAxis);
            if (!BoardPath.Known && !SkateActions.Finger.Initialized) flipLeft -= flipAxis * flipStep;
            shoveLeft -= shoveAxis * shoveStep; bodyLeft -= bodyAxis * bodyStep;
        }
    }

    private static void CancelFlipFlickForOwner()
    {
        if (SkateActions.Owns || SkateActions.Finger.Initialized || SkateOldschoolRide.Active ||
            SkateSurfaceRide.BodyFlip.Known || BoardPath.Known || Grabbing) initialFlipFlick = false;
    }

    // The rider turns for as long as the turn is held and the next half turn can still be reached
    // before the ground; from there on, and when the turn is let go, he is brought round to the
    // nearest half turn. So a turn never ends between two half turns, which would be a bail: the
    // landing itself takes a few degrees either way.
    private static float SpinAxis(float left, float dt)
    {
        var rate = (float)SkateMotion.RotationRate * CurrentRotationRateScale; var step = rate * dt;
        var want = -SkateKeys.Spin * spinSide;
        // A fall without a jump becomes a trick when a turn is asked for afresh, not by a stick that
        // was steering when the ground ended.
        if (!TrickAir && !(want != 0f && SkateKeys.SpinPressed)) { SpinLead = 0f; return 0f; }
        var at = TrickAir ? (float)Trick.Spin : 0f;
        if (want != 0f)
        {
            var next = at + want * step;
            var ahead = want > 0f ? (float)Math.Ceiling(next / 180.0 - 0.0001) * 180f - next : next - (float)Math.Floor(next / 180.0 + 0.0001) * 180f;
            if (ahead <= (left - dt) * rate + SpinSlack || spinLate) { TrickAir = spinning = true; SpinLead = want * SpinLeadMost; return want; }
            // A turn asked for afresh too late is started all the same, and turns for as long as it
            // is held: not round before the ground, it is a bail.
            // Only in a trick: a fall that was not popped (rolling off an edge) is steered, not spun.
            if (SkateKeys.SpinPressed && TrickAir) { Late(Math.Abs(at) > 45f ? "the next half spin" : "a spin", left); spinLate = true; TrickAir = spinning = true; SpinLead = want * SpinLeadMost; return want; }
        }
        else spinLate = false;
        if (!TrickAir) { SpinLead = 0f; return 0f; }
        var rest = (float)Math.Round(at / 180.0, MidpointRounding.AwayFromZero) * 180f - at;
        spinning = Math.Abs(rest) > 1f;
        SpinLead = Clamp(rest / 90f, -1f, 1f) * SpinLeadMost;
        return Clamp(rest / step, -1f, 1f);
    }

    // A press in the air that the arc cannot finish: started all the same (it will be a bail), and
    // logged for the ride summary, and said on screen for a moment.
    private static void Late(string what, float left)
    {
        Refused++; SkateHud.Notice("Too late for " + what);
        Out.Say("SKATE late " + what + ": " + (left < 0f ? 0f : left).ToString("F2") + " s of air left, " + AirTime.ToString("F2") + " s in");
    }

    private static string AirLine(string end)
    {
        return "air " + end + " time=" + AirTime.ToString("F2") + " peak=" + AirPeak.ToString("F2") + " impact=" + (-VSpeed).ToString("F1") + " spin=" + SpinDeg.ToString("F0") + " flip=" + FlipDeg.ToString("F0")
            + (Math.Abs(ShoveDeg) > 0.5 ? " shove=" + ShoveDeg.ToString("F0") : "") + " jumped=" + Jumped + " speed=" + Speed.ToString("F1")
            + " profile=" + (flatgroundFlight ? "flatground" : "standard") + " gravity=" + CurrentAirGravity.ToString("F2") + " rotation-rate=" + CurrentRotationRateScale.ToString("F2");
    }

    private static void Land(Vector3 incoming, bool recovery, float dt)
    {
        LastAir = AirLine("land");
        if (!SkateActions.LandingReady || !SkateOldschoolRide.LandingReady || !SkateSurfaceRide.LandingReady) { Bail(); return; }
        landRoll = Vector3.zero; hasLandRoll = false;
        var normal = OnRamp ? BelowNormal : Grounded ? Normal : ContactNormal;
        if (recovery) incoming.y = 0f;
        // Back down onto a transition's face past the shared landing's 72 degrees (only ever a
        // ramp's, from Sense): the fall's part into the face is the impact, the rest rolls along it.
        var steep = normal.y <= SharedNormalLeast;
        var impact = 0f; var r = default(SkateLandingResult); string error = null;
        var valid = !recovery && (steep || SkateRidePhysics.TryLand(new SkateLandingInput(V(incoming), V(normal), V(Dir(Yaw)), BailImpact, LandRoll), out r, out error));
        if (valid && steep) impact = Math.Max(0f, -Vector3.Dot(incoming, normal));
        else if (valid) impact = r.Bailed ? Math.Max((float)r.NormalImpact, BailImpact + 0.01f) : (float)r.NormalImpact;
        else if (!recovery) { Physics_(error); Bail(); return; }
        if (AirTime > 0.12f || impact > 1.5f) { Lands++; LandAt = now; LandImpact = impact; }
        landedTrick = TrickAir; spinAtLand = SkateKeys.Spin;
        // A revert is a sweep made after the wheels are down: one from the air (a body varial's
        // circle, with the stick still on the rim) must not turn the board round on landing.
        SkatePad.LeftSweep = 0f;
        // As in Skate: a grab still held as the wheels touch down is a bail.
        var heldGrab = TrickAir && !recovery && !SkateActions.Owns && !SkateOldschoolRide.Active && (Grab != SkateGrabKind.None || SkateKeys.Grab != SkateGrabKind.None);
        Enter(RideMode.Ground);
        if (BoardPath.Active) { Out.Say("SKATE bail: the special board path was unfinished"); Bail(); return; }
        if (heldGrab) { Out.Say("SKATE bail: the grab was still held at the landing"); Bail(); return; }
        if (TrickAir) TrickTick(false, false, 0, 0, 0, impact, dt);
        else if (impact > BailImpact) Bail();
        else TrickTick(false, false, 0, 0, 0, 0, dt);
        if (Mode == RideMode.Ground)
        {
            Yaw += airNoseOffset + airDeckOffset; airNoseOffset = airDeckOffset = 0f; comingRound = false;
            SpinDeg = FlipDeg = ShoveDeg = BodyDeg = 0;
            if (SkateOldschoolRide.Active)
            {
                var physical = Flat(SkateOldschoolRide.FinalForward);
                DeckReversed = Vector3.Dot(Dir(Yaw + (Trick.Switch ? 180 : 0)), physical) < 0;
                if (physical.sqrMagnitude > .0001f)
                    Yaw = (float)(Math.Atan2(physical.x, physical.z) * 180 / Math.PI) - (Trick.Switch ? 180 : 0) - (DeckReversed ? 180 : 0);
            }
            var axis = Dir(Yaw + (Trick.Switch ? 180f : 0f) + (DeckReversed ? 180f : 0f));
            Vector3 roll;
            if (steep) roll = incoming - normal * Vector3.Dot(incoming, normal);
            else if (SkateRidePhysics.TryLand(new SkateLandingInput(V(incoming), V(normal), V(axis), BailImpact, recovery ? 0f : LandRoll), out r, out error)) roll = V(r.RollVelocity);
            else { Physics_(error); Speed = Flat(incoming).magnitude * (Speed < 0f ? -1f : 1f); Bail(); return; }
            Tangent = SurfaceTangent(normal, Dir(Yaw)); Normal = normal;
            Speed = Vector3.Dot(roll, Tangent);
            LimitSpeed(Cap, Cap);
            landRoll = Tangent * Speed; hasLandRoll = true;
            SkateSurfaceRide.Landed();
        }
        flipLeft = shoveLeft = 0f; BoardPath = default(SkateBoardPathSample); spinning = false; TrickAir = Jumped = false; Grab = SkateGrabKind.None; AirTime = 0f;
    }

    private static void TrickTick(bool airborne, bool grinding, float spinAxis, float flipAxis, float shoveAxis, float impact, float dt, float bodyAxis = 0f)
    {
        SkateTrickResult r; string error;
        var rolling = !airborne && !grinding;
        // Which end of the deck leads, physically: the scorer latches it at the first airborne step
        // and names the flight Fakie (switch, the tail leading) or Switch (switch, the nose leading).
        // The pop end stays the rider's: an ollie pops the end under the back foot, whichever end
        // of the deck that is after a shove.
        var input = new SkateTrickInput(spinAxis, flipAxis, shoveAxis, airborne ? Grab : SkateGrabKind.None, airborne, grinding, impact, Manual && rolling, NoseManual && !Manual && rolling,
            Math.Abs(Speed), airborne ? popKind : SkatePopKind.None, grinding ? GrindKind : SkateGrindKind.None, grinding && GrindFrontside,
            airborne && LateTrick && (flipAxis != 0f || shoveAxis != 0f), airborne ? bodyAxis : 0f, TailFirst != DeckReversed,
            airborne ? BoardPath : default(SkateBoardPathSample));
        if (!SkateTricks.TryStep(Trick, input, dt, BailImpact, CurrentRotationRateScale, out r, out error))
        {
            Out.Say("SKATE trick step refused: " + error);
            return;
        }
        SpinDeg = airborne ? r.BoardSpin : 0; FlipDeg = airborne ? r.BoardFlip : 0; ShoveDeg = airborne ? r.BoardShove : 0; BodyDeg = airborne ? r.BodySpin : 0;
        Take(r);
    }

    private static void Take(SkateTrickResult r)
    {
        bool airLanded = Trick.Airborne && (r.Events & SkateTrickEvents.Landed) != 0;
        long rotationBonus = (airLanded && SkateOldschoolRide.Active ?
            (long)Math.Round(Math.Abs(SkateOldschoolRide.CompletedFlip) / 360) * 100 + (long)Math.Round(Math.Abs(SkateOldschoolRide.CompletedShove) / 180) * 100 : 0) +
            (airLanded ? SkateSurfaceRide.LandingBonus : 0);
        bool endedPlant = Trick.Grinding && Trick.GrindKind == SkateGrindKind.Handplant && (r.Events & SkateTrickEvents.GrindEnded) != 0;
        var name = endedPlant ? SkateHandplants.Name(HandplantVariantUsed, Trick.GrindFrontside) :
            SkateSurfaceRide.Name(SkateOldschoolRide.Name(SkateActions.Name(r.TrickName, (r.Events & SkateTrickEvents.Landed) != 0), (r.Events & SkateTrickEvents.Landed) != 0), airLanded);
        if (airLanded && rotationBonus > 0)
        {
            SkateTrickResult awarded; string awardError;
            if (SkateTricks.TryAwardLanding(r, name, rotationBonus, out awarded, out awardError)) r = awarded;
            else Out.Say("SKATE landing award refused: " + awardError);
        }
        Trick = r.State; ComboPoints = r.ComboPoints;
        if ((r.Events & SkateTrickEvents.Bailed) != 0) { Bail(); return; }
        // The landing hands the turns back rounded: an odd number of half shoves turned the board round.
        if ((r.Events & SkateTrickEvents.Landed) != 0 && ((long)Math.Round(r.BoardShove / 180.0) & 1) != 0) DeckReversed = !DeckReversed;
        // A body varial of an odd half round leaves the deck the other way round under the feet as well.
        if ((r.Events & SkateTrickEvents.Landed) != 0 && ((long)Math.Round(r.BodySpin / 180.0) & 1) != 0) DeckReversed = !DeckReversed;
        if ((r.Events & (SkateTrickEvents.Landed | SkateTrickEvents.GrindEnded | SkateTrickEvents.ManualEnded)) != 0)
        {
            TrickName = name;
            TrickPoints = r.Points; TrickAt = now;
            if (r.Points > 0) SkateHud.Landed(name, r.Points);
            Out.Say("SKATE trick " + name + " +" + r.Points + " combo=" + r.ComboPoints + " x" + Trick.ComboCount + " switch=" + Trick.Switch);
        }
        if ((r.Events & SkateTrickEvents.ComboBanked) != 0)
        {
            Banked = r.BankedPoints; BankedAt = now;
            SkateHud.Banked(r.BankedPoints, Trick.TotalPoints);
            Out.Say("SKATE combo banked +" + r.BankedPoints + " total=" + Trick.TotalPoints);
        }
    }

    public static void Bail()
    {
        if (Mode == RideMode.Bail || Mode == RideMode.Off) return;
        if (SkateOldschoolRide.Active) SkateBoard.ReleaseIndependent(SkateOldschoolRide.Frame, V(SkateOldschoolRide.State.Velocity));
        if (Stalling) { Stalling = false; Manual = NoseManual = false; SkateGrind.Release(0.02f); }
        SkateTrickResult r; string error;
        SkateHud.Bailed(ComboPoints);
        if (SkateTricks.TryBail(Trick, out r, out error)) Trick = r.State;
        ComboPoints = 0; Manual = NoseManual = false;
        if (Mode == RideMode.Grind) { GrindEnds++; Yaw = SkateGrind.Heading; SkateGrind.Release(Time.fixedDeltaTime); }
        Speed = HorizontalSpeed;
        Enter(RideMode.Bail); SkateActions.Reset(); SkateOldschoolRide.Reset(); SkateSurfaceRide.Reset(); HandplantVariantUsed = SkateHandplantKind.None; GroundGrab = default(SkateGroundGrabState); Bails++;
        TrickName = "Bail"; TrickPoints = 0; TrickAt = now;
        flipLeft = shoveLeft = 0f; BoardPath = default(SkateBoardPathSample); spinning = false; TrickAir = Jumped = false; Grab = SkateGrabKind.None;
        Out.Say("SKATE bail | " + LastAir);
    }

    private static void BailStep(Vector3 game, float dt)
    {
        Speed = Toward(Speed, 0f, BailDecel * dt);
        velocity = Dir(Yaw) * Speed; velocity.y = game.y < 0f ? game.y : 0f;
        if (now - ModeAt > BailSeconds) Dismount("bail");
    }

    // How the board meets the edge it stops on: square with a grab held is a handplant, with the
    // stick back or forward a tail or nose stall, otherwise a board stall; slowly along it, a 50-50 stall.
    private static void BeginStall(Vector3 pos, float dt)
    {
        HandplantVariantUsed = SkateHandplantKind.None;
        LastAir = AirLine("stall");
        var impact = -VSpeed;
        stallSign = Speed < 0f ? -1f : 1f;
        // A handplant is asked for with a bumper (LB frontside, RB backside) or, on the keyboard, the grab key.
        var plant = SkateKeys.HandplantFront || SkateKeys.HandplantBack || (!SkateKeys.Pad && (Grab != SkateGrabKind.None || SkateKeys.Grab != SkateGrabKind.None));
        GrindKind = plant ? SkateGrindKind.Handplant : !SkateGrind.StallSquare ? SkateGrindKind.FiftyFiftyStall
            : SkateKeys.Manual ? SkateGrindKind.TailStall : SkateKeys.NoseManual ? SkateGrindKind.NoseStall : SkateGrindKind.BoardStall;
        SkateGrind.BeginStall(pos);
        var to = SkateGrind.Target - pos; to.y = 0f;
        GrindFrontside = Vector3.Dot(to, Dir(NoseYaw + 90f * StanceSign)) > 0f; GrindAlong = false;
        // The board stops on the edge as it came: square to it, or along it for a 50-50 stall.
        var stallRel = Delta(SkateGrind.Heading, DeckYaw);
        grindNose = Delta(SkateGrind.Heading, NoseYaw);
        GrindDeck = SkateGrind.StallSquare ? stallRel : Math.Abs(stallRel) > 90f ? 180f : 0f;
        if (GrindKind == SkateGrindKind.Handplant && (SkateKeys.HandplantFront || SkateKeys.HandplantBack)) GrindFrontside = SkateKeys.HandplantFront;
        Enter(RideMode.Grind); GrindStarts++; Stalls++; Stalling = true; stallAt = now; handDownAt = -99f;
        Speed = 0f; VSpeed = 0f;
        TrickTick(false, true, 0, 0, 0, impact, dt);
        TrickAir = Jumped = false; AirTime = 0f; Grab = SkateGrabKind.None;
        Manual = GrindKind == SkateGrindKind.TailStall; NoseManual = GrindKind == SkateGrindKind.NoseStall;
        StallVelocity(pos);
        Out.Say("GRIND " + SkateTricks.GrindName(GrindKind, GrindFrontside) + " begins");
    }

    // Held still on the edge until a pop or StallMax; then back the way the board came, tail first
    // (a rock to fakie), or nose first out of a handplant, which comes round.
    private static void StallStep(Vector3 pos, float dt)
    {
        if (Handplant && !SkateGrind.StallSupported && !SkateKeys.Scripted) { Bail(); return; }
        var popped = now - jumpWanted < 0.12f && (SkateKeys.Scripted || SkateKeys.ControlsActive) && jumpInputEpoch == SkateKeys.InputEpoch;
        var leaving = popped || now - stallAt > StallMax;
        if (GrindKind == SkateGrindKind.Handplant)
        {
            // The pop (or the time) starts the way down; the edge is left once the body is back on the board.
            if (leaving && handDownAt < 0f) { handDownAt = now; handDownPop = popped; handDownInputEpoch = SkateKeys.InputEpoch; jumpWanted = -99f; }
            if (!(SkateKeys.Scripted || SkateKeys.ControlsActive) || handDownInputEpoch != SkateKeys.InputEpoch) handDownPop = false;
            leaving = handDownAt > 0f && now - handDownAt >= HandDownSeconds;
            popped = leaving && handDownPop;
        }
        if (leaving)
        {
            Stalling = false; Manual = NoseManual = false; GrindEnds++; SkateGrind.Release(dt);
            var planted = GrindKind == SkateGrindKind.Handplant;
            var noseStall = NoseYaw; var deckStall = DeckYaw;
            if (planted) { Yaw += 180f; Speed = stallSign * StallOut; }
            else Speed = -stallSign * StallOut;
            if (popped)
            {
                Pops++; jumpWanted = -99f;
                TakeOff(true, OlliePop * wantScale * PopHeight(wantKind) + AirGravity * dt);
                if (planted) ComeRound(noseStall, deckStall);
                AirStep(pos, dt);
            }
            else
            {
                TakeOff(false, 0f);
                if (planted) ComeRound(noseStall, deckStall);
                velocity = Dir(Yaw) * Speed;
                TrickTick(false, false, 0, 0, 0, 0, dt);
            }
            return;
        }
        SkateGrind.HoldStall(pos);
        StallVelocity(pos);
        TrickTick(false, true, 0, 0, 0, 0, dt);
    }

    private static void StallVelocity(Vector3 pos)
    {
        var to = SkateGrind.Target - pos;
        var side = to; side.y = 0f;
        var pull = side * 8f;
        if (pull.sqrMagnitude > 6.25f) pull = pull.normalized * 2.5f;
        velocity = pull;
        velocity.y = Clamp(to.y * 10f, -3f, 3f);
    }

    private static void GrindStep(Vector3 pos, float dt)
    {
        if (Stalling) { StallStep(pos, dt); return; }
        bool ended;
        if (now - jumpWanted < 0.12f)
        {
            var nose = NoseYaw; var deck = DeckYaw;
            // Out of a darkslide the board finishes the flip it was caught in: the remaining half,
            // the same way round, before the ground; the scorer carries the half it has.
            var dark = Trick.DarkslideCaught ? Math.Sign(Trick.Flip) : 0;
            Pops++; GrindEnds++; jumpWanted = -99f; SkateGrind.Release(dt);
            Speed = SkateGrind.Speed; Yaw = SkateGrind.Heading;
            LimitSpeed(Cap, Cap);
            if (dark != 0) { wantFlip = 0; wantShove = 0f; }
            TakeOff(true, OlliePop * wantScale * PopHeight(wantKind) + AirGravity * dt);
            if (dark != 0) { flipLeft = 180f * dark; SkateBoard.GrindRoll = 0f; FlipDeg = Trick.Flip; }
            KeepAirPose(nose, deck);
            AirStep(pos, dt);
            return;
        }
        if (Held)
        {
            // Held back on the edge (a post, a wall at its end): the grind is over, and the board
            // comes down with the pace it actually had.
            GrindEnds++; SkateGrind.Release(dt);
            SkateGrind.Speed = Speed = (Speed < 0f ? -1f : 1f) * Math.Min(Math.Abs(Speed), Math.Abs(Actual) + HeldLeft);
            var noseHeld = NoseYaw; var deckHeld = DeckYaw;
            Yaw = SkateGrind.Heading;
            TakeOff(false, 0f);
            ComeRound(noseHeld, deckHeld);
            velocity = Dir(Yaw) * Speed;
            TrickTick(false, false, 0, 0, 0, 0, dt);
            return;
        }
        if (!SkateGrind.TryContinue(pos, dt, out ended) || ended)
        {
            GrindEnds++; SkateGrind.Release(dt);
            // Off the edge the board comes round to the way of travel, as it does in Skate: from the
            // angle it had on the edge, over a moment.
            var noseOff = NoseYaw; var deckOff = DeckYaw;
            Speed = SkateGrind.Speed; Yaw = SkateGrind.Heading;
            LimitSpeed(Cap, Cap);
            TakeOff(false, 0f);
            ComeRound(noseOff, deckOff);
            velocity = Dir(Yaw) * Speed;
            TrickTick(false, false, 0, 0, 0, 0, dt);
            return;
        }
        Speed = SkateGrind.Speed; Yaw = SkateGrind.Heading;
        LimitSpeed(Cap, Cap);
        Tangent = Dir(SkateGrind.Heading);
        GrindVelocity(pos);
        // Along the edge the stick leans the board onto one truck: back for a 5-0, forward for a nosegrind.
        if (GrindKind == SkateGrindKind.Darkslide) { }
        else if (grindClassified) GrindKind = SkateGrinds.Classify(grindAcross, grindCrossing, grindNoseFar > 0f, SkateKeys.Manual, SkateKeys.NoseManual);
        else if (GrindAlong) GrindKind = SkateKeys.Manual ? SkateGrindKind.FiveO : SkateKeys.NoseManual ? SkateGrindKind.Nosegrind : SkateGrindKind.FiftyFifty;
        // Across the edge on the tail or the nose, the stick held back or forward stands the deck up: a bluntslide.
        else if (GrindKind == SkateGrindKind.Tailslide || GrindKind == SkateGrindKind.Bluntslide) GrindKind = SkateKeys.Manual ? SkateGrindKind.Bluntslide : SkateGrindKind.Tailslide;
        else if (GrindKind == SkateGrindKind.Noseslide || GrindKind == SkateGrindKind.Nosebluntslide) GrindKind = SkateKeys.NoseManual ? SkateGrindKind.Nosebluntslide : SkateGrindKind.Noseslide;
        TrickTick(false, true, 0, 0, 0, 0, dt);
    }

    // How the board meets the edge: along it (both trucks), across it (a slide, by which part of the
    // board is on the edge), or at an angle (one truck on, the nose over or beside the edge). The
    // side is frontside when the edge is on the side the rider's chest faces.
    public static void ClassifyGrindForTest(Vector3 pos) { ClassifyGrind(pos); }
    private static void ClassifyGrind(Vector3 pos)
    {
        var rail = Dir(SkateGrind.Heading); var deck = Dir(DeckYaw);
        var rel = Delta(SkateGrind.Heading, DeckYaw); var across = Math.Abs(rel) > 90f ? 180f - Math.Abs(rel) : Math.Abs(rel);
        var to = SkateGrind.Target - pos; to.y = 0f;
        var chest = Dir(NoseYaw + 90f * StanceSign);
        GrindFrontside = Vector3.Dot(to, chest) > 0f;
        // Where the edge crosses the board's own axis, from its middle in metres; the nose is "far"
        // when it points away from the side the rider came from.
        var cross = deck.x * rail.z - deck.z * rail.x;
        grindCross = cross; var t = 0f;
        if (Math.Abs(cross) > 0.2f) t = (to.x * rail.z - to.z * rail.x) / cross;
        // Keep the side from which the popped flight approached. Recomputing it from the landed
        // board centre makes every front-truck contact look identical after crossing the rail.
        var approach = airFrom - SkateGrind.Target; approach.y = 0f;
        approach -= rail * Vector3.Dot(approach, rail);
        var near = airFromValid && approach.sqrMagnitude > 0.0025f ? approach.normalized : -to.normalized;
        grindNoseFar = Vector3.Dot(deck, near) < 0f ? 1f : -1f;
        GrindAlong = across < 25f;
        // Along the edge the deck is squared to it; across or at an angle it keeps the angle it had.
        grindNose = Delta(SkateGrind.Heading, NoseYaw);
        GrindDeck = GrindAlong ? (Math.Abs(rel) > 90f ? 180f : 0f) : rel;
        grindAcross = across; grindCrossing = t; grindClassified = true;
        GrindKind = SkateGrinds.Classify(across, t, grindNoseFar > 0f, SkateKeys.Manual, SkateKeys.NoseManual);
        Out.Say("GRIND kind " + SkateTricks.GrindName(GrindKind, GrindFrontside) + " (deck " + rel.ToString("F0") + " deg to the edge, crossing at " + t.ToString("F2") + " m)");
    }

    private static void GrindVelocity(Vector3 pos)
    {
        var to = SkateGrind.Target - pos;
        var side = to; side.y = 0f;
        var dir = Dir(SkateGrind.Heading);
        side = side - dir * Vector3.Dot(side, dir);
        var pull = side * 8f;
        if (pull.sqrMagnitude > 6.25f) pull = pull.normalized * 2.5f;
        velocity = dir * Speed + pull;
        velocity.y = Clamp(to.y * 10f, -3f, 3f);
    }
}

// Must be created after the game's walk component exists: its FixedUpdate and LateUpdate then run
// after the game's own.
public class LateDriver : MonoBehaviour
{
    public LateDriver(IntPtr p) : base(p) { }

    public static int Calls;

    private void FixedUpdate()
    {
        Calls++;
        var t0 = SkateClock.Now(); SkateRide.Step(); SkateClock.Add(SkateClock.Ride, t0);
        SkateRig.SyncRootProxy(0);
    }

    private void LateUpdate()
    {
        SkateRig.LateFrame();
    }
}
