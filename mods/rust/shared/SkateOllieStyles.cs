using System;

namespace Shortcut.RustMod
{
    // These are modifiers of a foot pop, not new pop kinds: the original Ollie/Nollie,
    // stance and popped end remain available to the contact and scoring systems.
    public enum SkateOllieStyle { None = 0, Boned = 1, FrontOneFoot = 2, BackOneFoot = 3 }

    public readonly struct SkateOllieStyleState
    {
        public readonly SkateOllieStyle Kind;
        public readonly double Seconds, Weight;
        public readonly bool Releasing, Reached;
        internal SkateOllieStyleState(SkateOllieStyle kind, double seconds, double weight, bool releasing, bool reached)
        { Kind = kind; Seconds = seconds; Weight = weight; Releasing = releasing; Reached = reached; }
        public bool Known { get { return Kind != SkateOllieStyle.None; } }
        public bool Active { get { return Known && (!Releasing || Weight > 0); } }
        public bool Complete { get { return Known && Releasing && Weight == 0; } }
        public bool LandingReady { get { return Weight == 0; } }
        public double FrontContact { get { return Kind == SkateOllieStyle.FrontOneFoot ? 1 - Ease(Weight) : 1; } }
        public double BackContact { get { return Kind == SkateOllieStyle.BackOneFoot ? 1 - Ease(Weight) : 1; } }
        private static double Ease(double x) { return x * x * (3 - 2 * x); }
    }

    // Absolute ankle/toe targets. A consumer must solve the measured limbs to these targets,
    // preserving the attached ankle planes; Targets alone are not a solved rider pose.
    public readonly struct SkateOllieTargets
    {
        public readonly SkateBoardPose Frame;
        public readonly SkateVector LeftAnkle, RightAnkle, LeftToe, RightToe, LeftUp, RightUp;
        public readonly double LeftContact, RightContact, FrontExtension;
        internal SkateOllieTargets(SkateBoardPose frame, SkateVector leftAnkle, SkateVector rightAnkle,
            SkateVector leftToe, SkateVector rightToe, SkateVector leftUp, SkateVector rightUp,
            double leftContact, double rightContact, double frontExtension)
        { Frame = frame; LeftAnkle = leftAnkle; RightAnkle = rightAnkle; LeftToe = leftToe; RightToe = rightToe;
          LeftUp = leftUp; RightUp = rightUp; LeftContact = leftContact; RightContact = rightContact; FrontExtension = frontExtension; }
    }

    public enum SkateFingerFlipPhase { None = 0, Holding = 1, Throwing = 2, Rotating = 3, Catching = 4, Complete = 5 }

    public readonly struct SkateFingerFlipState
    {
        public readonly bool Initialized, LeftHand;
        public readonly int Direction;
        public readonly double Seconds;
        // The grabbed deck and grip relative to a carrier frame. Moving the rider carries the
        // whole trajectory; no world-space snapshot is left behind while the body travels.
        public readonly SkateBoardPose HeldLocal;
        public readonly SkateVector GripLocal;
        internal SkateFingerFlipState(bool leftHand, int direction, double seconds, SkateBoardPose heldLocal, SkateVector gripLocal)
        { Initialized = true; LeftHand = leftHand; Direction = direction; Seconds = seconds; HeldLocal = heldLocal; GripLocal = gripLocal; }
        public bool Active { get { return Initialized && Seconds < SkateOllieStyles.FingerDuration; } }
        public bool Complete { get { return Initialized && Seconds >= SkateOllieStyles.FingerDuration; } }
        public bool LandingReady { get { return !Initialized || Complete; } }
    }

