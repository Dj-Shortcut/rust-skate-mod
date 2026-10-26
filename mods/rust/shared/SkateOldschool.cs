using System;

namespace Shortcut.RustMod
{
    public enum SkateOldschoolKind { None = 0, Caveman = 1, HippieJump = 2 }
    public enum SkateOldschoolPhase { None = 0, Pickup = 1, Carrying = 2, Released = 3, Catching = 4, Caught = 5, Complete = 6, Failed = 7 }
    public enum SkateOldschoolFailure { None = 0, Interrupted = 1, LostSupport = 2, Obstructed = 3, OutOfReach = 4, UncaughtLanding = 5, UnfinishedRotation = 6, TimedOut = 7 }
    [Flags]
    public enum SkateOldschoolEvents { None = 0, ReadyToJump = 1, Released = 2, RotationStarted = 4, DeckLanded = 8, CatchStarted = 16, Caught = 32, Completed = 64, Failed = 128, RotationRefused = 256 }

    // A real native observation at the next predicted deck location. PathClear covers the swept
    // deck volume; RotationClear covers the full rotating deck, not just a ray through its centre.
    // Missing floor is allowed while the deck is flying, never while it is rolling.
    public readonly struct SkateOldschoolSupport
    {
        public readonly bool Observed, PathClear, RotationClear;
        public readonly SkateVector Point, Normal;
        public SkateOldschoolSupport(bool observed, SkateVector point, SkateVector normal, bool pathClear, bool rotationClear)
        { Observed = observed; Point = point; Normal = normal; PathClear = pathClear; RotationClear = rotationClear; }
    }

    public readonly struct SkateOldschoolInput
    {
        // RiderFeet is the ordinary upright deck-top target beneath the real movement body.
        // It follows the real body's jump; it must never be derived from this independent deck.
        public readonly SkateVector RiderFeet, RiderVelocity, HandAt;
        public readonly SkateOldschoolSupport Support;
        public readonly bool ControlsActive, RiderGrounded, ReleaseRequested;
        // Fresh requests only. One physical rotation burst is accepted per action.
        public readonly int FlipTurns, ShoveHalfTurns;
        public SkateOldschoolInput(SkateVector riderFeet, SkateVector riderVelocity, SkateVector handAt,
            SkateOldschoolSupport support, bool controlsActive, bool riderGrounded, bool releaseRequested,
            int flipTurns, int shoveHalfTurns)
        { RiderFeet = riderFeet; RiderVelocity = riderVelocity; HandAt = handAt; Support = support;
          ControlsActive = controlsActive; RiderGrounded = riderGrounded; ReleaseRequested = releaseRequested;
          FlipTurns = flipTurns; ShoveHalfTurns = shoveHalfTurns; }
    }

    public readonly struct SkateOldschoolState
    {
        public readonly SkateOldschoolKind Kind;
        public readonly SkateOldschoolPhase Phase;
        public readonly SkateOldschoolFailure Failure;
        // Position is the upright deck-top reference; Deck rotates around its thickness centre.
        public readonly SkateVector Position, Velocity, Forward, Up;
        public readonly double Seconds, PhaseSeconds, FlipDegrees, ShoveDegrees, FlipTarget, ShoveTarget;
        public readonly bool Flying, RotationIssued, RotationStarted;
        public bool Active { get { return Phase >= SkateOldschoolPhase.Pickup && Phase <= SkateOldschoolPhase.Caught; } }
        public bool Caught { get { return Phase == SkateOldschoolPhase.Caught || Phase == SkateOldschoolPhase.Complete; } }
        public bool Rotating { get { return RotationStarted && (Math.Abs(FlipTarget - FlipDegrees) > 1e-8 || Math.Abs(ShoveTarget - ShoveDegrees) > 1e-8); } }
        public double FootContact
        { get { return Caught ? 1 : Phase == SkateOldschoolPhase.Catching ? SkateOldschool.Ease(PhaseSeconds / SkateOldschool.CatchSeconds) :
            Kind == SkateOldschoolKind.HippieJump && Phase == SkateOldschoolPhase.Released && !RotationStarted ?
                1 - SkateOldschool.Ease(PhaseSeconds / SkateOldschool.FootReleaseSeconds) : 0; } }
        // A Caveman pickup is a required hand constraint, not a free-floating deck lift. Before
        // beginning, the native measured pose must reach Grip; failure to solve it cancels pickup.
        public double HandContact { get { return Phase == SkateOldschoolPhase.Pickup || Phase == SkateOldschoolPhase.Carrying ? 1 : 0; } }
        public SkateBoardPose Deck
        {
            get
            {
                if (Kind == SkateOldschoolKind.None) return default(SkateBoardPose);
                var forward = SkateOldschool.Turn(Forward, Up, ShoveDegrees);
                var up = SkateOldschool.Turn(Up, forward, FlipDegrees);
                return new SkateBoardPose(Position - Up * SkateOldschool.PivotDepth + up * SkateOldschool.PivotDepth, forward, up);
            }
        }
        public SkateVector Grip { get { return Deck.Point(new SkateVector(SkateOldschool.GripAcross, 0, 0)); } }
        internal SkateOldschoolState(SkateOldschoolKind kind, SkateOldschoolPhase phase, SkateOldschoolFailure failure,
            SkateVector position, SkateVector velocity, SkateVector forward, SkateVector up,
            double seconds, double phaseSeconds, double flip, double shove, double flipTarget, double shoveTarget,
            bool flying, bool rotationIssued, bool rotationStarted)
        { Kind = kind; Phase = phase; Failure = failure; Position = position; Velocity = velocity; Forward = forward; Up = up;
          Seconds = seconds; PhaseSeconds = phaseSeconds; FlipDegrees = flip; ShoveDegrees = shove;
          FlipTarget = flipTarget; ShoveTarget = shoveTarget; Flying = flying; RotationIssued = rotationIssued; RotationStarted = rotationStarted; }
    }

