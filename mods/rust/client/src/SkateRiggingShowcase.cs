using System;
using Shortcut.RustMod;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// A visual diagnostic on an observed flat stage. It samples authored poses; it does not run a
// trick, add points, move the physics body to a pose or certify that the trick works in a ride.
public static partial class SkateRiggingShowcase
{
    public const float CycleSeconds = 5f;
    public const string ControlsText = "Dpad L/R: previous/next | A: pause | B/Y/Back: stop | LB/RB: view | X: stance | Up: switch | Down: reverse | RS click: zoom";
    private static bool active, running, paused, goofy, ridingSwitch, reversed, snapshot;
    private static int index, version, cacheVersion = -1, endApplied = -1, completedCount;
    private static double elapsed;
    private static float lastAt, waitingAt = -1, lastAppliedAt, checkAt;
    private static int lastApplied, stageCell, monitorCell;
    private static bool candidate, inputPrimed;
    private static bool[] completed;
    private static Vector3 candidateBody, leasePosition, focus;
    private static SkateBoardPose stageFrame, currentFrame;
    private static Collider groundCollider;
    private static int groundId, bodyId;
    private static Rigidbody leaseBody;
    private static Transform leaseRoot, leaseModel;
    private static float leaseLife;
    private static uint leaseEpoch, padEpoch;
    private static bool padShared;
    private static uint waitingPadEpoch;
    private static bool waitingPadShared;
    private static int blockedPad, lastPad, blockedKeys, lastKeys;
    private static bool savedScripted, savedFrozen, savedFixed, savedChase, savedStaged;
    private static SkateHud.Shown savedHud;
    private static float savedBearing, savedHeight, savedDistance, ownedBearing, ownedHeight, ownedDistance;
    private static bool ownedFixed, ownedChase;
    private static SkateRiderRig cacheRig;
    private static SkateRiderPose cachePose;
    private static SkateVector cacheUp, cacheLeft, cacheRight;
    private const int StopKey = 1, NextKey = 2, PreviousKey = 4, PauseKey = 8, StanceKey = 16,
        SwitchKey = 32, ReverseKey = 64, LeftViewKey = 128, RightViewKey = 256, ZoomKey = 512;
    private const int GridGround = 25, GridAcross = 11, GridHeights = 10, GridCells = GridGround + GridAcross * GridHeights;

    public static bool Active { get { return active; } }
    public static bool Running { get { return running; } }
    public static bool Paused { get { return paused; } }
    public static int Index { get { return index; } }
    public static double Progress { get { return Math.Max(0, Math.Min(1, elapsed / PlaybackSeconds(index))); } }
    public static SkateBoardPose StageFrame { get { return stageFrame; } }
    public static SkateBoardPose CurrentFrame { get { return running ? currentFrame : default(SkateBoardPose); } }
    public static Vector3 Focus { get { return focus; } }

    public static bool Start() { return Start(Time.realtimeSinceStartup); }
    public static bool Start(float now)
    {
        if (active) return true;
        if (Scenarios.Name != "" && Scenarios.Name != "rigging") return false;
        Scenarios.Name = "rigging"; active = true; running = paused = snapshot = candidate = inputPrimed = false;
        waitingAt = -1; lastAt = now; elapsed = 0; index = completedCount = stageCell = monitorCell = 0;
        version++; cacheVersion = endApplied = -1; currentFrame = stageFrame = default(SkateBoardPose);
        completed = null; Scenarios.Phase = "Rigging preview: stand on clear, flat ground";
        return true;
    }