    public readonly struct SkateFingerFlipSample
    {
        public readonly SkateFingerFlipPhase Phase;
        public readonly SkateBoardPose Frame;
        public readonly SkateVector HandTarget;
        public readonly bool LeftHand;
        public readonly double HandContact, FrontContact, BackContact, FootClearance, RollDegrees;
        internal SkateFingerFlipSample(SkateFingerFlipPhase phase, SkateBoardPose frame, SkateVector handTarget, bool leftHand,
            double handContact, double feet, double clearance, double roll)
        { Phase = phase; Frame = frame; HandTarget = handTarget; LeftHand = leftHand; HandContact = handContact;
          FrontContact = BackContact = feet; FootClearance = clearance; RollDegrees = roll; }
        public bool LandingReady { get { return Phase == SkateFingerFlipPhase.None || Phase == SkateFingerFlipPhase.Complete; } }
    }

    public static class SkateOllieStyles
    {
        public const double EnterSeconds = .12, MaximumHoldSeconds = .28, ReleaseSeconds = .14;
        // Smoothstep's peak speed is 1.5 times its mean. This duration keeps every
        // sampled flip increment within the existing scorer's FlipRate axis bound.
        public const double FingerHoldSeconds = .10, FingerThrowSeconds = .08, FingerTurnSeconds = 540 / SkateMotion.FlipRate, FingerCatchSeconds = .14;
        public const double FingerThrowDegrees = 15;
        public const double FingerReleaseAt = FingerHoldSeconds + FingerThrowSeconds;
        public const double FingerCatchAt = FingerReleaseAt + FingerTurnSeconds;
        public const double FingerDuration = FingerCatchAt + FingerCatchSeconds;

        public static bool Valid(SkateOllieStyle kind) { return kind >= SkateOllieStyle.None && kind <= SkateOllieStyle.BackOneFoot; }
        public static bool Valid(SkateOllieStyleState state)
        { return Valid(state.Kind) && Finite(state.Seconds) && state.Seconds >= 0 && state.Seconds <= MaximumHoldSeconds + ReleaseSeconds + .1 &&
            Fraction(state.Weight) && (state.Kind != SkateOllieStyle.None || state.Seconds == 0 && state.Weight == 0 && !state.Releasing && !state.Reached); }
        public static string Name(SkateOllieStyle kind)
        { return kind == SkateOllieStyle.Boned ? "Boned Ollie" : kind == SkateOllieStyle.FrontOneFoot ? "Front Foot One-Foot Ollie" :
            kind == SkateOllieStyle.BackOneFoot ? "Back Foot One-Foot Ollie" : ""; }
        public static SkateOllieStyle Select(bool modifier, bool front, bool back)
        { return !modifier || front && back ? SkateOllieStyle.None : front ? SkateOllieStyle.FrontOneFoot : back ? SkateOllieStyle.BackOneFoot : SkateOllieStyle.Boned; }

        public static bool TryBegin(SkateOllieStyle kind, out SkateOllieStyleState state, out string error)
        {
            state = default(SkateOllieStyleState); error = null;
            if (!Valid(kind)) { error = "Invalid ollie style."; return false; }
            if (kind != SkateOllieStyle.None) state = new SkateOllieStyleState(kind, 0, 0, false, false);
            return true;
        }

        // held=false requests recovery once. A bounded automatic release prevents a forgotten
        // held modifier from keeping a foot loose indefinitely. Early landings still inspect
        // LandingReady BEFORE resetting this state; an unrecovered foot is not a clean landing.
        public static bool TryStep(SkateOllieStyleState state, bool held, double dt, out SkateOllieStyleState next, out string error)
        {
            next = state; error = null;
            if (!Valid(state) || !Delta(dt)) { error = "Invalid ollie style step."; return false; }
            if (!state.Known || state.Complete) return true;
            double seconds = Math.Min(MaximumHoldSeconds + ReleaseSeconds, state.Seconds + dt);
            bool release = state.Releasing || !held || seconds >= MaximumHoldSeconds;
            // Split a step crossing the automatic release boundary so 20/32/60/100 Hz reach
            // identical entry/hold/recovery endpoints rather than dropping a whole step.
            double entering = release ? state.Releasing || !held ? 0 : Math.Max(0, MaximumHoldSeconds - state.Seconds) : dt;
            double weight = Math.Min(1, state.Weight + entering / EnterSeconds);
            bool reached = state.Reached || weight >= 1 - 1e-10;
            if (release) weight = Math.Max(0, weight - (dt - entering) / ReleaseSeconds);
            next = new SkateOllieStyleState(state.Kind, seconds, weight, release, reached);
            return true;
        }