    public readonly struct SkateOldschoolResult
    {
        public readonly SkateOldschoolState State;
        public readonly SkateOldschoolEvents Events;
        public readonly double FlipStep, ShoveStep;
        public SkateOldschoolResult(SkateOldschoolState state, SkateOldschoolEvents events, double flipStep, double shoveStep)
        { State = state; Events = events; FlipStep = flipStep; ShoveStep = shoveStep; }
    }

    // Independent kinematic deck action, driven only by supplied native observations. This never
    // moves the rider, invents support, teleports to a catch or treats an ordinary ollie as a Hippie.
    public static class SkateOldschool
    {
        public const double DeckClearance = 0.092, PivotDepth = 0.02, GripAcross = 0.1;
        public const double Gravity = 16, MaximumPace = 40, MaximumSeconds = 2;
        public const double PickupReach = 1.1, PickupSpeed = 4.5, CarrySeconds = 0.12, CarryTimeout = 0.55;
        public const double ReleaseFlightSeconds = 0.34, CatchSeconds = 0.045, CatchReach = 0.60, CatchAcross = 0.32;
        public const double RotationWindow = 0.12, FootReleaseSeconds = 0.07;
        public const double DeckHop = 2.8, FlipRate = 1440, ShoveRate = 1080;
        private static readonly SkateVector WorldUp = new SkateVector(0, 1, 0);

        public static bool TryBegin(SkateOldschoolKind kind, SkateBoardPose actualDeck, SkateVector deckVelocity,
            SkateVector riderFeet, SkateVector riderVelocity, SkateVector handAt, bool supported,
            out SkateOldschoolState state, out string error)
        {
            state = default(SkateOldschoolState); error = null;
            if ((kind != SkateOldschoolKind.Caveman && kind != SkateOldschoolKind.HippieJump) ||
                !SkateBoardPath.Valid(actualDeck) || !actualDeck.Initialized || !Vector(actualDeck.Origin) || !Vector(deckVelocity) ||
                !Vector(riderFeet) || !Vector(riderVelocity) || !Vector(handAt) || !supported ||
                deckVelocity.LengthSquared > MaximumPace * MaximumPace || riderVelocity.LengthSquared > MaximumPace * MaximumPace ||
                actualDeck.Up.Y < 0.65)
            { error = "Invalid oldschool take-off."; return false; }
            var separation = actualDeck.Origin - riderFeet;
            if (kind == SkateOldschoolKind.Caveman ? separation.Length > PickupReach :
                Flat(separation).Length > CatchAcross || Math.Abs(separation.Y) > 0.2)
            { error = "The skateboard is outside pickup reach."; return false; }
            if ((handAt - riderFeet).Length > 1.6) { error = "The carrying hand is outside rider reach."; return false; }
            state = new SkateOldschoolState(kind, kind == SkateOldschoolKind.Caveman ? SkateOldschoolPhase.Pickup : SkateOldschoolPhase.Released,
                SkateOldschoolFailure.None, actualDeck.Origin, deckVelocity, actualDeck.Forward, actualDeck.Up,
                0, 0, 0, 0, 0, 0, false, false, false);
            return true;
        }

