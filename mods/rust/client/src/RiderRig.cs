// The pose must be written after the game's own animation (the late driver's LateUpdate): what is
// written there is what gets drawn. The game still adjusts the skeleton before the next frame
// starts, so nothing may be read back from the bones as if it were this module's pose.
// The rider module is given the plane the ankle joints rest on, not the deck: its ankles are the
// skeleton's ankle joints. Where the deck is below that plane goes in separately, for the grab.
using System;
using Shortcut.RustMod;
using UnityEngine;

public static class RiderRig
{
    private static float grabSince = -1f;
    private static float crouchSpeed;
    public const float CrouchSpring = 220f, CrouchDamp = 21f;
    private static float grabHold = -1f;
    // A view that holds the grab at a given moment of its folding in (the pose check).
    public static void GrabFor(float seconds) { grabHold = seconds; }
    // How far into the handplant the pose is (0..1): up over PlantSeconds when the edge is taken
    // with a hand, down again when the ride says the body is coming down; a scripted check can hold it.
    public const float PlantSeconds = 0.4f, PlantIn = 0.07f;
    private static float plantBlend, plantHold = -1f;
    public static float PlantBlend { get { return plantHold >= 0f ? plantHold : plantBlend; } }
    public static void PlantFor(float blend) { plantHold = blend; }
    // A landing is taken up over LandSettle seconds: the crouch's spring dips the legs, and the
    // module folds the chest forward and brings the arms out by what is left of it.
    public const float LandSettle = 0.45f;
    public const float AnkleHeight = 0.095f, ToeHeight = 0.025f, DeckTop = 0.092f, HeelShift = 0.055f;
    public const float HeadLiftFrom = 15f, HeadLiftShare = 1f, HeadLiftMost = 60f;
    public static bool Bound;
    public static Animator Anim;
    public static Transform Root, Pelvis, Neck, Head, LHip, LKnee, LFoot, LToe, RHip, RKnee, RFoot, RToe, LUpper, LFore, LHand, RUpper, RFore, RHand;
    public static bool Mirrored;
    private static float bindHandedness, bindDeterminant;
    private static void Swap(ref Transform a, ref Transform b) { var t = a; a = b; b = t; }
    public static SkateRiderRig Rig;
    public static float Thigh, Shin, Foot, UpperArm, Forearm, Crouch = 0.18f;
    private static int chargePosePop = -1;
    private static uint chargePoseEpoch;
    private static float chargePoseAmount;
    private static double? chargeInitialPelvisOffset;
    private static bool chargeOwnedGround, chargeSetupGrounded, chargeNosePop, chargeEntryShown;
    private static Transform chargeOwnerPlayer, chargeEntryPlayer;
    private static bool chargeOwnerGoofy, chargeEntryGoofy;
    private static SkateCaptureFrame? chargeEntry;
    private static SkateBoardPose chargeEntryCarrier;
    private static int chargeEntryPop = -1;
    private static uint chargeEntryEpoch;
    public static double ChargePoseSeed { get { return chargePosePop == SkateRide.Pops && chargePoseEpoch == SkateKeys.InputEpoch ? chargePoseAmount : 0; } }
    // True only for the measured setup presented by this current ground owner.
    public static bool GroundChargePresented { get { return chargeOwnedGround && chargeSetupGrounded &&
        SkateRide.Mode == RideMode.Ground && SkateKeys.ControlsActive && chargeOwnerPlayer == SkateRig.LocalT &&
        chargeOwnerGoofy == SkateRide.Goofy && chargePoseEpoch == SkateKeys.InputEpoch && chargePosePop == SkateRide.Pops + 1; } }
    public static float Miss;
    public static string MissAt = "", Error = "", MotionError = "";
    private static SkateRiderMotionState motion;
    private static bool airLookFrame;
    private static RideMode motionMode = RideMode.Ground;
    public static int Applied, Refusals, WithoutPush;
    // Where the head was left by the last pose, so the watch can tell a head moved after it.
    public static Vector3 HeadPosed;
    // The module's last pose as computed, and as shown after the secondary motion; for the checks.
    public static SkateRiderPose Posed, Shown; public static Vector3 PosedPlane;
    private static Transform[] spine = new Transform[0];
    private static Vector3 lSole, rSole;
    private struct GrabHandBasis
    {
        public bool Ready, Saved;
        public Vector3 Finger, Back;
        public SkateHandMeasurements Measurements;
        public Quaternion OriginalLocal;
    }
    private struct GrabFingerBone
    {
        public Transform Bone;
        public Vector3 Direction;
        public Quaternion EntryLocal;
        public float Length, Curl;
    }
    private sealed class GrabFingers
    {
        public bool Ready, Saved;
        public GrabFingerBone[] Bones;
        public Vector3 ThumbBase;
        public float Radial, Reach;
    }
    private static GrabFingers lGrabFingers, rGrabFingers;
    private static GrabHandBasis lGrabHand, rGrabHand;
    private static SkateBoardPose lGrabFrame, rGrabFrame;
    private static double lGrabWeight, rGrabWeight;
    private static float travelLookWeight;
    private static bool motionLookFrame;
    private static bool failed;
    private static SkateVector leftFootCalibration, rightFootCalibration;
    private static bool footCalibrated;
    private static int calibratedFootVersion = -1;
    private static string reportedFootStatus, reportedFootFit;
    public static string FootSupportError { get; private set; } = "";

    // The module answers every frame from that frame's state alone. Between two states (rolling, a
    // push, the air, a grab, a bail) the joints ease over, in the board's own frame so that a turning
    // or tipping board does not leave them behind.
    public const float EaseSeconds = 0.07f;
    private static bool pinLeft, pinRight, pinHips;
    private static SkateRiderContactState contact;
    private static SkateHandplantState plantStyle;
    private static int plantId = -1;
    public static SkateHandplantState HandplantStyle { get { return plantStyle; } }
    private static string contactError;
    public static SkateRiderContactState Contact { get { return contact; } }
    public static SkateGrabKind ShownGrab { get { return contact.Initialized
        ? contact.VisibleGrab : SkateRide.Grab; } }
    private static float chestLead;
    private const float SideBendShare = 0.6f;
    private const int PelvisJ = 0, LHipJ = 1, RHipJ = 2, LKneeJ = 3, RKneeJ = 4, LAnkleJ = 5, RAnkleJ = 6, LToeJ = 7, RToeJ = 8, ChestJ = 9, NeckJ = 10, HeadJ = 11,
        LShoulderJ = 12, RShoulderJ = 13, LElbowJ = 14, RElbowJ = 15, LHandJ = 16, RHandJ = 17, Points = 18,
        PelvisForwardJ = 18, PelvisUpJ = 19, ChestForwardJ = 20, ChestUpJ = 21, HeadForwardJ = 22, All = 23;
    private static readonly Vector3[] eased = new Vector3[All], joint = new Vector3[All];
    private static float easedAt = -99f;

    // The game draws the local player's model twice: the full body casts shadows only, and a second
    // set of skinned meshes named "leg-..." is what first person shows, with the upper body folded
    // out of view. A camera behind the rider needs the full body drawn and that set hidden; both
    // must be put back for first person.
    private static SkinnedMeshRenderer[] body = new SkinnedMeshRenderer[0], legSet = new SkinnedMeshRenderer[0];
    private static bool third;
    private static int lookFailures;
    private static long skinSet;
    private static float nextScan, lookRetryAt;
    private static bool lookPrepared;
    public const float ScanEvery = 5f;
    public static string LookState = "first person";

    // The item in the hands and the first-person arms are drawn right in front of the camera,
    // wherever it is: behind the rider they would cover him. The game makes a new set for every
    // item taken in hand, so the set is looked for again several times a second.
    // Switching the parts off is not enough in this client: with a rock in hand they stayed on the
    // screen. So the set is also made too small to see. The game may set the size again at any
    // time, so it is made small again every frame, and the size the game last gave is put back.
    // A look through the whole scene for held items costs the game a fifth of a second, so it is
    // done at most every HeldWhole seconds, and each time it finds nothing the next one waits twice
    // as long, up to HeldWholeMost (empty hands would otherwise cost a frame every two seconds);
    // in between the object that held the last item found (their parent) is looked through, which
    // is cheap, every HeldEvery seconds.
    public const float HeldEvery = 0.25f, HeldWhole = 2f, HeldWholeMost = 32f, HeldSmall = 0.0001f;
    private const int HeldTries = 5, HeldScans = 3;

    // One thing of the set: the object that carries it (Root) or one drawn part of it (Part). What
    // was changed on it stays noted until it has been put back, each thing on its own: one that
    // throws is tried again later and never leaves the others small or off. A thing of a set that
    // has been replaced is only ever put back (Old).
    private sealed class HeldThing
    {
        public Transform Root; public Renderer Part;
        public Vector3 Size; public bool Was, Changed, Forced, Old;
        public int Tries;
    }
    private static HeldThing[] held = new HeldThing[0];
    private static long heldSet;
    private static float nextHeld;
    private static bool heldForced = true, heldThird, heldOff, heldPrepared;
    private static long heldSlow;
    // A frame's worth of this is a frame lost: past this the handling is switched off for the session.
    private const double HeldBudgetMs = 8.0; private const int HeldSlowFrames = 30;
    private static Transform heldHolder;
    private static float nextWhole, wholeWait = HeldWhole;
    private static int heldScanFailures;

    private static void Held(bool wantThird, float now, bool preparing = false)
    {
        if (heldOff) return;
        // Empty hands are a riding rule in both cameras; full-body visibility still uses wantThird.
        var hideHeld = SkateRide.On || wantThird;
        var began = SkateClock.Now();
        var transition = hideHeld && !heldThird;
        var emptyTransition = transition && heldPrepared && !HeldPresent();
        // A prepared empty set needs a fresh local look, without a cold scene scan on boarding.
        if (transition)
        {
            if (!heldPrepared) { nextHeld = nextWhole = 0f; wholeWait = HeldWhole; }
            else if (emptyTransition) nextHeld = 0f;
            else { nextHeld = now + HeldEvery; nextWhole = Math.Max(nextWhole, now + wholeWait); }
        }
        heldThird = hideHeld;
        // A set that lost a thing is looked for again at once, but once: the lost thing is dropped
        // so that it cannot ask for a look every frame, which cost the owner his frame rate.
        if (hideHeld && HeldGone()) { nextHeld = nextWhole = 0f; wholeWait = HeldWhole; }
        if ((hideHeld || preparing) && now >= nextHeld && heldScanFailures < HeldScans)
        {
            nextHeld = now + HeldEvery;
            try
            {
                Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<BaseViewModel> models = null;
                if (!ReferenceEquals(heldHolder, null) && heldHolder != null) models = heldHolder.GetComponentsInChildren<BaseViewModel>(true);
                if ((models == null || models.Length == 0) && emptyTransition)
                {
                    var cam = Camera.main;
                    if (cam != null) models = cam.GetComponentsInChildren<BaseViewModel>(true);
                    if ((models == null || models.Length == 0) && SkateRig.LocalT != null)
                        models = SkateRig.LocalT.GetComponentsInChildren<BaseViewModel>(true);
                    if (models != null && models.Length > 0 && models[0] != null) heldHolder = models[0].transform.parent;
                }
                // The local anchors need not contain every view model; retain the existing fallback deadline.
                if ((models == null || models.Length == 0) && now >= nextWhole && !emptyTransition)
                {
                    nextWhole = now + wholeWait;
                    models = UnityEngine.Object.FindObjectsOfType<BaseViewModel>();
                    heldHolder = models.Length > 0 && models[0] != null ? models[0].transform.parent : null;
                    wholeWait = models.Length > 0 ? HeldWhole : Math.Min(wholeWait * 2f, HeldWholeMost);
                }
                if (models == null) return;
                var found = new System.Collections.Generic.List<HeldThing>(); long set = models.Length; var names = ""; var parts = 0;
                for (var i = 0; i < models.Length; i++)
                {
                    if (models[i] == null) continue;
                    found.Add(new HeldThing { Root = models[i].transform }); set = set * 31 + models[i].Pointer.ToInt64();
                    if (i < 3) names += (names == "" ? "" : ", ") + models[i].gameObject.name;
                    var drawn = models[i].GetComponentsInChildren<Renderer>(true);
                    for (var k = 0; k < drawn.Length; k++) { found.Add(new HeldThing { Part = drawn[k] }); set = set * 31 + drawn[k].Pointer.ToInt64(); parts++; }
                }
                if (set != heldSet)
                {
                    HeldRestore(false);
                    HeldKeep(found);
                    held = found.ToArray(); heldSet = set;
                    if (parts > 0 || names != "") Say("held item: " + names + ", " + parts + " parts out of the camera's way");
                }
                heldScanFailures = 0; heldPrepared = true;
            }
            catch (Exception e)
            {
                heldScanFailures++;
                Say("held item: looking for it threw " + e.GetType().Name + ": " + e.Message + (heldScanFailures >= HeldScans ? "; given up until the rider is found anew" : ""));
            }
        }
        if (hideHeld) HeldHide();
        HeldRestore(hideHeld);
        var ms = (SkateClock.Now() - began) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        heldSlow = ms > HeldBudgetMs ? heldSlow + 1 : 0;
        if (heldSlow >= HeldSlowFrames)
        {
            heldOff = true; HeldRestore(false);
            Say("held item: keeping it out of the way cost " + ms.ToString("F0") + " ms a frame for " + HeldSlowFrames + " frames; it is left alone from here on");
        }
    }

    private static bool HeldPresent()
    {
        for (var i = 0; i < held.Length; i++) if (!held[i].Old && held[i].Root != null) return true;
        return false;
    }

    // A held item that was swapped is destroyed with its parts: the set is looked for again then.
    private static bool HeldGone()
    {
        var gone = false;
        for (var i = 0; i < held.Length; i++)
        {
            var t = held[i];
            if (t.Old) continue;
            if (!ReferenceEquals(t.Root, null) && t.Root == null) { t.Old = true; t.Changed = false; gone = true; }
        }
        return gone;
    }

    // What could not be put back yet goes along with the next set, to be tried again.
    private static void HeldKeep(System.Collections.Generic.List<HeldThing> into)
    {
        for (var i = 0; i < held.Length; i++)
            if (held[i].Changed) { held[i].Old = true; into.Add(held[i]); }
    }