        public static bool TryTargets(SkateOllieStyleState state, SkateRiderRig rig, SkateStance stance,
            SkateBoardPose frame, SkateRiderPose basis, out SkateOllieTargets targets, out string error)
        {
            targets = default(SkateOllieTargets); error = null;
            if (!Valid(state) || !Frame(frame) || !Rig(rig) || !Stance(stance) || !Points(basis))
            { error = "Invalid ollie style targets."; return false; }
            bool frontLeft = stance == SkateStance.Regular;
            var front = frontLeft ? basis.LeftAnkle : basis.RightAnkle;
            var back = frontLeft ? basis.RightAnkle : basis.LeftAnkle;
            var frontDirection = frame.Forward * (SkateVector.Dot(front - back, frame.Forward) >= 0 ? 1 : -1);
            var toeDirection = frame.Right * (frontLeft ? 1 : -1);
            var shown = frame;
            var left = basis.LeftAnkle; var right = basis.RightAnkle;
            var leftToe = basis.LeftToe; var rightToe = basis.RightToe;
            double weight = Smooth(state.Weight), leg = rig.ThighLength + rig.ShinLength;
            if (state.Kind == SkateOllieStyle.Boned && weight > 0)
            {
                double sign = SkateVector.Dot(frontDirection, frame.Forward) >= 0 ? 1 : -1;
                // Pitch around the supporting rear foot, retaining its contact. The front foot
                // slides toward the physical front of the rider even after a deck reversal.
                if (!TryFrame(state, frame, back, frontDirection, out shown, out error)) return false;
                left = Reframe(left, frame, shown); right = Reframe(right, frame, shown);
                leftToe = Reframe(leftToe, frame, shown); rightToe = Reframe(rightToe, frame, shown);
                var slide = shown.Forward * (sign * .10 * weight);
                if (frontLeft) { left += slide; leftToe += slide; } else { right += slide; rightToe += slide; }
            }
            else if (state.Kind == SkateOllieStyle.FrontOneFoot || state.Kind == SkateOllieStyle.BackOneFoot)
            {
                var free = frame.Up * (.24 * leg * weight) + toeDirection * (.17 * leg * weight) +
                    frontDirection * ((state.Kind == SkateOllieStyle.FrontOneFoot ? 1 : -1) * .08 * leg * weight);
                bool releaseLeft = state.Kind == SkateOllieStyle.FrontOneFoot ? frontLeft : !frontLeft;
                if (releaseLeft) { left += free; leftToe += free; } else { right += free; rightToe += free; }
            }
            double lc = frontLeft ? state.FrontContact : state.BackContact, rc = frontLeft ? state.BackContact : state.FrontContact;
            targets = new SkateOllieTargets(shown, left, right, leftToe, rightToe, shown.Up, shown.Up, lc, rc,
                state.Kind == SkateOllieStyle.Boned ? .92 * weight : 0);
            return true;
        }