        // Query the world at this point before TryStep. The native adapter should sample both
        // axles, sweep all the travel and check rotation clearance around the resulting frame.
        public static SkateVector Predict(SkateOldschoolState state, double dt)
        { return state.Position + state.Velocity * dt - (state.Flying ? WorldUp * (Gravity * dt * dt * 0.5) : default(SkateVector)); }

        // Use this overload for the native sweep before a pickup, catch or fresh rotation. A
        // first-frame deck hop and a lift toward the hand must be included in that same sweep.
        public static SkateVector Predict(SkateOldschoolState state, SkateVector riderFeet, SkateVector riderVelocity,
            SkateVector handAt, int flipTurns, int shoveHalfTurns, double dt)
        {
            if (state.Phase == SkateOldschoolPhase.Pickup || state.Phase == SkateOldschoolPhase.Carrying)
                return Toward(state.Position, handAt - state.Deck.Right * GripAcross, (PickupSpeed + riderVelocity.Length) * dt);
            if (state.Phase == SkateOldschoolPhase.Catching || state.Phase == SkateOldschoolPhase.Caught)
            {
                if (!state.Flying) return state.Position + Project(state.Velocity, state.Up) * dt;
                var predicted = state.Position + state.Velocity * dt - WorldUp * (Gravity * dt * dt * 0.5);
                return state.Phase == SkateOldschoolPhase.Catching ? predicted : Toward(predicted, riderFeet, 6 * dt);
            }
            var velocity = state.Velocity; bool flying = state.Flying;
            var gap = riderFeet - state.Position;
            if (!state.RotationStarted && (state.RotationIssued || state.PhaseSeconds + dt <= RotationWindow && (flipTurns != 0 || shoveHalfTurns != 0)) &&
                (state.Kind != SkateOldschoolKind.HippieJump || state.PhaseSeconds + dt >= FootReleaseSeconds) &&
                (Math.Abs(gap.Y) >= 0.26 || Flat(gap).Length >= 0.42) && !flying)
            { velocity += WorldUp * DeckHop; flying = true; }
            return state.Position + velocity * dt - (flying ? WorldUp * (Gravity * dt * dt * 0.5) : default(SkateVector));
        }