    private static void HeldHide()
    {
        for (var i = 0; i < held.Length; i++)
        {
            var t = held[i];
            if (t.Old || t.Tries >= HeldTries) continue;
            try
            {
                if (t.Root != null)
                {
                    var size = t.Root.localScale;
                    if (Math.Abs(size.x) > HeldSmall * 2f || Math.Abs(size.y) > HeldSmall * 2f || Math.Abs(size.z) > HeldSmall * 2f)
                    { t.Size = size; t.Changed = true; t.Root.localScale = Vector3.one * HeldSmall; }
                }
                else if (t.Part != null && !t.Changed)
                {
                    // Off without touching what the game itself switches on and off. A client build
                    // without that setter switches the part itself off and remembers how it was.
                    if (heldForced)
                    {
                        try
                        {
                            t.Was = t.Part.forceRenderingOff;
                            t.Forced = true; t.Changed = true;
                            t.Part.forceRenderingOff = true;
                            continue;
                        }
                        catch (Exception e)
                        {
                            heldForced = false;
                            Say("held item: no rendering switch in this client (" + e.GetType().Name + "); the parts are switched off instead");
                            // A native setter may throw after writing. Release that exact flag
                            // before taking an enabled-state lease; retain it for retries on failure.
                            if (t.Changed)
                            {
                                try { RestoreHeldPart(t); t.Changed = false; t.Tries = 0; }
                                catch (Exception restore) { t.Old = true; HeldTrouble(t, "putting it back", restore); continue; }
                            }
                        }
                    }
                    t.Was = t.Part.enabled; t.Forced = false; t.Changed = true; t.Part.enabled = false;
                }
            }
            catch (Exception e) { HeldTrouble(t, "taking it out of the way", e); }
        }
    }

    // oldOnly: while the view is behind the rider, only what is left of a replaced set is put back.
    private static void HeldRestore(bool oldOnly)
    {
        for (var i = 0; i < held.Length; i++)
        {
            var t = held[i];
            if (!t.Changed || (oldOnly && !t.Old)) continue;
            try
            {
                if (!ReferenceEquals(t.Root, null)) { if (t.Root != null) t.Root.localScale = t.Size; }
                else if (t.Part != null) RestoreHeldPart(t);
                t.Changed = false; t.Tries = 0;
            }
            catch (Exception e)
            {
                HeldTrouble(t, "putting it back", e);
                // Given up: an object the engine no longer lets anyone touch has nothing left to put back.
                if (t.Tries >= HeldTries) t.Changed = false;
            }
        }
    }

    private static void RestoreHeldPart(HeldThing thing)
    {
        // Restore only the value this lease changed. A later owner that already changed the
        // renderer away from our hidden state keeps its value; pre-hidden parts stay hidden.
        if (thing.Forced)
        {
            if (thing.Part.forceRenderingOff && !thing.Was)
            {
                thing.Part.forceRenderingOff = false;
                if (thing.Part.forceRenderingOff) throw new InvalidOperationException("held rendering flag restoration did not verify");
            }
        }
        else if (!thing.Part.enabled && thing.Was)
        {
            thing.Part.enabled = true;
            if (!thing.Part.enabled) throw new InvalidOperationException("held renderer restoration did not verify");
        }
    }

    private static void HeldTrouble(HeldThing t, string doing, Exception e)
    {
        t.Tries++;
        if (t.Tries == 1 || t.Tries == HeldTries)
            Say("held item: " + doing + " threw " + e.GetType().Name + ": " + e.Message + (t.Tries >= HeldTries ? "; left alone from here on" : "; tried again"));
    }

    // For the scripted pose check: what is drawn within arm's length of the camera, the way to find
    // out what a client build puts in front of a camera that has left the player's eyes.
    private static string nearSaid = "";

    public static void Near()
    {
        try
        {
            var cam = Camera.main;
            if (cam == null) return;
            var at = cam.transform.position;
            var all = UnityEngine.Object.FindObjectsOfType<Renderer>();
            var text = ""; var n = 0;
            for (var i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (r == null || !r.enabled) continue;
                var b = r.bounds;
                if ((b.center - at).sqrMagnitude > 1.44f || b.extents.sqrMagnitude > 9f) continue;
                n++;
                if (n <= 10) text += " | " + r.gameObject.name + " layer=" + r.gameObject.layer + " size=" + r.transform.lossyScale.x.ToString("F4") + " seen=" + r.isVisible + " under " + Out.Chain(r.transform);
            }
            var line = n + " drawn parts within 1.2 m of the camera" + text;
            if (line == nearSaid) return;
            nearSaid = line; Say("near: " + line);
        }
        catch (Exception e) { Say("near threw " + e.GetType().Name + ": " + e.Message); }
    }

    // Read/cache presentation inputs before boarding, without hiding a hand or changing camera mode.
    public static void PrepareLook(bool heldViews)
    {
        if (heldViews) Held(false, Time.realtimeSinceStartup, true);
        else { lookPrepared = true; Look(false, true); }
    }

    public static void Look(bool wantThird) { Look(wantThird, false); }

    private static void Look(bool wantThird, bool preparing)
    {
        var now = Time.realtimeSinceStartup;
        var began = SkateClock.Now(); Held(wantThird, now); SkateClock.Add(SkateClock.Held, began);
        if ((!lookPrepared && !SkateRide.On) || !Bound || Root == null || lookFailures >= 3 || now < lookRetryAt) return;
        try
        {
            if (preparing || (SkateRide.On && now >= nextScan) || SkinGone())
            {
                // Clothing changes replace the meshes, also one for one, so the set is compared by
                // identity now and then. Walking the whole model costs a visible hitch, so it is done
                // periodically only on board, during preparation, or when a known mesh is destroyed. The old set goes
                // back to the game's own state before the new one is sorted: a mesh is recognised as
                // body by that state.
                nextScan = now + ScanEvery;
                var all = Root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                long set = all.Length;
                for (var i = 0; i < all.Length; i++) set = set * 31 + all[i].Pointer.ToInt64();
                if (set != skinSet)
                {
                    Show(false, true);
                    var b = new System.Collections.Generic.List<SkinnedMeshRenderer>(); var l = new System.Collections.Generic.List<SkinnedMeshRenderer>();
                    for (var i = 0; i < all.Length; i++)
                    {
                        if (all[i].gameObject.name.StartsWith("leg-")) l.Add(all[i]);
                        else if (all[i].shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) b.Add(all[i]);
                    }
                    body = b.ToArray(); legSet = l.ToArray(); skinSet = set;
                    Say("look: " + all.Length + " skinned meshes, " + body.Length + " shadow-only body, " + legSet.Length + " first-person");
                }
            }
            if (wantThird != third) Show(wantThird, false);
            lookFailures = 0;
        }
        catch (Exception e)
        {
            // Back to the game's own state with the set as it was known, and another try later.
            lookFailures++; lookRetryAt = now + 3f; skinSet = 0;
            Say("look threw " + e.GetType().Name + ": " + e.Message);
            try { Show(false, true); } catch (Exception) { }
        }
    }

    // A destroyed Unity object compares equal to null; checking the few known meshes is cheap, and
    // catches a clothing change the moment it happens.
    private static bool SkinGone()
    {
        for (var i = 0; i < body.Length; i++) if (body[i] == null) return true;
        for (var i = 0; i < legSet.Length; i++) if (legSet[i] == null) return true;
        return false;
    }

    public static void Unbind()
    {
        RestoreGrabFingers(lGrabFingers); RestoreGrabFingers(rGrabFingers);
        RestoreGrabHand(LHand, ref lGrabHand); RestoreGrabHand(RHand, ref rGrabHand);
        lGrabFingers = rGrabFingers = null;
        lGrabHand = rGrabHand = default(GrabHandBasis);
        ResetPresentation();
        try { Show(false, true); } catch (Exception) { }
        // What cannot be put back now is kept, and tried again from the next frame on.
        HeldRestore(false);
        var left = new System.Collections.Generic.List<HeldThing>(); HeldKeep(left);
        held = left.ToArray(); heldSet = 0; heldScanFailures = 0; nextHeld = nextWhole = 0f; wholeWait = HeldWhole; heldHolder = null; heldOff = false; heldSlow = 0; heldPrepared = false;
        Bound = false; Mirrored = false; lookPrepared = false; body = new SkinnedMeshRenderer[0]; legSet = new SkinnedMeshRenderer[0]; skinSet = 0;
    }

    private static void Show(bool wantThird, bool always)
    {
        if (third == wantThird && !always) return;
        third = wantThird;
        LookState = wantThird ? "third person" : "first person";
        // Each mesh on its own: one that fails must not leave the others in the wrong state.
        var failures = 0; var first = "";
        for (var i = 0; i < body.Length; i++)
        {
            try { if (body[i] != null) body[i].shadowCastingMode = wantThird ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }
            catch (Exception e) { if (failures++ == 0) first = e.GetType().Name + ": " + e.Message; }
        }
        for (var i = 0; i < legSet.Length; i++)
        {
            try { if (legSet[i] != null) legSet[i].enabled = !wantThird; }
            catch (Exception e) { if (failures++ == 0) first = e.GetType().Name + ": " + e.Message; }
        }
        if (failures > 0) throw new InvalidOperationException(failures + " meshes could not be switched, first " + first);
    }

    private static void Say(string m) { Out.Say("RIDER " + m); }
    private static float Dist(Transform a, Transform b) { return Vector3.Distance(a.position, b.position); }
    private static Vector3 V(SkateVector v) { return new Vector3((float)v.X, (float)v.Y, (float)v.Z); }
    private static SkateVector V(Vector3 v) { return new SkateVector(v.x, v.y, v.z); }

    private static bool BindHandedness()
    {
        var wanted = new[] { Pelvis, Neck, LHip, RHip, LFoot, LToe, RFoot, RToe };
        var meshes = Root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (var m = 0; m < meshes.Length; m++)
        {
            var renderer = meshes[m];
            if (renderer == null || renderer.sharedMesh == null) continue;
            var bones = renderer.bones;
            var bind = renderer.sharedMesh.bindposes;
            if (bones == null || bind == null || bones.Length != bind.Length) continue;
            var points = new Vector3[wanted.Length]; var found = 0;
            for (var i = 0; i < bones.Length; i++)
            {
                for (var j = 0; j < wanted.Length; j++)
                {
                    if (bones[i] != wanted[j] || (found & (1 << j)) != 0) continue;
                    points[j] = bind[i].inverse.MultiplyPoint3x4(Vector3.zero);
                    found |= 1 << j;
                }
            }
            if (found != (1 << wanted.Length) - 1) continue;
            // Animated joint positions cannot establish an invariant mapping for an entire ride.
            var toes = (points[5] - points[4]) + (points[7] - points[6]);
            var spineUp = points[1] - points[0];
            var hips = points[3] - points[2];
            var handedness = Vector3.Dot(hips.normalized, Vector3.Cross(spineUp.normalized, toes.normalized));
            var determinant = renderer.transform.localToWorldMatrix.determinant;
            if (float.IsNaN(handedness) || float.IsInfinity(handedness) || Math.Abs(handedness) < 0.0001f
                || float.IsNaN(determinant) || float.IsInfinity(determinant) || determinant == 0f) continue;
            Mirrored = (handedness < 0f) != (determinant < 0f);
            bindHandedness = handedness; bindDeterminant = determinant;
            return true;
        }
        return false;
    }

    public static bool Bind(Transform playerT)
    {
        Unbind(); nextScan = 0f; lookFailures = 0; lookRetryAt = 0f;
        var models = UnityEngine.Object.FindObjectsOfType<PlayerModel>();
        PlayerModel best = null; var bd = 2f;
        for (var i = 0; i < models.Length; i++)
        {
            var d = Vector3.Distance(models[i].transform.position, playerT.position);
            if (d < bd) { bd = d; best = models[i]; }
        }
        if (best == null) return false;
        var a = best.GetComponentInChildren<Animator>(true);
        if (a == null || !a.isHuman) { Say("model without a humanoid animator"); return false; }
        Anim = a; Root = best.transform;
        Pelvis = a.GetBoneTransform(HumanBodyBones.Hips); Neck = a.GetBoneTransform(HumanBodyBones.Neck); Head = a.GetBoneTransform(HumanBodyBones.Head);
        LHip = a.GetBoneTransform(HumanBodyBones.LeftUpperLeg); LKnee = a.GetBoneTransform(HumanBodyBones.LeftLowerLeg); LFoot = a.GetBoneTransform(HumanBodyBones.LeftFoot); LToe = a.GetBoneTransform(HumanBodyBones.LeftToes);
        RHip = a.GetBoneTransform(HumanBodyBones.RightUpperLeg); RKnee = a.GetBoneTransform(HumanBodyBones.RightLowerLeg); RFoot = a.GetBoneTransform(HumanBodyBones.RightFoot); RToe = a.GetBoneTransform(HumanBodyBones.RightToes);
        LUpper = a.GetBoneTransform(HumanBodyBones.LeftUpperArm); LFore = a.GetBoneTransform(HumanBodyBones.LeftLowerArm); LHand = a.GetBoneTransform(HumanBodyBones.LeftHand);
        RUpper = a.GetBoneTransform(HumanBodyBones.RightUpperArm); RFore = a.GetBoneTransform(HumanBodyBones.RightLowerArm); RHand = a.GetBoneTransform(HumanBodyBones.RightHand);
        if (Pelvis == null || Neck == null || Head == null || LHip == null || LKnee == null || LFoot == null || LToe == null || RHip == null || RKnee == null || RFoot == null || RToe == null
            || LUpper == null || LFore == null || LHand == null || RUpper == null || RFore == null || RHand == null)
        { Say("humanoid mapping incomplete"); return false; }

        try
        {
            if (!BindHandedness()) Say("no usable skeleton bind pose; humanoid limb mapping retained");
        }
        catch (Exception e) { Say("skeleton bind pose unavailable: " + e.GetType().Name + "; humanoid limb mapping retained"); }
        // The same question asked of the joints as they stand now: the right hip on the right of the
        // spine and the toes. Logged beside the bind pose's answer, which is the one used.
        var toesNow = (LToe.position - LFoot.position) + (RToe.position - RFoot.position);
        var visualRight = Vector3.Cross(Neck.position - Pelvis.position, toesNow);
        var acrossNow = visualRight.sqrMagnitude > 0.0001f ? Vector3.Dot(RHip.position - LHip.position, visualRight.normalized) : 0f;
        Say("limbs: bind pose handedness=" + bindHandedness.ToString("F3") + " determinant=" + bindDeterminant.ToString("F3")
            + ", standing right hip across=" + acrossNow.ToString("F3") + "; shown " + (Mirrored ? "SWAPPED" : "as is"));
        if (Mirrored)
        {
            Swap(ref LHip, ref RHip); Swap(ref LKnee, ref RKnee); Swap(ref LFoot, ref RFoot); Swap(ref LToe, ref RToe);
            Swap(ref LUpper, ref RUpper); Swap(ref LFore, ref RFore); Swap(ref LHand, ref RHand);
        }

        // The spine is every bone between the pelvis and the neck, whatever the avatar calls them.
        var chain = new System.Collections.Generic.List<Transform>();
        var t = Neck.parent;
        while (t != null && t != Pelvis && chain.Count < 8) { chain.Insert(0, t); t = t.parent; }
        if (t != Pelvis || chain.Count == 0) { Say("no spine between the pelvis and the neck"); return false; }
        spine = chain.ToArray();
        var back = Dist(Pelvis, spine[0]);
        for (var i = 0; i < spine.Length; i++) back += Dist(spine[i], i + 1 < spine.Length ? spine[i + 1] : Neck);

        Thigh = (Dist(LHip, LKnee) + Dist(RHip, RKnee)) * 0.5f; Shin = (Dist(LKnee, LFoot) + Dist(RKnee, RFoot)) * 0.5f;
        Foot = (Dist(LFoot, LToe) + Dist(RFoot, RToe)) * 0.5f;
        UpperArm = (Dist(LUpper, LFore) + Dist(RUpper, RFore)) * 0.5f; Forearm = (Dist(LFore, LHand) + Dist(RFore, RHand)) * 0.5f;
        var hipWidth = Dist(LHip, RHip);
        var hipDrop = Vector3.Distance(Pelvis.position, (LHip.position + RHip.position) * 0.5f);
        var drop = AnkleHeight - ToeHeight;
        var footFlat = (float)Math.Sqrt(Math.Max(0.0001f, Foot * Foot - drop * drop));
        // The module puts the neck 0.12 of the spine above the chest, and the chest a spine above the
        // hips; the arm joints sit a little above the chest point, which the module is told.
        var up = Root.rotation * Vector3.up;
        var spineLength = (back + hipDrop) / 1.12f;
        var rise = Vector3.Dot((LUpper.position + RUpper.position) * 0.5f - Pelvis.position, up) - spineLength;
        Rig = new SkateRiderRig((Thigh + Shin) * 0.945f, hipWidth, Thigh, Shin, footFlat, spineLength, Dist(LUpper, RUpper), UpperArm, Forearm, Dist(Neck, Head),
            SkateRide.Clamp(rise, 0f, 0.5f));
        // Which way the soles face, seen from each foot bone, while the player stands.
        lSole = Quaternion.Inverse(LFoot.rotation) * up; rSole = Quaternion.Inverse(RFoot.rotation) * up;
        lGrabHand = MeasureGrabHand(a, LHand); rGrabHand = MeasureGrabHand(a, RHand);
        lGrabFingers = MeasureGrabFingers(a, LHand, lGrabHand);
        rGrabFingers = MeasureGrabFingers(a, RHand, rGrabHand);
        Rig = Rig.WithHands(lGrabHand.Measurements, rGrabHand.Measurements);
        Say("grab wrist frames left=" + lGrabHand.Ready + " right=" + rGrabHand.Ready +
            "; measured MCP left=" + lGrabHand.Measurements.Measured + " right=" + rGrabHand.Measurements.Measured +
            "; unavailable measurements use authored 65 mm reach");
        failed = false; Bound = true; Error = "";
        Say("bound to " + Root.gameObject.name + " spine bones=" + spine.Length + " thigh=" + Thigh.ToString("F3") + " shin=" + Shin.ToString("F3") + " foot=" + Foot.ToString("F3") + " upperArm=" + UpperArm.ToString("F3")
            + " forearm=" + Forearm.ToString("F3") + " hips=" + hipWidth.ToString("F3") + " hipDrop=" + hipDrop.ToString("F3") + " back=" + back.ToString("F3") + " shoulders=" + Rig.ShoulderWidth.ToString("F3")
            + " neck=" + Rig.NeckToHeadLength.ToString("F3") + " headScale=" + Out.V(Head.localScale));
        return true;
    }