    public static void Update(float now)
    {
        if (!active && (Scenarios.Name != "rigging" || !Start(now))) return;
        if (Scenarios.Name != "rigging") { Stop("Rigging preview replaced"); return; }
        try
        {
            SkatePad.Poll(now); // Scripted Keys.Poll deliberately does not poll the real reader.
            var keyboard = Keyboard.current;
            var keys = ReadKeys(keyboard); var buttons = SkatePad.Shared ? SkatePad.Buttons : 0;
            var pressed = SkatePad.Pressed();
            if (!inputPrimed)
            {
                blockedPad = lastPad = buttons; blockedKeys = lastKeys = keys;
                waitingPadEpoch = SkatePad.InputEpoch; waitingPadShared = SkatePad.Shared; inputPrimed = true; pressed = 0;
            }
            else if (!running && (waitingPadEpoch != SkatePad.InputEpoch || waitingPadShared != SkatePad.Shared))
            { waitingPadEpoch = SkatePad.InputEpoch; waitingPadShared = SkatePad.Shared; BlockHeld(buttons, keys); pressed = 0; }
            blockedPad &= buttons; blockedKeys &= keys;
            var padDown = (pressed | buttons & ~lastPad) & ~blockedPad;
            var keyDown = (keys & ~lastKeys) & ~blockedKeys;
            lastPad = buttons; lastKeys = keys;
            if ((padDown & (SkatePad.B | SkatePad.Y | SkatePad.Back)) != 0 || (keyDown & StopKey) != 0)
            { Stop("Rigging preview stopped"); return; }
            if (!running)
            {
                if (!InputAvailable(keyboard)) { candidate = false; BlockHeld(buttons, keys); return; }
                TryRun(now); return;
            }
            if (!InputAvailable(keyboard)) { Stop("Rigging preview stopped: a menu or another window opened"); return; }
            if (!LeaseCurrent()) { Stop("Rigging preview stopped: the player or movement owner changed"); return; }
            if (padShared != SkatePad.Shared || padEpoch != SkatePad.InputEpoch || leaseEpoch != SkateKeys.InputEpoch)
            { Stop("Rigging preview stopped: the input device changed"); return; }
            var real = leaseBody.position;
            if ((real - leasePosition).sqrMagnitude > .25f)
            { Stop("Rigging preview stopped: the player moved away from the stage"); return; }
            if (now >= checkAt)
            {
                checkAt = now + .25f;
                if (!GroundCurrent(real)) { Stop("Rigging preview stopped: the supporting ground changed"); return; }
            }
            // Recheck the static visual envelope incrementally, never a large query burst.
            for (int n = 0; n < 2; n++)
            {
                if (!StageCellClear(monitorCell++ % GridCells))
                { Stop("Rigging preview stopped: the stage is obstructed"); return; }
            }
            HandleControls(padDown, keyDown, now);
            if (!running) return;
            if (RiderRig.Applied != lastApplied) { lastApplied = RiderRig.Applied; lastAppliedAt = now; }
            else if (now - lastAppliedAt > 2f) { Stop("Rigging preview stopped: the rider pose was not displayed"); return; }
            var dt = Math.Max(0, Math.Min(.25f, now - lastAt)); lastAt = now;
            if (!paused)
            {
                if (elapsed >= PlaybackSeconds(index) && endApplied >= 0 && RiderRig.Applied > endApplied)
                {
                    if (!completed[index]) { completed[index] = true; completedCount++; }
                    if (index + 1 >= Count)
                    { Stop("Rigging preview finished: " + completedCount + "/" + Count + " entries displayed"); return; }
                    Select(index + 1, now);
                }
                else if (elapsed < PlaybackSeconds(index)) { elapsed = Math.Min(PlaybackSeconds(index), elapsed + dt); version++; }
            }
        }
        catch (Exception e) { Stop("Rigging preview unavailable: " + e.GetType().Name); }
    }