        public static bool TryStep(SkateOldschoolState state, SkateOldschoolInput input, double dt,
            out SkateOldschoolResult result, out string error)
        {
            result = new SkateOldschoolResult(state, SkateOldschoolEvents.None, 0, 0); error = null;
            if (!Valid(state) || !state.Active || !Finite(dt) || dt < 0.001 || dt > 0.05 ||
                !Vector(input.RiderFeet) || !Vector(input.RiderVelocity) || !Vector(input.HandAt) ||
                input.RiderVelocity.LengthSquared > MaximumPace * MaximumPace ||
                !SupportValid(input.Support) || input.FlipTurns < -1 || input.FlipTurns > 1 || input.ShoveHalfTurns < -2 || input.ShoveHalfTurns > 2)
            { error = "Invalid oldschool step."; return false; }
            if (!input.ControlsActive) return Fail(state, SkateOldschoolFailure.Interrupted, out result);
            if (!input.Support.PathClear) return Fail(state, SkateOldschoolFailure.Obstructed, out result);
            double seconds = state.Seconds + dt, phaseSeconds = state.PhaseSeconds + dt;
            if (seconds > MaximumSeconds) return Fail(state, SkateOldschoolFailure.TimedOut, out result);
            var phase = state.Phase; var position = state.Position; var velocity = state.Velocity;
            var forward = state.Forward; var up = state.Up;
            double flip = state.FlipDegrees, shove = state.ShoveDegrees, flipTarget = state.FlipTarget, shoveTarget = state.ShoveTarget;
            bool flying = state.Flying, issued = state.RotationIssued, started = state.RotationStarted;
            var events = SkateOldschoolEvents.None;

            if (phase == SkateOldschoolPhase.Pickup || phase == SkateOldschoolPhase.Carrying)
            {
                if ((input.HandAt - input.RiderFeet).Length > 1.6) return Fail(state, SkateOldschoolFailure.OutOfReach, out result);
                var desired = input.HandAt - state.Deck.Right * GripAcross;
                var previous = position;
                position = Toward(position, desired, (PickupSpeed + input.RiderVelocity.Length) * dt);
                velocity = (position - previous) * (1 / dt);
                if (phase == SkateOldschoolPhase.Pickup && (desired - position).Length < 0.02)
                { phase = SkateOldschoolPhase.Carrying; phaseSeconds = 0; events |= SkateOldschoolEvents.ReadyToJump; }
                else if (phase == SkateOldschoolPhase.Carrying)
                {
                    if ((desired - position).Length > 0.1 || phaseSeconds > CarryTimeout)
                        return Fail(state, SkateOldschoolFailure.OutOfReach, out result);
                    if (input.ReleaseRequested && phaseSeconds >= CarrySeconds && input.RiderVelocity.Y > 0.3)
                    {
                        phase = SkateOldschoolPhase.Released; phaseSeconds = 0; flying = true;
                        velocity = input.RiderVelocity + (input.RiderFeet - position) * (1 / ReleaseFlightSeconds);
                        events |= SkateOldschoolEvents.Released;
                    }
                }
            }
            else if (phase == SkateOldschoolPhase.Caught || phase == SkateOldschoolPhase.Catching)
            {
                // Compare points at the same physics time. At speed the body legitimately moves
                // farther than the catch radius in one step; that is not a missed skateboard.
                position += (flying ? velocity : Project(velocity, up)) * dt - (flying ? WorldUp * (Gravity * dt * dt * 0.5) : default(SkateVector));
                if (flying) velocity -= WorldUp * (Gravity * dt);
                if (input.Support.Observed && (!flying || SkateVector.Dot(position - input.Support.Point, input.Support.Normal) <= DeckClearance && SkateVector.Dot(velocity, input.Support.Normal) < 0))
                {
                    position = input.Support.Point + input.Support.Normal * DeckClearance;
                    up = input.Support.Normal; forward = Unit(Project(forward, up)); flying = false;
                }
                if (!flying && !input.Support.Observed) return Fail(state, SkateOldschoolFailure.LostSupport, out result);
                var offset = input.RiderFeet - position;
                if (Flat(offset).Length > CatchAcross + 0.15 || Math.Abs(offset.Y) > CatchReach + 0.15)
                    return Fail(state, SkateOldschoolFailure.OutOfReach, out result);
                // The approaching feet reach the rolling deck. They do not pull a grounded
                // skateboard upward into the jumping body. A native measured pose must satisfy
                // the required foot constraint before its own anticipatory landing is accepted.
                if (phase == SkateOldschoolPhase.Caught && flying) position = Toward(position, input.RiderFeet, 6 * dt);
                if (phase == SkateOldschoolPhase.Caught) velocity = flying ? input.RiderVelocity : Project(input.RiderVelocity, up);
                if (phase == SkateOldschoolPhase.Catching && phaseSeconds >= CatchSeconds && Flat(offset).Length <= CatchAcross && Math.Abs(offset.Y) <= CatchReach)
                { phase = SkateOldschoolPhase.Caught; phaseSeconds = 0; events |= SkateOldschoolEvents.Caught; }
                if (input.RiderGrounded)
                {
                    if (phase != SkateOldschoolPhase.Caught) return Fail(state, SkateOldschoolFailure.UncaughtLanding, out result);
                    if (!input.Support.Observed) return Fail(state, SkateOldschoolFailure.LostSupport, out result);
                    phase = SkateOldschoolPhase.Complete; events |= SkateOldschoolEvents.Completed;
                }
            }
            else
            {
                if (!issued && (input.FlipTurns != 0 || input.ShoveHalfTurns != 0))
                {
                    // The rotation impulse belongs to the initial foot kick / hand throw. A
                    // later rider separated from the deck cannot remotely flick a rolling board.
                    if (phaseSeconds <= RotationWindow)
                    { issued = true; flipTarget = input.FlipTurns * 360; shoveTarget = input.ShoveHalfTurns * 180; }
                    else events |= SkateOldschoolEvents.RotationRefused;
                }
                if (issued && !started)
                {
                    if (!input.Support.RotationClear) return Fail(state, SkateOldschoolFailure.Obstructed, out result);
                    var gap = input.RiderFeet - position;
                    if ((state.Kind != SkateOldschoolKind.HippieJump || phaseSeconds >= FootReleaseSeconds) &&
                        (Math.Abs(gap.Y) >= 0.26 || Flat(gap).Length >= 0.42))
                    {
                        started = true; events |= SkateOldschoolEvents.RotationStarted;
                        if (!flying) { flying = true; velocity += WorldUp * DeckHop; }
                    }
                }
                if (started)
                {
                    if ((Math.Abs(flipTarget - flip) > 1e-8 || Math.Abs(shoveTarget - shove) > 1e-8) && !input.Support.RotationClear)
                        return Fail(state, SkateOldschoolFailure.Obstructed, out result);
                    flip = Toward(flip, flipTarget, FlipRate * dt); shove = Toward(shove, shoveTarget, ShoveRate * dt);
                }
                position += velocity * dt - (flying ? WorldUp * (Gravity * dt * dt * 0.5) : default(SkateVector));
                if (flying) velocity -= WorldUp * (Gravity * dt);
                bool unfinished = issued && (!started || Math.Abs(flipTarget - flip) > 1e-8 || Math.Abs(shoveTarget - shove) > 1e-8);
                if (flying && input.Support.Observed)
                {
                    var floor = input.Support.Point + input.Support.Normal * DeckClearance;
                    var gap = SkateVector.Dot(position - floor, input.Support.Normal);
                    if (gap <= 0 && SkateVector.Dot(velocity, input.Support.Normal) < 0)
                    {
                        if (unfinished) return Fail(state, SkateOldschoolFailure.UnfinishedRotation, out result);
                        position = floor; velocity = Project(velocity, input.Support.Normal); flying = false;
                        up = input.Support.Normal; forward = Unit(Project(forward, up)); events |= SkateOldschoolEvents.DeckLanded;
                    }
                }
                if (!flying)
                {
                    if (!input.Support.Observed) return Fail(state, SkateOldschoolFailure.LostSupport, out result);
                    var floor = input.Support.Point + input.Support.Normal * DeckClearance;
                    if (Flat(position - floor).Length > 0.35 || Math.Abs(position.Y - floor.Y) > 0.16)
                        return Fail(state, SkateOldschoolFailure.LostSupport, out result);
                    position = floor; up = input.Support.Normal; forward = Unit(Project(forward, up));
                    velocity = Project(velocity - WorldUp * (Gravity * dt), up);
                    var pace = velocity.Length; velocity = pace > 0.2 * dt ? velocity * ((pace - 0.2 * dt) / pace) : default(SkateVector);
                }
                var separation = input.RiderFeet - position;
                bool canCatch = !unfinished && phaseSeconds >= (state.Kind == SkateOldschoolKind.Caveman ? 0.16 : 0.12) &&
                    (state.Kind == SkateOldschoolKind.Caveman || input.RiderVelocity.Y <= 0) &&
                    Flat(separation).Length <= CatchAcross && separation.Y >= -0.12 && separation.Y <= CatchReach;
                if (canCatch)
                { phase = SkateOldschoolPhase.Catching; phaseSeconds = 0; events |= SkateOldschoolEvents.CatchStarted; }
                if (input.RiderGrounded)
                    return Fail(state, unfinished ? SkateOldschoolFailure.UnfinishedRotation : SkateOldschoolFailure.UncaughtLanding, out result);
            }
            if (!Vector(position) || velocity.LengthSquared > MaximumPace * MaximumPace || forward.LengthSquared < 0.99)
                return Fail(state, SkateOldschoolFailure.OutOfReach, out result);
            var next = new SkateOldschoolState(state.Kind, phase, SkateOldschoolFailure.None, position, velocity, forward, up,
                seconds, phaseSeconds, flip, shove, flipTarget, shoveTarget, flying, issued, started);
            result = new SkateOldschoolResult(next, events, flip - state.FlipDegrees, shove - state.ShoveDegrees); return true;
        }