        // Allows board presentation to use the same Boned frame before solving the rider.
        // rearSupport can be the rear ankle; its projection is the actual deck pivot.
        public static bool TryFrame(SkateOllieStyleState state, SkateBoardPose frame, SkateVector rearSupport,
            SkateVector anatomicalForward, out SkateBoardPose shown, out string error)
        {
            shown = frame; error = null;
            if (!Valid(state) || !Frame(frame) || !Point(rearSupport) || !Point(anatomicalForward) ||
                anatomicalForward.LengthSquared < .5 || Math.Abs(SkateVector.Dot(anatomicalForward, frame.Up)) > .01)
            { error = "Invalid ollie frame support."; return false; }
            if (state.Kind != SkateOllieStyle.Boned || state.Weight == 0) return true;
            double sign = SkateVector.Dot(anatomicalForward, frame.Forward) >= 0 ? 1 : -1;
            double angle = 18 * Smooth(state.Weight) * sign;
            var pivot = frame.Project(rearSupport, 0);
            shown = new SkateBoardPose(pivot + Turn(frame.Origin - pivot, frame.Right, angle),
                Turn(frame.Forward, frame.Right, angle), Turn(frame.Up, frame.Right, angle));
            return Frame(shown);
        }

        // A common pelvis translation along the rider's up solves BOTH reach constraints.
        // For Boned, the requested front extension chooses the height within that feasible
        // interval. Return values are exact knees; upper-body points translate by pelvisOffset.
        public static bool TryLegs(SkateRiderRig rig, SkateStance stance, SkateRiderPose basis, SkateOllieTargets targets,
            out SkateVector pelvisOffset, out SkateVector leftKnee, out SkateVector rightKnee, out string error)
        {
            pelvisOffset = leftKnee = rightKnee = default(SkateVector); error = null;
            if (!Rig(rig) || !Stance(stance) || !Points(basis) || !Frame(targets.Frame) ||
                !Point(basis.LeftHip) || !Point(basis.RightHip) || !Point(basis.PelvisUp) || basis.PelvisUp.LengthSquared < .5 ||
                !Point(targets.LeftAnkle) || !Point(targets.RightAnkle) || !Finite(targets.FrontExtension) || targets.FrontExtension < 0 || targets.FrontExtension > .92)
            { error = "Invalid action leg targets."; return false; }
            var up = basis.PelvisUp * (1 / basis.PelvisUp.Length);
            double near = Math.Abs(rig.ThighLength - rig.ShinLength) + .000001;
            double far = rig.ThighLength + rig.ShinLength - .000001;
            double la, lb, lc, ld, ra, rb, rc, rd;
            if (!HeightBands(basis.LeftHip, targets.LeftAnkle, up, near, far, out la, out lb, out lc, out ld) ||
                !HeightBands(basis.RightHip, targets.RightAnkle, up, near, far, out ra, out rb, out rc, out rd))
            { error = "Action feet have no common measured pelvis height."; return false; }
            double height = 0;
            if (targets.FrontExtension > 0)
            {
                bool frontLeft = stance == SkateStance.Regular;
                var hip = frontLeft ? basis.LeftHip : basis.RightHip;
                var ankle = frontLeft ? targets.LeftAnkle : targets.RightAnkle;
                var previous = frontLeft ? basis.LeftAnkle : basis.RightAnkle;
                double from = (previous - hip).Length;
                double distance = from + (.92 * (rig.ThighLength + rig.ShinLength) - from) * (targets.FrontExtension / .92);
                var delta = ankle - hip; double vertical = SkateVector.Dot(delta, up);
                double horizontal = Math.Max(0, delta.LengthSquared - vertical * vertical);
                height = vertical + Math.Sqrt(Math.Max(0, distance * distance - horizontal));
            }
            // A tucked grab can put the hips beside or below the ankles. Keep both physical
            // square-root branches instead of forcing a valid captured pose above its feet.
            double best = double.PositiveInfinity, solved = 0;
            for (int i = 0; i < 4; i++)
            {
                double minimum = Math.Max((i & 1) == 0 ? la : lc, (i & 2) == 0 ? ra : rc);
                double maximum = Math.Min((i & 1) == 0 ? lb : ld, (i & 2) == 0 ? rb : rd);
                if (minimum > maximum) continue;
                double candidate = Math.Max(minimum, Math.Min(maximum, height));
                double score = Math.Abs(candidate - height);
                if (score < best || score == best && Math.Abs(candidate) < Math.Abs(solved)) { best = score; solved = candidate; }
            }
            if (double.IsPositiveInfinity(best)) { error = "Action feet have no common measured pelvis height."; return false; }
            height = solved;
            pelvisOffset = up * height;
            var leftBend = basis.LeftKnee - (basis.LeftHip + basis.LeftAnkle) * .5;
            var rightBend = basis.RightKnee - (basis.RightHip + basis.RightAnkle) * .5;
            return TryLimb(basis.LeftHip + pelvisOffset, targets.LeftAnkle, rig.ThighLength, rig.ShinLength, leftBend, out leftKnee, out error) &&
                TryLimb(basis.RightHip + pelvisOffset, targets.RightAnkle, rig.ThighLength, rig.ShinLength, rightBend, out rightKnee, out error);
        }