    private static void TryRun(float now)
    {
        if (!SkateRig.Ready || SkateRig.Body == null || SkateRig.LocalT == null || !RiderRig.Bound || RiderRig.Root == null ||
            (!SkateRide.On && !SkateRig.CanMount) || SkateRide.On && SkateRide.Mode != RideMode.Ground)
        { candidate = false; Scenarios.Phase = "Rigging preview: waiting for the rider and movement body"; return; }
        if (waitingAt < 0) waitingAt = now;
        if (now - waitingAt > 60f) { Stop("Rigging preview needs a ready rider on clear, flat ground"); return; }
        if (SkateKeys.Scripted || SkateRide.Frozen)
        { Stop("Rigging preview cannot take over another diagnostic"); return; }
        var body = SkateRig.Body; var pos = body.position; var motion = body.linearVelocity;
        if (!Finite(pos) || !Finite(motion)) { Stop("Rigging preview needs a valid movement body"); return; }
        if (motion.x * motion.x + motion.z * motion.z > .04f || Math.Abs(motion.y) > .6f)
        { candidate = false; Scenarios.Phase = "Rigging preview: stop moving on clear, flat ground"; return; }
        RaycastHit hit;
        if (!FlatBelow(pos, out hit))
        { candidate = false; Scenarios.Phase = "Rigging preview: find dry, flat ground"; return; }
        if (!candidate || (pos - candidateBody).sqrMagnitude > .0025f || groundCollider == null || hit.collider != groundCollider ||
            Math.Abs(hit.m_Point.y - ((float)stageFrame.Origin.Y - RiderRig.DeckTop)) > .04f)
        {
            candidate = true; candidateBody = pos; groundCollider = hit.collider; groundId = groundCollider.GetInstanceID(); stageCell = 0;
            stageFrame = new SkateBoardPose(V(new Vector3(pos.x, hit.m_Point.y + RiderRig.DeckTop, pos.z)), V(SkateRide.Dir(SkateCamera.LookYaw)), new SkateVector(0, 1, 0));
        }
        for (int n = 0; n < 8 && stageCell < GridCells; n++, stageCell++)
            if (!StageCellClear(stageCell))
            { candidate = false; Scenarios.Phase = "Rigging preview: leave clear space around and above the rider"; return; }
        if (stageCell < GridCells)
        { Scenarios.Phase = "Rigging preview: checking the flat stage " + (stageCell * 100 / GridCells) + "%"; return; }
        if (Count <= 0) { Stop("Rigging preview has no pose catalogue"); return; }
        savedScripted = SkateKeys.Scripted; savedFrozen = SkateRide.Frozen;
        savedFixed = SkateCamera.Fixed; savedChase = SkateCamera.Chase; savedBearing = SkateCamera.Bearing;
        savedHeight = SkateCamera.FixedHeight; savedDistance = SkateCamera.FixedDistance;
        savedStaged = SkateHud.Staged; savedHud = SkateHud.Stage; snapshot = true;
        leaseBody = body; leaseRoot = SkateRig.LocalT; leaseModel = RiderRig.Root;
        SkateKeys.Scripted = true; SkateKeys.Reset();
        var yaw = (float)(Math.Atan2(stageFrame.Forward.X, stageFrame.Forward.Z) * 180 / Math.PI);
        if (!SkateRide.On) SkateRig.MountNow(yaw);
        else
        {
            // The already-mounted body owns a valid RootProxy lease. Reuse the canonical mount
            // reset on that SAME body so no releasing grab, held board, path or private air offset
            // can leak into a visual diagnostic. Mount never changes the body position.
            SkateRide.Mount(body, yaw); SkateCamera.BeginRide();
        }
        if (!SkateRide.On || SkateRide.Mode != RideMode.Ground)
        { Stop("Rigging preview could not acquire the skateboard"); return; }
        leaseBody = SkateRig.Body; leaseRoot = SkateRig.LocalT; leaseModel = RiderRig.Root;
        bodyId = leaseBody.GetInstanceID(); leaseLife = SkateRig.AwakeAt; leasePosition = leaseBody.position;
        leaseEpoch = SkateKeys.InputEpoch; padEpoch = SkatePad.InputEpoch; padShared = SkatePad.Shared;
        SkateRide.Frozen = true; SkateRide.Speed = SkateRide.VSpeed = SkateRide.PushPhase = SkateRide.Lean = SkateRide.SteerRate = 0;
        running = true; paused = false; elapsed = 0; goofy = SkateRide.Goofy; ridingSwitch = reversed = false;
        completed = new bool[Count]; completedCount = 0; index = 0; lastAt = lastAppliedAt = now;
        lastApplied = RiderRig.Applied; endApplied = cacheVersion = -1; version++; monitorCell = 0; checkAt = now + .25f;
        focus = U(stageFrame.Origin) + Vector3.up * .9f;
        SetView(90f, 4.5f);
        SkateHud.Staged = true; SkateHud.Stage = new SkateHud.Shown { Riding = true, Live = "Rigging preview", Pad = SkatePad.Shared };
        BlockHeld(SkatePad.Buttons, ReadKeys(Keyboard.current));
        Describe(); SkateHud.Notice("Rigging preview: Dpad L/R next/previous, A pause, B stop. " + CaptureStatus);
    }

