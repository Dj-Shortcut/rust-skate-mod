using System;
using System.Linq;
using System.Reflection;
using Shortcut.RustMod;
using UnityEngine;
using UnityEngine.InputSystem;

// Extends owner-approved skate_park_build_items / skate_park_gesture_selection.
// Executes production input, receipt and inventory-reader code. Engine focus,
// Planner/cursor calls and network delivery are controlled observations, not a game.
internal static class SkateBuildRouteScenarios
{
    private static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly string[] Slugs = { "bank", "kicker", "quarterpipe", "ledge-low", "ledge-high", "long-ledge", "funbox", "halfpipe", "mini-quarterpipe", "large-quarterpipe", "half-wall-kicker", "mega-halfpipe" };
    private static Keyboard keyboard;
    private static int checks;
    private static object Get(string name) { return typeof(SkatePieces).GetField(name, Flags).GetValue(null); }
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
    private static int Frame(int down = 0, bool blocked = false)
    { Time.realtimeSinceStartup += .05f; return SkatePieces.Input(down, Time.realtimeSinceStartup, keyboard, null, blocked); }
    private static void Reply(string text)
    { Deliver("[SKATE-BUILDER] " + text); }
    private static void Deliver(string text)
    { Application.Emit(text); }
    private static Item ParkItem(int kind, int slot)
    { return new Item { name = SkateParkBuildItems.Name((SkateParkKind)kind), position = slot, instanceData = new ItemData { dataInt = 0x534B5001 + kind } }; }
    private static void Setup()
    {
        SkateConsole.SendOk = true; SkatePieces.Reset(); SkateConsole.Commands.Clear();
        SkateHeld.Planner = SkateHeld.AllowClose = SkateHeld.Closed = SkateHeld.AllowCursor = true;
        SkateHeld.Waiting = false; SkateHeld.Acquires = SkateHeld.Resumes = 0;
        SkateBuildWheel.Available = true;
        SkateHeld.Plan = new object();
        Application.isFocused = SkateRig.Ready = SkatePad.Shared = true; SkateRide.On = false;
        SkateRig.Local = new BasePlayer(); SkateRig.Body = new object(); SkateRig.AwakeAt = 1;
        SkatePad.Buttons = 0; SkatePad.RightTrigger = SkatePad.RightX = SkatePad.RightY = 0;
        Time.realtimeSinceStartup = 100; keyboard = new Keyboard(); Mouse.current = null;
        SkatePieces.Chosen = 11; Frame();
    }
    private static string Select(int kind, bool acknowledge = true)
    {
        Setup(); SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back);
        Check(SkatePieces.Open && SkatePieces.OwnsCursor, "Skate cursor acquired");
        for (int i = 0; i <= kind; i++) { Frame(SkatePad.DpadRight); Frame(); }
        Check(SkatePieces.Chosen == kind && SkatePieces.Hover == kind, "D-pad selection " + Slugs[kind]);
        SkatePad.Buttons = 0; Frame();
        string token = (string)Get("selectionToken");
        Check(token.Length == 32 && SkateConsole.Commands.Last() == "skate.builder " + Slugs[kind] + " " + token, "canonical select request");
        Check(!SkatePieces.Open && !SkatePieces.OwnsCursor && SkatePieces.BuilderKind == -1, "selection awaits receipt and releases cursor");
        if (acknowledge) Reply("selected " + Slugs[kind] + " [selection=" + token + "]");
        Check(SkatePieces.BuilderKind == (acknowledge ? kind : -1), "matching selection receipt");
        SkateConsole.Commands.Clear(); return token;
    }
    private static void TwelveInventoryAndPlacementRoutes()
    {
        Check(SkatePieces.Names.SequenceEqual(Slugs) && SkateParkBuildItems.PieceCount == 12, "independent twelve-kind contract");
        for (int kind = 0; kind < 12; kind++)
        {
            for (int slot = 0; slot < 6; slot++)
            {
                Setup(); Item item = ParkItem(kind, slot); SkateRig.Local.Current = item;
                SkateRig.Local.inventory.containerBelt.itemList.Add(item);
                Check(SkateHeld.Read(SkateRig.Local) && SkateHeld.HeldKind() == kind, "actual held identity");
                Check(SkateHeld.FindInBelt(SkateRig.Local, kind) == slot, "all twelve in each belt slot");
                SkatePieces.Frame(Time.realtimeSinceStartup); Check(SkatePieces.HeldKind == kind, "inventory preview route");
                Check(Frame(SkatePad.A) == SkatePad.A && SkateConsole.Commands.Count == 0 && SkatePieces.BuilderKind == -1,
                    "marked inventory route passes native input without selecting a paid builder");
                item.amount = 2; SkatePieces.Frame(Time.realtimeSinceStartup);
                Check(SkatePieces.HeldKind == -1 && SkatePieces.GhostKind == -1, "invalid identity clears preview");
            }
            Select(kind); Frame(SkatePad.RightBumper); SkatePad.RightTrigger = 1; Frame();
            string token = (string)Get("placementToken");
            Check(SkateConsole.Commands.SequenceEqual(new[] { "skate.builder place 1 " + token }), "quarter-turn placement command");
            Frame(); Check(SkateConsole.Commands.Count == 1, "held trigger cannot repeat spend");
            Reply("placed " + Slugs[kind] + " 10 Wood [request=wrong]");
            Check((string)Get("placementToken") == token, "foreign placement receipt ignored");
            int before = SkatePieces.Placed;
            Reply("placed " + Slugs[kind] + " 10 Wood [request=" + token + "]");
            Reply("placed " + Slugs[kind] + " 10 Wood [request=" + token + "]");
            Check(SkatePieces.Placed == before + 1 && (string)Get("placementToken") == "", "one matching completion only");
        }
    }
    private static int Cancel(string key)
    {
        keyboard.escapeKey.wasPressedThisFrame = key == "Escape";
        keyboard.f1Key.wasPressedThisFrame = key == "F1";
        keyboard.tabKey.wasPressedThisFrame = key == "Tab";
        return Frame(key == "B" ? SkatePad.B : 0);
    }
    private static void CancellationAndPayment()
    {
        foreach (string key in new[] { "B", "Escape", "F1", "Tab" })
        for (int kind = 0; kind < 12; kind++)
        {
            string token = Select(kind); SkatePad.RightTrigger = 1;
            Check(Cancel(key) == 0 && SkatePieces.BlocksControls, "cancel consumes simultaneous place");
            Check(SkateConsole.Commands.SequenceEqual(new[] { "skate.builder off " + token }), "cancel before placement");
            Check(SkateHeld.Resumes == 0, "no planner handback before off acknowledgement");
            Reply("off [selection=wrong]"); Check((bool)Get("offPending"), "wrong off receipt ignored");
            Reply("off [selection=" + token + "]"); Check(!(bool)Get("offPending") && SkateHeld.Resumes == 1, "acknowledged handback");
        }
        string selected = Select(0); SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back);
        SkateConsole.SendOk = false; SkatePad.RightTrigger = 1; Cancel("B");
        Check(SkateConsole.Commands.SequenceEqual(new[] { "skate.builder off " + selected }) && (bool)Get("builderRequested"), "failed off-send keeps ownership and suppresses place");
        Select(3); SkatePad.RightTrigger = 1; Frame(); string placement = (string)Get("placementToken");
        Reply("payment-review operator required [request=" + placement + "]");
        SkateConsole.Commands.Clear(); SkatePad.RightTrigger = 0; Frame(); Cancel("B");
        Check((bool)Get("placementUnknown") && (string)Get("placementToken") == placement, "cancel retains unresolved payment");
        SkatePieces.Place(Time.realtimeSinceStartup, false);
        Check(!SkateConsole.Commands.Any(c => c.StartsWith("skate.builder place ", StringComparison.Ordinal)), "unresolved payment cannot retry");
        Select(1); SkatePad.RightTrigger = 1; Frame();
        Time.realtimeSinceStartup += 6; SkatePieces.Frame(Time.realtimeSinceStartup);
        Check((bool)Get("placementUnknown"), "placement timeout means uncertainty");
        SkatePad.RightTrigger = 0; Frame(); SkatePad.RightTrigger = 1; Frame();
        Check(SkateConsole.Commands.Count == 1, "placement timeout is not permission for a second debit");
    }
    private static void NativeModeRoundTripAndStaleReplies()
    {
        string token = Select(11); SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back); Frame(SkatePad.X);
        Check(SkatePieces.Open && !SkatePieces.RustBuilding && (bool)Get("offPending"), "switch waits for server off");
        Reply("off [selection=" + token + "]");
        Check(SkatePieces.RustBuilding && !SkatePieces.Open && !SkatePieces.OwnsCursor && SkateHeld.Resumes == 1, "Rust mode owns native planner");
        SkateConsole.Commands.Clear(); Frame(SkatePad.B);
        Check(SkatePieces.RustBuilding && SkateConsole.Commands.Count == 0, "ordinary Rust input is not a Skate cancel");
        SkateHeld.AllowClose = false; SkateHeld.Waiting = true; SkateHeld.Closed = false;
        Frame(SkatePad.X, true); int acquired = SkateHeld.Acquires;
        Check(!SkatePieces.Open, "wait for owned native radial closure");
        SkateHeld.Closed = true; Frame();
        Check(SkatePieces.Open && SkatePieces.OwnsCursor && !SkatePieces.RustBuilding && SkateHeld.Acquires == acquired + 1, "Skate reacquires only after native radial closes");
        token = Select(2, false); SkateRig.Body = new object(); Reply("selected quarterpipe [selection=" + token + "]");
        Check(SkatePieces.BuilderKind == -1, "old body receipt ignored");
        token = Select(4, false); SkateRig.AwakeAt++;
        Reply("selected ledge-high [selection=" + token + "]"); Check(SkatePieces.BuilderKind == -1, "old life receipt ignored");
        token = Select(8, false); Time.realtimeSinceStartup += 6; SkatePieces.Frame(Time.realtimeSinceStartup);
        Reply("selected mini-quarterpipe [selection=" + token + "]");
        Check(SkatePieces.BuilderKind == -1 && (bool)Get("offPending"), "expired selection cannot reactivate");
    }
    private static void ServerObservedInventoryIsPreviewOnly()
    {
        for (int kind = 0; kind < 12; kind++)
        {
            Setup(); SkateRig.Local.ActiveReadFails = true; SkatePieces.Frame(Time.realtimeSinceStartup);
            Check(SkateConsole.Commands.Count == 1 && SkateConsole.Commands[0].StartsWith("skate.held ", StringComparison.Ordinal), "unavailable reader asks for observation only");
            string nonce = SkateConsole.Commands[0].Substring(11);
            Deliver("[SKATE-HELD] 1 " + new string('0', 32) + " item " + kind);
            SkatePieces.Frame(Time.realtimeSinceStartup);
            Check(SkatePieces.HeldKind == -1, "unrelated observation nonce refused");
            Deliver("[SKATE-HELD] 1 " + nonce + " item " + kind); SkatePieces.Frame(Time.realtimeSinceStartup);
            Check(SkatePieces.HeldKind == kind && SkatePieces.BuilderKind == -1 && !(bool)Get("builderRequested"), "observation never arms paid builder");
            Check(SkateConsole.Commands.Count == 1, "observation creates no placement or payment request");
            Deliver("[SKATE-HELD] 1 " + nonce + " clear"); SkatePieces.Frame(Time.realtimeSinceStartup);
            Check(SkatePieces.HeldKind == -1, "server invalidation clears observed preview");
            Time.realtimeSinceStartup += 1; SkatePieces.Frame(Time.realtimeSinceStartup);
            Deliver("[SKATE-HELD] 1 " + nonce + " item " + kind); SkatePieces.Frame(Time.realtimeSinceStartup);
            Check(SkatePieces.HeldKind == -1, "old observation cannot revive expired preview");
        }
        Setup(); SkateHeld.Planner = false; SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back);
        Check(!SkatePieces.Open && !SkatePieces.OwnsCursor && SkateConsole.Commands.Count == 0, "no plan cannot acquire build cursor");
        Setup(); SkateRide.On = true; SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back);
        Check(!SkatePieces.Open && SkateConsole.Commands.Count == 0, "riding cannot open build wheel");
    }
    private static void KeyboardModeRoundTrip()
    {
        Setup(); SkatePad.Shared = false; keyboard.pKey.isPressed = true; Frame();
        Check(SkatePieces.Open && SkatePieces.OwnsCursor, "keyboard P opens Skate wheel");
        keyboard.pKey.isPressed = false; keyboard.rightArrowKey.wasPressedThisFrame = true; Frame();
        keyboard.rightArrowKey.wasPressedThisFrame = false; keyboard.enterKey.wasPressedThisFrame = true; Frame();
        keyboard.enterKey.wasPressedThisFrame = false; string token = (string)Get("selectionToken");
        Check(SkateConsole.Commands.Last() == "skate.builder bank " + token, "keyboard Enter selects independent canonical kind");
        Reply("selected bank [selection=" + token + "]");
        keyboard.pKey.isPressed = true; Frame(); keyboard.pKey.isPressed = false; Frame();
        keyboard.leftShiftKey.isPressed = keyboard.pKey.isPressed = true; Frame();
        Check((bool)Get("offPending") && !SkatePieces.RustBuilding, "keyboard switch waits for off receipt");
        Reply("off [selection=" + token + "]"); Check(SkatePieces.RustBuilding && !SkatePieces.Open, "keyboard enters Rust building");
        keyboard.pKey.isPressed = false; Frame(); keyboard.pKey.isPressed = true; Frame();
        Check(SkatePieces.Open && !SkatePieces.RustBuilding && SkatePieces.OwnsCursor, "Shift+P returns to Skate building");
        Setup(); SkateBuildWheel.Available = false; SkatePad.Buttons = SkatePad.Back; Frame(SkatePad.Back);
        Check(!SkatePieces.Open && SkateHeld.Acquires == 0 && SkateConsole.Commands.Count == 0, "unavailable renderer cannot take cursor");
        token = Select(6); SkateHeld.Planner = false; Frame();
        Check((bool)Get("offPending") && SkateConsole.Commands.SequenceEqual(new[] { "skate.builder off " + token }), "plan loss requests acknowledged off");
    }
    public static int Main()
    {
        try
        {
            TwelveInventoryAndPlacementRoutes(); CancellationAndPayment(); NativeModeRoundTripAndStaleReplies(); ServerObservedInventoryIsPreviewOnly(); KeyboardModeRoundTrip();
            Console.WriteLine("PASS skate_park_build_items / skate_park_gesture_selection: " + checks + " actual-source route assertions.");
            Console.WriteLine("Controlled engine, D-pad, Planner/cursor and transport observations; no native UI/raycast/placement/debit/refund acceptance.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
