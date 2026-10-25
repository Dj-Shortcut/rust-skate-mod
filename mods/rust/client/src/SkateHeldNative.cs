using System;
using System.Reflection;

public static partial class SkateHeld
{
    private static Planner pausedPlanner;
    private static bool plannerWasEnabled;
    private static object plannerPlayer;
    private static object plannerBody;
    private static float plannerAwakeAt;
    // Wheel/builder previews are a separate owner from the holstered riding models.
    private static readonly System.Collections.Generic.List<UnityEngine.GameObject> plannerGuides = new System.Collections.Generic.List<UnityEngine.GameObject>();
    public static bool PausePlanner(object player, out string why)
    {
        why = "";
        if (PausedPlanner(player) != null)
        { Guides(pausedPlanner, false, plannerGuides, true); return true; }
        if (pausedPlanner != null) ResumePlanner();
        var root = player as UnityEngine.Component;
        if (root == null) { why = "The local player is unavailable."; return false; }
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        var planners = model.GetComponentsInChildren<Planner>(false);
        Planner found = null;
        for (int i = 0; i < planners.Length; i++)
        {
            Planner candidate = planners[i];
            if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
            var parent = candidate.transform.parent;
            if (parent == null || !parent.name.EndsWith("_prop", StringComparison.Ordinal)) continue;
            if (found != null) { why = "More than one active building plan was found."; return false; }
            found = candidate;
        }
        if (found == null) { why = "The active building plan is unavailable."; return false; }
        plannerWasEnabled = found.enabled;
        pausedPlanner = found;
        plannerPlayer = player;
        plannerBody = SkateRig.Body;
        plannerAwakeAt = SkateRig.AwakeAt;
        found.enabled = false;
        Guides(found, false, plannerGuides, true);
        Out.Say("PIECES ordinary building plan paused on local hand");
        return true;
    }

    public static object PausedPlanner(object player)
    {
        try
        {
            if (player == null || !ReferenceEquals(player, SkateRig.Local) || !ReferenceEquals(plannerPlayer, player) ||
                plannerBody == null || !ReferenceEquals(plannerBody, SkateRig.Body) || plannerAwakeAt <= 0f ||
                plannerAwakeAt != SkateRig.AwakeAt || pausedPlanner == null || pausedPlanner.enabled ||
                !pausedPlanner.gameObject.activeInHierarchy) return null;
            var root = player as UnityEngine.Component;
            var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
            var parent = pausedPlanner.transform.parent;
            return model != null && pausedPlanner.transform.IsChildOf(model.transform) && parent != null &&
                parent.name.EndsWith("_prop", StringComparison.Ordinal) ? pausedPlanner : null;
        }
        catch { return null; }
    }

    public static bool HasPlanner(object player)
    {
        if (WheelPlannerCurrent(player)) return true;
        if (pausedPlanner != null)
        {
            var parent = pausedPlanner.transform.parent;
            var pausedRoot = player as UnityEngine.Component;
            var pausedModel = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : pausedRoot;
            return ReferenceEquals(plannerPlayer, player) && pausedModel != null && pausedPlanner.gameObject.activeInHierarchy &&
                pausedPlanner.transform.IsChildOf(pausedModel.transform) && parent != null && parent.name.EndsWith("_prop", StringComparison.Ordinal);
        }
        var root = player as UnityEngine.Component;
        if (root == null) return false;
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        var planners = model.GetComponentsInChildren<Planner>(false);
        int found = 0;
        for (int i = 0; i < planners.Length; i++)
        {
            var p = planners[i];
            if (p == null || !p.enabled || !p.gameObject.activeInHierarchy) continue;
            var parent = p.transform.parent;
            if (parent != null && parent.name.EndsWith("_prop", StringComparison.Ordinal)) found++;
        }
        return found == 1;
    }

    private static Planner wheelPlanner;
    private static object wheelPlayer, wheelBody;
    private static float wheelAwakeAt;
    private static PieMenu closingWheel;
    private static object closingPlayer, closingBody;
    private static float closingAwakeAt;
    private static UnityEngine.GameObject skateCursorHost;
    private static NeedsCursor skateCursorRequest;
    private static Planner skateCursorPlanner;
    private static UnityEngine.Component skateCursorModel;
    private static object skateCursorPlayer, skateCursorBody;
    private static float skateCursorAwakeAt;
    private static string cursorReleaseNotice = "";
    public static string RustWheelGate { get; private set; } = "not-checked";
    private static string wheelBindingNotice = "";