    private static bool LeaseCurrent()
    {
        return SkateRig.Ready && SkateRide.On && SkateRide.Mode == RideMode.Ground && SkateRide.Frozen && SkateKeys.Scripted &&
            SkateRig.Body != null && SkateRig.Body == leaseBody && leaseBody.GetInstanceID() == bodyId && SkateRig.LocalT == leaseRoot &&
            SkateRig.AwakeAt == leaseLife && RiderRig.Bound && RiderRig.Root != null && RiderRig.Root == leaseModel;
    }
    private static bool InputAvailable(Keyboard keyboard)
    { return Application.isFocused && keyboard != null && Cursor.lockState == CursorLockMode.Locked && !SkatePieces.BlocksControls && !SkatePieces.OwnsCursor; }
    private static void BlockHeld(int buttons, int keys)
    { blockedPad = lastPad = buttons; blockedKeys = lastKeys = keys; SkatePad.Pressed(); inputPrimed = true; }
    private static bool FlatBelow(Vector3 pos, out RaycastHit hit)
    {
        return SkateParks.Ray(pos + Vector3.up * .35f, Vector3.down, out hit, .9f, SkateRide.GroundMask) &&
            hit.collider != null && hit.m_Normal.y >= .995f && Math.Abs(hit.m_Point.y - pos.y) <= .2f && hit.m_Point.y > SkateRide.SeaLevel + .2f;
    }
    private static bool GroundCurrent(Vector3 pos)
    {
        RaycastHit hit;
        return FlatBelow(pos, out hit) && hit.collider == groundCollider && hit.collider.GetInstanceID() == groundId &&
            Math.Abs(hit.m_Point.y - ((float)stageFrame.Origin.Y - RiderRig.DeckTop)) <= .04f;
    }
    private static bool StageCellClear(int cell)
    {
        var origin = U(stageFrame.Origin) - Vector3.up * RiderRig.DeckTop;
        var across = U(stageFrame.Right); var along = U(stageFrame.Forward);
        RaycastHit hit;
        if (cell < GridGround)
        {
            var point = origin + across * ((cell % 5 - 2) * (float)StageRadius / 2) + along * ((cell / 5 - 2) * (float)StageRadius / 2);
            return SkateParks.Ray(point + Vector3.up * .35f, Vector3.down, out hit, .7f, SkateRide.GroundMask) && hit.collider != null && hit.m_Normal.y >= .995f && Math.Abs(hit.m_Point.y - origin.y) <= .04f;
        }
        var at = cell - GridGround; var x = -((float)StageRadius) + at % GridAcross * .5f;
        var height = .43f + at / GridAcross * .5f;
        var from = origin + across * x - along * (float)StageRadius + Vector3.up * height;
        return !SkateParks.Sphere(from, .4f, along, out hit, (float)StageRadius * 2, SkateRide.GroundMask);
    }

