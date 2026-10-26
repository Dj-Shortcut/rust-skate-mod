using System;
using Shortcut.RustMod;
using UnityEngine;

// Owns explicit trick phases; physical body ownership stays with SkateRide.
public static class SkateActions
{
    public static SkateOllieStyleState Style;
    public static SkateFingerFlipState Finger;
    public static SkateFingerFlipSample FingerSample;
    public static SkateBoardPose DisplayFrame;
    private static SkateBoardPose lastFrame, lastCarrier;
    private static SkateRiderPose lastPose, fingerPose; private static SkateBoardPose fingerCarrier;
    private static SkateGrabKind lastGrab;
    private static float posedAt = -99f;
    private static uint inputEpoch;
    private static double rollStep;
    private static bool poseFailed, nameUsed, styleShown;
    public static bool Owns { get { return SkateRide.Mode == RideMode.Air && (Style.Known || Finger.Initialized); } }
    public static bool LandingReady { get { return !poseFailed && Style.LandingReady && Finger.LandingReady; } }
    public static void Reset()
    {
        Style = default(SkateOllieStyleState); Finger = default(SkateFingerFlipState); FingerSample = default(SkateFingerFlipSample);
        DisplayFrame = lastFrame = lastCarrier = default(SkateBoardPose); posedAt = -99f; rollStep = 0; poseFailed = nameUsed = styleShown = false; lastGrab = SkateGrabKind.None;
    }
    public static void Begin(SkateOllieStyle kind)
    {
        Reset(); inputEpoch = SkateKeys.InputEpoch;
        string error; SkateOllieStyles.TryBegin(kind, out Style, out error);
    }
    public static void Prepare() { DisplayFrame = default(SkateBoardPose); }
    public static void Tick(float dt, float remaining)
    {
        rollStep = 0;
        if (poseFailed) { SkateRide.Bail(); return; }
        var controls = (SkateKeys.Scripted || SkateKeys.ControlsActive) && inputEpoch == SkateKeys.InputEpoch;
        string error;
        if (Style.Active)
        {
            SkateOllieStyleState next;
            if (!SkateOllieStyles.TryStep(Style, controls && (SkatePad.Buttons & SkatePad.LeftStickClick) != 0, dt, out next, out error)) { SkateRide.Bail(); return; }
            Style = next;
        }
        if (SkateKeys.FingerFlip != 0 && !Style.Known && !Finger.Initialized && controls && !SkateRide.BoardPath.Known && !SkateRide.Rotating && !SkateRide.FootPlanted)
        {
            bool left, right; SkateRider.GrabHands(SkateRide.Goofy ? SkateStance.Goofy : SkateStance.Regular, SkateRide.TailFirst, lastGrab, out left, out right);
            if (left != right && RiderRig.Contact.GrabWeight >= .99 && Time.realtimeSinceStartup - posedAt < .1f && remaining >= SkateOllieStyles.FingerDuration + .04)
            {
                SkateFingerFlipState captured;
                if (SkateOllieStyles.TryBeginFinger(lastFrame, lastCarrier, left ? lastPose.LeftHand : lastPose.RightHand, left, SkateKeys.FingerFlip, out captured, out error)) { Finger = captured; fingerPose = lastPose; fingerCarrier = lastCarrier; }
            }
            else SkateHud.Notice("Finger Flip needs a held one-hand grab and more air");
        }
        if (Finger.Initialized)
        {
            if (!controls || poseFailed) { SkateRide.Bail(); return; }
            SkateFingerFlipSample before, after; SkateFingerFlipState next;
            if (!SkateOllieStyles.TrySampleFinger(Finger, lastCarrier, out before, out error) || !SkateOllieStyles.TryStepFinger(Finger, dt, out next, out error) ||
                !SkateOllieStyles.TrySampleFinger(next, lastCarrier, out after, out error)) { SkateRide.Bail(); return; }
            Finger = next; rollStep = after.RollDegrees - before.RollDegrees;
        }
    }
    public static float FlipAxis(float dt) { return SkateRide.Clamp((float)(rollStep / (SkateMotion.FlipRate * dt)), -1, 1); }
    public static void Record(SkateRiderPose pose, SkateBoardPose frame, SkateBoardPose carrier, SkateGrabKind grab)
    { lastPose = pose; lastFrame = frame; lastCarrier = carrier; lastGrab = grab; posedAt = Time.realtimeSinceStartup; }
    public static bool Pose(SkateRiderRig rig, SkateRiderInput input, ref SkateRiderPose pose)
    {
        if (!Owns) return true;
        SkateRiderPose next; SkateBoardPose frame; string error;
        if (!SkateRider.TryCreateAction(rig, input, pose, Style, Finger, Finger.Initialized ? (SkateRiderPose?)fingerPose : null, fingerCarrier, out next, out frame, out error))
        { poseFailed = true; SkateHud.Notice("Trick contact could not be reached"); return false; }
        pose = next; DisplayFrame = frame; if (Style.Weight >= .99) styleShown = true;
        if (Finger.Initialized) SkateOllieStyles.TrySampleFinger(Finger, input.DeckFrame, out FingerSample, out error);
        return true;
    }
    public static string Name(string original, bool landed)
    {
        if (!landed || nameUsed) return original;
        nameUsed = true;
        if (Finger.Complete)
        {
            if (original != null && original.Contains("Kickflip")) return original.Replace("Kickflip", "Finger Flip");
            if (original != null && original.Contains("Heelflip")) return original.Replace("Heelflip", "Finger Flip");
            return "Finger Flip";
        }
        if (Style.Reached && styleShown)
        {
            var title = SkateOllieStyles.Name(Style.Kind);
            if (original != null && original.Contains("Nollie")) return original.Replace("Nollie", title.Replace("Ollie", "Nollie"));
            if (original != null && original.Contains("Ollie")) return original.Replace("Ollie", title);
            return string.IsNullOrEmpty(original) ? title : title + " + " + original;
        }
        return original;
    }
}