        public static bool Valid(SkateFingerFlipState state)
        {
            if (!state.Initialized) return !state.LeftHand && state.Direction == 0 && state.Seconds == 0 &&
                !state.HeldLocal.Initialized && SkateBoardPath.Valid(state.HeldLocal) && state.GripLocal.LengthSquared == 0;
            return (state.Direction == -1 || state.Direction == 1) && Finite(state.Seconds) && state.Seconds >= 0 && state.Seconds <= FingerDuration &&
                Frame(state.HeldLocal) && state.HeldLocal.Origin.LengthSquared <= 2.25 && state.HeldLocal.Forward.Z >= .25 && state.HeldLocal.Up.Y >= .15 && Point(state.GripLocal) &&
                Math.Abs(state.GripLocal.X) <= .18 && Math.Abs(state.GripLocal.Y) <= .045 && Math.Abs(state.GripLocal.Z) <= .44 &&
                (Math.Abs(state.GripLocal.X) >= .07 || Math.Abs(state.GripLocal.Z) >= .25);
        }

        // Begin only from an attained single-hand grab. gripPoint is the solved hand, not a
        // desired target. Both feet release during Holding before the hand throws the deck.
        public static bool TryBeginFinger(SkateBoardPose captured, SkateBoardPose carrier, SkateVector gripPoint,
            bool leftHand, int direction, out SkateFingerFlipState state, out string error)
        {
            state = default(SkateFingerFlipState); error = null;
            if (!Frame(captured) || !Frame(carrier) || !Point(gripPoint) || (direction != -1 && direction != 1))
            { error = "Invalid finger flip capture."; return false; }
            var local = new SkateBoardPose(Local(captured.Origin, carrier), LocalDirection(captured.Forward, carrier), LocalDirection(captured.Up, carrier));
            var grip = Local(gripPoint, captured);
            var candidate = new SkateFingerFlipState(leftHand, direction, 0, local, grip);
            // Blend only an upright held deck into the ordinary catch frame; a dark catch or
            // opposite nose needs a separate physical transition and is deliberately refused.
            if (!Valid(candidate) || local.Forward.Z < .25 || local.Up.Y < .15)
            { error = "Finger flip needs a reachable upright edge grip."; return false; }
            state = candidate; return true;
        }

        public static bool TryStepFinger(SkateFingerFlipState state, double dt, out SkateFingerFlipState next, out string error)
        {
            next = state; error = null;
            if (!Valid(state) || !state.Initialized || !Delta(dt)) { error = "Invalid finger flip step."; return false; }
            next = new SkateFingerFlipState(state.LeftHand, state.Direction, Math.Min(FingerDuration, state.Seconds + dt), state.HeldLocal, state.GripLocal);
            return true;
        }