    private static void HandleControls(int pad, int keys, float now)
    {
        if ((pad & SkatePad.DpadRight) != 0 || (keys & NextKey) != 0) Select((index + 1) % Count, now);
        else if ((pad & SkatePad.DpadLeft) != 0 || (keys & PreviousKey) != 0) Select((index + Count - 1) % Count, now);
        if ((pad & SkatePad.A) != 0 || (keys & PauseKey) != 0) { paused = !paused; lastAt = now; Describe(); }
        if ((pad & SkatePad.X) != 0 || (keys & StanceKey) != 0) { goofy = !goofy; Invalidate(); Describe(); }
        if ((pad & SkatePad.DpadUp) != 0 || (keys & SwitchKey) != 0) { ridingSwitch = !ridingSwitch; Invalidate(); Describe(); }
        if ((pad & SkatePad.DpadDown) != 0 || (keys & ReverseKey) != 0) { reversed = !reversed; Invalidate(); Describe(); }
        if ((pad & SkatePad.LeftBumper) != 0 || (keys & LeftViewKey) != 0) SetView(ownedBearing - 45, ownedDistance);
        if ((pad & SkatePad.RightBumper) != 0 || (keys & RightViewKey) != 0) SetView(ownedBearing + 45, ownedDistance);
        if ((pad & SkatePad.RightStickClick) != 0 || (keys & ZoomKey) != 0)
            SetView(ownedBearing, ownedDistance < 4 ? 4.5f : ownedDistance < 6 ? 6.5f : 3.2f);
    }
    private static void SetView(float bearing, float distance)
    {
        ownedFixed = ownedChase = true; ownedBearing = (bearing % 360 + 360) % 360; ownedDistance = distance; ownedHeight = 1.8f;
        SkateCamera.Fixed = ownedFixed; SkateCamera.Chase = ownedChase; SkateCamera.Bearing = ownedBearing;
        SkateCamera.FixedDistance = ownedDistance; SkateCamera.FixedHeight = ownedHeight;
    }
    private static void Select(int to, float now)
    { index = to; elapsed = paused ? PlaybackSeconds(index) * .5 : 0; lastAt = now; Invalidate(); Describe(); }
    private static void Invalidate() { version++; cacheVersion = endApplied = -1; currentFrame = default(SkateBoardPose); }
    private static void Describe()
    {
        Scenarios.Phase = "RIGGING " + (index + 1) + "/" + Count + " " + Label(index) + " | " + Group(index) + " | " +
            (goofy ? "Goofy" : "Regular") + (ridingSwitch ? " Switch/Fakie" : "") + (reversed ? " Reversed deck" : "") + (paused ? " | PAUSED" : " | AUTO " + PlaybackSeconds(index).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "s") + " | " + CaptureStatus;
        Out.Say(Scenarios.Phase);
        DescribeCaptureSource();
    }
    private static bool Down(KeyControl key) { return key != null && key.isPressed; }
    private static int ReadKeys(Keyboard keyboard)
    {
        if (keyboard == null) return 0;
        return (Down(keyboard.escapeKey) ? StopKey : 0) | (Down(keyboard.rightArrowKey) ? NextKey : 0) |
            (Down(keyboard.leftArrowKey) ? PreviousKey : 0) | (Down(keyboard.spaceKey) ? PauseKey : 0) |
            (Down(keyboard.f2Key) ? StanceKey : 0) | (Down(keyboard.upArrowKey) ? SwitchKey : 0) |
            (Down(keyboard.downArrowKey) ? ReverseKey : 0) | (Down(keyboard.f5Key) ? LeftViewKey : 0) |
            (Down(keyboard.f6Key) ? RightViewKey : 0) | (Down(keyboard.f7Key) ? ZoomKey : 0);
    }

    public static bool TryCurrent(SkateRiderRig rig, out SkateRiderPose pose, out SkateBoardPose board,
        out SkateVector riderUp, out SkateVector leftSoleUp, out SkateVector rightSoleUp, out string error)
    { return TryCurrent(rig, stageFrame, out pose, out board, out riderUp, out leftSoleUp, out rightSoleUp, out error); }
    public static bool TryCurrent(SkateRiderRig rig, SkateBoardPose stage, out SkateRiderPose pose, out SkateBoardPose board,
        out SkateVector riderUp, out SkateVector leftSoleUp, out SkateVector rightSoleUp, out string error)
    {
        pose = default(SkateRiderPose); board = default(SkateBoardPose); riderUp = leftSoleUp = rightSoleUp = default(SkateVector); error = null;
        if (!running) return false;
        if (!stage.Initialized || !SkateBoardPath.Valid(stage) || (stage.Origin - stageFrame.Origin).Length > .02 ||
            SkateVector.Dot(stage.Up, stageFrame.Up) < .999 || SkateVector.Dot(stage.Forward, stageFrame.Forward) < .999)
        { error = "The rigging stage changed."; currentFrame = default(SkateBoardPose); return false; }
        if (cacheVersion != version || !SameRig(cacheRig, rig))
        {
            if (!TrySample(index, Progress, rig, stage, goofy ? SkateStance.Goofy : SkateStance.Regular, ridingSwitch, reversed,
                out cachePose, out currentFrame, out cacheUp, out cacheLeft, out cacheRight, out error))
            { currentFrame = default(SkateBoardPose); cacheVersion = -1; return false; }
            if (!SkateBoardPath.Valid(currentFrame) || !currentFrame.Initialized || !Unit(cacheUp) || !Unit(cacheLeft) || !Unit(cacheRight))
            { error = "The rigging sample is invalid."; currentFrame = default(SkateBoardPose); cacheVersion = -1; return false; }
            cacheRig = rig; cacheVersion = version;
            focus = (U(cachePose.Head) + U(cachePose.Pelvis) + (U(cachePose.LeftAnkle) + U(cachePose.RightAnkle)) * .5f) / 3;
        }
        pose = cachePose; board = currentFrame; riderUp = cacheUp; leftSoleUp = cacheLeft; rightSoleUp = cacheRight;
        if (elapsed >= PlaybackSeconds(index) && endApplied < 0) endApplied = RiderRig.Applied;
        return true;
    }
    public static void ReportPoseFailure(string error)
    { Stop("Rigging preview stopped: " + (string.IsNullOrEmpty(error) ? "the pose could not be displayed" : error)); }