    private static bool WheelPlannerCurrent(object player)
    {
        if (wheelPlanner == null || !ReferenceEquals(player, SkateRig.Local) || !ReferenceEquals(wheelPlayer, player) || !ReferenceEquals(wheelBody, SkateRig.Body) ||
            wheelAwakeAt != SkateRig.AwakeAt || !wheelPlanner.enabled || !wheelPlanner.gameObject.activeInHierarchy) return false;
        var root = player as UnityEngine.Component;
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        var parent = wheelPlanner.transform.parent;
        return model != null && wheelPlanner.transform.IsChildOf(model.transform) && parent != null && parent.name.EndsWith("_prop", StringComparison.Ordinal);
    }

    private static bool ConstructionWheel(object player, out PieMenu menu, out string why)
    {
        bool registryAbsent;
        return ConstructionWheel(player, out menu, out why, out registryAbsent);
    }

    private static bool ConstructionWheel(object player, out PieMenu menu, out string why, out bool registryAbsent)
    {
        menu = null; why = ""; registryAbsent = false;
        var root = player as UnityEngine.Component;
        if (root == null || !ReferenceEquals(player, SkateRig.Local)) { why = "The local player is unavailable."; return false; }
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        var plan = pausedPlanner;
        if (plan == null || !ReferenceEquals(plannerPlayer, player))
        {
            plan = wheelPlanner;
            if (!WheelPlannerCurrent(player))
            {
                plan = null;
                var planners = model.GetComponentsInChildren<Planner>(false);
                for (var i = 0; i < planners.Length; i++)
                {
                    var candidate = planners[i];
                    if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy) continue;
                    var parent = candidate.transform.parent;
                    if (parent == null || !parent.name.EndsWith("_prop", StringComparison.Ordinal)) continue;
                    if (plan != null) { why = "More than one active building plan was found."; return false; }
                    plan = candidate;
                }
            }
        }
        if (plan == null || !plan.gameObject.activeInHierarchy || !plan.transform.IsChildOf(model.transform) || plan.transform.parent == null ||
            !plan.transform.parent.name.EndsWith("_prop", StringComparison.Ordinal))
        { why = "Hold your ordinary building plan first."; return false; }
        wheelPlanner = plan; wheelPlayer = player; wheelBody = SkateRig.Body; wheelAwakeAt = SkateRig.AwakeAt;
        // This is the same construction-menu registry member that the native planner reads.
        // Its intact static-field getter and checked IL2CPP cast do not invoke an erased method.
        var native = PieMenu.ObjectNPublicAbstractSealedBoObBoObBoObBoObBoObUnique.field_Public_Static_Object_15;
        if (ReferenceEquals(native, null))
        { registryAbsent = true; why = "Rust's construction wheel is not available yet."; return false; }
        menu = native.TryCast<PieMenu>();
        if (menu == null || menu.escapeCapture == null)
        { why = "Rust's construction-wheel binding is unavailable."; return false; }
        return true;
    }

    public static bool RustWheelOpen(object player)
    {
        try
        {
            PieMenu menu; string why;
            return ConstructionWheel(player, out menu, out why) && menu.gameObject.activeInHierarchy && menu.escapeCapture.enabled;
        }
        catch (Exception) { return false; }
    }

    public static bool RustWheelHiddenForSkate(object player)
    {
        try
        {
            string ownerWhy;
            if (!SkateCursorCurrent(player, out ownerWhy)) { RustWheelGate = ownerWhy; return false; }
            PieMenu menu; string why; bool registryAbsent;
            if (ConstructionWheel(player, out menu, out why, out registryAbsent))
            {
                bool hidden = ConstructionWheelVisuallyHidden(menu);
                RustWheelGate = hidden ? "native-wheel-hidden" : "native-wheel-input-or-fade-active";
                return hidden;
            }
            // Rust may not have created its radial yet. Our own active native cursor request
            // is the lease; its next Update, not a one-time raw cursor setter, asks Rust for focus.
            RustWheelGate = registryAbsent ? "native-wheel-uncreated-owned-cursor" : "planner-or-native-binding-unavailable";
            return registryAbsent;
        }
        catch (Exception e) { RustWheelGate = "native-wheel-query-" + e.GetType().Name; return false; }
    }

    private static bool SkateCursorCurrent(object player, out string why)
    {
        why = "cursor-lease-not-owned";
        if (!UnityEngine.Application.isFocused || SkateRide.On || !SkateRig.Ready)
        { why = "cursor-lease-not-offboard-focused"; return false; }
        if (!ReferenceEquals(player, SkateRig.Local) || !ReferenceEquals(skateCursorPlayer, player) ||
            ReferenceEquals(SkateRig.Body, null) || !ReferenceEquals(skateCursorBody, SkateRig.Body) ||
            skateCursorAwakeAt <= 0f || skateCursorAwakeAt != SkateRig.AwakeAt)
        { why = "cursor-lease-life-changed"; return false; }
        if (skateCursorHost == null || skateCursorRequest == null || !skateCursorHost.activeInHierarchy || !skateCursorRequest.isActiveAndEnabled)
        { why = "cursor-lease-inactive"; return false; }
        if (pausedPlanner == null || !ReferenceEquals(skateCursorPlanner, pausedPlanner) || !ReferenceEquals(plannerPlayer, player) ||
            pausedPlanner.enabled || !pausedPlanner.gameObject.activeInHierarchy)
        { why = "cursor-lease-plan-changed"; return false; }
        var root = player as UnityEngine.Component;
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        if (model == null || !ReferenceEquals(skateCursorModel, model) || !pausedPlanner.transform.IsChildOf(model.transform))
        { why = "cursor-lease-model-changed"; return false; }
        why = "cursor-lease-current"; return true;
    }

    public static bool SkateCursorOwned(object player)
    {
        try { string why; return SkateCursorCurrent(player, out why); }
        catch (Exception) { return false; }
    }

    public static bool AcquireSkateCursor(object player, out string why)
    {
        why = "";
        try
        {
            var admittedPlanner = pausedPlanner;
            var admittedBody = SkateRig.Body;
            float admittedLife = SkateRig.AwakeAt;
            var root = player as UnityEngine.Component;
            var admittedModel = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
            // Acquire after PausePlanner: replacing an old paused plan cannot erase a new lease.
            if (!UnityEngine.Application.isFocused || SkateRide.On || !SkateRig.Ready || ReferenceEquals(SkateRig.Body, null) || SkateRig.AwakeAt <= 0f ||
                pausedPlanner == null || !ReferenceEquals(plannerPlayer, player) || pausedPlanner.enabled)
            { why = "cursor-acquire-needs-paused-plan"; ReleaseSkateCursor(); return false; }
            PieMenu menu; bool registryAbsent;
            bool bound = ConstructionWheel(player, out menu, out why, out registryAbsent);
            if ((!bound && !registryAbsent) || (bound && !ConstructionWheelHidden(menu)) ||
                (registryAbsent && (UnityEngine.Cursor.lockState != UnityEngine.CursorLockMode.Locked || UnityEngine.Cursor.visible)))
            { why = "cursor-acquire-native-screen-not-clear"; ReleaseSkateCursor(); return false; }
            if (skateCursorHost == null)
            {
                skateCursorHost = new UnityEngine.GameObject("Skate build cursor");
                skateCursorHost.SetActive(false);
                skateCursorRequest = skateCursorHost.AddComponent<NeedsCursor>();
            }
            if (skateCursorRequest == null) { why = "cursor-acquire-component-unavailable"; ReleaseSkateCursor(true); return false; }
            skateCursorPlanner = admittedPlanner; skateCursorModel = admittedModel; skateCursorPlayer = player;
            skateCursorBody = admittedBody; skateCursorAwakeAt = admittedLife;
            skateCursorRequest.enabled = true;
            skateCursorHost.SetActive(true);
            if (!SkateCursorCurrent(player, out why)) { ReleaseSkateCursor(); return false; }
            RustWheelGate = why; why = ""; return true;
        }
        catch (Exception e) { why = "cursor-acquire-" + e.GetType().Name; ReleaseSkateCursor(true); return false; }
    }

    public static void ReleaseSkateCursor(bool destroy = false)
    {
        skateCursorPlanner = null; skateCursorModel = null; skateCursorPlayer = skateCursorBody = null; skateCursorAwakeAt = 0;
        try { if (skateCursorHost != null) skateCursorHost.SetActive(false); }
        catch (Exception e)
        {
            if (cursorReleaseNotice != e.GetType().Name)
            { cursorReleaseNotice = e.GetType().Name; Out.Say("PIECES own cursor release unavailable: " + cursorReleaseNotice); }
        }
        try { if (skateCursorRequest != null) skateCursorRequest.enabled = false; }
        catch (Exception e)
        {
            if (cursorReleaseNotice != e.GetType().Name)
            { cursorReleaseNotice = e.GetType().Name; Out.Say("PIECES own cursor release unavailable: " + cursorReleaseNotice); }
        }
        if (!destroy) return;
        var host = skateCursorHost; skateCursorHost = null; skateCursorRequest = null;
        try { if (host != null) UnityEngine.Object.Destroy(host); }
        catch (Exception e) { Out.Say("PIECES own cursor cleanup unavailable: " + e.GetType().Name); }
    }

    private static bool ConstructionWheelVisuallyHidden(PieMenu menu)
    {
        if (!menu.gameObject.activeInHierarchy) return true;
        var needsCursor = menu.GetComponent<NeedsCursor>();
        var canvas = menu.GetComponent<UnityEngine.CanvasGroup>();
        if (needsCursor == null || canvas == null) return false;
        return !needsCursor.enabled && !menu.escapeCapture.enabled && canvas.alpha <= 0f;
    }

    private static bool ConstructionWheelHidden(PieMenu menu)
    { return ConstructionWheelVisuallyHidden(menu) && UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked; }

    public static bool RustWheelClosed(object player, out string why)
    {
        why = "";
        try
        {
            PieMenu menu;
            if (!ReferenceEquals(closingPlayer, player) || !ReferenceEquals(closingBody, SkateRig.Body) ||
                closingAwakeAt != SkateRig.AwakeAt || !ConstructionWheel(player, out menu, out why) || menu != closingWheel)
            { why = "The building plan or Rust wheel changed while switching."; return false; }
            return ConstructionWheelHidden(menu);
        }
        catch (Exception e) { why = "Rust construction-wheel access failed: " + e.GetType().Name; return false; }
    }

    public static bool CloseRustWheel(object player, bool nativeUiExpected, out bool waiting, out string why)
    {
        waiting = false; why = "";
        try
        {
            PieMenu menu; bool registryAbsent;
            if (ConstructionWheel(player, out menu, out why, out registryAbsent))
            {
                bool open = menu.gameObject.activeInHierarchy && menu.escapeCapture.enabled;
                if (nativeUiExpected && !open) why = "Close the other Rust screen before switching build modes.";
                else if (!open && ConstructionWheelHidden(menu)) { wheelBindingNotice = ""; return true; }
                else if (open)
                {
                    // Use the normal cancel route; its own cursor release and fade finish before
                    // the custom wheel may take focus. No native UI object is toggled or rebuilt.
                    closingWheel = menu; closingPlayer = player; closingBody = SkateRig.Body; closingAwakeAt = SkateRig.AwakeAt;
                    menu.Close(false);
                    if (ConstructionWheelHidden(menu)) { wheelBindingNotice = ""; return true; }
                    waiting = true; why = "Waiting for the Rust build wheel to close.";
                    return false;
                }
                else why = "Rust's build wheel is still closing, or another screen owns the cursor.";
            }
            else if (registryAbsent && !nativeUiExpected && UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked && !UnityEngine.Cursor.visible)
            {
                // No native radial exists to close. An owned plan with a clear, locked cursor
                // may open the custom wheel without manufacturing a native UI instance.
                wheelBindingNotice = ""; why = ""; return true;
            }
        }
        catch (Exception e) { why = "Rust construction-wheel access failed: " + e.GetType().Name; }
        if (why != wheelBindingNotice)
        { wheelBindingNotice = why; Out.Say("PIECES Rust-wheel switch refused: " + why); }
        return false;
    }

    public static void ResumePlanner()
    {
        ReleaseSkateCursor();
        var planner = pausedPlanner;
        pausedPlanner = null;
        plannerPlayer = null;
        plannerBody = null;
        plannerAwakeAt = 0f;
        for (var i = plannerGuides.Count - 1; i >= 0; i--)
        {
            try
            {
                var go = plannerGuides[i];
                if (go != null && !go.activeSelf) go.SetActive(true);
                plannerGuides.RemoveAt(i);
            }
            catch (Exception e) { Out.Say("PIECES building preview restore unavailable: " + e.GetType().Name); }
        }
        try { if (planner != null && plannerWasEnabled && !planner.enabled) planner.enabled = true; }
        catch (Exception e) { Out.Say("PIECES building plan restore unavailable: " + e.GetType().Name); }
        plannerWasEnabled = false;
    }

    // Resolve only the normal local routes while off the board. Do not select, hide or survey the scene.
    public static void Prepare(object player)
    {
        if (player == null) return;
        Read(player);
        if (!selectLooked) LookForBelt(player);
        var root = player as UnityEngine.Component;
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        if (model != null) model.GetComponentsInChildren<HeldEntity>(false);
    }

    // Puts away what the hand holds, as on a vehicle: nothing is built, swung or fired while riding.
    // The game's belt is asked to clear its selection (ClearSelection, else SelectSlot(-1)); the
    // route is logged once, and what still sits in the hand is logged each time it fails.
    private static bool clearLooked; private static MethodInfo clearSelection; private static string holsterRoute = "";
    // This client build has no GetActiveItem (the log: "HELD methods returning Item: (none)"), so
    // the held item cannot be read by name; then the belt's own selected slot says what is held.
    public static bool Holster(object player, out string how)
    {
        how = "";
        if (player == null) return false;
        var named = Read(player);
        if (named && Shortname == "" && Name == "") return false;
        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (!selectLooked) LookForBelt(player);
            if (selectSlot == null) return Hide(player, out how);
            var field = selectMember as FieldInfo;
            var owner = field != null ? field.GetValue(player) : ((PropertyInfo)selectMember).GetValue(player, null);
            if (owner == null) { how = selectOwner + " is empty"; return false; }
            if (!named)
            {
                var selected = SelectedSlot(owner);
                if (selected == NotReadable) { how = "neither the held item nor the belt's selected slot can be read"; return false; }
                if (selected < 0 || selected >= 6) return false;
                Slot = selected; Name = "belt slot " + (selected + 1);
            }
            if (!clearLooked)
            {
                clearLooked = true;
                clearSelection = owner.GetType().GetMethod("ClearSelection", flags, null, Type.EmptyTypes, null);
                holsterRoute = clearSelection != null ? selectOwner + ".ClearSelection()" : selectOwner + ".SelectSlot(-1)";
                Out.Say("HELD holster route: " + holsterRoute);
            }
            if (clearSelection != null) clearSelection.Invoke(owner, null);
            else selectSlot.Invoke(owner, SlotArgs(-1));
            how = holsterRoute + " put away " + (Name != "" ? Name : Shortname);
            return true;
        }
        catch (Exception e) { how = "holster threw " + e.GetType().Name + ": " + e.Message; return false; }
    }

    // Without a belt to clear (this build: no member of the player has a SelectSlot), the held
    // entity itself is switched off on this client while riding: its model, its view model and a
    // building plan's ghost go with it, and a switched-off plan takes no clicks. Each one is
    // switched on again on stepping off (Show). What the held entity offers is logged once a type.
    private static readonly System.Collections.Generic.List<UnityEngine.GameObject> hidden = new System.Collections.Generic.List<UnityEngine.GameObject>();
    private static readonly System.Collections.Generic.HashSet<string> probed = new System.Collections.Generic.HashSet<string>();
    private static bool Hide(object player, out string how)
    {
        how = "";
        var root = player as UnityEngine.Component;
        if (root == null) { how = "the player is not a component"; return false; }
        // This client's member names are obfuscated (its type names are kept), and nothing on the
        // player returns its held entity; so the held entity is found by its type among the
        // player's children. (The first-person view model is RiderRig's: it hides it from the
        // chase camera.)
        // The held entity sits on a hand bone (r_prop, l_prop) of the rider's model, which is not
        // under the player (the log: a building planner under r_prop in player_model.prefab);
        // what hangs on the back (a gun under spine3) is left alone.
        var model = RiderRig.Bound && RiderRig.Root != null ? (UnityEngine.Component)RiderRig.Root : root;
        var off = 0; var names = "";
        var heldOnes = model.GetComponentsInChildren<HeldEntity>(false);
        for (var i = 0; i < heldOnes.Length; i++)
        {
            var c = heldOnes[i];
            if (c == null) continue;
            var bone = c.transform.parent;
            if (bone == null || !bone.name.EndsWith("_prop", StringComparison.Ordinal)) continue;
            var go = c.gameObject;
            if (go == null || !go.activeSelf) continue;
            var kind = c.GetType().Name;
            var first = probed.Add(kind);
            if (first) Out.Say("HELD switching off a " + kind + " (" + go.name + ") while riding");
            go.SetActive(false);
            hidden.Add(go);
            names += " " + kind + " (" + go.name + ")" + Guides(c, first);
            off++;
        }
        // A scene-wide survey is diagnostic work, not part of every first boarding.
        if (off == 0) return false;
        how = "switched off" + names;
        return true;
    }

    private static bool Derives(Type t, string name)
    {
        for (; t != null && t != typeof(object); t = t.BaseType) if (t.Name == name) return true;
        return false;
    }

    // A building plan switched off on the hand bone still leaves its red preview standing in front
    // of the rider (the Shadow test of 18 October): that preview is not under the plan, or it would
    // have gone off with it. So what the plan refers to is read by shape, with its member names
    // obfuscated: every member whose type is a scene object (a GameObject, or a component) and
    // whose object is not under the plan. One named like a guide is switched off with the plan and
    // comes back with it (Show). The first plan of a type logs those bounded local references;
    // normal holstering does not survey every renderer in the scene.
    private static string Guides(UnityEngine.Component plan, bool log)
    { return Guides(plan, log, hidden, false); }

    private static string Guides(UnityEngine.Component plan, bool log, System.Collections.Generic.List<UnityEngine.GameObject> owned, bool plannerReferences)
    {
        var names = ""; var line = ""; var n = 0;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        try
        {
            for (var t = plan.GetType(); t != null && t != typeof(object) && n < 24; t = t.BaseType)
            {
                // Fields and properties of a drawn object's type only: a game property of another
                // type could do work in its getter, and nothing of the plan is to be set off here.
                var members = new System.Collections.Generic.List<MemberInfo>();
                foreach (var p in t.GetProperties(flags))
                    if (p.GetIndexParameters().Length == 0 && (Drawn(p.PropertyType) ||
                        (plannerReferences && t == typeof(Planner) && p.PropertyType == typeof(Il2CppSystem.Object) &&
                         t.GetField("NativeFieldInfoPtr_" + p.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) != null))) members.Add(p);
                foreach (var f in t.GetFields(flags)) if (Drawn(f.FieldType)) members.Add(f);
                foreach (var p in members)
                {
                    if (n >= 24) break;
                    object v;
                    try { v = p is PropertyInfo ? ((PropertyInfo)p).GetValue(plan, null) : ((FieldInfo)p).GetValue(plan); } catch (Exception) { continue; }
                    var o = v as UnityEngine.Object;
                    // This build erases Planner's scene references to Object. Only its field-backed
                    // getters are admitted above; TryCast checks the live IL2CPP class, not a guessed ABI.
                    if (plannerReferences && ReferenceEquals(o, null))
                    {
                        var native = v as Il2CppSystem.Object;
                        if (!ReferenceEquals(native, null))
                        { try { o = native.TryCast<UnityEngine.GameObject>(); if (o == null) o = native.TryCast<UnityEngine.Component>(); } catch (Exception) { continue; } }
                    }
                    if (ReferenceEquals(o, null) || o == null) continue;
                    var go = o as UnityEngine.GameObject;
                    var part = o as UnityEngine.Component;
                    if (ReferenceEquals(go, null) && !ReferenceEquals(part, null) && part != null) go = part.gameObject;
                    n++;
                    var typeName = p is PropertyInfo ? ((PropertyInfo)p).PropertyType.Name : ((FieldInfo)p).FieldType.Name;
                    if (ReferenceEquals(go, null) || go == null) { line += " " + p.Name + ":" + typeName + "=" + o.name; continue; }
                    var outside = !go.transform.IsChildOf(plan.transform);
                    line += " " + p.Name + ":" + typeName + "=" + go.name + (go.activeInHierarchy ? "" : ",off") + (outside ? ",outside" : "");
                    if (outside && (!plannerReferences || !plan.transform.IsChildOf(go.transform)) && go.activeSelf && GuideLike(go.name))
                    {
                        if (plannerReferences)
                        {
                            // Record before changing it, so even a throwing setter can be restored.
                            if (!owned.Contains(go)) owned.Add(go);
                        }
                        go.SetActive(false);
                        if (!plannerReferences) owned.Add(go);
                        names += " and its " + go.name;
                    }
                }
            }
            if (log) Out.Say("HELD the " + plan.GetType().Name + " refers to" + (line == "" ? " no scene object" : ":" + Short(line, 1200)));
        }
        catch (Exception e) { if (log) Out.Say("HELD reading the " + plan.GetType().Name + " threw: " + e.GetType().Name + ": " + e.Message); }
        return names;
    }

    private static bool Drawn(Type t)
    {
        return t == typeof(UnityEngine.GameObject) || typeof(UnityEngine.Transform).IsAssignableFrom(t) || typeof(UnityEngine.Renderer).IsAssignableFrom(t);
    }

    private static bool GuideLike(string name)
    {
        return name.IndexOf("guide", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("ghost", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // What is drawn within 40 m of the rider under a guide-like name: once, for the log.
    private static void NearbyGuides(UnityEngine.Vector3 at)
    {
        try
        {
            var all = UnityEngine.Object.FindObjectsOfType<UnityEngine.Renderer>();
            var line = ""; var n = 0;
            var seen = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < all.Length && n < 10; i++)
            {
                var r = all[i];
                if (ReferenceEquals(r, null) || r == null) continue;
                var go = r.gameObject;
                if (!GuideLike(go.name) && go.name.IndexOf("planner", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var d = (go.transform.position - at).magnitude;
                if (d > 40f) continue;
                var root = go.transform.root;
                var key = root.name + "/" + go.name;
                if (!seen.Add(key)) continue;
                var material = "";
                try { var m = r.sharedMaterial; if (!ReferenceEquals(m, null) && m != null) material = ", " + m.name; } catch (Exception) { }
                line += " " + go.name + (root == go.transform ? "" : " in " + root.name) + " " + d.ToString("F1") + " m" + (r.enabled ? "" : ", off") + material + ";";
                n++;
            }
            Out.Say("HELD drawn near the rider under a guide-like name (" + all.Length + " renderers looked at):" + (line == "" ? " (none)" : Short(line, 1200)));
        }
        catch (Exception e) { Out.Say("HELD looking for guides threw: " + e.GetType().Name + ": " + e.Message); }
    }

    // Where the belt could be when nothing on the player is belt-like (this build: member names
    // obfuscated, type names kept; the log said "belt-like members: (none)"): every loaded game
    // type named like a belt, with what it offers by shape, logged once. A static int can carry
    // the selected slot; a method taking one int can be the selection; a component's instances
    // in the scene are counted and the first one's ints read. Nothing is called.
    private static bool surveyed;
    public static void SurveyBelt()
    {
        if (surveyed) return;
        surveyed = true;
        try
        {
            var found = 0;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var an = a.GetName().Name;
                if (a == typeof(SkateHeld).Assembly || an.StartsWith("System", StringComparison.Ordinal) || an.StartsWith("mscorlib", StringComparison.Ordinal) || an.StartsWith("netstandard", StringComparison.Ordinal) ||
                    an.StartsWith("BepInEx", StringComparison.Ordinal) || an.StartsWith("Il2CppInterop", StringComparison.Ordinal) || an.StartsWith("Il2Cppmscorlib", StringComparison.Ordinal) ||
                    an.StartsWith("UnityEngine", StringComparison.Ordinal) || an.StartsWith("Microsoft", StringComparison.Ordinal) || an.StartsWith("Mono.", StringComparison.Ordinal)) continue;
                Type[] types;
                try { types = a.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; } catch (Exception) { continue; }
                foreach (var t in types)
                {
                    if (t == null || found >= 8 || t.Name.IndexOf("Belt", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    found++;
                    Out.Say("HELD belt survey " + found + ": " + Short(Shape(t, an), 1500));
                }
            }
            if (found == 0) Out.Say("HELD belt survey: no loaded game type is named like a belt");
        }
        catch (Exception e) { Out.Say("HELD belt survey threw: " + e.GetType().Name + ": " + e.Message); }
    }

    // One type's shape for the log: its base, its static ints with their values, its instance ints
    // (with the first scene instance's values when it is a component), and its methods taking
    // one int.
    private static string Shape(Type t, string assembly)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var s = t.FullName + " (" + assembly + ")" + (t.BaseType == null ? "" : " : " + t.BaseType.Name) + (t.IsAbstract ? " abstract" : "");
        object instance = null; var instances = -1;
        if (typeof(UnityEngine.Object).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters)
        {
            try
            {
                foreach (var m in typeof(UnityEngine.Object).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "FindObjectsOfType" || !m.IsGenericMethodDefinition || m.GetParameters().Length != 0) continue;
                    var found = m.MakeGenericMethod(t).Invoke(null, null) as System.Collections.IEnumerable;
                    if (found == null) continue;
                    instances = 0;
                    foreach (var o in found) { if (o != null && instance == null) instance = o; instances++; }
                    break;
                }
            }
            catch (Exception e) { s += " [instances: " + e.GetType().Name + "]"; }
            if (instances >= 0) s += " [" + instances + " in the scene]";
        }
        var statics = ""; var ints = ""; var methods = "";
        // The game's own types only, not the engine's bases under them.
        for (var at = t; at != null && at.Assembly == t.Assembly; at = at.BaseType)
        {
            foreach (var p in at.GetProperties(all))
            {
                if (p.GetIndexParameters().Length != 0 || p.PropertyType != typeof(int)) continue;
                var getter = p.GetGetMethod(true);
                if (getter == null) continue;
                if (getter.IsStatic) statics += " " + p.Name + "=" + Value(p, null);
                else ints += " " + p.Name + (instance == null ? "" : "=" + Value(p, instance));
            }
            foreach (var f in at.GetFields(all))
            {
                if (f.FieldType != typeof(int)) continue;
                if (f.IsStatic) { try { statics += " " + f.Name + "=" + f.GetValue(null); } catch (Exception) { statics += " " + f.Name + "=?"; } }
                else ints += " " + f.Name + (instance == null ? "" : "=" + Value(f, instance));
            }
            foreach (var m in at.GetMethods(all))
            {
                if (m.IsSpecialName) continue;
                var ps = m.GetParameters();
                if (ps.Length != 1 || ps[0].ParameterType != typeof(int)) continue;
                methods += " " + (m.IsStatic ? "static " : "") + m.ReturnType.Name + " " + m.Name + "(int)";
            }
        }
        return s + "; static ints:" + (statics == "" ? " (none)" : statics) + "; ints:" + (ints == "" ? " (none)" : ints) + "; methods taking one int:" + (methods == "" ? " (none)" : methods);
    }

    private static string Value(MemberInfo m, object o)
    {
        try
        {
            var p = m as PropertyInfo;
            var v = p != null ? p.GetValue(o, null) : ((FieldInfo)m).GetValue(o);
            return v == null ? "null" : v.ToString();
        }
        catch (Exception e) { return "?" + e.GetType().Name; }
    }

    private static string Short(string s, int most) { return s.Length > most ? s.Substring(0, most) + "…" : s; }

    // Stepping off: every held entity switched off for the ride is switched on again.
    public static int Show()
    {
        var n = 0;
        foreach (var go in hidden) if (go != null && !go.activeSelf) { go.SetActive(true); n++; }
        hidden.Clear();
        return n;
    }

    // The belt's selected slot (0..5, -1 for none) as the belt keeps it: a member named like
    // SelectedSlot, static or not; NotReadable when the belt has none. The route is logged once.
    public const int NotReadable = int.MinValue;
    private static bool slotLooked; private static MemberInfo slotMember;
    private static int SelectedSlot(object belt)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        if (!slotLooked)
        {
            slotLooked = true;
            var t = belt.GetType();
            foreach (var name in new[] { "SelectedSlot", "selectedSlot", "_selectedSlot", "currentSlot" })
            {
                var p = t.GetProperty(name, all);
                if (p != null && p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0) { slotMember = p; break; }
                var f = t.GetField(name, all);
                if (f != null && f.FieldType == typeof(int)) { slotMember = f; break; }
            }
            var ints = "";
            if (slotMember == null) foreach (var m in t.GetMembers(all))
                {
                    var pi = m as PropertyInfo; var fi = m as FieldInfo;
                    if ((pi != null && pi.PropertyType == typeof(int) && pi.GetIndexParameters().Length == 0) || (fi != null && fi.FieldType == typeof(int))) ints += " " + m.Name;
                }
            Out.Say("HELD belt selected slot: " + (slotMember != null ? t.Name + "." + slotMember.Name : "none found; int members of " + t.Name + ":" + (ints == "" ? " (none)" : ints)));
        }
        if (slotMember == null) return NotReadable;
        var target = belt;
        var sp = slotMember as PropertyInfo; var sf = slotMember as FieldInfo;
        if ((sp != null && sp.GetGetMethod(true).IsStatic) || (sf != null && sf.IsStatic)) target = null;
        var v = sp != null ? sp.GetValue(target, null) : sf.GetValue(target);
        return v is int ? (int)v : NotReadable;
    }

    // The building plan's own turn (R), in degrees about up: the held planner's rotationOffset, as
    // the native target carries it to the server; 0 when it cannot be read.
    public static float PlannerTurn(object player)
    {
        try
        {
            if (player == null) return 0f;
            var held = player.GetType().GetMethod("GetHeldEntity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            var planner = held == null ? null : held.Invoke(player, null);
            var offset = planner == null ? null : Get(planner, "rotationOffset");
            return offset is UnityEngine.Vector3 ? ((UnityEngine.Vector3)offset).y : 0f;
        }
        catch (Exception) { return 0f; }
    }
}