        public static bool TrySampleFinger(SkateFingerFlipState state, SkateBoardPose carrier, out SkateFingerFlipSample sample, out string error)
        {
            sample = default(SkateFingerFlipSample); error = null;
            if (!Valid(state) || !Frame(carrier)) { error = "Invalid finger flip frame."; return false; }
            if (!state.Initialized) { sample = new SkateFingerFlipSample(SkateFingerFlipPhase.None, carrier, default(SkateVector), false, 0, 1, 0, 0); return true; }
            var held = World(state.HeldLocal, carrier);
            double t = state.Seconds, throwing = Smooth((t - FingerHoldSeconds) / FingerThrowSeconds);
            double rotation = Smooth((t - FingerReleaseAt) / FingerTurnSeconds);
            double catching = Smooth((t - FingerCatchAt) / FingerCatchSeconds);
            var releaseOrigin = held.Origin + carrier.Up * .06;
            var baseline = BlendFrame(held, carrier, rotation);
            var origin = t < FingerReleaseAt ? held.Origin + carrier.Up * (.06 * throwing) :
                releaseOrigin * (1 - rotation) + carrier.Origin * rotation + carrier.Up * (.10 * Math.Sin(rotation * Math.PI));
            // The hand imparts the first part of the roll while still in contact. After
            // release the deck completes the remaining turn independently of that hand.
            double roll = state.Direction * (FingerThrowDegrees * throwing + (360 - FingerThrowDegrees) * rotation);
            var frame = new SkateBoardPose(origin, baseline.Forward, Turn(baseline.Up, baseline.Forward, roll));
            var release = new SkateBoardPose(releaseOrigin, held.Forward, Turn(held.Up, held.Forward, state.Direction * FingerThrowDegrees));
            var hand = t <= FingerReleaseAt ? frame.Point(state.GripLocal) : release.Point(state.GripLocal) + carrier.Up * (.06 * Smooth((t - FingerReleaseAt) / .10));
            // The hand remains exactly on the edge through the throw, then releases. A consumer
            // may ease the arm toward HandTarget after release, but must not pin it to the deck.
            double handContact = t < FingerReleaseAt ? 1 : 0;
            double feet = t < FingerReleaseAt ? 1 - Smooth(t / FingerHoldSeconds) : catching;
            var phase = state.Complete ? SkateFingerFlipPhase.Complete : t < FingerHoldSeconds ? SkateFingerFlipPhase.Holding :
                t < FingerReleaseAt ? SkateFingerFlipPhase.Throwing : t < FingerCatchAt ? SkateFingerFlipPhase.Rotating : SkateFingerFlipPhase.Catching;
            sample = new SkateFingerFlipSample(phase, frame, hand, state.LeftHand, handContact, feet, 1 - feet, roll);
            if (!Frame(frame) || !Point(hand)) { sample = default(SkateFingerFlipSample); error = "Finger flip exceeds the allowed frame bounds."; return false; }
            return true;
        }

        // Exact measured two-segment solve for consumers applying these targets. An unreachable
        // end is rejected rather than clamped away from its deck/hand contact. bendHint is a
        // direction (e.g. the previous knee's perpendicular offset), not a world-space point.
        public static bool TryLimb(SkateVector root, SkateVector end, double first, double second, SkateVector bendHint,
            out SkateVector joint, out string error)
        {
            joint = default(SkateVector); error = null;
            if (!Point(root) || !Point(end) || !Point(bendHint) || !Measure(first) || !Measure(second))
            { error = "Invalid measured limb."; return false; }
            var offset = end - root; double distance = offset.Length;
            if (distance < .000001 || distance > first + second + .00000001 || distance < Math.Abs(first - second) - .00000001)
            { error = "Action contact exceeds measured limb reach."; return false; }
            var direction = offset * (1 / distance);
            var bend = bendHint - direction * SkateVector.Dot(bendHint, direction);
            if (bend.LengthSquared < .00000001)
            {
                var axis = Math.Abs(direction.Y) < .9 ? new SkateVector(0, 1, 0) : new SkateVector(1, 0, 0);
                bend = SkateBoardPath.Cross(direction, axis);
            }
            bend *= 1 / bend.Length;
            double along = (first * first - second * second + distance * distance) / (2 * distance);
            double rise = Math.Sqrt(Math.Max(0, first * first - along * along));
            joint = root + direction * along + bend * rise;
            if (!Point(joint)) { joint = default(SkateVector); error = "Measured limb exceeds the allowed bounds."; return false; }
            return true;
        }