    public static void Stop() { Stop("Rigging preview stopped"); }
    public static void Stop(string reason)
    {
        if (!active && !snapshot) return;
        running = active = false; currentFrame = stageFrame = default(SkateBoardPose); candidate = false; inputPrimed = false;
        try
        {
            if (snapshot && SkateRide.On && SkateKeys.Scripted && SkateRide.Frozen) SkateRide.Dismount("rigging preview ended");
        }
        catch (Exception) { }
        if (snapshot)
        {
            if (SkateKeys.Scripted) SkateKeys.Scripted = savedScripted;
            if (SkateRide.Frozen) SkateRide.Frozen = savedFrozen;
            if (SkateCamera.Fixed == ownedFixed) SkateCamera.Fixed = savedFixed;
            if (SkateCamera.Chase == ownedChase) SkateCamera.Chase = savedChase;
            if (Math.Abs(SkateCamera.Bearing - ownedBearing) < .001f) SkateCamera.Bearing = savedBearing;
            if (Math.Abs(SkateCamera.FixedHeight - ownedHeight) < .001f) SkateCamera.FixedHeight = savedHeight;
            if (Math.Abs(SkateCamera.FixedDistance - ownedDistance) < .001f) SkateCamera.FixedDistance = savedDistance;
            if (SkateHud.Staged && SkateHud.Stage.Live == "Rigging preview") { SkateHud.Staged = savedStaged; SkateHud.Stage = savedHud; }
            SkateKeys.Reset(); RiderRig.BeginRide();
        }
        snapshot = false; completed = null; cacheVersion = endApplied = -1; leaseBody = null; leaseRoot = leaseModel = null; groundCollider = null;
        if (Scenarios.Name == "rigging") { Scenarios.Name = ""; Scenarios.Phase = ""; }
        if (!string.IsNullOrEmpty(reason)) { SkateHud.Notice(reason); Out.Say("RIGGING " + reason); }
    }
    private static bool SameRig(SkateRiderRig a, SkateRiderRig b)
    { return a.PelvisHeight == b.PelvisHeight && a.HipWidth == b.HipWidth && a.ThighLength == b.ThighLength && a.ShinLength == b.ShinLength &&
        a.FootLength == b.FootLength && a.SpineLength == b.SpineLength && a.ShoulderWidth == b.ShoulderWidth && a.UpperArmLength == b.UpperArmLength &&
        a.ForearmLength == b.ForearmLength && a.NeckToHeadLength == b.NeckToHeadLength && a.ShoulderRise == b.ShoulderRise; }
    private static bool Unit(SkateVector vector) { return !double.IsNaN(vector.LengthSquared) && !double.IsInfinity(vector.LengthSquared) && Math.Abs(vector.LengthSquared - 1) < .001; }
    private static bool Finite(Vector3 point)
    { return !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z) && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z); }
    private static SkateVector V(Vector3 point) { return new SkateVector(point.x, point.y, point.z); }
    private static Vector3 U(SkateVector point) { return new Vector3((float)point.X, (float)point.Y, (float)point.Z); }
}
