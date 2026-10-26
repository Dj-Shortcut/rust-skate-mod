using System;
using System.Collections.Generic;
using Shortcut.RustMod;

// Deterministic visual samples of the shipped pose helpers. The observations below are
// authored flat-stage observations, not collision queries or evidence of a playable trick.
public static partial class SkateRiggingShowcase
{
    public const double StageRadius = 2.5, StageHeight = 5;
    private const double PreviewDeckTop = .092, PreviewAnkle = .095;
    private static readonly SkateVector PreviewUp = new SkateVector(0, 1, 0);
    private static readonly SkateVector PreviewForward = new SkateVector(0, 0, 1);
    private static readonly SkateBoardPose PreviewStage = new SkateBoardPose(new SkateVector(0, PreviewDeckTop, 0), new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
    private enum PreviewKind { Riding, Air, Grab, GroundGrab, Grind, Handplant, Style, Finger, Oldschool, Wall, Wallie, Drop, BodyFlip }
    private readonly struct PreviewEntry
    {
        public readonly PreviewKind Kind;
        public readonly string Title, Family;
        public readonly int A, B;
        public readonly double Flip, Shove, Spin, Body;
        public readonly bool Late;
        public PreviewEntry(PreviewKind kind, string title, string family, int a = 0, int b = 0,
            double flip = 0, double shove = 0, double spin = 0, double body = 0, bool late = false)
        { Kind = kind; Title = title; Family = family; A = a; B = b; Flip = flip; Shove = shove; Spin = spin; Body = body; Late = late; }
    }
    // Do not build the catalog when the ordinary renderer first reads Running.
    private static PreviewEntry[] previewEntries;
    private static PreviewEntry[] PreviewCatalog()
    { if (previewEntries == null) previewEntries = BuildPreviewCatalog(); return previewEntries; }
    public static int Count { get { return PreviewCatalog().Length; } }
    public static string Label(int index)
    { var entries = PreviewCatalog(); return index >= 0 && index < entries.Length ? entries[index].Title : "Unknown pose"; }
    public static string Group(int index)
    { var entries = PreviewCatalog(); return index >= 0 && index < entries.Length ? entries[index].Family : "Unknown"; }

    private static PreviewEntry[] BuildPreviewCatalog()
    {
        var entries = new List<PreviewEntry>(224);
        string[] riding = { "Cruising", "Push", "Mongo push", "Manual", "Nose manual", "Pump posture", "Brake posture", "Frontside revert", "Backside revert", "Fakie roll", "Bail and recovery pose" };
        for (int i = 0; i < riding.Length; i++) entries.Add(new PreviewEntry(PreviewKind.Riding, riding[i], "Riding", i));
        for (int pop = 1; pop <= 2; pop++)
        {
            string prefix = pop == 1 ? "Ollie" : "Nollie";
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix, "Pop and rotation", pop));
            string[] flips = { "Kickflip", "Heelflip", "Double kickflip", "Double heelflip", "Triple kickflip", "Triple heelflip" };
            for (int i = 0; i < flips.Length; i++) entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " " + flips[i], "Pop and rotation", pop, flip: (i % 2 == 0 ? 1 : -1) * (i / 2 + 1)));
            for (int turns = 1; turns <= 2; turns++) for (int side = -1; side <= 1; side += 2)
                entries.Add(new PreviewEntry(PreviewKind.Air, prefix + (side > 0 ? " Frontside " : " Backside ") + (turns * 180) + " shove-it", "Pop and rotation", pop, shove: side * turns * 180));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Varial kickflip", "Pop and rotation", pop, flip: 1, shove: -180));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Varial heelflip", "Pop and rotation", pop, flip: -1, shove: 180));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " 360 flip", "Pop and rotation", pop, flip: 1, shove: -360));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Laser flip", "Pop and rotation", pop, flip: -1, shove: 360));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Hardflip", "Pop and rotation", pop, flip: 1, shove: 180));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Inward heelflip", "Pop and rotation", pop, flip: -1, shove: -180));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " 360 hardflip", "Pop and rotation", pop, flip: 1, shove: 360));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " 360 inward heelflip", "Pop and rotation", pop, flip: -1, shove: -360));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Late kickflip", "Late flip", pop, flip: 1, late: true));
            entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " Late heelflip", "Late flip", pop, flip: -1, late: true));
            for (int path = 1; path <= 3; path++) entries.Add(new PreviewEntry(PreviewKind.Air, prefix + " " + (path == 1 ? "Impossible" : path == 2 ? "Front-foot impossible" : "Underflip"), "Special board path", pop, path));
        }
        for (int side = -1; side <= 1; side += 2)
        {
            string name = side > 0 ? "Frontside " : "Backside ";
            entries.Add(new PreviewEntry(PreviewKind.Air, name + "180 ollie", "Body rotation", 1, spin: side * 180));
            entries.Add(new PreviewEntry(PreviewKind.Air, name + "360 ollie", "Body rotation", 1, spin: side * 360));
            entries.Add(new PreviewEntry(PreviewKind.Air, name + "body varial", "Body rotation", 1, body: side * 180));
        }
        entries.Add(new PreviewEntry(PreviewKind.Air, "Dark catch and release", "Special board path", 1, 4, flip: 1));
        string[] foot = { "No comply", "Boneless", "Fastplant", "Beanplant" };
        for (int i = 0; i < foot.Length; i++) entries.Add(new PreviewEntry(PreviewKind.Air, foot[i], "Footplant", i + 3));
        for (int grab = 1; grab < SkateGrabCatalog.Count; grab++) entries.Add(new PreviewEntry(PreviewKind.Grab, SkateGrabCatalog.Name((SkateGrabKind)grab), "Air grab", grab));
        for (int ground = 1; ground <= 4; ground++) entries.Add(new PreviewEntry(PreviewKind.GroundGrab, SkateGroundGrabs.Name((SkateGroundGrabKind)ground), "Ground grab", ground));
        for (int side = 0; side <= 1; side++)
        {
            bool fs = side == 1;
            for (int grind = 1; grind <= (int)SkateGrindKind.OverWilly; grind++)
                if (grind != (int)SkateGrindKind.Handplant) entries.Add(new PreviewEntry(PreviewKind.Grind, SkateTricks.GrindName((SkateGrindKind)grind, fs), "Grind and stall", grind, side));
            for (int plant = 0; plant <= (int)SkateHandplantKind.Judo; plant++)
                if (SkateHandplants.Valid((SkateHandplantKind)plant, fs)) entries.Add(new PreviewEntry(PreviewKind.Handplant, SkateHandplants.Name((SkateHandplantKind)plant, fs), "Handplant", plant, side));
        }
        for (int style = 1; style <= 3; style++) entries.Add(new PreviewEntry(PreviewKind.Style, SkateOllieStyles.Name((SkateOllieStyle)style), "Ollie style", style));
        entries.Add(new PreviewEntry(PreviewKind.Finger, "Frontside grab finger flip", "Finger flip", (int)SkateGrabKind.Frontside));
        entries.Add(new PreviewEntry(PreviewKind.Finger, "Backside grab finger flip", "Finger flip", (int)SkateGrabKind.Backside));
        for (int old = 1; old <= 2; old++)
        {
            string name = old == 1 ? "Caveman" : "Hippie jump";
            entries.Add(new PreviewEntry(PreviewKind.Oldschool, name, "Independent skateboard", old));
            entries.Add(new PreviewEntry(PreviewKind.Oldschool, name + " kickflip", "Independent skateboard", old, flip: 1));
            entries.Add(new PreviewEntry(PreviewKind.Oldschool, name + " heelflip", "Independent skateboard", old, flip: -1));
            for (int turns = 1; turns <= 2; turns++) for (int side = -1; side <= 1; side += 2)
            {
                string sideName = side > 0 ? " frontside " : " backside ";
                entries.Add(new PreviewEntry(PreviewKind.Oldschool, name + sideName + (turns * 180) + " shove-it", "Independent skateboard", old, shove: side * turns * 180));
                entries.Add(new PreviewEntry(PreviewKind.Oldschool, name + sideName + (turns * 180) + " body turn", "Independent skateboard", old, spin: side * turns * 180));
            }
            entries.Add(new PreviewEntry(PreviewKind.Oldschool, name + " varial kickflip", "Independent skateboard", old, flip: 1, shove: -180));
        }
        entries.Add(new PreviewEntry(PreviewKind.Wallie, "Wallie rebound pose", "Wall and transition"));
        entries.Add(new PreviewEntry(PreviewKind.Wall, "Frontside wallride pose", "Wall and transition", 1));
        entries.Add(new PreviewEntry(PreviewKind.Wall, "Backside wallride pose", "Wall and transition", -1));
        entries.Add(new PreviewEntry(PreviewKind.Drop, "Drop-in (current transition preview)", "Wall and transition"));
        entries.Add(new PreviewEntry(PreviewKind.BodyFlip, "Front flip with frontside grab", "Body flip", (int)SkateBodyFlipKind.Front));
        entries.Add(new PreviewEntry(PreviewKind.BodyFlip, "Back flip with frontside grab", "Body flip", (int)SkateBodyFlipKind.Back));
        return entries.ToArray();
    }

    private struct PreviewInput
    {
        public SkateBoardPose Carrier, Frame;
        public SkateVector Forward;
        public bool Air, Push, Mongo, Bailed, PitchFrame;
        public double Crouch, Speed, PushPhase, Flip, Shove, GroundDrop, Plant, Handplant,
            Pitch, Roll, Support, Bail, GrabSeconds;
        public SkateGrabKind Grab;
        public SkateRiderContactState Contact;
        public SkateBoardPathSample Path;
    }
    private static PreviewInput PreviewBase(double height, bool reversed)
    {
        var frame = new SkateBoardPose(PreviewStage.Origin + PreviewUp * height, PreviewForward * (reversed ? -1 : 1), PreviewUp);
        return new PreviewInput { Carrier = frame, Frame = frame, Forward = PreviewForward, GroundDrop = PreviewDeckTop, Crouch = .2, Speed = 2 };
    }
    private static SkateRiderInput PreviewRiderInput(PreviewInput s, SkateStance stance, bool ridingSwitch)
    {
        var toes = SkateBoardPath.Cross(PreviewUp, s.Forward) * (stance == SkateStance.Regular ? 1 : -1);
        return new SkateRiderInput(s.Carrier.Origin + PreviewUp * (PreviewAnkle - SkatePose.FootLift) - toes * .055,
            s.Forward, PreviewUp, stance, ridingSwitch, s.Speed, 0, s.Crouch, s.Air, s.Push, s.PushPhase,
            s.Flip, s.Grab, s.Bailed, 0, SkatePose.FootLift - PreviewAnkle, s.GroundDrop, s.Bail,
            s.Mongo, s.Plant, s.GrabSeconds, s.Handplant, s.Pitch, 0, 0, s.Shove, 0, s.Contact, .8,
            s.Carrier.Forward, s.PitchFrame, s.Roll, s.Support, s.Frame, s.Path);
    }

    public static bool TrySample(int index, double progress, SkateRiderRig rig, SkateBoardPose stage,
        SkateStance stance, bool ridingSwitch, bool reversed, out SkateRiderPose pose, out SkateBoardPose board,
        out SkateVector leftSoleUp, out SkateVector rightSoleUp, out string error)
    { SkateVector riderUp; return TrySample(index, progress, rig, stage, stance, ridingSwitch, reversed, out pose, out board, out riderUp, out leftSoleUp, out rightSoleUp, out error); }

    public static bool TrySample(int index, double progress, SkateRiderRig rig, SkateBoardPose stage,
        SkateStance stance, bool ridingSwitch, bool reversed, out SkateRiderPose pose, out SkateBoardPose board,
        out SkateVector riderUp, out SkateVector leftSoleUp, out SkateVector rightSoleUp, out string error)
    {
        pose = default(SkateRiderPose); board = default(SkateBoardPose);
        riderUp = leftSoleUp = rightSoleUp = PreviewUp; error = null;
        var entries = PreviewCatalog();
        if (index < 0 || index >= entries.Length || !PreviewFinite(progress) || progress < 0 || progress > 1 ||
            !stage.Initialized || !SkateBoardPath.Valid(stage) || stage.Up.Y < .85 ||
            stance < SkateStance.Regular || stance > SkateStance.Goofy)
        { error = "Invalid rigging preview input."; return false; }
        PreviewInput s;
        bool previewSwitch = entries[index].Kind == PreviewKind.Riding && entries[index].A == 9 ? !ridingSwitch : ridingSwitch;
        if (!PreviewLocal(entries[index], progress, rig, stance, previewSwitch, reversed, out s,
            out pose, out board, out riderUp, out leftSoleUp, out rightSoleUp, out error)) return false;
        if (!PreviewBounds(pose, board)) { error = "The measured pose exceeds the clear preview stage."; return false; }
        SkateRiderPose transformed;
        if (!SkateRider.TryTransformPose(pose, PreviewStage, stage, out transformed))
        { error = "Could not transform the rigging preview."; return false; }
        pose = transformed;
        board = new SkateBoardPose(PreviewMapPoint(board.Origin, stage), stage.Rotate(board.Forward), stage.Rotate(board.Up));
        riderUp = stage.Rotate(riderUp); leftSoleUp = stage.Rotate(leftSoleUp); rightSoleUp = stage.Rotate(rightSoleUp);
        return true;
    }

    private static bool PreviewLocal(PreviewEntry entry, double p, SkateRiderRig rig, SkateStance stance,
        bool sw, bool reversed, out PreviewInput s, out SkateRiderPose pose, out SkateBoardPose board,
        out SkateVector up, out SkateVector left, out SkateVector right, out string error)
    {
        s = PreviewBase(0, reversed); pose = default(SkateRiderPose); board = s.Frame;
        up = left = right = PreviewUp; error = null;
        bool solved = false;
        double w = PreviewWindow(p, .12, .30, .65, .90);
        if (entry.Kind == PreviewKind.Riding) PreviewRiding(entry.A, p, stance, sw, reversed, ref s);
        else if (entry.Kind == PreviewKind.Air) { if (!PreviewAir(entry, p, stance, sw, reversed, out s, out error)) return false; }
        else if (entry.Kind == PreviewKind.Grab || entry.Kind == PreviewKind.BodyFlip)
        {
            var grab = entry.Kind == PreviewKind.Grab ? (SkateGrabKind)entry.A : SkateGrabKind.Frontside;
            if (!PreviewGrab(grab, p, stance, sw, reversed, out s, out error)) return false;
        }
        else if (entry.Kind == PreviewKind.GroundGrab)
        {
            SkateGroundGrabState state = default(SkateGroundGrabState);
            double time = 0, target = p * 2;
            while (time < target - 1e-10)
            {
                double dt = PreviewSegment(time, target, .30, 1.4);
                var request = time >= .30 - 1e-10 && time < 1.4 - 1e-10 ? (SkateGroundGrabKind)entry.A : SkateGroundGrabKind.None;
                time += dt; SkateGroundGrabResult result;
                if (!SkateGroundGrabs.TryStep(state, request, true, dt, out result, out error)) return false;
                state = result.State;
            }
            if (!SkateRider.TryCreateGroundGrab(rig, PreviewRiderInput(s, stance, sw), state, out pose, out error)) return false;
            solved = true;
        }
        else if (entry.Kind == PreviewKind.Grind)
        {
            var kind = (SkateGrindKind)entry.A; double pitch, roll, crouch;
            SkateRider.GrindDeck(kind, out pitch, out roll, out crouch);
            s.Crouch = .2 + (crouch - .2) * w;
            s.Carrier = new SkateBoardPose(s.Carrier.Origin + PreviewUp * (.55 * w), s.Carrier.Forward, PreviewUp);
            double yaw = PreviewGrindYaw(kind) * w * (entry.B == 1 ? 1 : -1) * (stance == SkateStance.Regular ? 1 : -1);
            s.Frame = new SkateBoardPose(s.Carrier.Origin, PreviewTurn(s.Carrier.Forward, PreviewUp, yaw), PreviewUp);
            int truck = SkateGrinds.Truck(kind); s.Support = .23 * truck * w; s.PitchFrame = truck != 0;
            s.Pitch = pitch * w * Math.PI / 180; s.Roll = roll * w * Math.PI / 180;
            s.Frame = PreviewPitch(s.Frame, pitch * w, s.Support);
            s.Frame = new SkateBoardPose(s.Frame.Origin, s.Frame.Forward, PreviewTurn(s.Frame.Up, s.Frame.Forward, -roll * w));
        }
        else if (entry.Kind == PreviewKind.Handplant)
        {
            SkateHandplantState state = default(SkateHandplantState); double time = 0, target = p * 2;
            while (time < target - 1e-10)
            {
                double dt = PreviewSegment(time, target, .24, 1.30);
                var request = time >= .24 - 1e-10 && time < 1.30 - 1e-10 ? (SkateHandplantKind)entry.A : SkateHandplantKind.None;
                time += dt;
                if (!SkateHandplants.TryStep(state, request, entry.B == 1, true, dt, out state, out error)) return false;
            }
            // Handplants normally use a coping/ledge. Present that support slightly above
            // the flat stage so the authored deck can pass below the planted hand.
            double supportLift = .15 * PreviewWindow(p, 0, .12, .90, 1);
            s.Carrier = s.Frame = new SkateBoardPose(s.Frame.Origin + PreviewUp * supportLift, s.Frame.Forward, PreviewUp);
            s.Handplant = w; s.Crouch = .2 + .3 * w;
            if (!SkateRider.TryCreateHandplant(rig, PreviewRiderInput(s, stance, sw), state, entry.B == 1,
                PreviewUp * supportLift, new SkateVector(1, 0, 0), out pose, out board, out error)) return false;
            if (!board.Initialized) board = s.Frame;
            var profile = SkateHandplants.Get(state.Kind);
            left = PreviewSole(board.Up, PreviewUp, (stance == SkateStance.Goofy ? profile.BackFoot : profile.FrontFoot).LengthSquared > 0 ? state.Weight : 0);
            right = PreviewSole(board.Up, PreviewUp, (stance == SkateStance.Goofy ? profile.FrontFoot : profile.BackFoot).LengthSquared > 0 ? state.Weight : 0);
            solved = true;
        }
        else if (entry.Kind == PreviewKind.Style)
        {
            s = PreviewBase(PreviewAirHeight(p), reversed); s.Air = p > .1 && p < .9; s.Crouch = .2 + .45 * PreviewWindow(p, .08, .18, .80, .92);
            SkateOllieStyleState state = default(SkateOllieStyleState);
            if (p >= .18 && p < .9)
            {
                if (!SkateOllieStyles.TryBegin((SkateOllieStyle)entry.A, out state, out error)) return false;
                double time = 0, target = PreviewClamp((p - .18) / .58) * (SkateOllieStyles.MaximumHoldSeconds + SkateOllieStyles.ReleaseSeconds);
                while (time < target - 1e-10)
                { double dt = Math.Min(.02, target - time); time += dt; if (!SkateOllieStyles.TryStep(state, true, dt, out state, out error)) return false; }
            }
            if (!SkateRider.TryCreate(rig, PreviewRiderInput(s, stance, sw), out pose, out error)) return false;
            if (s.Air && !SkateRider.TryCreateAction(rig, PreviewRiderInput(s, stance, sw), pose, state, default(SkateFingerFlipState), out pose, out board, out error)) return false;
            if (!board.Initialized) board = s.Frame;
            left = PreviewSole(PreviewUp, board.Up, stance == SkateStance.Goofy ? state.BackContact : state.FrontContact);
            right = PreviewSole(PreviewUp, board.Up, stance == SkateStance.Goofy ? state.FrontContact : state.BackContact);
            solved = true;
        }
        else if (entry.Kind == PreviewKind.Finger)
        {
            if (!PreviewFinger(entry, p, rig, stance, sw, reversed, out s, out pose, out board, out left, out right, out error)) return false;
            solved = true;
        }
        else if (entry.Kind == PreviewKind.Oldschool)
        {
            if (!PreviewOldschool(entry, p, rig, stance, sw, reversed, out s, out pose, out board, out left, out right, out error)) return false;
            solved = true;
        }
        else if (entry.Kind == PreviewKind.Wallie)
        {
            s = PreviewBase(PreviewAirHeight(p), reversed); s.Air = p > .1 && p < .9;
            double wallie = PreviewWindow(p, .10, .22, .35, .65);
            s.Crouch = .25 + .55 * wallie; s.Frame = PreviewPitch(s.Frame, 20 * wallie, -.23);
        }
        if (!solved)
        {
            if (!SkateRider.TryCreate(rig, PreviewRiderInput(s, stance, sw), out pose, out error)) return false;
            board = s.Frame;
            left = right = board.Up;
            if (s.Contact.Initialized)
            {
                var deckUp = s.Path.Active ? PreviewUp : board.Up;
                left = PreviewSole(PreviewUp, deckUp, stance == SkateStance.Goofy ? s.Contact.BackFootContact : s.Contact.FrontFootContact);
                right = PreviewSole(PreviewUp, deckUp, stance == SkateStance.Goofy ? s.Contact.FrontFootContact : s.Contact.BackFootContact);
            }
            if (s.Plant > 0)
            {
                bool backLeft = (stance == SkateStance.Goofy) != sw;
                if (backLeft) right = PreviewSole(right, PreviewUp, s.Plant); else left = PreviewSole(left, PreviewUp, s.Plant);
            }
        }
        if (entry.Kind == PreviewKind.Wall || entry.Kind == PreviewKind.Drop)
        {
            double degrees;
            SkateVector axis;
            if (entry.Kind == PreviewKind.Wall)
            { degrees = 90 * w * entry.A * (stance == SkateStance.Regular ? 1 : -1); axis = PreviewForward; }
            else
            {
                SkateDropInResult drop;
                var deck = new SkateSurfaceContact(true, default(SkateVector), PreviewUp, false, false);
                var transition = new SkateSurfaceContact(true, new SkateVector(0, -.4, .3), new SkateVector(0, .5, Math.Sqrt(.75)), true, false);
                if (!SkateWallTricks.TryDropIn(default(SkateVector), default(SkateVector), PreviewForward, deck, transition, true, true, out drop, out error) || !drop.Began)
                { if (error == null) error = "The current drop-in preview did not begin."; return false; }
                degrees = 60 * w; axis = new SkateVector(1, 0, 0);
            }
            var target = new SkateBoardPose(board.Origin + PreviewUp * (entry.Kind == PreviewKind.Wall ? 1.1 : .65) * w + PreviewForward * (entry.Kind == PreviewKind.Drop ? .45 * w : 0),
                PreviewTurn(board.Forward, axis, degrees), PreviewTurn(board.Up, axis, degrees));
            if (!SkateRider.TryTransformPose(pose, board, target, out pose)) { error = "Invalid supported preview transform."; return false; }
            board = target; up = PreviewTurn(PreviewUp, axis, degrees); left = PreviewTurn(left, axis, degrees); right = PreviewTurn(right, axis, degrees);
        }
        if (entry.Kind == PreviewKind.BodyFlip && p >= .28 && p < .9)
        {
            SkateBodyFlipState state = default(SkateBodyFlipState); double time = 0, target = PreviewClamp((p - .28) / .42) * SkateWallTricks.BodyFlipDuration;
            while (time < target - 1e-10)
            {
                double dt = Math.Min(.02, target - time); time += dt; SkateBodyFlipResult step;
                if (!SkateWallTricks.TryBodyFlip(state, (SkateBodyFlipKind)entry.A, true, true, true, 2, dt, out step, out error)) return false;
                state = step.State;
            }
            SkateBoardPose transformed;
            var axis = SkateBoardPath.Cross(PreviewUp, s.Forward);
            if (!SkateWallTricks.TryFlipFrame(state, board, pose.Pelvis, axis, out transformed, out error) ||
                !SkateRider.TryTransformPose(pose, board, transformed, out pose)) return false;
            board = transformed; up = PreviewTurn(up, axis, state.Degrees); left = PreviewTurn(left, axis, state.Degrees); right = PreviewTurn(right, axis, state.Degrees);
        }
        return true;
    }

    private static void PreviewRiding(int kind, double p, SkateStance stance, bool sw, bool reversed, ref PreviewInput s)
    {
        double w = PreviewWindow(p, .12, .30, .65, .9);
        if (kind == 1 || kind == 2)
        {
            s.Push = true; s.Mongo = kind == 2; s.PushPhase = p; s.Speed = 2;
            s.Carrier = new SkateBoardPose(s.Carrier.Origin + PreviewForward * ((p - .5) * 1.6 * (sw ? -1 : 1)), s.Carrier.Forward, PreviewUp); s.Frame = s.Carrier;
        }
        else if (kind == 3 || kind == 4)
        { double sign = kind == 3 ? 1 : -1; s.Crouch = .3; s.Pitch = sign * 15 * w * Math.PI / 180; s.PitchFrame = true; s.Support = -.23 * sign; s.Frame = PreviewPitch(s.Frame, sign * 15 * w, s.Support); }
        else if (kind == 5) s.Crouch = .15 + .65 * w;
        else if (kind == 6) s.Crouch = .2 + .2 * w;
        else if (kind == 7 || kind == 8)
        {
            double turn = 180 * PreviewEase((p - .15) / .65) * (kind == 7 ? 1 : -1) * (stance == SkateStance.Regular ? 1 : -1);
            s.Forward = PreviewTurn(PreviewForward, PreviewUp, turn);
            s.Frame = s.Carrier = new SkateBoardPose(s.Carrier.Origin, PreviewTurn(s.Carrier.Forward, PreviewUp, turn), PreviewUp);
        }
        else if (kind == 9) s.Speed = 2;
        else if (kind == 10) { s.Bailed = true; s.Bail = p; }
    }

    private static bool PreviewAir(PreviewEntry entry, double p, SkateStance stance, bool sw, bool reversed,
        out PreviewInput s, out string error)
    {
        s = PreviewBase(PreviewAirHeight(p) + (entry.B >= 1 && entry.B <= 3 ? .5 * PreviewWindow(p, .1, .18, .35, .5) : 0), reversed); s.Air = p > .1 && p < .9; s.Crouch = .2 + .25 * PreviewWindow(p, .08, .18, .80, .92); error = null;
        double t = PreviewClamp((p - .1) / .8) * 3.2;
        double side = (sw ? -1 : 1) * (stance == SkateStance.Regular ? 1 : -1);
        double flip, shove, spin, body; SkateBoardPathSample path;
        PreviewAngles(entry, t, reversed, out flip, out shove, out spin, out body, out path);
        s.Forward = PreviewTurn(PreviewForward, PreviewUp, -(spin + body) * side);
        s.Carrier = new SkateBoardPose(s.Carrier.Origin, PreviewTurn(PreviewForward * (reversed ? -1 : 1), PreviewUp, -(spin + shove) * side), PreviewUp);
        s.Frame = new SkateBoardPose(s.Carrier.Origin, s.Carrier.Forward, PreviewTurn(PreviewUp, s.Carrier.Forward, -flip));
        s.Path = path; if (path.Known) s.Frame = s.Frame.WithPath(path);
        s.Flip = flip; s.Shove = shove;
        if (s.Air)
        {
            double elapsed = 0;
            var context = new SkateRiderTrickContext((SkatePopKind)entry.A, sw, sw, sw, entry.Late);
            while (elapsed < t - 1e-10)
            {
                double dt = Math.Min(.02, t - elapsed); elapsed += dt;
                double f, h, r, b; SkateBoardPathSample pathAt;
                PreviewAngles(entry, elapsed, reversed, out f, out h, out r, out b, out pathAt);
                bool rotating = elapsed < 1.9 && (Math.Abs(f - entry.Flip * 360) > 1e-6 || Math.Abs(h - entry.Shove) > 1e-6 || pathAt.Active || entry.B == 4 && elapsed < 1.85);
                if (!SkateRider.TryStepContact(s.Contact, true, rotating, elapsed > 1.6, entry.A >= 3,
                    f, h, SkateGrabKind.None, 0, 0, context, pathAt, dt, out s.Contact, out error)) return false;
            }
        }
        if (entry.A >= 3)
        {
            s.Plant = PreviewWindow(p, .08, .16, .22, .35); s.Crouch = .35 + .45 * s.Plant;
            double lift = Math.Max(PreviewAirHeight(p), .3 * s.Plant * s.Plant);
            s.Carrier = new SkateBoardPose(PreviewStage.Origin + PreviewUp * lift, s.Carrier.Forward, PreviewUp);
            s.Frame = PreviewPitch(new SkateBoardPose(s.Carrier.Origin + PreviewUp * (.12 * s.Plant), s.Carrier.Forward, PreviewUp), 38 * s.Plant, -.2);
        }
        return true;
    }
    private static void PreviewAngles(PreviewEntry entry, double t, bool reversed,
        out double flip, out double shove, out double spin, out double body, out SkateBoardPathSample path)
    {
        flip = PreviewAngle(entry.Flip * 360, t - (entry.Late ? 1.25 : .06), SkateMotion.FlipRate);
        shove = PreviewAngle(entry.Shove, t - .06, SkateMotion.ShoveRate);
        spin = PreviewAngle(entry.Spin, t - .06, SkateMotion.RotationRate);
        body = PreviewAngle(entry.Body, t - .06, SkateMotion.RotationRate);
        path = default(SkateBoardPathSample);
        if (entry.B >= 1 && entry.B <= 3)
        {
            string ignored; var kind = (SkateBoardTrickKind)entry.B;
            SkateBoardPath.TrySample(kind, PreviewClamp((t - .06) / SkateBoardPath.Duration(kind)), 1, entry.A == 2, reversed, out path, out ignored);
        }
        else if (entry.B == 4)
            flip = t < 1.6 ? PreviewAngle(180, t - .06, SkateMotion.FlipRate) : 180 + PreviewAngle(180, t - 1.6, SkateMotion.FlipRate);
    }

    private static bool PreviewGrab(SkateGrabKind grab, double p, SkateStance stance, bool sw, bool reversed,
        out PreviewInput s, out string error)
    {
        s = PreviewBase(PreviewAirHeight(p), reversed); s.Air = p > .1 && p < .9; error = null;
        double held = Math.Max(0, (Math.Min(p, .68) - .22) * 4);
        if (s.Air && p > .22)
        {
            if (!SkateRider.TryStepContact(default(SkateRiderContactState), true, false, p > .5, false, 0, 0,
                grab, held, 0, .02, out s.Contact, out error)) return false;
            double released = Math.Max(0, (p - .68) * 4), time = 0;
            while (time < released - 1e-10)
            {
                double dt = Math.Min(.02, released - time); time += dt;
                if (!SkateRider.TryStepContact(s.Contact, true, false, true, false, 0, 0,
                    SkateGrabKind.None, 0, 0, dt, out s.Contact, out error)) return false;
            }
        }
        s.Grab = s.Contact.VisibleGrab; s.GrabSeconds = s.Contact.GrabSeconds; s.Crouch = .2 + .75 * s.Contact.GrabWeight;
        s.Carrier = new SkateBoardPose(s.Carrier.Origin + PreviewUp * (.3 * s.Contact.BoardFold), s.Carrier.Forward, PreviewUp);
        var definition = SkateGrabCatalog.Get(s.Grab);
        s.Frame = new SkateBoardPose(s.Carrier.Origin,
            SkateGrabCatalog.TurnPoint(definition, s.Contact.BoardFold, s.Carrier.Forward, s.Carrier.Forward, PreviewUp),
            SkateGrabCatalog.TurnPoint(definition, s.Contact.BoardFold, PreviewUp, s.Carrier.Forward, PreviewUp));
        return true;
    }

    private static bool PreviewFinger(PreviewEntry entry, double p, SkateRiderRig rig, SkateStance stance, bool sw, bool reversed,
        out PreviewInput s, out SkateRiderPose pose, out SkateBoardPose board, out SkateVector left, out SkateVector right, out string error)
    {
        pose = default(SkateRiderPose); board = default(SkateBoardPose); left = right = PreviewUp;
        if (p < .3 || p >= .9)
        {
            if (!PreviewGrab((SkateGrabKind)entry.A, p, stance, sw, reversed, out s, out error) ||
                !SkateRider.TryCreate(rig, PreviewRiderInput(s, stance, sw), out pose, out error)) return false;
            board = s.Frame; left = right = board.Up; return true;
        }
        PreviewInput held;
        if (!PreviewGrab((SkateGrabKind)entry.A, .3, stance, sw, reversed, out held, out error)) { s = held; return false; }
        // Capture the actual attained grab at the current moving carrier, just as the native adapter does.
        double lift = PreviewAirHeight(p) - PreviewAirHeight(.3) - .3 * held.Contact.BoardFold * PreviewEase((p - .75) / .15);
        held.Carrier = new SkateBoardPose(held.Carrier.Origin + PreviewUp * lift, held.Carrier.Forward, PreviewUp);
        held.Frame = new SkateBoardPose(held.Frame.Origin + PreviewUp * lift, held.Frame.Forward, held.Frame.Up);
        SkateRiderPose captured;
        if (!SkateRider.TryCreate(rig, PreviewRiderInput(held, stance, sw), out captured, out error)) { s = held; return false; }
        bool handLeft, handRight; SkateRider.GrabHands(stance, sw, (SkateGrabKind)entry.A, out handLeft, out handRight);
        SkateFingerFlipState finger;
        if (!SkateOllieStyles.TryBeginFinger(held.Frame, held.Carrier, handLeft ? captured.LeftHand : captured.RightHand, handLeft, 1, out finger, out error)) { s = held; return false; }
        double time = 0, target = PreviewClamp((p - .3) / .45) * SkateOllieStyles.FingerDuration;
        while (time < target - 1e-10)
        { double dt = Math.Min(.02, target - time); time += dt; if (!SkateOllieStyles.TryStepFinger(finger, dt, out finger, out error)) { s = held; return false; } }
        s = PreviewBase(held.Carrier.Origin.Y - PreviewDeckTop, reversed); s.Air = true; s.Crouch = .65 - .45 * PreviewEase((p - .75) / .15);
        SkateRiderPose basis; var input = PreviewRiderInput(s, stance, sw);
        if (!SkateRider.TryCreate(rig, input, out basis, out error) ||
            !SkateRider.TryCreateAction(rig, input, basis, default(SkateOllieStyleState), finger, captured, held.Carrier, out pose, out board, out error)) return false;
        SkateFingerFlipSample sample;
        if (!SkateOllieStyles.TrySampleFinger(finger, s.Carrier, out sample, out error)) return false;
        left = PreviewSole(PreviewUp, board.Up, sample.FrontContact); right = PreviewSole(PreviewUp, board.Up, sample.BackContact);
        return true;
    }

    private static bool PreviewOldschool(PreviewEntry entry, double p, SkateRiderRig rig, SkateStance stance, bool sw, bool reversed,
        out PreviewInput s, out SkateRiderPose pose, out SkateBoardPose board, out SkateVector left, out SkateVector right, out string error)
    {
        s = PreviewBase(0, reversed); pose = default(SkateRiderPose); board = s.Frame; left = right = PreviewUp;
        var kind = (SkateOldschoolKind)entry.A; var travel = PreviewForward;
        var feet = PreviewStage.Origin;
        double handHeight = rig.PelvisHeight * .75 + rig.SpineLength * .15;
        var toes = new SkateVector(stance == SkateStance.Regular ? 1 : -1, 0, 0);
        var hand = feet + PreviewUp * handHeight + toes * .22;
        var initial = new SkateBoardPose(feet + PreviewForward * (kind == SkateOldschoolKind.Caveman ? .25 : 0), s.Frame.Forward, PreviewUp);
        SkateOldschoolState state;
        if (!SkateOldschool.TryBegin(kind, initial, travel, feet, travel + PreviewUp * (kind == SkateOldschoolKind.HippieJump ? 6 : 0), hand, true, out state, out error)) return false;
        double time = 0, target = p * 1.4, jumpAt = kind == SkateOldschoolKind.HippieJump ? 0 : -1, releaseAt = -1, height = 0;
        var releasePoint = default(SkateVector); bool requested = false, grounded = kind == SkateOldschoolKind.Caveman;
        while (time < target - 1e-10 && state.Active)
        {
            double dt = Math.Min(.01, target - time); time += dt;
            double air = jumpAt < 0 ? 0 : time - jumpAt;
            height = air > 0 ? Math.Max(0, 6 * air - 8 * air * air) : 0;
            double vy = air > 0 && height > 0 ? 6 - 16 * air : 0;
            feet = PreviewStage.Origin + travel * time + PreviewUp * height;
            double bodyAngle = -entry.Spin * PreviewEase(air / .65) * (stance == SkateStance.Regular ? 1 : -1) * (sw ? -1 : 1);
            s.Forward = PreviewTurn(PreviewForward, PreviewUp, bodyAngle);
            toes = SkateBoardPath.Cross(PreviewUp, s.Forward) * (stance == SkateStance.Regular ? 1 : -1);
            hand = feet + PreviewUp * handHeight + toes * .22;
            var velocity = travel + PreviewUp * vy; int flip = 0, shove = 0;
            if (!requested && state.Phase == SkateOldschoolPhase.Released)
            { flip = (int)entry.Flip; shove = (int)(entry.Shove / 180); requested = true; }
            var predicted = SkateOldschool.Predict(state, feet, velocity, hand, flip, shove, dt);
            var support = new SkateOldschoolSupport(true, new SkateVector(predicted.X, 0, predicted.Z), PreviewUp, true, true);
            grounded = jumpAt < 0 || air > .75;
            SkateOldschoolResult step;
            if (!SkateOldschool.TryStep(state, new SkateOldschoolInput(feet, velocity, hand, support, true, grounded, true, flip, shove), dt, out step, out error)) return false;
            if ((step.Events & SkateOldschoolEvents.ReadyToJump) != 0) jumpAt = time;
            if ((step.Events & SkateOldschoolEvents.Released) != 0) { releaseAt = time; releasePoint = step.State.Grip - feet; }
            state = step.State;
        }
        if (state.Phase == SkateOldschoolPhase.Failed) { error = "Independent skateboard preview: " + state.Failure; return false; }
        s.Carrier = s.Frame = new SkateBoardPose(feet, s.Forward * (reversed ? -1 : 1), PreviewUp);
        s.Air = !grounded; s.Crouch = s.Air ? .65 : .95; s.GroundDrop = PreviewDeckTop + (s.Air ? 0 : height);
        double follow = releaseAt < 0 ? 0 : PreviewClamp(1 - (time - releaseAt) / .16);
        if (!SkateRider.TryCreateIndependentDeck(rig, PreviewRiderInput(s, stance, sw), state, releasePoint + feet, follow,
            stance == SkateStance.Goofy, out pose, out board, out error)) return false;
        left = right = PreviewSole(PreviewUp, board.Up, state.FootContact); return true;
    }

    private static double PreviewGrindYaw(SkateGrindKind kind)
    {
        switch (kind)
        {
            case SkateGrindKind.Boardslide: case SkateGrindKind.Lipslide: case SkateGrindKind.Noseslide:
            case SkateGrindKind.Tailslide: case SkateGrindKind.Bluntslide: case SkateGrindKind.Nosebluntslide:
            case SkateGrindKind.BoardStall: case SkateGrindKind.Darkslide: return 90;
            case SkateGrindKind.Smith: case SkateGrindKind.Feeble: case SkateGrindKind.Crooked: case SkateGrindKind.Overcrook:
            case SkateGrindKind.Salad: case SkateGrindKind.Willy: case SkateGrindKind.OverWilly: return 35;
            default: return 0;
        }
    }
    private static SkateBoardPose PreviewPitch(SkateBoardPose frame, double degrees, double along)
    {
        var pivot = frame.Origin + frame.Forward * along;
        return new SkateBoardPose(pivot + PreviewTurn(frame.Origin - pivot, frame.Right, -degrees),
            PreviewTurn(frame.Forward, frame.Right, -degrees), PreviewTurn(frame.Up, frame.Right, -degrees));
    }
    private static SkateVector PreviewTurn(SkateVector value, SkateVector axis, double degrees)
    {
        double angle = degrees * Math.PI / 180, c = Math.Cos(angle), n = Math.Sin(angle);
        return value * c + SkateBoardPath.Cross(axis, value) * n + axis * (SkateVector.Dot(axis, value) * (1 - c));
    }
    private static SkateVector PreviewSole(SkateVector a, SkateVector b, double weight)
    {
        var value = a * (1 - PreviewClamp(weight)) + b * PreviewClamp(weight);
        return value.LengthSquared > 1e-10 ? value * (1 / value.Length) : (weight < .5 ? a : b);
    }
    private static double PreviewAirHeight(double p)
    { double q = PreviewClamp((p - .1) / .8), h = Math.Sin(Math.PI * q); return h * h; }
    private static double PreviewWindow(double p, double a, double b, double c, double d)
    { return p <= a || p >= d ? 0 : p < b ? PreviewEase((p - a) / (b - a)) : p <= c ? 1 : 1 - PreviewEase((p - c) / (d - c)); }
    private static double PreviewSegment(double time, double target, double enter, double release)
    {
        double dt = Math.Min(.04, target - time);
        if (time < enter - 1e-10) dt = Math.Min(dt, enter - time);
        else if (time < release - 1e-10) dt = Math.Min(dt, release - time);
        return dt;
    }
    private static double PreviewAngle(double target, double time, double rate)
    { return Math.Sign(target) * Math.Min(Math.Abs(target), Math.Max(0, time) * rate); }
    private static double PreviewClamp(double value) { return Math.Max(0, Math.Min(1, value)); }
    private static double PreviewEase(double value) { value = PreviewClamp(value); return value * value * (3 - 2 * value); }
    private static bool PreviewFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    private static SkateVector PreviewMapPoint(SkateVector value, SkateBoardPose stage)
    { return stage.Point(value - PreviewStage.Origin); }
    private static bool PreviewBound(SkateVector point)
    { return PreviewFinite(point.X) && PreviewFinite(point.Y) && PreviewFinite(point.Z) && point.Y >= -.025 && point.Y <= StageHeight && point.X * point.X + point.Z * point.Z <= StageRadius * StageRadius; }
    private static bool PreviewBounds(SkateRiderPose p, SkateBoardPose board)
    {
        return PreviewBound(p.Pelvis) && PreviewBound(p.LeftHip) && PreviewBound(p.RightHip) && PreviewBound(p.LeftKnee) && PreviewBound(p.RightKnee) &&
            PreviewBound(p.LeftAnkle) && PreviewBound(p.RightAnkle) && PreviewBound(p.LeftToe) && PreviewBound(p.RightToe) &&
            PreviewBound(p.Chest) && PreviewBound(p.Neck) && PreviewBound(p.Head) && PreviewBound(p.LeftShoulder) && PreviewBound(p.RightShoulder) &&
            PreviewBound(p.LeftElbow) && PreviewBound(p.RightElbow) && PreviewBound(p.LeftHand) && PreviewBound(p.RightHand) &&
            PreviewBound(board.Point(new SkateVector(-.14, -.02, -.43))) && PreviewBound(board.Point(new SkateVector(.14, -.02, -.43))) &&
            PreviewBound(board.Point(new SkateVector(-.14, -.02, .43))) && PreviewBound(board.Point(new SkateVector(.14, -.02, .43)));
    }
}