    // Measure each actual hand, including the existing mirrored bone mapping. A missing finger
    // basis disables this optional wrist refinement, not riding or the arm/contact solve.
    private static GrabHandBasis MeasureGrabHand(Animator animator, Transform hand)
    {
        var result = default(GrabHandBasis);
        try
        {
            bool humanLeft = hand == animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var middle = animator.GetBoneTransform(humanLeft ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var index = animator.GetBoneTransform(humanLeft ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(humanLeft ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            if (middle == null || index == null || little == null) return result;
            var finger = middle.position - hand.position;
            var across = index.position - little.position;
            var back = humanLeft ? Vector3.Cross(finger, across) : Vector3.Cross(across, finger);
            if (finger.sqrMagnitude < .000001f || back.sqrMagnitude < .000000001f) return result;
            SkateHandMeasurements measurements;
            if (!SkateHandMeasurements.TryMeasured(finger.magnitude, across.magnitude, out measurements)) return result;
            result.Measurements = measurements;
            var inverse = Quaternion.Inverse(hand.rotation);
            result.Finger = inverse * finger.normalized; result.Back = inverse * back.normalized;
            result.Ready = true;
        }
        catch (Exception e) { Say("grab wrist basis unavailable: " + e.GetType().Name + "; animated hand retained"); }
        return result;
    }

    private static void RestoreGrabHand(Transform hand, ref GrabHandBasis basis)
    {
        try { if (basis.Saved && hand != null) hand.localRotation = basis.OriginalLocal; }
        catch (Exception e) { Say("grab wrist restore unavailable: " + e.GetType().Name); basis.Ready = false; }
        basis.Saved = false;
    }

    private static void ApplyGrabHand(Transform hand, ref GrabHandBasis basis, SkateBoardPose frame, double weight)
    {
        if (hand == null) return;
        if (!basis.Ready || !frame.Initialized || weight <= 0)
        { RestoreGrabHand(hand, ref basis); return; }
        try
        {
            if (!basis.Saved) { basis.OriginalLocal = hand.localRotation; basis.Saved = true; }
            // Rebuild from the entry's real local hand rotation each frame; never accumulate last
            // frame's correction. Releasing or another action restores it, including ordinary captures.
            hand.localRotation = basis.OriginalLocal;
            var target = Quaternion.LookRotation(V(frame.Forward), V(frame.Up)) *
                Quaternion.Inverse(Quaternion.LookRotation(basis.Finger, basis.Back));
            hand.rotation = Quaternion.Slerp(hand.rotation, target, SkateRide.Clamp((float)weight, 0, 1));
        }
        catch (Exception e)
        {
            RestoreGrabHand(hand, ref basis); basis.Ready = false;
            Say("grab wrist posing unavailable: " + e.GetType().Name + "; animated hand retained");
        }
    }

    private static GrabFingers MeasureGrabFingers(Animator animator, Transform hand, GrabHandBasis basis)
    {
        var result = new GrabFingers();
        try
        {
            if (!basis.Ready) throw new InvalidOperationException("wrist basis unavailable");
            var left = hand == animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (!left && hand != animator.GetBoneTransform(HumanBodyBones.RightHand))
                throw new InvalidOperationException("hand mapping ambiguous");
            var first = left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal;
            var thumb = left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal;
            var bones = new GrabFingerBone[15];
            for (var i = 0; i < bones.Length; i++)
            {
                var id = i < 12 ? (HumanBodyBones)((int)first + i) : (HumanBodyBones)((int)thumb + i - 12);
                var bone = animator.GetBoneTransform(id);
                if (bone == null) throw new InvalidOperationException("finger mapping incomplete");
                for (var j = 0; j < i; j++) if (bones[j].Bone == bone)
                    throw new InvalidOperationException("finger mapping ambiguous");
                var parent = i % 3 == 0 ? hand : bones[i - 1].Bone;
                if (bone.parent != parent) throw new InvalidOperationException("finger chain is not direct");
                bones[i].Bone = bone;
            }
            for (var i = 0; i < bones.Length; i++)
            {
                var direction = i % 3 < 2 ? bones[i + 1].Bone.position - bones[i].Bone.position
                    : bones[i].Bone.position - bones[i - 1].Bone.position;
                var length = direction.magnitude;
                if (float.IsNaN(length) || float.IsInfinity(length) || length < .004f || length > .12f)
                    throw new InvalidOperationException("finger segment invalid");
                bones[i].Direction = Quaternion.Inverse(bones[i].Bone.rotation) * direction.normalized;
                // Humanoid mapping has no fingertip. The last reach is an authored proportion.
                bones[i].Length = length * (i % 3 == 2 ? .8f : 1f);
            }
            for (var i = 0; i < 12; i += 3)
            {
                var l1 = bones[i].Length; var l2 = bones[i + 1].Length; var l3 = bones[i + 2].Length;
                var dip = 25 * Math.PI / 180;
                var effective = Math.Sqrt(l2 * l2 + l3 * l3 + 2 * l2 * l3 * Math.Cos(dip));
                var offset = Math.Atan2(l3 * Math.Sin(dip), l2 + l3 * Math.Cos(dip));
                var scale = basis.Measurements.McpLength / .065;
                var angle = 20 * Math.PI / 180;
                var ny = -Math.Cos(angle); var nz = Math.Sin(angle);
                var oy = Math.Sin(angle); var oz = Math.Cos(angle);
                var pad = (i == 3 ? .0085 : i == 9 ? .0067 : .008) * .8 * scale;
                var edgeY = .065 * scale - oy * .020 * scale - ny * .013 * scale;
                var edgeZ = -oz * .020 * scale - nz * .013 * scale;
                var y = edgeY - oy * .018 * scale - ny * (.010 * scale + pad) - .065 * scale;
                var inside = -(edgeZ - oz * .018 * scale - nz * (.010 * scale + pad));
                var cosine = (y * y + inside * inside - l1 * l1 - effective * effective) / (2 * l1 * effective);
                var bend = Math.Acos(Math.Max(-1, Math.Min(1, cosine)));
                var start = Math.Atan2(inside, y) - Math.Atan2(effective * Math.Sin(bend), l1 + effective * Math.Cos(bend));
                bones[i].Curl = (float)start;
                bones[i + 1].Curl = (float)(start + bend - offset);
                bones[i + 2].Curl = (float)(start + bend - offset + dip);
            }
            result.Bones = bones; result.Radial = left ? -1 : 1;
            result.Reach = (float)basis.Measurements.McpLength;
            result.ThumbBase = Quaternion.Inverse(hand.rotation) * (bones[12].Bone.position - hand.position);
            result.Ready = true;
            Say("grab fingers " + (left ? "human left" : "human right") + ": 15 cached bones; authored closure enabled");
        }
        catch (Exception e)
        {
            Say("grab fingers " + (hand == LHand ? "shown left" : "shown right") + " unavailable: " + e.Message + "; avatar finger animation retained");
        }
        return result;
    }

    private static void RestoreGrabFingers(GrabFingers fingers)
    {
        if (fingers == null || !fingers.Saved) return;
        var failed = false;
        for (var i = 0; i < fingers.Bones.Length; i++)
        {
            try { if (fingers.Bones[i].Bone != null) fingers.Bones[i].Bone.localRotation = fingers.Bones[i].EntryLocal; }
            catch (Exception) { failed = true; }
        }
        fingers.Saved = false;
        if (failed)
        {
            fingers.Ready = false;
            Say("grab finger restore unavailable; remaining avatar finger animation retained");
        }
    }

    private static void AimGrabFinger(GrabFingerBone bone, Vector3 direction, float weight)
    {
        var current = bone.Bone.rotation;
        var target = Quaternion.FromToRotation(current * bone.Direction, direction) * current;
        bone.Bone.rotation = Quaternion.Slerp(current, target, weight);
    }

    private static void ApplyGrabFingers(Transform hand, GrabHandBasis basis, GrabFingers fingers,
        SkateBoardPose frame, double weight)
    {
        if (fingers == null) return;
        if (!fingers.Ready || !basis.Ready || hand == null || !frame.Initialized || weight <= 0)
        { RestoreGrabFingers(fingers); return; }
        try
        {
            if (!fingers.Saved)
            {
                for (var i = 0; i < fingers.Bones.Length; i++)
                    fingers.Bones[i].EntryLocal = fingers.Bones[i].Bone.localRotation;
                fingers.Saved = true;
            }
            for (var i = 0; i < fingers.Bones.Length; i++)
                fingers.Bones[i].Bone.localRotation = fingers.Bones[i].EntryLocal;
            var w = SkateRide.Clamp((float)weight, 0, 1);
            var forward = hand.rotation * basis.Finger; var back = hand.rotation * basis.Back;
            var across = Vector3.Cross(forward, back).normalized;
            for (var i = 0; i < 12; i++)
            {
                var angle = fingers.Bones[i].Curl;
                AimGrabFinger(fingers.Bones[i], forward * (float)Math.Cos(angle) - back * (float)Math.Sin(angle), w);
            }
            var scale = fingers.Reach / .065f; var angle20 = 20 * Math.PI / 180;
            var normal = forward * -(float)Math.Cos(angle20) + back * (float)Math.Sin(angle20);
            var outward = forward * (float)Math.Sin(angle20) + back * (float)Math.Cos(angle20);
            var target = hand.position + forward * fingers.Reach - outward * (.031f * scale)
                - normal * (.003f * scale) + across * (fingers.Radial * .008f * scale);
            var root = hand.position + hand.rotation * fingers.ThumbBase;
            var upper = fingers.Bones[12].Length; var lower = fingers.Bones[13].Length + fingers.Bones[14].Length;
            var middle = Bend(root, target, upper, lower, across * fingers.Radial + back * .45f);
            AimGrabFinger(fingers.Bones[12], middle - root, w);
            AimGrabFinger(fingers.Bones[13], target - middle, w);
            AimGrabFinger(fingers.Bones[14], target - middle, w);
        }
        catch (Exception e)
        {
            RestoreGrabFingers(fingers); fingers.Ready = false;
            Say("grab finger posing unavailable: " + e.GetType().Name + "; avatar finger animation retained");
        }
    }

    private static void Aim(Transform bone, Transform child, Vector3 want)
    {
        var cur = child.position - bone.position;
        if (cur.sqrMagnitude < 0.000001f || want.sqrMagnitude < 0.000001f) return;
        bone.rotation = Quaternion.FromToRotation(cur, want) * bone.rotation;
    }

    private static void Roll(Transform bone, Vector3 axis, Vector3 local, Vector3 want)
    {
        var cur = bone.rotation * local;
        cur = cur - axis * Vector3.Dot(cur, axis); want = want - axis * Vector3.Dot(want, axis);
        if (cur.sqrMagnitude < 0.0001f || want.sqrMagnitude < 0.0001f) return;
        bone.rotation = Quaternion.FromToRotation(cur, want) * bone.rotation;
    }

    public static Vector3 Bend(Vector3 root, Vector3 target, float upper, float lower, Vector3 pole)
    {
        var to = target - root; var d = to.magnitude;
        var max = (upper + lower) * 0.999f; var min = Math.Abs(upper - lower) * 1.001f + 0.001f;
        if (d > max) d = max; if (d < min) d = min;
        var axis = to.sqrMagnitude > 0.000001f ? to.normalized : Vector3.down;
        var along = (upper * upper - lower * lower + d * d) / (2f * d);
        var h2 = upper * upper - along * along;
        var side = pole - axis * Vector3.Dot(pole, axis);
        side = side.sqrMagnitude > 0.000001f ? side.normalized : Vector3.Cross(axis, Vector3.right).normalized;
        return root + axis * along + side * (float)Math.Sqrt(h2 > 0f ? h2 : 0f);
    }

    private static void ResetPresentation()
    {
        RestoreGrabFingers(lGrabFingers); RestoreGrabFingers(rGrabFingers);
        RestoreGrabHand(LHand, ref lGrabHand); RestoreGrabHand(RHand, ref rGrabHand);
        lGrabFrame = rGrabFrame = default(SkateBoardPose); lGrabWeight = rGrabWeight = 0;
        travelLookWeight = 0; motionLookFrame = false;
        airLookFrame = false;
        SkateCapturedTricks.Reset();
        SkateFootContacts.Reset();
        footCalibrated = false; calibratedFootVersion = -1; FootSupportError = "";
        reportedFootStatus = reportedFootFit = null;
        grabSince = grabHold = -1f; plantBlend = chestLead = crouchSpeed = 0f; Crouch = 0.18f;
        chargePosePop = -1; chargePoseEpoch = 0; chargePoseAmount = 0;
        chargeOwnedGround = chargeSetupGrounded = false;
        chargeNosePop = chargeEntryShown = false; chargeInitialPelvisOffset = null; chargeOwnerPlayer = null; ClearChargeEntry();
        contact = default(SkateRiderContactState);
        plantStyle = default(SkateHandplantState); plantId = -1; SkateBoard.HeldFrame = default(SkateBoardPose);
        pinLeft = pinRight = pinHips = false;
        motion = default(SkateRiderMotionState); easedAt = -99f;
        SkateBoard.GrabFold = SkateBoard.Plant = 0f; SkateBoard.Held = false;
    }
    public static void BeginRide() { ResetPresentation(); SkateCapturedTricks.Preload(); }

    public static void Prepare(float frameSeconds)
    {
        SkateActions.Prepare();
        SkateBoard.Held = false;
        SkateBoard.HeldFrame = default(SkateBoardPose);
        if (!SkateRide.On) { ResetPresentation(); return; }
        if (SkateRiggingShowcase.Running) return;
        var now = Time.realtimeSinceStartup;
        var wall = SkateSurfaceRide.DisplayWall;
        var grab = SkateRide.Mode == RideMode.Air && !wall && !SkateActions.Owns && !SkateOldschoolRide.Active ? SkateRide.Grab : SkateGrabKind.None;
        if (grab == SkateGrabKind.None) { grabSince = -1f; grabHold = -1f; } else if (grabSince < 0f) grabSince = now;
        var held = grab == SkateGrabKind.None ? 0f : grabHold >= 0f ? grabHold : now - grabSince;
        var since = now - SkateRide.LandAt;
        var settle = SkateRide.Mode == RideMode.Ground && since >= 0f && since < LandSettle ? 1f - since / LandSettle : 0f;
        var landing = settle * settle * SkateRide.Clamp(SkateRide.LandImpact / 8f, 0.2f, 1f);
        if (wall) contact = default(SkateRiderContactState);
        else if (SkateRide.Mode == RideMode.Ground || SkateRide.Mode == RideMode.Air)
        {
            // A pause/hitch must not erase the current flight or replay its initial flick.
            if (!float.IsNaN(frameSeconds) && !float.IsInfinity(frameSeconds) && frameSeconds > 0f)
            {
                SkateRiderContactState next; string error;
                var contactDelta = Math.Min(frameSeconds, 0.05f);
                // A passive fall has no scored take-off. Do not turn a stale/default trick into an
                // ollie, and do not reinterpret a sticky late flag as a new release request.
                var trick = SkateRide.Mode == RideMode.Air && SkateRide.Trick.Airborne
                    ? new SkateRiderTrickContext(SkateRide.Trick.PopKind, SkateRide.Trick.PopSwitch,
                        SkateRide.Trick.PopTailLeads, SkateRide.TailFirst, SkateRide.Trick.Late)
                    : default(SkateRiderTrickContext);
                if (SkateRider.TryStepContact(contact, SkateRide.Mode == RideMode.Air, SkateRide.Rotating, SkateRide.VSpeed <= 0f,
                    SkateRide.FootPlanted, SkateRide.FlipDeg, SkateRide.ShoveDeg, grab, held, landing, trick, SkateRide.BoardPath, contactDelta, out next, out error))
                    contact = next;
                else
                {
                    contact = default(SkateRiderContactState);
                    if (error != contactError) { contactError = error; Say("contact phase refused: " + error); }
                }
            }
        }
        else contact = default(SkateRiderContactState);
        // Body, held hands and board tweak consume this same release/catch phase in this frame.
        SkateBoard.GrabFold = wall || SkateActions.Owns ? 0 : (float)contact.BoardFold;
        SkateBoard.Plant = !wall && SkateRide.FootPlanted ? SkateRide.Clamp((SkateRide.PlantedUntil - now) / SkateRide.PlantSeconds * 1.5f, 0f, 1f) * SkateRide.Clamp((now - (SkateRide.PlantedUntil - SkateRide.PlantSeconds)) / PlantIn, 0f, 1f) : 0f;
        var plantWant = SkateRide.Handplant && !SkateRide.HandplantDown ? 1f : 0f;
        var plantStep = SkateRide.Clamp(frameSeconds / PlantSeconds, 0f, 1f);
        plantBlend = SkateRide.Mode == RideMode.Bail ? 0f : SkateRide.Clamp(plantBlend + SkateRide.Clamp(plantWant - plantBlend, -plantStep, plantStep), 0f, 1f);
        if (wall) plantBlend = 0;
        if (!SkateRide.Handplant || plantId != SkateRide.Stalls) plantStyle = default(SkateHandplantState);
        plantId = SkateRide.Stalls;
        if (SkateRide.Handplant && frameSeconds > 0 && !float.IsInfinity(frameSeconds))
        {
            SkateHandplantState nextStyle; string styleError;
            var request = SkateRide.HandplantDown ? SkateHandplantKind.None : SkateKeys.HandplantVariant;
            if (SkateHandplants.TryStep(plantStyle, request, SkateRide.GrindFrontside, true, Math.Min(.05, frameSeconds), out nextStyle, out styleError)) plantStyle = nextStyle;
        }
    }

    public static void Frame(Vector3 feet, Vector3 up, bool legsOnly, float frameSeconds)
    {
        RestoreGrabFingers(lGrabFingers); RestoreGrabFingers(rGrabFingers);
        SkateCapturedTricks.ClearFrame();
        lGrabFrame = rGrabFrame = default(SkateBoardPose); lGrabWeight = rGrabWeight = 0;
        motionLookFrame = false;
        airLookFrame = false;
        if (!Bound || failed) return;
        if (SkateRide.Handplant) up = Vector3.up;
        try
        {
            if (Pelvis == null) { Bound = false; return; }
            SkateFootContacts.UseView(third, skinSet);
            if (SkateRiggingShowcase.Running) { FramePreview(frameSeconds); return; }
            var wall = SkateSurfaceRide.DisplayWall;
            var bodyFlip = SkateSurfaceRide.BodyFlip.Known;
            var wallDeck = default(SkateBoardPose);
            if (wall)
            {
                if (SkateRig.LocalT == null) return;
                wallDeck = SkateBoard.UnflippedFor(SkateRig.LocalT);
                up = V(wallDeck.Up);
                feet = V(wallDeck.Origin) - up * DeckTop;
            }
            // The rider stands square to the deck all the way up a transition to the vertical, as the
            // board does: only a missing or upside-down up falls back to the world's.
            double ux = up.x, uy = up.y, uz = up.z;
            var ul = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            if (ul < 0.5 || !wall && uy / ul < -0.1) { ux = 0; uy = 1; uz = 0; ul = 1; }
            ux /= ul; uy /= ul; uz /= ul;
            // The nose's way on the face: the part of it into a steep face goes up the face, so the
            // body still has a forward where the nose points straight into a vertical wall.
            var face = SkateBoard.OnFace(new Vector3((float)ux, (float)uy, (float)uz), SkateRide.Dir(SkateRide.NoseYaw));
            if (wall)
            {
                face = V(wallDeck.Forward);
                if (Vector3.Dot(face, SkateRide.Dir(SkateRide.NoseYaw)) < 0) face = -face;
            }
            if (SkateOldschoolRide.Active && (SkateOldschoolRide.State.Caught || SkateOldschoolRide.Phase == SkateOldschoolPhase.Catching))
            {
                var deckWay = SkateBoard.OnFace(new Vector3((float)ux, (float)uy, (float)uz), V(SkateOldschoolRide.Frame.Forward));
                if (Vector3.Dot(face, deckWay) < 0) deckWay = -deckWay;
                face = Vector3.Lerp(face, deckWay, SkateOldschoolRide.FootContact).normalized;
            }
            double fx = face.x, fy = face.y, fz = face.z;
            var fl = Math.Sqrt(fx * fx + fy * fy + fz * fz);
            if (fl < 0.2) return;
            fx /= fl; fy /= fl; fz /= fl;
            var upV = new Vector3((float)ux, (float)uy, (float)uz);
            // The module's ankles are on the board's centre line; a foot is longer than the deck is
            // wide, so the plane goes toward the heels. The pushing foot's place beside the board
            // (SkatePose.PushFootSide) counts that shift in.
            var toToes = new Vector3((float)(uy * fz - uz * fy), (float)(uz * fx - ux * fz), (float)(ux * fy - uy * fx));
            toToes = toToes * SkateRide.StanceSign;
            var plane = feet + upV * (DeckTop + AnkleHeight - (float)SkatePose.FootLift) - toToes * HeelShift;

            var now = Time.realtimeSinceStartup;
            var mode = SkateRide.Mode;
            // In a jump the legs fold by as much as the board has come up, so the body keeps its line.
            // The ollie's pitch about the tail lifts the deck's middle too; the legs fold by that as well.
            var pitchRise = SkateBoard.Axle * (float)Math.Abs(Math.Sin(SkateBoard.AirPitch * Math.PI / 180.0));
            var inAir = SkateRide.Jumped ? 0.2f + (SkateBoard.Pop + pitchRise) / (float)SkatePose.CrouchDrop : 0.45f;
            inAir += .10f * SkateRide.TransitionAirTuck / (float)SkatePose.CrouchDrop;
            inAir = Math.Max(inAir, .2f + .4f * SkateRide.TransitionLandingApproach);
            var shownGrab = mode == RideMode.Air && !wall && !SkateActions.Owns && !SkateOldschoolRide.Active ? ShownGrab : SkateGrabKind.None;
            if (shownGrab != SkateGrabKind.None) inAir += (0.95f - inAir) * (contact.Initialized ? (float)contact.GrabWeight : 1f);
            var want = mode == RideMode.Air ? inAir : mode == RideMode.Grind ? GrindCrouch(SkateRide.GrindKind) : SkateRide.Braking ? 0.4f : 0f;
            var ordinaryRolling = mode == RideMode.Ground && Math.Abs(SkateRide.Speed) > .5f &&
                !wall && !bodyFlip && !SkateActions.Owns && !SkateOldschoolRide.Active &&
                !SkateRide.GroundGrab.Active && !SkateRide.Handplant && PlantBlend <= .001f &&
                !SkateRide.FootPlanted && SkateRide.PushPhase <= .001f && !SkateSurfaceRide.DropInMotion.Active;
            if (ordinaryRolling) want = Math.Max(want, .18f);
            var chargeRoute = OrdinaryFooting(true) && SkateKeys.ControlsActive && !wall && !bodyFlip && PlantBlend <= .001f &&
                !SkateSurfaceRide.DropInMotion.Active;
            bool preparationNose = false; double preparationCharge = 0;
            bool padPreparation = mode == RideMode.Ground && SkateKeys.Pad &&
                SkatePad.TryGroundPreparation(out preparationNose, out preparationCharge);
            var ordinaryCharging = mode == RideMode.Ground && chargeRoute && (SkateKeys.Charging || padPreparation);
            chargeSetupGrounded = chargeEntryShown = false;
            // A fresh charge owns its setup, including after a captured landing.
            if (ordinaryCharging && (!chargeOwnedGround || chargeOwnerPlayer != SkateRig.LocalT ||
                chargeOwnerGoofy != SkateRide.Goofy || chargePoseEpoch != SkateKeys.InputEpoch || chargePosePop != SkateRide.Pops + 1))
            {
                // The held stick end is known now; PopKind still belongs to the last release.
                chargeNosePop = padPreparation ? preparationNose : SkateKeys.Pad && SkatePad.RightY > 0; chargeInitialPelvisOffset = null;
                chargeOwnerPlayer = SkateRig.LocalT; chargeOwnerGoofy = SkateRide.Goofy; ClearChargeEntry();
                SkateCapturedTricks.Reset();
            }
            chargeOwnedGround = ordinaryCharging;
            if (ordinaryCharging)
            { chargePosePop = SkateRide.Pops + 1; chargePoseEpoch = SkateKeys.InputEpoch; chargePoseAmount = padPreparation ? (float)preparationCharge : SkateRide.Clamp(SkateKeys.Charge, 0, 1); }
            else if (!chargeRoute || chargePoseEpoch != SkateKeys.InputEpoch ||
                !(mode == RideMode.Air && SkateRide.Jumped && chargePosePop == SkateRide.Pops ||
                mode == RideMode.Ground && (SkateKeys.JumpPressed || SkateRide.PresentationQueued) && chargePosePop == SkateRide.Pops + 1))
            { chargePosePop = -1; chargePoseEpoch = 0; chargePoseAmount = 0; }
            if (wall) want = .55f;
            else if (SkateSurfaceRide.WallieWeight > 0) want = Math.Max(want, .25f + .55f * SkateSurfaceRide.WallieWeight);
            if (SkateOldschoolRide.Active && SkateOldschoolRide.Kind == SkateOldschoolKind.Caveman &&
                (SkateOldschoolRide.Phase == SkateOldschoolPhase.Pickup || SkateOldschoolRide.Phase == SkateOldschoolPhase.Carrying)) want = .95f;
            // A right stick held back is the rider gathering for the pop; a trigger held is the crouch of a pump.
            if ((SkateKeys.Charging || ordinaryCharging) && mode == RideMode.Ground)
                want = Math.Max(want, 0.4f + 0.4f * (ordinaryCharging ? chargePoseAmount : SkateKeys.Charge));
            if (mode == RideMode.Ground && SkateRide.PumpHeld > 0f) want = Math.Max(want, 0.25f + 0.45f * SkateRide.PumpHeld / SkateRide.PumpCharge);
            var since = now - SkateRide.LandAt;
            if (mode == RideMode.Ground && since < 0.35f) want += (1f - since / 0.35f) * SkateRide.Clamp(SkateRide.LandImpact / 8f, 0.2f, 1f) * 0.45f;
            var landing = contact.Initialized ? contact.Landing : 0;
            // The legs are a spring, a little under critical: the body dips past the crouch on a
            // landing and comes back, and settles into a pop with a small overshoot.
            var dt = SkateRide.Clamp(frameSeconds, 0f, 0.05f);
            crouchSpeed += ((SkateRide.Clamp(want, 0f, 1f) - Crouch) * CrouchSpring - crouchSpeed * CrouchDamp) * dt;
            Crouch = SkateRide.Clamp(Crouch + crouchSpeed * dt, 0f, 1f);
            // The board's rise is eased already; the legs follow it at once, or the body would bob.
            if (mode == RideMode.Air && SkateRide.Jumped && !SkateRide.Grabbing && Crouch < want) Crouch = SkateRide.Clamp(want, 0f, 1f);
            // The module leans toward the board's own right; the ride leans toward the right of travel.
            var lean = SkateRide.Clamp(SkateRide.Trick.Switch ? -SkateRide.Lean : SkateRide.Lean, -(float)SkatePose.MaximumLean, (float)SkatePose.MaximumLean);
            var speed = Math.Min((float)SkateMotion.MaximumGroundSpeed, Math.Abs(SkateRide.Speed));
            var grab = shownGrab;
            var plant = SkateBoard.Plant;
            // The chest ahead of a spin in the air; on the ground, wound up against the spin the
            // left stick asks for while the right stick is held back for the pop.
            var leadWant = mode == RideMode.Air ? SkateRide.SpinLead
                : mode == RideMode.Ground && SkateKeys.Charging && SkateKeys.Spin != 0f ? SkateKeys.Spin * SkateRide.SpinSide * SkateRide.SpinWindUp : 0f;
            chestLead += (SkateRide.Clamp(leadWant, -(float)SkateRider.MaximumChestTurn, (float)SkateRider.MaximumChestTurn) - chestLead) * SkateRide.Clamp(frameSeconds * 10f, 0f, 1f);
            var handplant = wall || bodyFlip ? 0 : PlantBlend;
            // The opposite foot slides away from the popped end, including when the tail leads.
            // The shared take-off description chooses that foot without changing the ride's inputs.
            var noseSlide = !wall && !SkateOldschoolRide.Active && !SkateActions.Finger.Initialized && mode == RideMode.Air && SkateRide.Jumped && grab == SkateGrabKind.None && contact.Trick.FootPop ? SkateRide.Clamp(1f - SkateRide.VSpeed / SkateRide.LaunchPopImpulse, 0f, 1f) * (1f - (float)contact.Catch) : 0f;
            var groundGrab = !wall && (mode == RideMode.Ground || mode == RideMode.Air) && SkateRide.GroundGrab.Active;
            var pushing = mode == RideMode.Ground && !groundGrab && SkateRide.PushPhase > 0f;
            var deckFace = SkateBoard.OnFace(upV, SkateRide.Dir(SkateRide.DeckYaw));
            var input = new SkateRiderInput(new SkateVector(plane.x, plane.y, plane.z), new SkateVector(fx, fy, fz), new SkateVector(ux, uy, uz), SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular, SkateRide.TailFirst,
                speed, lean, Crouch, mode == RideMode.Air, pushing, SkateRide.Clamp(SkateRide.PushPhase, 0f, 1f),
                SkateRide.FlipDeg, grab, mode == RideMode.Bail, SkateKeys.LookYaw % 360f, SkatePose.FootLift - AnkleHeight,
                DeckTop + (SkateOldschoolRide.Active && mode == RideMode.Ground ? Math.Max(0, SkateRide.Clearance) : 0), mode == RideMode.Bail ? SkateRide.Clamp((now - SkateRide.ModeAt) / SkateRide.BailSeconds, 0f, 1f) : 0f,
                SkateRide.PushLead, plant, contact.Initialized ? contact.GrabSeconds : grabHold >= 0f ? grabHold : grabSince < 0f ? 0f : now - grabSince, handplant,
                SkateRide.Clamp(SkateBoard.PitchDegrees, -40f, 40f) * Math.PI / 180.0,
                noseSlide, chestLead, mode == RideMode.Air ? SkateRide.ShoveDeg : 0.0, landing, SkateActions.Owns || SkateOldschoolRide.Active ? default(SkateRiderContactState) : contact, SkateRide.PushPeriod, new SkateVector(deckFace.x, deckFace.y, deckFace.z),
                SkateBoard.SupportTruck != 0, SkateBoard.GrindRoll * Math.PI / 180.0,
                SkateBoard.SupportTruck * SkateBoard.Axle * SkateBoard.SupportWeight,
                SkateRig.LocalT != null && mode != RideMode.Bail ? (SkateActions.Owns || SkateOldschoolRide.Active ? SkateBoard.CarrierFor(SkateRig.LocalT) : SkateBoard.UnflippedFor(SkateRig.LocalT)) : default(SkateBoardPose), SkateRide.BoardPath);
            SkateRiderPose pose; string error;
            SkateBoardPose plantBoard = default(SkateBoardPose);
            bool posed;
            if (wall) posed = WallPose(input, out pose, out error);
            else if (groundGrab) posed = SkateRider.TryCreateGroundGrab(Rig, input, SkateRide.GroundGrab, out pose, out error);
            else if (handplant > .001f && SkateRide.Handplant)
            {
                var edgeWay = SkateGrind.StallSupported ? SkateRide.Dir(SkateGrind.Heading) : Vector3.Cross(upV, face);
                var anchor = SkateGrind.StallSupported ? SkateGrind.HandAnchor : plane + upV * (Math.Min(0, (float)input.DeckOffset) - DeckTop) + toToes * .22f + face * (SkateRide.TailFirst ? -.1f : .1f);
                posed = SkateRider.TryCreateHandplant(Rig, input, plantStyle, SkateRide.GrindFrontside, new SkateVector(anchor.x, anchor.y, anchor.z),
                    new SkateVector(edgeWay.x, edgeWay.y, edgeWay.z), out pose, out plantBoard, out error);
            }
            else posed = SkateRider.TryCreate(Rig, input, out pose, out error);
            if (!posed)
            {
                var withoutPush = pushing ? new SkateRiderInput(input.BoardPosition, input.BoardForward, input.BoardUp, input.Stance, input.Switch,
                    speed, lean, Crouch, false, false, 0, SkateRide.FlipDeg, grab, false, SkateKeys.LookYaw % 360f, SkatePose.FootLift - AnkleHeight,
                    DeckTop, 0, false, plant, 0, handplant, SkateRide.Clamp(SkateBoard.PitchDegrees, -40f, 40f) * Math.PI / 180.0, 0, chestLead, 0.0, landing, contact) : input;
                if (!pushing || !SkateRider.TryCreate(Rig, withoutPush, out pose, out error))
                {
                    if (Refusals++ == 0 || error != Error) Say("pose refused: " + error);
                    Error = error;
                    if (wall || bodyFlip) SkateRide.Bail();
                    return;
                }
                if (WithoutPush++ == 0 || error != Error) Say("pose shown without the push: " + error);
                Error = error; pushing = false;
            }
            // Secondary motion: the pelvis springs and the shoulders and hands lag a little behind
            // the pose, so the body is not rigid. In the air the legs' own spring carries the pop, so
            // the module's pelvis spring is off there, and a grabbing hand is pinned to its target so
            // it stays on the board while the rest lags. Not in a handplant or a bail, where the
            // module's contact must stand as it is: the motion is reset and the pose shown as is.
            if (SkateOldschoolRide.Active)
            {
                SkateBoardPose independent; SkateRiderPose freePose;
                var goal = SkateOldschoolRide.HandTarget;
                if (!SkateRider.TryCreateIndependentDeck(Rig, input, SkateOldschoolRide.State, new SkateVector(goal.x, goal.y, goal.z),
                    SkateOldschoolRide.HandFollow, SkateOldschoolRide.HandLeft, out freePose, out independent, out error))
                { SkateRide.Bail(); return; }
                pose = freePose;
            }
            if (!SkateActions.Pose(Rig, input, ref pose)) return;
            var dropping = false;
            if (!wall && !bodyFlip && !SkateActions.Owns && !SkateOldschoolRide.Active && !groundGrab &&
                handplant <= .001f && mode == RideMode.Ground && SkateSurfaceRide.DropInMotion.Active)
            {
                SkateRiderPose entryPose;
                if (SkateRider.TryCreateDropIn(Rig, input, pose, SkateSurfaceRide.DropInMotion, out entryPose, out error))
                { pose = entryPose; dropping = true; }
                else if (error != Error) { Error = error; Say("drop-in pose refused: " + error); }
            }
            Posed = pose; PosedPlane = plane;
            SkateActions.Record(pose, SkateActions.DisplayFrame.Initialized ? SkateActions.DisplayFrame : input.DeckFrame, SkateRig.LocalT != null ? SkateBoard.CarrierFor(SkateRig.LocalT) : input.DeckFrame, grab);
            var hardContact = wall || bodyFlip || SkateOldschoolRide.Active || SkateActions.Owns || handplant > 0.001f || mode == RideMode.Bail || groundGrab || dropping;
            // Between the air and the ground the lag carries on, so the chest and the hands follow
            // through the catch into the landing instead of jumping onto the pose; into and out of a
            // grind or a bail the motion starts over.
            var flowing = (mode == RideMode.Air || mode == RideMode.Ground) && (motionMode == RideMode.Air || motionMode == RideMode.Ground);
            if (hardContact || (mode != motionMode && !flowing)) motion = default(SkateRiderMotionState);
            motionMode = mode;
            bool pinL = false, pinR = false;
            if (plantBoard.Initialized || SkateActions.Owns || SkateOldschoolRide.Active) pinL = pinR = true;
            if (groundGrab)
            {
                var groundKind = SkateRide.GroundGrab.Kind;
                if (groundKind == SkateGroundGrabKind.Coffin || groundKind == SkateGroundGrabKind.Double) pinL = pinR = true;
                else SkateRider.GrabHands(input.Stance, input.Switch, groundKind == SkateGroundGrabKind.Frontside ? SkateGrabKind.Frontside : SkateGrabKind.Backside, out pinL, out pinR);
            }
            if (!hardContact)
            {
                var airborne = mode == RideMode.Air;
                // The authored gripping hand stays the same through entry and release; a blended
                // target's distance from the deck must not choose a different hand mid-transition.
                if (airborne && grab != SkateGrabKind.None)
                    SkateRider.GrabHands(input.Stance, input.Switch, grab, out pinL, out pinR);
                var options = new SkateRiderMotionOptions(airborne, pinL, pinR);
                SkateRiderPose shown; SkateRiderMotionState next; string motionError;
                var motionDelta = float.IsNaN(frameSeconds) || float.IsInfinity(frameSeconds)
                    ? (double)frameSeconds
                    : Math.Min(SkateRiderMotion.MaximumDelta, Math.Max(SkateRiderMotion.MinimumDelta, (double)frameSeconds));
                if (SkateRiderMotion.TryStep(motion, Rig, pose, motionDelta, options, out shown, out next, out motionError))
                { pose = shown; motion = next; }
                else { motion = default(SkateRiderMotionState); if (motionError != MotionError) { MotionError = motionError; Say("secondary motion refused: " + motionError); } }
            }
            var ordinaryLook = (mode == RideMode.Ground || mode == RideMode.Air) && !wall && !bodyFlip &&
                !SkateActions.Owns && !SkateOldschoolRide.Active && !groundGrab && handplant <= .001f && !dropping;
            var landingLook = SkateRide.ActionVelocity;
            if (SkateRide.OnRamp && landingLook.y < 0)
                landingLook -= SkateRide.BelowNormal * Vector3.Dot(landingLook, SkateRide.BelowNormal);
            var lookWant = ordinaryLook && SkateRide.OnRamp ? SkateRide.Clamp(-Vector3.Dot(landingLook, V(input.BoardForward)) / 2f, 0, 1) : 0;
            travelLookWeight = ordinaryLook ? travelLookWeight + SkateRide.Clamp(lookWant - travelLookWeight, -6 * dt, 6 * dt) : 0;
            SkateRiderPose looked;
            if (ordinaryLook && mode == RideMode.Air && SkateRider.TryReferenceGrabLook(Rig, input, pose, V(landingLook), out looked))
            { pose = looked; motionLookFrame = true; }
            if (ordinaryLook && travelLookWeight > 0 && SkateRider.TryTravelLook(Rig, input, pose, V(landingLook), travelLookWeight, out looked))
            { pose = looked; motionLookFrame = true; }
            if (ordinaryLook && mode == RideMode.Air && grab == SkateGrabKind.None && !SkateRide.FootPlanted)
            {
                if (SkateRider.TryAirLook(Rig, input, pose, V(landingLook), Math.Min(1, SkateRide.AirTime / .12f), out looked))
                { pose = looked; airLookFrame = true; }
            }
            SkateSurfaceRide.Record(pose, input.DeckFrame, wall || handplant > .001f ? SkateGrabKind.None : SurfaceGrab(pose, input, grab));
            // In a handplant the board is against the feet, held: the board follows the pose, from
            // the first of the way up to the last of the way down.
            SkateBoard.Held = handplant > 0.001f;
            if (SkateBoard.Held && plantBoard.Initialized)
            {
                SkateBoard.HeldFrame = plantBoard;
                if (handplant >= .95 && plantStyle.Weight >= .99) SkateRide.HandplantVariantUsed = plantStyle.Kind;
            }
            if (SkateBoard.Held)
            {
                // Under the feet, its top toward them: flat on the ground while they are still on it
                // there, turning with them as they lift and fold in.
                Vector3 la = V(pose.LeftAnkle), ra = V(pose.RightAnkle), pel = V(pose.Pelvis);
                var mid = (la + ra) * 0.5f;
                var lifted = SkateRide.Clamp(Vector3.Dot(mid - plane, upV) / 0.15f, 0f, 1f);
                var grip = Vector3.Lerp(upV, (pel - mid).normalized, lifted).normalized;
                // The left ankle is toward the nose for a regular rider, the right one for a goofy one.
                SkateBoard.HeldNose = (la - ra).normalized * (SkateRide.TailFirst ? -1f : 1f) * SkateRide.StanceSign;
                SkateBoard.HeldUp = grip;
                SkateBoard.HeldAt = mid - grip * (AnkleHeight + DeckTop);
            }
            // The pushing foot, while it is down: the trailing foot, or the leading one (a mongo
            // push, or any push rolling tail first in the same stance). Which foot trails follows
            // the stance and which end leads, as the module has it.
            var footDown = mode == RideMode.Ground && SkateRide.PushPhase > (float)SkateRider.PushDown && SkateRide.PushPhase < (float)SkateRider.PushUp;
            var trailingIsLeft = SkateRide.Goofy != SkateRide.TailFirst;
            var pushLeft = SkateRide.PushLead ? !trailingIsLeft : trailingIsLeft;
            // A gripping hand must remain reachable from the solved upper body. Its shared
            // secondary motion is already contact-aware; another independent trunk lag is not.
            pinHips = wall || bodyFlip || SkateOldschoolRide.Active || SkateActions.Owns || plantBoard.Initialized || groundGrab || dropping || pushing || pinL || pinR || SkateRide.BoardPath.Known;
            pinLeft = wall || bodyFlip || SkateOldschoolRide.Active || SkateActions.Owns || plantBoard.Initialized || groundGrab || dropping || SkateRide.BoardPath.Known || pushing && (!pushLeft || footDown); pinRight = wall || bodyFlip || SkateOldschoolRide.Active || SkateActions.Owns || plantBoard.Initialized || groundGrab || dropping || SkateRide.BoardPath.Known || pushing && (pushLeft || footDown);
            // The trunk keeps the support frame; the soles meet the pitched deck. Only the
            // pushing foot rolls onto the ground plane during its existing contact window.
            var groundContact = pushing ? (float)SkateRider.PushContact(input.PushPhase) : 0f;
            var footTravel = input.DeckPitchFrame ? deckFace : new Vector3((float)fx, (float)fy, (float)fz) * (input.Switch ? -1f : 1f);
            var leftPitch = mode == RideMode.Bail ? 0 : input.DeckPitch * (pushing && pushLeft ? 1f - groundContact : 1f);
            var rightPitch = mode == RideMode.Bail ? 0 : input.DeckPitch * (pushing && !pushLeft ? 1f - groundContact : 1f);
            var leftSole = upV * (float)Math.Cos(leftPitch) - footTravel * (float)Math.Sin(leftPitch);
            var rightSole = upV * (float)Math.Cos(rightPitch) - footTravel * (float)Math.Sin(rightPitch);
            if (input.DeckPitchFrame && Math.Abs(input.DeckRoll) > 0.0001)
            {
                var axis = footTravel * (float)Math.Cos(input.DeckPitch) + upV * (float)Math.Sin(input.DeckPitch);
                var roll = SkateRide.Turn(-(float)(input.DeckRoll * 180 / Math.PI), axis);
                leftSole = roll * leftSole; rightSole = roll * rightSole;
            }
            if (input.DeckFrame.Initialized)
            {
                var sole = SkateRide.BoardPath.Active ? upV : V(input.DeckFrame.Up);
                var lc = mode == RideMode.Air ? (SkateRide.Goofy ? contact.BackFootContact : contact.FrontFootContact) : 1;
                var rc = mode == RideMode.Air ? (SkateRide.Goofy ? contact.FrontFootContact : contact.BackFootContact) : 1;
                if (pushing) { if (pushLeft) lc *= 1 - groundContact; else rc *= 1 - groundContact; }
                leftSole = Vector3.Lerp(upV, sole, (float)lc).normalized;
                rightSole = Vector3.Lerp(upV, sole, (float)rc).normalized;
            }
            else if (mode == RideMode.Air && grab != SkateGrabKind.None)
            {
                var definition = SkateGrabCatalog.Get(grab); var tweak = SkateBoard.GrabTweak(upV);
                if (!(input.Stance == SkateStance.Regular ? definition.FrontReleased : definition.BackReleased)) leftSole = tweak * leftSole;
                if (!(input.Stance == SkateStance.Regular ? definition.BackReleased : definition.FrontReleased)) rightSole = tweak * rightSole;
            }
            if (plantBoard.Initialized)
            {
                var profile = SkateHandplants.Get(plantStyle.Kind); var deckUp = V(plantBoard.Up);
                bool leftReleased = SkateRide.Goofy ? profile.BackFoot.LengthSquared > 0 : profile.FrontFoot.LengthSquared > 0;
                bool rightReleased = SkateRide.Goofy ? profile.FrontFoot.LengthSquared > 0 : profile.BackFoot.LengthSquared > 0;
                leftSole = Vector3.Lerp(deckUp, upV, leftReleased ? (float)plantStyle.Weight : 0).normalized;
                rightSole = Vector3.Lerp(deckUp, upV, rightReleased ? (float)plantStyle.Weight : 0).normalized;
            }
            if (SkateActions.DisplayFrame.Initialized)
            {
                var actionUp = V(SkateActions.DisplayFrame.Up);
                double lc = 1, rc = 1;
                if (SkateActions.Finger.Initialized) lc = rc = SkateActions.FingerSample.FrontContact;
                else { lc = SkateRide.Goofy ? SkateActions.Style.BackContact : SkateActions.Style.FrontContact; rc = SkateRide.Goofy ? SkateActions.Style.FrontContact : SkateActions.Style.BackContact; }
                leftSole = Vector3.Lerp(upV, actionUp, (float)lc).normalized; rightSole = Vector3.Lerp(upV, actionUp, (float)rc).normalized;
            }
            if (SkateOldschoolRide.Active)
            {
                var deckUp = V(SkateOldschoolRide.Frame.Up);
                leftSole = rightSole = Vector3.Lerp(upV, deckUp, SkateOldschoolRide.FootContact).normalized;
            }
            if (wall) leftSole = rightSole = V(wallDeck.Up);
            SkateRiderPose plantedPose; SkateVector plantedLeftUp, plantedRightUp; SkateCaptureContacts plantedContacts;
            if (SkateCapturedTricks.TryGroundedFooting(Rig, SkateRig.LocalT, pose, V(leftSole), V(rightSole), out plantedPose, out plantedLeftUp, out plantedRightUp, out plantedContacts))
            {
                pose = plantedPose; leftSole = V(plantedLeftUp); rightSole = V(plantedRightUp); pinHips = true;
                pinLeft |= (plantedContacts & SkateCaptureContacts.LeftFoot) != 0;
                pinRight |= (plantedContacts & SkateCaptureContacts.RightFoot) != 0;
            }
            FitProceduralFooting(ref pose, leftSole, rightSole);
            // Neutral loading has no trick-family foreknowledge. Its observed
            // charge survives only into this exact pop's short torso handoff.
            double chargeFold = ordinaryCharging ? chargePoseAmount :
                mode == RideMode.Air && SkateRide.Jumped && chargeRoute
                ? ChargePoseSeed * (1 - SkateCaptureFootSupport.CatchPreparationWeight(SkateRide.AirTime, .08)) : 0;
            if (chargeFold > 0)
            {
                SkateRiderPose charged; string chargeError;
                var chargeCarrier = new SkateBoardPose(new SkateVector(plane.x, plane.y, plane.z),
                    new SkateVector(fx, fy, fz), new SkateVector(ux, uy, uz));
                if (ordinaryCharging && SkateFootContacts.Ready)
                {
                    SkateVector chargeLeftUp, chargeRightUp;
                    if (!chargeInitialPelvisOffset.HasValue)
                        chargeInitialPelvisOffset = SkateVector.Dot(pose.Pelvis - chargeCarrier.Origin, chargeCarrier.Up);
                    if (SkateCaptureKickflipUpper.TryChargeSetup(Rig, pose, chargeCarrier,
                        SkateBoard.PoseFor(SkateRig.LocalT), chargeFold, chargeNosePop,
                        SkateFootContacts.LeftFitter, SkateFootContacts.RightFitter,
                        out charged, out chargeLeftUp, out chargeRightUp, out chargeError, chargeInitialPelvisOffset))
                    {
                        Posed = pose = charged; leftSole = V(chargeLeftUp); rightSole = V(chargeRightUp);
                        pinHips = pinLeft = pinRight = chargeSetupGrounded = true; FootSupportError = "";
                        var entryCarrier = ChargeCarrier();
                        chargeEntry = new SkateCaptureFrame(charged, entryCarrier, default(SkateCaptureRotation),
                            chargeLeftUp, chargeRightUp, false, default(SkateCaptureEvent), default(SkateVector), default(SkateVector), default(SkateVector));
                        chargeEntryCarrier = entryCarrier; chargeEntryPlayer = SkateRig.LocalT;
                        chargeEntryGoofy = SkateRide.Goofy; chargeEntryPop = chargePosePop; chargeEntryEpoch = chargePoseEpoch;
                    }
                    else FootSupportError = "Charge setup: " + chargeError;
                }
                else if (SkateCaptureKickflipUpper.TryChargeFold(pose, chargeCarrier, chargeFold, out charged, out chargeError))
                    Posed = pose = charged;
                else FootSupportError = "Charge torso: " + chargeError;
            }
            chargeEntryShown = ApplyChargeEntry(chargeRoute, ordinaryCharging, ref pose, ref leftSole, ref rightSole);
            if (chargeEntryShown) { Posed = pose; pinHips = pinLeft = pinRight = true; }
            var kickturnPosed = ApplyKickturnPose(ref pose, ref leftSole, ref rightSole);
            if (kickturnPosed) { Posed = pose; pinHips = pinLeft = pinRight = true; }
            Shown = pose;
            Ease(pose, plane, toToes, upV, new Vector3((float)fx, (float)fy, (float)fz), now, frameSeconds, pinL, pinR, chargeSetupGrounded || chargeEntryShown || kickturnPosed);
            if (!chargeSetupGrounded && !chargeEntryShown && !wall && !bodyFlip && !SkateActions.Owns && !SkateOldschoolRide.Active && !dropping) Shape(upV);
            // Pitch/grind balance shapes the free arm, never the hand already holding the board.
            if (pinL) { joint[LHandJ] = V(pose.LeftHand); joint[LElbowJ] = V(pose.LeftElbow); }
            if (pinR) { joint[RHandJ] = V(pose.RightHand); joint[RElbowJ] = V(pose.RightElbow); }
            // Ease and Shape rebuild the head from the chest; restore the independent look afterward.
            if (motionLookFrame || airLookFrame)
            {
                joint[HeadJ] = joint[NeckJ] + V(pose.Head - pose.Neck).normalized * (float)Rig.NeckToHeadLength;
                joint[HeadForwardJ] = V(pose.HeadForward);
            }
            if (pinL && !pinR) ReachFreeHand(RShoulderJ, RHandJ);
            if (pinR && !pinL) ReachFreeHand(LShoulderJ, LHandJ);
            if (bodyFlip)
            {
                // All support/contact/easing work stays in the unflipped measured frame.
                // The same final rigid map moves the board, every joint and both soles once.
                for (int i = 0; i < Points; i++) joint[i] = SkateSurfaceRide.FlipPoint(joint[i]);
                for (int i = Points; i < All; i++) joint[i] = SkateSurfaceRide.FlipDirection(joint[i]);
                upV = SkateSurfaceRide.FlipDirection(upV);
                leftSole = SkateSurfaceRide.FlipDirection(leftSole); rightSole = SkateSurfaceRide.FlipDirection(rightSole);
                SkateRiderPose flipped;
                if (SkateRider.TryTransformPose(Shown, input.DeckFrame, SkateSurfaceRide.FlipFrame(input.DeckFrame), out flipped)) Shown = flipped;
            }
            // Preserve procedural loading. A known approved capture crossfades only after the live pop,
            // using this frame's measured procedural pose as the transition fallback.
            SkateCaptureFrame captured;
            if (SkateCapturedTricks.TryFrame(Rig, SkateRig.LocalT, pose, new SkateVector(leftSole.x, leftSole.y, leftSole.z), new SkateVector(rightSole.x, rightSole.y, rightSole.z), out captured))
            {
                airLookFrame = false;
                Posed = Shown = captured.Pose; PosedPlane = V(captured.Board.Origin); Error = "";
                pinHips = pinLeft = pinRight = true; motion = default(SkateRiderMotionState);
                // Keep the SAME local basis as the normal route, including on capture exit mid-flip.
                Ease(captured.Pose, plane, toToes, upV, new Vector3((float)fx, (float)fy, (float)fz),
                    now, frameSeconds, true, true, true);
                upV = V(captured.RiderUp); leftSole = V(captured.LeftSoleUp); rightSole = V(captured.RightSoleUp);
                SkateSurfaceRide.Record(captured.Pose, captured.Board, SkateGrabKind.None);
            }
            if (!wall && !bodyFlip && !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateCapturedTricks.HasFrame)
            {
                SkateRider.TryGrabHandFrame(input, Shown, true, out lGrabFrame, out lGrabWeight);
                SkateRider.TryGrabHandFrame(input, Shown, false, out rGrabFrame, out rGrabWeight);
            }
            Apply(upV, legsOnly, leftSole, rightSole);
            Applied++;
        }
        catch (Exception e)
        {
            RestoreGrabFingers(lGrabFingers); RestoreGrabFingers(rGrabFingers);
            RestoreGrabHand(LHand, ref lGrabHand); RestoreGrabHand(RHand, ref rGrabHand);
            failed = true; Say("pose threw " + e.GetType().Name + ": " + e.Message);
        }
    }

    // Reuse the measured production solver and the actual bone writer. The preview owns only
    // rendered joints and board frame, with no gameplay rotations, inputs or score side effects.
    private static void FramePreview(float frameSeconds)
    {
        SkateRiderPose pose; SkateBoardPose board; SkateVector riderUp, leftSole, rightSole; string error;
        if (!SkateRiggingShowcase.TryCurrent(Rig, out pose, out board, out riderUp, out leftSole, out rightSole, out error))
        { Refusals++; Error = error; SkateRiggingShowcase.ReportPoseFailure(error); return; }
        Posed = Shown = pose; PosedPlane = V(board.Origin); Error = "";
        pinHips = pinLeft = pinRight = true; motion = default(SkateRiderMotionState);
        Ease(pose, V(board.Origin), V(board.Right), V(board.Up), V(board.Forward),
            Time.realtimeSinceStartup, frameSeconds, true, true);
        Apply(V(riderUp), false, V(leftSole), V(rightSole)); Applied++;
    }

    // Shared upright poses keep their strict world-up validation. A wall is solved in its
    // own supported coordinates, then rigidly carried onto the exact observed wall plane.
    private static bool WallPose(SkateRiderInput input, out SkateRiderPose pose, out string error)
    {
        pose = default(SkateRiderPose); error = null;
        var support = new SkateBoardPose(input.BoardPosition, input.BoardForward, input.BoardUp);
        if (!SkateBoardPath.Valid(support) || !input.DeckFrame.Initialized || !SkateBoardPath.Valid(input.DeckFrame))
        { error = "The wall rider frame is invalid."; return false; }
        var localDeck = new SkateBoardPose(WallLocal(input.DeckFrame.Origin - support.Origin, support),
            WallLocal(input.DeckFrame.Forward, support), WallLocal(input.DeckFrame.Up, support));
        var look = WallLocal(new SkateVector(Math.Sin(input.LookYawDegrees * Math.PI / 180), 0,
            Math.Cos(input.LookYawDegrees * Math.PI / 180)), support);
        var lookYaw = look.X * look.X + look.Z * look.Z < .000001 ? 0 : Math.Atan2(look.X, look.Z) * 180 / Math.PI;
        var along = new SkateVector(0, 0, 1); var normal = new SkateVector(0, 1, 0);
        var canonical = new SkateRiderInput(default(SkateVector), along, normal, input.Stance, input.Switch,
            input.Speed, input.LeanDegrees, input.Crouch, input.Airborne, false, 0, 0, SkateGrabKind.None, false,
            lookYaw, input.DeckOffset, input.GroundDrop, 0, false, 0, 0, 0, 0, 0, 0, 0, input.Landing,
            default(SkateRiderContactState), input.PushSeconds, WallLocal(input.DeckForward, support), false, 0, 0,
            localDeck, default(SkateBoardPathSample));
        SkateRiderPose solved;
        if (!SkateRider.TryCreate(Rig, canonical, out solved, out error)) return false;
        if (!SkateRider.TryTransformPose(solved, new SkateBoardPose(default(SkateVector), along, normal), support, out pose))
        { error = "The measured rider could not follow the wall frame."; return false; }
        return true;
    }

    private static SkateVector WallLocal(SkateVector direction, SkateBoardPose support)
    { return new SkateVector(SkateVector.Dot(direction, support.Right), SkateVector.Dot(direction, support.Up), SkateVector.Dot(direction, support.Forward)); }

    private static SkateGrabKind SurfaceGrab(SkateRiderPose pose, SkateRiderInput input, SkateGrabKind kind)
    {
        var deck = input.DeckFrame;
        if (kind == SkateGrabKind.None || !SkateGrabCatalog.Valid(kind) || !deck.Initialized || !SkateBoardPath.Valid(deck)) return SkateGrabKind.None;
        bool left, right;
        SkateRider.GrabHands(input.Stance, input.Switch, kind, out left, out right);
        if (!left && !right) return SkateGrabKind.None;
        if (SkateGrabCatalog.HasReferenceHand(kind))
        {
            SkateVector contact;
            if (left && !SkateRider.TryReferenceGrabContact(Rig, input, pose, true, out contact) ||
                right && !SkateRider.TryReferenceGrabContact(Rig, input, pose, false, out contact)) return SkateGrabKind.None;
        }
        else if (left && !SurfaceHand(pose.LeftHand, deck) || right && !SurfaceHand(pose.RightHand, deck)) return SkateGrabKind.None;
        return kind;
    }

    private static bool SurfaceHand(SkateVector hand, SkateBoardPose deck)
    {
        var local = WallLocal(hand - deck.Origin, deck);
        return Math.Abs(local.Y) <= .025 && Math.Abs(local.X) <= .15 && Math.Abs(local.Z) <= .44 &&
            (Math.Abs(local.X) >= .07 || Math.Abs(local.Z) >= .25);
    }

    private static void Ease(SkateRiderPose p, Vector3 origin, Vector3 x, Vector3 y, Vector3 z, float now, float frameSeconds, bool pinLeftHand, bool pinRightHand, bool exactCapture = false)
    {
        joint[PelvisJ] = V(p.Pelvis); joint[LHipJ] = V(p.LeftHip); joint[RHipJ] = V(p.RightHip); joint[LKneeJ] = V(p.LeftKnee); joint[RKneeJ] = V(p.RightKnee);
        joint[LAnkleJ] = V(p.LeftAnkle); joint[RAnkleJ] = V(p.RightAnkle); joint[LToeJ] = V(p.LeftToe); joint[RToeJ] = V(p.RightToe);
        joint[ChestJ] = V(p.Chest); joint[NeckJ] = V(p.Neck); joint[HeadJ] = V(p.Head); joint[LShoulderJ] = V(p.LeftShoulder); joint[RShoulderJ] = V(p.RightShoulder);
        joint[LElbowJ] = V(p.LeftElbow); joint[RElbowJ] = V(p.RightElbow); joint[LHandJ] = V(p.LeftHand); joint[RHandJ] = V(p.RightHand);
        joint[PelvisForwardJ] = V(p.PelvisForward); joint[PelvisUpJ] = V(p.PelvisUp); joint[ChestForwardJ] = V(p.ChestForward); joint[ChestUpJ] = V(p.ChestUp); joint[HeadForwardJ] = V(p.HeadForward);
        // After a pause (getting on, a new player model) there is nothing to ease from.
        var share = now - easedAt > 0.25f ? 1f : 1f - (float)Math.Exp(-frameSeconds / EaseSeconds);
        easedAt = now;
        for (var i = 0; i < All; i++)
        {
            var fresh = joint[i];
            var w = i < Points ? fresh - origin : fresh;
            var local = new Vector3(Vector3.Dot(w, x), Vector3.Dot(w, y), Vector3.Dot(w, z));
            // A foot on the ground is where the module puts it, at once: eased in the board's frame
            // it would trail the board and slide along the ground.
            // The solved hips must reach a planted foot; easing them independently can break that contact.
            // The trunk goes with the hips in a push: the module turns the chest with the pelvis, and
            // a chest eased behind pinned hips would untwist the spine for a moment each stroke.
            // Keep a grab's solved hand target through the final filter too. The free hand and
            // the rest of the pose still ease; releasing starts from the last held hand and elbow targets.
            var pinned = exactCapture || SkateRiggingShowcase.Running || SkateSurfaceRide.DisplayWall || SkateSurfaceRide.BodyFlip.Known || SkateOldschoolRide.Active || SkateActions.Owns || (pinHips && (i == PelvisJ || i == LHipJ || i == RHipJ || i == PelvisForwardJ || i == PelvisUpJ ||
                    i == LShoulderJ || i == RShoulderJ || i == ChestForwardJ || i == ChestUpJ)) ||
                (pinLeft && (i == LAnkleJ || i == LToeJ)) || (pinRight && (i == RAnkleJ || i == RToeJ)) ||
                (pinLeftHand && (i == LHandJ || i == LElbowJ)) || (pinRightHand && (i == RHandJ || i == RElbowJ));
            eased[i] = pinned ? local : eased[i] + (local - eased[i]) * share;
            var back = x * eased[i].x + y * eased[i].y + z * eased[i].z;
            joint[i] = i < Points ? origin + back : back.sqrMagnitude > 0.0001f ? back.normalized : fresh;
        }
        // Halfway between two poses the back would come out shorter than it is: the chest, the neck
        // and the head are put back on the eased line of the back, at the module's distances.
        if (exactCapture) return;
        var hips = (joint[LHipJ] + joint[RHipJ]) * 0.5f; var line = joint[ChestUpJ];
        joint[ChestJ] = hips + line * (float)Rig.SpineLength;
        joint[NeckJ] = hips + line * (float)(Rig.SpineLength * 1.12);
        joint[HeadJ] = joint[NeckJ] + line * (float)Rig.NeckToHeadLength;
    }

    // The held arm keeps the solved trunk. The free hand may still lag and balance, but
    // cannot ask that shoulder to reach beyond the arm's physical length.
    private static void ReachFreeHand(int shoulder, int hand)
    {
        var offset = joint[hand] - joint[shoulder];
        var distance = offset.magnitude;
        var reach = (float)(Rig.UpperArmLength + Rig.ForearmLength) * 0.999f;
        if (distance > reach) joint[hand] = joint[shoulder] + offset * (reach / distance);
    }

    // What the module does not know: in a manual the weight goes over the axle that carries it, the
    // body back toward the tail (or forward over the nose), and the arms come out for balance.
    public const float ManualArms = 0.14f;
    // A stall is a held balance on the edge: deep in the board stall, the weight over the tail or
    // the nose in the others, and the arms out wider than in a manual.
    private static float GrindCrouch(SkateGrindKind kind)
    {
        double pitch, roll, crouch;
        SkateRider.GrindDeck(kind, out pitch, out roll, out crouch);
        return (float)crouch;
    }

    private static void Shape(Vector3 up)
    {
        // The weight over a pitched deck is the module's; here the arms come out for balance, in a
        // manual as far as it is pitched, on an edge (a grind or a stall) always.
        var tip = SkateBoard.PitchDegrees / SkateBoard.TipDegrees;
        var balance = SkateRide.Mode == RideMode.Grind && !SkateRide.Handplant;
        if (Math.Abs(tip) < 0.01f && !balance) return;
        var lead = SkateRide.Dir(SkateRide.Yaw); lead = lead - up * Vector3.Dot(lead, up);
        if (lead.sqrMagnitude < 0.0001f) return;
        lead = SkateRide.Speed >= 0f ? lead.normalized : -lead.normalized;
        var side = Vector3.Cross(up, lead);
        var amount = balance ? StallArms : Math.Min(1f, Math.Abs(tip));
        joint[LHandJ] += (-side * ManualArms + up * ManualArms * 0.8f) * amount; joint[LElbowJ] += (-side * ManualArms * 0.5f + up * ManualArms * 0.3f) * amount;
        joint[RHandJ] += (side * ManualArms + up * ManualArms * 0.8f) * amount; joint[RElbowJ] += (side * ManualArms * 0.5f + up * ManualArms * 0.3f) * amount;
    }
    public const float StallArms = 1.6f;

    private static void Apply(Vector3 up, bool legsOnly, Vector3 leftSole, Vector3 rightSole)
    {
        var hipsNow = RHip.position - LHip.position; var upNow = spine[0].position - Pelvis.position;
        if (hipsNow.sqrMagnitude > 0.000001f && upNow.sqrMagnitude > 0.000001f)
        {
            var from = Quaternion.LookRotation(Vector3.Cross(hipsNow.normalized, upNow.normalized), upNow.normalized);
            var to = Quaternion.LookRotation(joint[PelvisForwardJ], joint[PelvisUpJ]);
            Pelvis.rotation = to * Quaternion.Inverse(from) * Pelvis.rotation;
        }
        Pelvis.position = Pelvis.position + ((joint[LHipJ] + joint[RHipJ]) * 0.5f - (LHip.position + RHip.position) * 0.5f);

        Miss = 0f; MissAt = "";
        if (!legsOnly)
        {
            var chestUp = joint[ChestUpJ];
            // The module folds the body at the hip joints; the skeleton's spine starts above them, on
            // the pelvis bone. So the pelvis tips about the line through the hips until the spine's
            // root is where the module's straight back passes, or the neck would be out of reach.
            var hipLine = RHip.position - LHip.position;
            var hips = (LHip.position + RHip.position) * 0.5f;
            if (hipLine.sqrMagnitude > 0.000001f)
            {
                hipLine = hipLine.normalized;
                var now = spine[0].position - hips; now = now - hipLine * Vector3.Dot(now, hipLine);
                var want = chestUp - hipLine * Vector3.Dot(chestUp, hipLine);
                if (now.sqrMagnitude > 0.000001f && want.sqrMagnitude > 0.000001f)
                {
                    var tip = Quaternion.FromToRotation(now, want);
                    Pelvis.rotation = tip * Pelvis.rotation;
                    Pelvis.position = hips + tip * (Pelvis.position - hips);
                }
                // A side bend (the chest rolled toward a grabbing hand) is taken at the spine's root as
                // well, a share of it, so the neck still lands where the module bends the back to.
                var side = Vector3.Dot(chestUp, hipLine);
                if (Math.Abs(side) > 0.05f)
                {
                    var rootNow = spine[0].position - hips;
                    if (rootNow.sqrMagnitude > 0.000001f)
                    {
                        var bend = Quaternion.FromToRotation(rootNow, rootNow.magnitude * chestUp.normalized);
                        bend = Quaternion.Slerp(Quaternion.identity, bend, SideBendShare);
                        Pelvis.rotation = bend * Pelvis.rotation;
                        Pelvis.position = hips + bend * (Pelvis.position - hips);
                    }
                }
            }
            for (var i = 0; i < spine.Length; i++) Aim(spine[i], i + 1 < spine.Length ? spine[i + 1] : Neck, chestUp);
            // The module turns the chest further toward travel than the pelvis: the spine twists,
            // bone by bone, until the shoulders are across the module's.
            var across = RUpper.position - LUpper.position; across = across - chestUp * Vector3.Dot(across, chestUp);
            var wanted = joint[RShoulderJ] - joint[LShoulderJ]; wanted = wanted - chestUp * Vector3.Dot(wanted, chestUp);
            if (across.sqrMagnitude > 0.0001f && wanted.sqrMagnitude > 0.0001f)
            {
                var twist = (float)(Math.Atan2(Vector3.Dot(Vector3.Cross(across, wanted), chestUp), Vector3.Dot(across, wanted)) * 180.0 / Math.PI);
                for (var i = 0; i < spine.Length; i++) spine[i].rotation = SkateRide.Turn(twist / spine.Length, chestUp) * spine[i].rotation;
            }
            Aim(spine[0], Neck, joint[NeckJ] - spine[0].position);
            Aim(Neck, Head, joint[HeadJ] - joint[NeckJ]);
            var chestForward = joint[ChestForwardJ]; var headForward = joint[HeadForwardJ];
            var lookUp = airLookFrame || motionLookFrame ? (joint[HeadJ] - joint[NeckJ]).normalized : chestUp;
            var cf = chestForward - lookUp * Vector3.Dot(chestForward, lookUp); var hf = headForward - lookUp * Vector3.Dot(headForward, lookUp);
            if (cf.sqrMagnitude > 0.0001f && hf.sqrMagnitude > 0.0001f)
            {
                cf = cf.normalized; hf = hf.normalized;
                var turn = (float)(Math.Atan2(Vector3.Dot(Vector3.Cross(cf, hf), lookUp), Vector3.Dot(cf, hf)) * 180.0 / Math.PI);
                turn = SkateRide.Clamp(turn, -75f, 75f);
                Neck.rotation = SkateRide.Turn(turn * 0.4f, lookUp) * Neck.rotation;
                Head.rotation = SkateRide.Turn(turn * 0.6f, lookUp) * Head.rotation;
            }
            // With the chest folded far forward the rider would look at the board; the neck and the
            // head lift most of the way back toward where the board's up is.
            var foldAxis = Vector3.Cross(chestUp, up);
            var fold = (float)(Math.Atan2(foldAxis.magnitude, Vector3.Dot(chestUp, up)) * 180.0 / Math.PI);
            if (!chargeSetupGrounded && !chargeEntryShown && !SkateCapturedTricks.HasFrame && !motionLookFrame && !airLookFrame && fold > HeadLiftFrom && foldAxis.sqrMagnitude > 0.0001f)
            {
                var lift = SkateRide.Clamp((fold - HeadLiftFrom) * HeadLiftShare, 0f, HeadLiftMost);
                Neck.rotation = SkateRide.Turn(lift * 0.35f, foldAxis) * Neck.rotation;
                Head.rotation = SkateRide.Turn(lift * 0.65f, foldAxis) * Head.rotation;
            }
            Note(Neck, joint[NeckJ], "neck");
            Limb(LUpper, LFore, LHand, joint[LHandJ], joint[LElbowJ], UpperArm, Forearm, "left hand");
            Limb(RUpper, RFore, RHand, joint[RHandJ], joint[RElbowJ], UpperArm, Forearm, "right hand");
        }
        ApplyGrabHand(LHand, ref lGrabHand, lGrabFrame, legsOnly ? 0 : lGrabWeight);
        ApplyGrabHand(RHand, ref rGrabHand, rGrabFrame, legsOnly ? 0 : rGrabWeight);
        ApplyGrabFingers(LHand, lGrabHand, lGrabFingers, lGrabFrame, legsOnly ? 0 : lGrabWeight);
        ApplyGrabFingers(RHand, rGrabHand, rGrabFingers, rGrabFrame, legsOnly ? 0 : rGrabWeight);

        if (!chargeSetupGrounded && !chargeEntryShown && !SkateCapturedTricks.HasFrame && OrdinarySupportedFeet()) FitEasedFooting(leftSole, rightSole);
        Limb(LHip, LKnee, LFoot, joint[LAnkleJ], joint[LKneeJ], Thigh, Shin, "left ankle");
        Limb(RHip, RKnee, RFoot, joint[RAnkleJ], joint[RKneeJ], Thigh, Shin, "right ankle");
        var soleL = SkateBoard.Held ? SkateBoard.HeldUp : leftSole;
        var soleR = SkateBoard.Held ? SkateBoard.HeldUp : rightSole;
        Aim(LFoot, LToe, joint[LToeJ] - soleL * (AnkleHeight - ToeHeight) - LFoot.position);
        Aim(RFoot, RToe, joint[RToeJ] - soleR * (AnkleHeight - ToeHeight) - RFoot.position);
        // The feet lie flat on the deck: with the board held against them, their tops face the pelvis.
        Roll(LFoot, (LToe.position - LFoot.position).normalized, lSole, soleL);
        Roll(RFoot, (RToe.position - RFoot.position).normalized, rSole, soleR);
        SkateFootContacts.ObserveAfterApply(third ? body : legSet, LFoot, LToe, RFoot, RToe,
            soleL, soleR, OrdinaryFooting(false) && !SkateCapturedTricks.HasFrame);
        if (OrdinaryFooting(false) && !SkateCapturedTricks.HasFrame)
            SkateCapturedTricks.WarmFootSupport(Rig, SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular);
        string footStatus = SkateFootContacts.Status;
        if (footStatus != reportedFootStatus || FootSupportError != reportedFootFit)
        {
            reportedFootStatus = footStatus; reportedFootFit = FootSupportError;
            Say("feet: " + footStatus + (string.IsNullOrEmpty(FootSupportError) ? "" : " | fit refused: " + FootSupportError));
        }
        HeadPosed = Head.position;
    }

    private static bool ApplyKickturnPose(ref SkateRiderPose pose, ref Vector3 leftUp, ref Vector3 rightUp)
    {
        if (!SkateRide.Kickturning || SkateRide.KickturnSetup <= 0) return false;
        Vector3 axle; RaycastHit ground; string error = "Kickturn measured support is unavailable.";
        SkateRiderPose prepared; SkateVector lu, ru;
        if (SkateRig.LocalT != null && SkateFootContacts.Ready &&
            SkateRide.TryObservedGround(SkateRide.Position, out ground) && ground.m_Normal.y >= .9995f &&
            SkateBoard.TryKickturnAxle(SkateRide.KickturnSide, out axle))
        {
            var deck = SkateBoard.PoseFor(SkateRig.LocalT);
            var pivot = deck.Point(new SkateVector(axle.x, axle.y - DeckTop, axle.z));
            if (SkateKickturnPose.TryApply(Rig, pose, deck, V(ground.m_Normal), pivot, SkateRide.KickturnSide,
                SkateRide.KickturnSetup, V(leftUp), V(rightUp), SkateFootContacts.LeftFitter, SkateFootContacts.RightFitter,
                out prepared, out lu, out ru, out error))
            {
                pose = prepared; leftUp = V(lu); rightUp = V(ru); FootSupportError = "";
                SkateRide.ConfirmKickturnPose(SkateRig.LocalT, SkateRide.KickturnSide, SkateKeys.InputEpoch, SkateFootContacts.Version);
                return true;
            }
        }
        FootSupportError = error; SkateRide.RefuseKickturnPose(); return false;
    }

    // Calibration is per visible shoe mesh. Carry that same model adaptation
    // through flat-ground flight; removing it at takeoff would create a jump.
    private static void FitProceduralFooting(ref SkateRiderPose pose, Vector3 leftUp, Vector3 rightUp)
    {
        if (calibratedFootVersion != SkateFootContacts.Version)
        { footCalibrated = false; calibratedFootVersion = SkateFootContacts.Version; }
        if (!SkateFootContacts.Ready || !OrdinaryFooting(true)) { footCalibrated = false; return; }
        SkateBoardPose lf, rf;
        if (!SkateCaptureFootSupport.TryFootFrame(pose.LeftAnkle, pose.LeftToe, V(leftUp), out lf) ||
            !SkateCaptureFootSupport.TryFootFrame(pose.RightAnkle, pose.RightToe, V(rightUp), out rf)) return;
        var l = leftFootCalibration; var r = rightFootCalibration;
        string error; double lo, ro;
        if (OrdinarySupportedFeet())
        {
            var board = SkateBoard.PoseFor(SkateRig.LocalT);
            if (!SkateFootContacts.LeftFitter.TryOffset(board, lf, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out lo, out error) ||
                !SkateFootContacts.RightFitter.TryOffset(board, rf, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out ro, out error))
            { FootSupportError = error; return; }
            l = FootLocal(board.Up * (lo + SkateCaptureFootSupport.ContactBand), lf);
            r = FootLocal(board.Up * (ro + SkateCaptureFootSupport.ContactBand), rf);
        }
        else if (!footCalibrated) return;
        SkateRiderPose fitted;
        if (!SkateCaptureFootSupport.TryShiftFeet(Rig, pose, lf.Rotate(l), rf.Rotate(r), false, out fitted, out error))
        { FootSupportError = error; return; }
        pose = fitted; leftFootCalibration = l; rightFootCalibration = r; footCalibrated = true; FootSupportError = "";
    }

    private static SkateVector FootLocal(SkateVector v, SkateBoardPose f)
    { return new SkateVector(SkateVector.Dot(v, f.Right), SkateVector.Dot(v, f.Up), SkateVector.Dot(v, f.Forward)); }

    // Takeoff can enter Air before either sole has released. Reusing the last
    // flat-ground calibration there ignores the board's new pop pitch. Refit
    // those still-supported soles; free feet only carry their calibration.
    private static bool OrdinarySupportedFeet()
    {
        return OrdinaryFooting(true) && (SkateRide.Mode == RideMode.Ground ||
            contact.Initialized && contact.FrontFootContact >= .999999 && contact.BackFootContact >= .999999);
    }

    private static void FitEasedFooting(Vector3 leftUp, Vector3 rightUp)
    {
        if (!SkateFootContacts.Ready) return;
        SkateBoardPose lf, rf; double lo, ro; string error;
        if (!SkateCaptureFootSupport.TryFootFrame(V(joint[LAnkleJ]), V(joint[LToeJ]), V(leftUp), out lf) ||
            !SkateCaptureFootSupport.TryFootFrame(V(joint[RAnkleJ]), V(joint[RToeJ]), V(rightUp), out rf)) return;
        var board = SkateBoard.PoseFor(SkateRig.LocalT);
        if (!SkateFootContacts.LeftFitter.TryOffset(board, lf, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out lo, out error) ||
            !SkateFootContacts.RightFitter.TryOffset(board, rf, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out ro, out error))
        { FootSupportError = error; return; }
        var ls = board.Up * (lo + SkateCaptureFootSupport.ContactBand); var rs = board.Up * (ro + SkateCaptureFootSupport.ContactBand);
        var la = V(joint[LAnkleJ]) + ls; var ra = V(joint[RAnkleJ]) + rs; SkateVector lk, rk;
        // Bend() clamps near a straight knee. Refuse that range explicitly here.
        if ((la - V(LHip.position)).Length >= (Thigh + Shin) * .999 ||
            (ra - V(RHip.position)).Length >= (Thigh + Shin) * .999 ||
            (la - V(LHip.position)).Length <= Math.Abs(Thigh - Shin) * 1.001 + .001 ||
            (ra - V(RHip.position)).Length <= Math.Abs(Thigh - Shin) * 1.001 + .001 ||
            !SkateOllieStyles.TryLimb(V(LHip.position), la, Thigh, Shin, V(joint[LKneeJ] - LHip.position), out lk, out error) ||
            !SkateOllieStyles.TryLimb(V(RHip.position), ra, Thigh, Shin, V(joint[RKneeJ] - RHip.position), out rk, out error))
        { FootSupportError = "Eased shaped-deck support exceeds measured leg reach"; return; }
        joint[LAnkleJ] = V(la); joint[RAnkleJ] = V(ra); joint[LToeJ] += V(ls); joint[RToeJ] += V(rs);
        joint[LKneeJ] = V(lk); joint[RKneeJ] = V(rk); FootSupportError = "";
    }

    private static void ClearChargeEntry()
    { chargeEntry = null; chargeEntryPlayer = null; chargeEntryPop = -1; chargeEntryEpoch = 0; }

    // Same physical upright carrier as capture entry; visual board pitch/roll is not a foot anchor.
    private static SkateBoardPose ChargeCarrier()
    {
        var up = SkateRide.ViewNormal;
        return new SkateBoardPose(V(SkateBoard.Contact(SkateRig.LocalT) - Vector3.up * SkateBoard.Pop),
            V(SkateBoard.OnFace(up, SkateRide.Dir(SkateRide.NoseYaw))), V(up));
    }

    private static bool ApplyChargeEntry(bool route, bool charging, ref SkateRiderPose pose, ref Vector3 left, ref Vector3 right)
    {
        if (!chargeEntry.HasValue) return false;
        var mode = SkateRide.Mode;
        bool queued = mode == RideMode.Ground && (SkateKeys.JumpPressed || SkateRide.PresentationQueued) && chargeEntryPop == SkateRide.Pops + 1;
        bool air = mode == RideMode.Air && SkateRide.Jumped && chargeEntryPop == SkateRide.Pops;
        if (!route || chargeEntryPlayer != SkateRig.LocalT || chargeEntryGoofy != SkateRide.Goofy ||
            chargeEntryEpoch != SkateKeys.InputEpoch || !(charging || queued || air))
        { ClearChargeEntry(); return false; }
        if (charging) return false;
        double elapsed = air ? Math.Max(0, Time.realtimeSinceStartup - SkateRide.ModeAt) : 0;
        if (elapsed >= .1) { ClearChargeEntry(); return false; }
        var carrier = ChargeCarrier();
        var from = SkateCapturePlayback.Transport(chargeEntry.Value, chargeEntryCarrier, carrier);
        if (!from.Board.Initialized) { ClearChargeEntry(); return false; }
        double u = elapsed / .1, weight = u * u * u * (10 + u * (-15 + 6 * u));
        var target = new SkateCaptureFrame(pose, carrier, default(SkateCaptureRotation), V(left), V(right), false,
            default(SkateCaptureEvent), default(SkateVector), default(SkateVector), default(SkateVector));
        SkateCaptureFrame shown; string error;
        if (weight == 0) shown = from;
        else if (!SkateCapturePlayback.TryBlend(Rig, from.Pose, from.LeftSoleUp, from.RightSoleUp,
            target, weight, out shown, out error))
        { FootSupportError = "Charge entry: " + error; ClearChargeEntry(); return false; }
        pose = shown.Pose; left = V(shown.LeftSoleUp); right = V(shown.RightSoleUp); return true;
    }

    private static bool OrdinaryFooting(bool allowAir)
    {
        return SkateRide.On && !SkateRide.Frozen && SkateRig.LocalT != null &&
            (SkateRide.Mode == RideMode.Ground || allowAir && SkateRide.Mode == RideMode.Air) &&
            !SkateRiggingShowcase.Running && !SkateKeys.Scripted && !Scenarios.Active &&
            !SkateRide.Manual && !SkateRide.NoseManual && !SkateRide.GroundGrab.Active &&
            !SkateRide.Grabbing && SkateKeys.Grab == SkateGrabKind.None && !SkateRide.Handplant && !SkateRide.FootPlanted &&
            !SkateActions.Owns && !SkateOldschoolRide.Active && !SkateRide.BoardPath.Known && !SkateBoard.Held &&
            !SkateSurfaceRide.DisplayWall && !SkateSurfaceRide.BodyFlip.Known && SkateSurfaceRide.WallieWeight <= 0 &&
            SkateRide.ViewNormal.y >= .98f && SkateRide.Normal.y >= .98f && !SkateRide.OnRamp &&
            Math.Abs(SkateBoard.Tip) < .001f && Math.Abs(SkateBoard.GrindPitch) < .01f && Math.Abs(SkateBoard.GrindRoll) < .01f &&
            SkateRide.RevertLeft == 0 && SkateRide.Powerslide <= .001f && SkateRide.PushPhase <= .001f &&
            (SkateRide.Mode == RideMode.Air || !SkateRide.Rotating);
    }

    private static void Limb(Transform a, Transform b, Transform c, Vector3 target, Vector3 hint, float upper, float lower, string name)
    {
        var mid = Bend(a.position, target, upper, lower, hint - a.position);
        Aim(a, b, mid - a.position);
        Aim(b, c, target - b.position);
        Note(c, target, name);
    }

    private static void Note(Transform bone, Vector3 target, string name)
    {
        var miss = Vector3.Distance(bone.position, target);
        if (miss > Miss) { Miss = miss; MissAt = name; }
    }
}