        private static bool Rig(SkateRiderRig rig)
        { return Measure(rig.ThighLength) && Measure(rig.ShinLength) && Measure(rig.FootLength); }
        private static bool Points(SkateRiderPose pose)
        { return Point(pose.LeftAnkle) && Point(pose.RightAnkle) && Point(pose.LeftToe) && Point(pose.RightToe) &&
            (pose.LeftAnkle - pose.RightAnkle).LengthSquared > .0001; }
        private static bool Stance(SkateStance stance) { return stance == SkateStance.Regular || stance == SkateStance.Goofy; }
        private static bool Frame(SkateBoardPose frame) { return frame.Initialized && SkateBoardPath.Valid(frame); }
        private static bool Point(SkateVector point)
        { return Finite(point.X) && Finite(point.Y) && Finite(point.Z) && Math.Abs(point.X) <= 100002 && Math.Abs(point.Y) <= 100002 && Math.Abs(point.Z) <= 100002; }
        private static bool Measure(double v) { return Finite(v) && v >= SkateRider.MinimumMeasurement && v <= SkateRider.MaximumMeasurement; }
        private static bool Fraction(double v) { return Finite(v) && v >= 0 && v <= 1; }
        private static bool Delta(double v) { return Finite(v) && v > 0 && v <= .1; }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
        private static double Smooth(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }
        private static SkateVector Turn(SkateVector value, SkateVector axis, double degrees)
        { double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a); return value * c + SkateBoardPath.Cross(axis, value) * s + axis * (SkateVector.Dot(axis, value) * (1 - c)); }
        private static SkateVector Local(SkateVector point, SkateBoardPose frame) { return LocalDirection(point - frame.Origin, frame); }
        private static SkateVector LocalDirection(SkateVector direction, SkateBoardPose frame)
        { return new SkateVector(SkateVector.Dot(direction, frame.Right), SkateVector.Dot(direction, frame.Up), SkateVector.Dot(direction, frame.Forward)); }
        private static SkateVector Reframe(SkateVector point, SkateBoardPose from, SkateBoardPose to) { return to.Point(Local(point, from)); }
        private static SkateBoardPose World(SkateBoardPose local, SkateBoardPose carrier)
        { return new SkateBoardPose(carrier.Point(local.Origin), carrier.Rotate(local.Forward), carrier.Rotate(local.Up)); }
        private static SkateBoardPose BlendFrame(SkateBoardPose a, SkateBoardPose b, double t)
        {
            var forward = a.Forward * (1 - t) + b.Forward * t; forward *= 1 / forward.Length;
            var up = a.Up * (1 - t) + b.Up * t; up -= forward * SkateVector.Dot(up, forward); up *= 1 / up.Length;
            return new SkateBoardPose(a.Origin * (1 - t) + b.Origin * t, forward, up);
        }
        private static bool HeightBands(SkateVector root, SkateVector end, SkateVector up, double near, double far,
            out double firstMinimum, out double firstMaximum, out double secondMinimum, out double secondMaximum)
        {
            firstMinimum = firstMaximum = secondMinimum = secondMaximum = 0;
            var delta = end - root; double vertical = SkateVector.Dot(delta, up);
            double horizontal = Math.Max(0, delta.LengthSquared - vertical * vertical);
            if (horizontal > far * far) return false;
            double outer = Math.Sqrt(Math.Max(0, far * far - horizontal));
            double inner = Math.Sqrt(Math.Max(0, near * near - horizontal));
            firstMinimum = vertical - outer; secondMaximum = vertical + outer;
            if (inner == 0) { firstMaximum = secondMaximum; secondMinimum = firstMinimum; }
            else { firstMaximum = vertical - inner; secondMinimum = vertical + inner; }
            return true;
        }
    }
}
