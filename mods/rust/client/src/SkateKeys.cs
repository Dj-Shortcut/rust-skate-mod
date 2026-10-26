// The legacy Input class is disabled in this client build; a controller arrives as keys through
// Steam Input, and its sticks only through the reader outside the game (SkatePad). Presses are
// latched until a physics step has seen them: the game steps physics at 32 Hz and draws frames
// more often. Nothing is read while the cursor is free (console, chat, inventory), so typing never
// rides.
// With a controller the layout is that of the skate. games: left stick steers and turns the rider
// in the air, right stick is the board (SkateFlick), A and X push, B brakes, Y gets on and off, a
// trigger grabs and the right stick picks the grab. Steam's layout keeps sending its keys for the
// same buttons, so while the controller rides the keys do not.
using System;
using Shortcut.RustMod;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public static class SkateKeys
{
    public const float PadRest = 0.5f, SteerDead = 0.18f, Straight = 0.35f, StickPush = 0.75f, SpinFrom = 0.5f, LeastPop = 0.7f, GrabStick = 0.6f;
    public static bool Scripted;
    public static bool ControlsActive { get; private set; }
    public static uint InputEpoch { get; private set; }
    public static bool Push, Brake, Left, Right, Crouch, Jump, Manual, NoseManual;
    // As in skate.: A pushes with the back foot, X with the front (mongo). A tapped just before the
    // pop's flick, while the right stick is held back for it, makes it a no comply. A trigger held and A or X pressed is a foot plant: LT+A a
    // boneless, LT+X or RT+X a fastplant, RT+A a beanplant. A bumper held when the board meets an
    // edge is a handplant: LB frontside, RB backside. On the keyboard Q (no comply) or Z (boneless)
    // held with Space.
    public const float NoComplyTap = 0.35f;
    // How long the left stick must stay on a side, and still, before it is a turn: a stick that
    // travels round the rim faster than RimMoving (degrees a second) is circling for a body varial,
    // not held to a side, however slowly it goes round.
    public const float SpinHold = 0.06f, RimMoving = 180f;
    private static int rawSide; private static float sideSince = -99f, rimAngle, rimAt = -99f, movedAt = -99f; private static bool onRim; private static int bodySeen;
    public static bool Mongo, HandplantFront, HandplantBack;
    private static float aTappedAt = -99f;
    // What a physics step has not seen yet. A pop comes with what the board does in it: whole flips
    // (a kickflip is +1) and degrees of shove under the feet (frontside is +). LateFlip is flips
    // asked for in the air; HeldFlip continues the same pop. SpinPressed: the turn was asked for
    // afresh, not held from before.
    public static bool JumpPressed, SpinPressed;
    public static SkatePopKind PopKind = SkatePopKind.Ollie;
    public static int PopFlip, LateFlip, HeldFlip;
    public static SkateBoardTrickKind PopSpecial;
    public static int PopSpecialDirection;
    private static int specialBumpers, triggerBlocked, keyboardHeld;
    // Asked for in the air: degrees of shove under the feet, and degrees of body varial (the rider
    // turns, the board does not).
    public static float LateShove, BodyVarial;
    public static float PopShove, PopScale = 1f;
    public static bool JumpTap, ToggleTap, CameraTap, CrouchTap;
    public static float LookYaw;
    // Pad: the controller rides, not the keyboard. Analog: Steer is a rate, not a direction to turn to.
    public static bool Pad, Analog, Charging;
    public static float Steer, Charge;
    public static bool Help;
    public static int HelpPage;
    private static int airGrabBlocked = SkateGrabCatalog.ModifierButtons;
    // Held in the air: the rider's turn, +1 to his right as the stick or the keys say, and the grab.
    public static float Spin;
    public static SkateGrabKind Grab;
    public static SkateGroundGrabKind GroundGrab;
    public static SkateHandplantKind HandplantVariant;
    private static bool wasGroundChord;
    private static int plantButtonsBlocked = SkateGrabCatalog.ModifierButtons;
    private static bool wasPush, wasBrake, wasLeft, wasRight, wasJump, wasCrouch, wasToggle, wasCamera, wasStance, wasBody;
    private static float stanceSince = -1f;
    private static float wasSpin;
    private static bool keyboardSuspended, stickPushBlocked, spinBlocked;
    private static int pumpPushButtons;
    private static uint padInputEpoch;
    private static bool popKeys = true, crouchKeys = true, cameraKey = true, manualKey = true, helpKey = true, taps = true, cursorKnown = true, failed;

    private static void SuspendControls()
    {
        if (ControlsActive) InputEpoch++;
        ControlsActive = false;
    }

    public static void Reset()
    {
        InputEpoch++; ControlsActive = false;
        Push = Brake = Left = Right = Crouch = Jump = Manual = NoseManual = Analog = Charging = Help = false; Steer = Charge = Spin = wasSpin = 0f; PopScale = 1f;
        Grab = SkateGrabKind.None; PopKind = SkatePopKind.Ollie; Mongo = HandplantFront = HandplantBack = false; aTappedAt = -99f;
        GroundGrab = SkateGroundGrabKind.None; HandplantVariant = SkateHandplantKind.None; plantButtonsBlocked = SkateGrabCatalog.ModifierButtons; wasGroundChord = false;
        EndStep(); SkatePad.SuspendGestures(); JumpTap = ToggleTap = CameraTap = CrouchTap = false;
        keyboardSuspended = true; keyboardHeld = 0; triggerBlocked = (SkatePad.LeftTrigger > SkatePad.Pulled ? 1 : 0) | (SkatePad.RightTrigger > SkatePad.Pulled ? 2 : 0); specialBumpers = SkatePad.Buttons & (SkatePad.LeftBumper | SkatePad.RightBumper); stickPushBlocked = SkatePad.LeftY > StickPush; pumpPushButtons = SkatePad.Buttons & (SkatePad.A | SkatePad.X); spinBlocked = SkatePad.LeftX * SkatePad.LeftX + SkatePad.LeftY * SkatePad.LeftY > SpinFrom * SpinFrom; stanceSince = -1f; HelpPage = 0; airGrabBlocked = SkateGrabCatalog.ModifierButtons;
    }

    public static void Poll()
    {
        HandplantVariant = SkateHandplantKind.None;
        JumpTap = ToggleTap = CameraTap = CrouchTap = false;
        if (Scripted || failed) { SuspendControls(); Help = false; stickPushBlocked = false; pumpPushButtons = 0; return; }
        var now = Time.realtimeSinceStartup;
        SkatePad.Poll(now);
        if (padInputEpoch != SkatePad.InputEpoch)
        {
            padInputEpoch = SkatePad.InputEpoch;
            if (Pad) InputEpoch++;
            EndStep(); SkatePad.SuspendGestures();
            keyboardSuspended = true; specialBumpers |= SkatePad.Buttons & (SkatePad.LeftBumper | SkatePad.RightBumper);
            triggerBlocked = (SkatePad.LeftTrigger > SkatePad.Pulled ? 1 : 0) | (SkatePad.RightTrigger > SkatePad.Pulled ? 2 : 0);
            aTappedAt = -99f; rawSide = 0; sideSince = movedAt = rimAt = -99f; onRim = false;
            stickPushBlocked = SkatePad.LeftY > StickPush; pumpPushButtons = SkatePad.Buttons & (SkatePad.A | SkatePad.X);
            spinBlocked = SkatePad.LeftX * SkatePad.LeftX + SkatePad.LeftY * SkatePad.LeftY > SpinFrom * SpinFrom;
            airGrabBlocked = SkateGrabCatalog.ModifierButtons;
        }
        var padDown = SkatePad.Pressed();
        SkatePadLog.Frame(now);
        try
        {
            var kb = Keyboard.current;
            var blocked = CursorFree() && !SkatePieces.OwnsCursor;
            padDown = SkatePieces.Input(padDown, now, kb, Mouse.current, blocked);
            if (kb == null || blocked || SkatePieces.BlocksControls)
            {
                SuspendControls();
                Push = Brake = Left = Right = Crouch = Jump = Manual = NoseManual = Charging = Help = false; Steer = Charge = Spin = wasSpin = 0f; Grab = SkateGrabKind.None;
                wasPush = wasBrake = wasLeft = wasRight = wasJump = wasCrouch = wasToggle = wasCamera = false; EndStep(); SkatePad.SuspendGestures();
                Mongo = HandplantFront = HandplantBack = false; aTappedAt = -99f; rawSide = 0; sideSince = movedAt = rimAt = -99f; onRim = false;
                GroundGrab = SkateGroundGrabKind.None; HandplantVariant = SkateHandplantKind.None; plantButtonsBlocked = SkateGrabCatalog.ModifierButtons; wasGroundChord = false;
                specialBumpers |= SkatePad.Buttons & (SkatePad.LeftBumper | SkatePad.RightBumper);
                triggerBlocked = (SkatePad.LeftTrigger > SkatePad.Pulled ? 1 : 0) | (SkatePad.RightTrigger > SkatePad.Pulled ? 2 : 0);
                // Held controller pushes must return after a menu; closing it is not a new stroke.
                var heldPad = SkateRide.On && SkatePad.Shared;
                stickPushBlocked = heldPad && SkatePad.LeftY > StickPush;
                pumpPushButtons = heldPad ? SkatePad.Buttons & (SkatePad.A | SkatePad.X) : 0;
                spinBlocked = SkatePad.LeftX * SkatePad.LeftX + SkatePad.LeftY * SkatePad.LeftY > SpinFrom * SpinFrom;
                keyboardSuspended = true; stanceSince = -1f; airGrabBlocked = SkateGrabCatalog.ModifierButtons;
                return;
            }
            var stanceKey = Down(kb.f2Key);
            bool push = Down(kb.wKey), brake = Down(kb.sKey), left = Down(kb.aKey), right = Down(kb.dKey);
            bool jump = Down(kb.spaceKey), toggle = Down(kb.kKey), crouch = false, camera = false, manual = false, noComply = false, boneless = false, body = false;
            if (popKeys)
            {
                try { noComply = Down(kb.qKey); boneless = Down(kb.zKey); body = Down(kb.xKey); }
                catch (Exception e) { popKeys = false; Out.Say("INPUT no comply, boneless and body varial keys unavailable: " + e.GetType().Name + ": " + e.Message); }
            }
            if (crouchKeys)
            {
                try { crouch = Down(kb.leftCtrlKey) || Down(kb.cKey); }
                catch (Exception e) { crouchKeys = false; Out.Say("INPUT crouch keys unavailable: " + e.GetType().Name + ": " + e.Message); }
            }
            if (cameraKey)
            {
                try { camera = Down(kb.lKey); }
                catch (Exception e) { cameraKey = false; Out.Say("INPUT camera key unavailable: " + e.GetType().Name + ": " + e.Message); }
            }
            if (manualKey)
            {
                try { manual = kb.leftShiftKey.isPressed; }
                catch (Exception e) { manualKey = false; Out.Say("INPUT manual key unavailable: " + e.GetType().Name + ": " + e.Message); }
            }
            var help = false;
            if (helpKey)
            {
                try { help = kb.hKey.isPressed; }
                catch (Exception e) { helpKey = false; Out.Say("INPUT help key unavailable: " + e.GetType().Name + ": " + e.Message); }
            }
            Help = help || (SkatePad.Shared && SkateRide.On && (SkatePad.Buttons & SkatePad.Back) != 0);
            if (Help && SkatePad.Shared && SkateRide.On && (padDown & (SkatePad.DpadLeft | SkatePad.DpadRight)) != 0) HelpPage = (HelpPage + ((padDown & SkatePad.DpadRight) != 0 ? 1 : 2)) % 3;
            // The first readable frame establishes held keys; it cannot distinguish a new press
            // from one carried through chat, a missing keyboard or a ride reset.
            if (keyboardSuspended)
            {
                wasPush = push; wasBrake = brake; wasLeft = left; wasRight = right;
                wasJump = jump; wasCrouch = crouch; wasToggle = toggle; wasCamera = camera;
                wasStance = stanceKey; wasBody = body;
            }
            if (!stanceKey) wasStance = false;
            SkateRide.StanceHold(ref stanceSince, stanceKey && !wasStance, now);
            // A key held across a menu/reset is not a fresh riding action. Native on-foot input
            // remains with Steam/the game; this filters only the plugin's riding fields.
            int heldKeys = (push ? 1 : 0) | (brake ? 2 : 0) | (left ? 4 : 0) | (right ? 8 : 0) |
                (jump ? 16 : 0) | (crouch ? 32 : 0) | (manual ? 64 : 0) | (body ? 128 : 0);
            if (keyboardSuspended) keyboardHeld = heldKeys;
            keyboardHeld &= heldKeys;
            push &= (keyboardHeld & 1) == 0; brake &= (keyboardHeld & 2) == 0;
            left &= (keyboardHeld & 4) == 0; right &= (keyboardHeld & 8) == 0;
            jump &= (keyboardHeld & 16) == 0; crouch &= (keyboardHeld & 32) == 0;
            manual &= (keyboardHeld & 64) == 0; body &= (keyboardHeld & 128) == 0;
            // Whose hands are on the ride: the controller's from the moment it is touched; the
            // keyboard's again when a riding key goes down while the controller has been at rest,
            // which Steam's layout cannot have sent.
            var keyDown = (push && !wasPush) || (brake && !wasBrake) || (left && !wasLeft) || (right && !wasRight) || (jump && !wasJump) || (crouch && !wasCrouch);
            var had = Pad;
            if (!SkatePad.Shared) Pad = false;
            else if (now - SkatePad.TouchedAt < 0.05f) Pad = true;
            else if (keyDown && now - SkatePad.TouchedAt > PadRest) Pad = false;
            if (Pad != had) { InputEpoch++; Out.Say("INPUT the " + (Pad ? "controller" : "keyboard") + " rides"); }

            if (toggle && !wasToggle) ToggleTap = true;
            if (camera && !wasCamera) CameraTap = true;
            // Two quick jumps get on the board, whoever rides: Steam sends the jump key for the controller too.
            if (jump && !wasJump) JumpTap = true;
            if (Pad)
            {
                if ((padDown & SkatePad.Y) != 0) ToggleTap = true;
                if ((padDown & SkatePad.RightStickClick) != 0) CameraTap = true;
                float x = SkatePad.LeftX, y = SkatePad.LeftY;
                if (x * x + y * y < SkateFlick.RearmZone * SkateFlick.RearmZone) spinBlocked = false;
                var straight = Math.Abs(x) < Straight;
                Analog = true;
                Steer = Math.Abs(x) <= SteerDead ? 0f : (x - Math.Sign(x) * SteerDead) / (1f - SteerDead);
                var triggers = (SkatePad.LeftTrigger > SkatePad.Pulled ? 1 : 0) | (SkatePad.RightTrigger > SkatePad.Pulled ? 2 : 0);
                triggerBlocked &= triggers;
                bool lt0 = (triggers & ~triggerBlocked & 1) != 0, rt0 = (triggers & ~triggerBlocked & 2) != 0;
                var groundChord = SkateRide.On && SkateRide.Mode == RideMode.Ground && ((SkatePad.Buttons & ~specialBumpers) & SkatePad.LeftBumper) != 0 && (lt0 || rt0);
                if (wasGroundChord && !groundChord)
                {
                    triggerBlocked |= triggers; lt0 = rt0 = false;
                    specialBumpers |= SkatePad.Buttons & SkatePad.LeftBumper;
                    SkatePad.BlockSpecialBumpers(SkatePad.LeftBumper);
                }
                wasGroundChord = groundChord;
                GroundGrab = SkateGroundGrabs.Select(groundChord, true, lt0, rt0, (SkatePad.Buttons & SkatePad.A) != 0, (SkatePad.Buttons & SkatePad.X) != 0);
                // A held forward stick must return before it pushes after a pump's release.
                if (!SkateRide.On || y <= StickPush) stickPushBlocked = false;
                else if (SkateRide.Mode == RideMode.Ground && SkateRide.Grounded && (lt0 || rt0)) stickPushBlocked = true;
                // A button held into compression must be released too; extending is not a new foot push.
                var pushButtons = SkatePad.Buttons & (SkatePad.A | SkatePad.X);
                pumpPushButtons &= pushButtons & ~padDown;
                if (!SkateRide.On) pumpPushButtons = 0;
                else if (SkateRide.Mode == RideMode.Ground && SkateRide.Grounded && (lt0 || rt0)) pumpPushButtons |= pushButtons;
                // Fresh A/X presses after extension still push; trigger + A/X plants below are unchanged.
                Push = !lt0 && !rt0 && ((pushButtons & ~pumpPushButtons) != 0 || (!stickPushBlocked && straight && y > StickPush));
                Mongo = (SkatePad.Buttons & SkatePad.X) != 0 && (SkatePad.Buttons & SkatePad.A) == 0;
                // Only while the right stick is held back for the pop: a push tapped on its own stays a push.
                if ((padDown & SkatePad.A) != 0 && SkatePad.Charging) aTappedAt = now;
                if (!groundChord && !SkateRide.Handplant && (lt0 || rt0) && (padDown & (SkatePad.A | SkatePad.X)) != 0 && SkateRide.Mode != RideMode.Air)
                {
                    JumpPressed = true; PopFlip = 0; PopShove = 0f; PopScale = 1f;
                    PopKind = (padDown & SkatePad.A) != 0 ? (lt0 ? SkatePopKind.Boneless : SkatePopKind.Beanplant) : SkatePopKind.Fastplant;
                    aTappedAt = -99f;
                }
                Brake = (SkatePad.Buttons & SkatePad.B) != 0 || (straight && y < -StickPush);
                // A side counts as held after a moment of standing still there: a circle of the
                // stick (a body varial) passes both sides without asking for a turn.
                var side = x < -SpinFrom ? -1 : x > SpinFrom ? 1 : 0;
                if (side != rawSide) { rawSide = side; sideSince = now; }
                // A circle completed between two frames (the reader replays every sample) is no
                // held side either: the body varial it made counts as the stick moving.
                if (SkatePad.BodyVarials != bodySeen) { bodySeen = SkatePad.BodyVarials; movedAt = now; }
                if (x * x + y * y > SpinFrom * SpinFrom)
                {
                    var angle = (float)(Math.Atan2(x, y) * 180.0 / Math.PI);
                    if (onRim && now - rimAt > 0.0005f && Math.Abs(SkateRide.Delta(rimAngle, angle)) / (now - rimAt) > RimMoving) movedAt = now;
                    rimAngle = angle; rimAt = now; onRim = true;
                }
                else onRim = false;
                var held = side != 0 && now - sideSince >= SpinHold && now - movedAt >= SpinHold;
                Left = held && side < 0; Right = held && side > 0;
                Spin = spinBlocked ? 0f : Left ? -1f : Right ? 1f : 0f;
                bool lt = lt0, rt = rt0;
                Crouch = lt || rt;
                var mirroredGrab = SkateRide.Goofy != (SkateRide.On && SkateRide.TailFirst);
                var airGrab = SkateRide.Mode == RideMode.Air;
                var grabButtons = SkateGrabCatalog.AirButtons(ref airGrabBlocked, airGrab, SkatePad.Buttons, padDown);
                var plantButtons = SkateGrabCatalog.AirButtons(ref plantButtonsBlocked, SkateRide.Handplant, SkatePad.Buttons, padDown);
                var basicGrab = GrabOf(lt, rt, SkatePad.RightX, SkatePad.RightY, mirroredGrab);
                Grab = airGrab ? SkateGrabCatalog.Select(lt, rt, SkatePad.RightX, SkatePad.RightY, mirroredGrab, grabButtons, basicGrab) : basicGrab;
                specialBumpers &= SkatePad.Buttons;
                if (SkateRide.BoardPath.Active || SkateRide.FootPlanted || SkateRide.GroundGrab.Active) Grab = SkateGrabKind.None;
                HandplantFront = ((SkatePad.Buttons & ~specialBumpers) & SkatePad.LeftBumper) != 0 && !SkateRide.BoardPath.Active && !(airGrab && Grab != SkateGrabKind.None);
                HandplantBack = ((SkatePad.Buttons & ~specialBumpers) & SkatePad.RightBumper) != 0 && !SkateRide.BoardPath.Active && !(airGrab && Grab != SkateGrabKind.None);
                Manual = SkatePad.Manual; NoseManual = SkatePad.NoseManual; Jump = false;
                Charging = SkatePad.Charging; Charge = SkatePad.Charge;
                BodyVarial += SkatePad.TakeBodyVarial();
                SkatePop pop;
                while (SkatePad.TakePop(out pop))
                {
                    var flips = (int)Math.Round(pop.FlipTurns);
                    if (pop.Kind == SkatePopKind.None) { LateFlip += flips; LateShove += (float)pop.ShoveDegrees; }
                    else
                    {
                        JumpPressed = true; PopSpecial = pop.Special; PopSpecialDirection = pop.SpecialDirection;
                        PopKind = pop.Special != SkateBoardTrickKind.None ? pop.Kind : PopWith(pop.Kind, now - aTappedAt < NoComplyTap && (SkatePad.Buttons & SkatePad.A) == 0, false);
                        if (pop.Special != SkateBoardTrickKind.None)
                        { specialBumpers |= SkatePad.Buttons & (SkatePad.LeftBumper | SkatePad.RightBumper); HandplantFront = HandplantBack = false; }
                        aTappedAt = -99f; PopFlip = flips; PopShove = (float)pop.ShoveDegrees; PopScale = LeastPop + (1f - LeastPop) * (float)pop.Strength;
                    }
                }
                HeldFlip += SkatePad.TakeHeldFlip();
                if (SkateRide.Handplant && (lt || rt))
                {
                    HandplantVariant = SkateHandplants.Select(SkateRide.GrindFrontside, lt, rt, mirroredGrab ? -SkatePad.RightX : SkatePad.RightX,
                        SkatePad.RightY, (plantButtons & SkatePad.A) != 0, (plantButtons & SkatePad.X) != 0);
                    Push = Crouch = Charging = Manual = NoseManual = false; Steer = Spin = Charge = 0;
                    Grab = SkateGrabKind.None; EndStep();
                }
                if (groundChord)
                {
                    Push = Crouch = Charging = Manual = NoseManual = HandplantFront = HandplantBack = false;
                    Charge = 0; Grab = SkateGrabKind.None; EndStep();
                }
            }
            else
            {
                GroundGrab = SkateGroundGrabKind.None; wasGroundChord = false;
                stickPushBlocked = false; pumpPushButtons = 0; airGrabBlocked = SkateGrabCatalog.ModifierButtons;
                SkatePad.SuspendGestures();
                Analog = Charging = false; Charge = 0f; NoseManual = false;
                Steer = (left ? -1f : 0f) + (right ? 1f : 0f);
                Spin = Steer;
                if (keyboardSuspended) wasSpin = Spin;
                if (push && !wasPush) LateFlip++;
                if (brake && !wasBrake) LateFlip--;
                // X tapped in the air: a body varial, the rider's half round without the board.
                if (body && !wasBody) BodyVarial += 180f;
                wasBody = body;
                Mongo = HandplantFront = HandplantBack = false;
                if (jump && !wasJump) { JumpPressed = true; PopKind = PopWith(SkatePopKind.Ollie, noComply, boneless); PopFlip = 0; PopShove = 0f; PopScale = 1f; }
                if (crouch && !wasCrouch) CrouchTap = true;
                Push = push; Brake = brake; Left = left; Right = right; Jump = jump; Crouch = crouch; Manual = manual;
                Grab = crouch ? SkateGrabKind.Legacy : SkateGrabKind.None;
            }
            if (Spin != 0f && Spin != wasSpin) SpinPressed = true;
            wasSpin = Spin;
            wasPush = push; wasBrake = brake; wasLeft = left; wasRight = right; wasJump = jump; wasCrouch = crouch; wasToggle = toggle; wasCamera = camera;
            keyboardSuspended = false; ControlsActive = true;
        }
        catch (Exception e) { failed = true; Reset(); Out.Say("INPUT key read threw " + e.GetType().Name + ": " + e.Message); }
    }

    // A no comply or a boneless takes the place of an ollie; a nollie stays a nollie.
    public static SkatePopKind PopWith(SkatePopKind kind, bool noComply, bool boneless)
    {
        if (kind != SkatePopKind.Ollie) return kind;
        return noComply ? SkatePopKind.NoComply : boneless ? SkatePopKind.Boneless : kind;
    }

    public static void EndStep() { JumpPressed = SpinPressed = false; PopFlip = LateFlip = HeldFlip = PopSpecialDirection = 0; PopSpecial = SkateBoardTrickKind.None; PopShove = LateShove = BodyVarial = 0f; }

    // Which grab the triggers and the right stick ask for. The triggers alone give the three plain
    // grabs; with the stick held up, down or to a side they give the others. The stick's sides are
    // the rider's own: mirrored when he rides tail first.
    public static SkateGrabKind GrabOf(bool lt, bool rt, float x, float y, bool mirrored)
    {
        if (!lt && !rt) return SkateGrabKind.None;
        if (mirrored) x = -x;
        float ax = Math.Abs(x), ay = Math.Abs(y);
        var up = y > GrabStick && ay >= ax; var down = y < -GrabStick && ay >= ax;
        var left = x < -GrabStick && ax > ay; var right = x > GrabStick && ax > ay;
        if (lt && rt) return up ? SkateGrabKind.Nosebone : down ? SkateGrabKind.Tailbone : SkateGrabKind.Double;
        if (rt) return up ? SkateGrabKind.Seatbelt : down ? SkateGrabKind.Nose : left ? SkateGrabKind.Melon : right ? SkateGrabKind.Method : SkateGrabKind.Backside;
        return up ? SkateGrabKind.Tail : down ? SkateGrabKind.Crail : right ? SkateGrabKind.Stalefish : SkateGrabKind.Frontside;
    }

    // A tap can begin and end between two frames; the key's state alone never shows such a tap.
    public static bool Down(KeyControl key)
    {
        if (key.isPressed) return true;
        if (!taps) return false;
        try { return key.wasPressedThisFrame; }
        catch (Exception e) { taps = false; Out.Say("INPUT taps between frames cannot be seen: " + e.GetType().Name + ": " + e.Message); return false; }
    }

    private static bool CursorFree()
    {
        if (!cursorKnown) return false;
        try { return Cursor.lockState != CursorLockMode.Locked; }
        catch (Exception e) { cursorKnown = false; Out.Say("INPUT cursor state unavailable: " + e.GetType().Name + ": " + e.Message); return false; }
    }
}