        public static bool Valid(SkateOldschoolState state)
        {
            if (state.Kind < SkateOldschoolKind.Caveman || state.Kind > SkateOldschoolKind.HippieJump ||
                state.Phase < SkateOldschoolPhase.Pickup || state.Phase > SkateOldschoolPhase.Failed ||
                state.Failure < SkateOldschoolFailure.None || state.Failure > SkateOldschoolFailure.TimedOut ||
                !Vector(state.Position) || !Vector(state.Velocity) || state.Velocity.LengthSquared > MaximumPace * MaximumPace ||
                !UnitVector(state.Forward) || !UnitVector(state.Up) || Math.Abs(SkateVector.Dot(state.Forward, state.Up)) > 0.0001 ||
                !Finite(state.Seconds) || state.Seconds < 0 || state.Seconds > MaximumSeconds ||
                !Finite(state.PhaseSeconds) || state.PhaseSeconds < 0 || state.PhaseSeconds > state.Seconds + 1e-8 ||
                !Finite(state.FlipDegrees) || !Finite(state.ShoveDegrees) || !Finite(state.FlipTarget) || !Finite(state.ShoveTarget) ||
                Math.Abs(state.FlipDegrees) > 360 || Math.Abs(state.ShoveDegrees) > 360 || Math.Abs(state.FlipTarget) > 360 || Math.Abs(state.ShoveTarget) > 360 ||
                state.RotationStarted && !state.RotationIssued ||
                (state.Phase == SkateOldschoolPhase.Failed) != (state.Failure != SkateOldschoolFailure.None) ||
                Math.Abs(state.FlipTarget / 360 - Math.Round(state.FlipTarget / 360)) > 1e-8 ||
                Math.Abs(state.ShoveTarget / 180 - Math.Round(state.ShoveTarget / 180)) > 1e-8 ||
                Math.Abs(state.FlipDegrees) > Math.Abs(state.FlipTarget) || Math.Abs(state.ShoveDegrees) > Math.Abs(state.ShoveTarget) ||
                state.FlipDegrees * state.FlipTarget < 0 || state.ShoveDegrees * state.ShoveTarget < 0)
                return false;
            if ((state.Phase == SkateOldschoolPhase.Pickup || state.Phase == SkateOldschoolPhase.Carrying) &&
                (state.Kind != SkateOldschoolKind.Caveman || state.Flying || state.RotationIssued)) return false;
            if (state.Caught && (state.Rotating || state.RotationIssued && !state.RotationStarted)) return false;
            return state.RotationIssued || state.FlipDegrees == 0 && state.ShoveDegrees == 0 && state.FlipTarget == 0 && state.ShoveTarget == 0;
        }
        private static bool Fail(SkateOldschoolState state, SkateOldschoolFailure failure, out SkateOldschoolResult result)
        {
            var failed = new SkateOldschoolState(state.Kind, SkateOldschoolPhase.Failed, failure, state.Position, state.Velocity,
                state.Forward, state.Up, state.Seconds, state.PhaseSeconds, state.FlipDegrees, state.ShoveDegrees,
                state.FlipTarget, state.ShoveTarget, state.Flying, state.RotationIssued, state.RotationStarted);
            result = new SkateOldschoolResult(failed, SkateOldschoolEvents.Failed, 0, 0); return true;
        }
        private static bool SupportValid(SkateOldschoolSupport support)
        { return !support.Observed || Vector(support.Point) && UnitVector(support.Normal) && support.Normal.Y >= 0.65; }
        private static SkateVector Flat(SkateVector v) { return new SkateVector(v.X, 0, v.Z); }
        private static SkateVector Project(SkateVector value, SkateVector normal) { return value - normal * SkateVector.Dot(value, normal); }
        private static SkateVector Unit(SkateVector value) { return value.LengthSquared > 1e-12 ? value * (1 / value.Length) : default(SkateVector); }
        private static SkateVector Toward(SkateVector value, SkateVector target, double distance)
        { var delta = target - value; var length = delta.Length; return length <= distance ? target : value + delta * (distance / length); }
        private static double Toward(double value, double target, double distance) { return value + Math.Max(-distance, Math.Min(distance, target - value)); }
        internal static double Ease(double value) { value = Math.Max(0, Math.Min(1, value)); return value * value * (3 - 2 * value); }
        internal static SkateVector Turn(SkateVector value, SkateVector axis, double degrees)
        { double angle = degrees * Math.PI / 180, c = Math.Cos(angle), s = Math.Sin(angle); return value * c + SkateBoardPath.Cross(axis, value) * s + axis * (SkateVector.Dot(axis, value) * (1 - c)); }
        private static bool UnitVector(SkateVector value) { return Vector(value) && Math.Abs(value.LengthSquared - 1) < 0.0001; }
        private static bool Vector(SkateVector value) { return Finite(value.X) && Finite(value.Y) && Finite(value.Z) && Math.Abs(value.X) <= 100000 && Math.Abs(value.Y) <= 100000 && Math.Abs(value.Z) <= 100000; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
