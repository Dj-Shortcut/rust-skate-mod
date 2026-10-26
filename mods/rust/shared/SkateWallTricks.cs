using System;

namespace Shortcut.RustMod
{
    // A fresh native query, not retained geometry. The adapter must identify its own ramp/coping
    // colliders; a near-vertical transition face is never evidence of a wallride.
    public readonly struct SkateSurfaceContact
    {
        public readonly bool Found, Ramp, Coping;
        public readonly SkateVector Point, Normal;
        public SkateSurfaceContact(bool found, SkateVector point, SkateVector normal, bool ramp, bool coping)
        { Found = found; Point = point; Normal = normal; Ramp = ramp; Coping = coping; }
    }

    public readonly struct SkateWallRideState
    {
        public readonly bool Active, Frontside;
        public readonly double Seconds, Cooldown;
        public readonly SkateVector Normal, Travel;
        internal SkateWallRideState(bool frontside, double seconds, SkateVector normal, SkateVector travel)
        { Active = true; Frontside = frontside; Seconds = seconds; Cooldown = 0; Normal = normal; Travel = travel; }
        internal SkateWallRideState(double cooldown)
        { Active = Frontside = false; Seconds = 0; Cooldown = cooldown; Normal = Travel = default(SkateVector); }
    }

    public readonly struct SkateWallRideResult
    {
        public readonly SkateWallRideState State;
        public readonly SkateVector Velocity, ContactOrigin;
        public readonly bool Began, Ended, Popped;
        internal SkateWallRideResult(SkateWallRideState state, SkateVector velocity, SkateVector contactOrigin,
            bool began, bool ended, bool popped)
        { State = state; Velocity = velocity; ContactOrigin = contactOrigin; Began = began; Ended = ended; Popped = popped; }
    }

    public readonly struct SkateWallieResult
    {
        public readonly bool Launched;
        public readonly SkateVector Velocity, ContactPoint, Normal;
        internal SkateWallieResult(SkateVector velocity, SkateVector contactPoint, SkateVector normal)
        { Launched = true; Velocity = velocity; ContactPoint = contactPoint; Normal = normal; }
    }

    public readonly struct SkateDropInResult
    {
        // Began is an entry candidate. The native adapter confirms actual transition contact
        // before scoring it; neither this result nor the visual pitch moves a body to the ramp.
        public readonly bool Began;
        public readonly SkateVector Velocity, Normal, SupportPoint;
        internal SkateDropInResult(SkateVector velocity, SkateVector normal, SkateVector supportPoint)
        { Began = true; Velocity = velocity; Normal = normal; SupportPoint = supportPoint; }
    }

    public enum SkateBodyFlipKind { None = 0, Front = 1, Back = 2 }

    public readonly struct SkateBodyFlipState
    {
        public readonly SkateBodyFlipKind Kind;
        public readonly double Progress, GrabSeconds;
        internal SkateBodyFlipState(SkateBodyFlipKind kind, double progress, double grabSeconds)
        { Kind = kind; Progress = progress; GrabSeconds = grabSeconds; }
        public bool Known { get { return Kind != SkateBodyFlipKind.None; } }
        public bool Active { get { return Known && Progress < 1; } }
        public bool Complete { get { return Known && Progress >= 1; } }
        public bool Qualified { get { return Complete && GrabSeconds >= SkateWallTricks.BodyFlipGrabSeconds; } }
        public double Degrees { get { return (Kind == SkateBodyFlipKind.Back ? -1 : 1) * 360 * Progress; } }
    }

    public readonly struct SkateBodyFlipResult
    {
        public readonly SkateBodyFlipState State;
        public readonly bool Began, Completed, TooLate, BailOnContact;
        internal SkateBodyFlipResult(SkateBodyFlipState state, bool began, bool completed, bool tooLate, bool bail)
        { State = state; Began = began; Completed = completed; TooLate = tooLate; BailOnContact = bail; }
    }

    public static class SkateWallTricks
    {
        public const double Gravity = 16, RideReach = 0.55, RideBodyGap = 0.35, RideMinimumSpeed = 2;
        public const double RideMaximumSeconds = 1.1, RideMaximumCorrection = 0.8, RideNormalCos = 0.94, RideRecaptureDelay = 0.35;
        public const double BodyFlipDuration = 0.64, BodyFlipGrabSeconds = 0.12;
        private static readonly SkateVector Up = new SkateVector(0, 1, 0);

        // Keeps momentum in the observed wall plane, applies gravity in that plane and only a
        // bounded normal correction. Every continuing step needs fresh nearby, coplanar support.
        // ContactOrigin is the wheel contact on the wall; add the real deck height along Normal
        // when constructing the common rendered deck/rider frame. Physics position is untouched.
        public static bool TryRide(SkateWallRideState state, SkateVector position, SkateVector velocity,
            SkateSurfaceContact wall, bool frontside, bool requested, bool pop, bool controlsActive,
            bool grounded, double dt, out SkateWallRideResult result, out string error)
        {
            result = default(SkateWallRideResult); error = null;
            if (!Valid(state) || !Position(position) || !Velocity(velocity) || !Valid(wall) || !Step(dt))
            { error = "Invalid wallride observation or step."; return false; }
            double distance;
            var supported = Wall(position, wall, out distance);
            var normal = wall.Normal;
            var tangent = supported ? Project(velocity, normal) : velocity;
            var alongSpeed = Horizontal(tangent).Length;
            var entering = !state.Active;
            if (entering && state.Cooldown > 0)
            {
                result = new SkateWallRideResult(new SkateWallRideState(Math.Max(0, state.Cooldown - dt)),
                    velocity, default(SkateVector), false, false, false); return true;
            }
            var usable = controlsActive && !grounded && supported &&
                (entering || SkateVector.Dot(state.Normal, normal) >= RideNormalCos) &&
                alongSpeed >= RideMinimumSpeed && -SkateVector.Dot(velocity, normal) <= 8 &&
                SkateVector.Dot(velocity, normal) <= 2;
            if (entering && (!requested || !usable))
            { result = new SkateWallRideResult(state, velocity, default(SkateVector), false, false, false); return true; }
            if (!entering && (!requested || !usable || pop || state.Seconds + dt > RideMaximumSeconds))
            {
                // A deliberate pop leaves outward/upward. A lost query/input simply releases
                // momentum; it cannot replay a pop or add a kick after opening a menu.
                var outgoing = supported ? velocity - normal * Math.Min(0, SkateVector.Dot(velocity, normal)) : velocity;
                var popped = pop && controlsActive && supported && !grounded;
                if (popped) outgoing = Project(outgoing, normal) + normal * 2 + Up * 2.5;
                result = new SkateWallRideResult(new SkateWallRideState(RideRecaptureDelay), outgoing, default(SkateVector), false, true, popped);
                return true;
            }
            tangent += Project(Up * -Gravity, normal) * dt;
            var correction = Clamp((RideBodyGap - distance) / dt, -RideMaximumCorrection, RideMaximumCorrection);
            var outgoingVelocity = tangent + normal * correction;
            var direction = Unit(tangent);
            var next = new SkateWallRideState(entering ? frontside : state.Frontside, state.Seconds + dt, normal, direction);
            result = new SkateWallRideResult(next, outgoingVelocity, position - normal * distance, entering, false, false);
            return true;
        }

        // Intentional wall pop from an actual low vertical face with an observed top. A normal
        // wall stop, a ramp, a missed top or a request made without ground support is not a wallie.
        public static bool TryWallie(SkateVector position, SkateVector velocity, SkateSurfaceContact wall,
            SkateSurfaceContact top, bool requested, bool grounded, out SkateWallieResult result, out string error)
        {
            result = default(SkateWallieResult); error = null;
            if (!Position(position) || !Velocity(velocity) || !Valid(wall) || !Valid(top))
            { error = "Invalid wallie observation."; return false; }
            double distance;
            if (!requested || !grounded || !Wall(position, wall, out distance) || !top.Found || top.Ramp || top.Coping || top.Normal.Y < 0.65) return true;
            var height = top.Point.Y - position.Y;
            var into = -SkateVector.Dot(velocity, wall.Normal);
            var topOffset = Horizontal(top.Point - wall.Point);
            // The down query must be just behind this face, not a different platform beside it.
            var depth = -SkateVector.Dot(topOffset, wall.Normal);
            var side = topOffset + wall.Normal * depth;
            if (height < 0.08 - 0.000001 || height > 0.75 + 0.000001 || wall.Point.Y > top.Point.Y + 0.05 ||
                wall.Point.Y < position.Y - 0.1 || depth < -0.05 || depth > 0.65 || side.Length > 0.45 ||
                into < 1.2 || into > 8 + 0.000001 || Horizontal(velocity).Length < 2 - 0.000001) return true;
            var outgoing = Project(velocity, wall.Normal) + wall.Normal * Math.Min(2, into * 0.3);
            outgoing += Up * (Math.Max(2.5, Math.Sqrt(2 * Gravity * (height + 0.18))) - outgoing.Y);
            result = new SkateWallieResult(outgoing, wall.Point, wall.Normal); return true;
        }

        // A slow, supported deck approach to a separately observed descending transition.
        // No position correction, fabricated plane or arbitrary edge can start a drop-in.
        public static bool TryDropIn(SkateVector position, SkateVector velocity, SkateVector heading,
            SkateSurfaceContact deck, SkateSurfaceContact transition, bool requested, bool grounded,
            out SkateDropInResult result, out string error)
        {
            result = default(SkateDropInResult); error = null;
            if (!Position(position) || !Velocity(velocity) || !Vector(heading) || heading.LengthSquared < 0.5 ||
                !Valid(deck) || !Valid(transition)) { error = "Invalid drop-in observation."; return false; }
            if (!requested || !grounded || !deck.Found || deck.Coping || deck.Normal.Y < 0.85 ||
                !transition.Found || !transition.Ramp || transition.Coping || transition.Normal.Y < 0.015 || transition.Normal.Y > 0.9) return true;
            var forward = Unit(Horizontal(heading));
            if (forward.LengthSquared < 0.5) { error = "Drop-in heading must have a horizontal direction."; return false; }
            var delta = transition.Point - position;
            var ahead = SkateVector.Dot(Horizontal(delta), forward);
            var sideways = Horizontal(delta) - forward * ahead;
            var fall = deck.Point.Y - transition.Point.Y;
            if (Math.Abs(deck.Point.Y - position.Y) > 0.35 || Horizontal(deck.Point - position).Length > 0.45 ||
                ahead < 0.15 || ahead > 0.95 || sideways.Length > 0.25 || fall < 0.04 || fall > 1.1 ||
                Horizontal(velocity).Length > 3.5 || SkateVector.Dot(velocity, forward) < -0.2 ||
                SkateVector.Dot(forward, Unit(Horizontal(transition.Normal))) < 0.35) return true;
            var tangent = Unit(Project(forward, transition.Normal));
            if (tangent.Y >= -0.03) return true;
            // A small committed forward roll tips the front wheels over the lip. Gravity owns
            // the acceleration after that; the normal ground/transition solver owns contact.
            var speed = Math.Max(0.8, Math.Max(0, SkateVector.Dot(velocity, tangent)));
            result = new SkateDropInResult(tangent * speed, transition.Normal, transition.Point); return true;
        }

        // Rotation continues after releasing the grab so the rider can prepare a safe landing.
        // Starting too late is explicit and the unfinished turn must bail at contact. A menu
        // prevents a new action but never freezes a rotation halfway through the body.
        public static bool TryBodyFlip(SkateBodyFlipState state, SkateBodyFlipKind requested, bool airborne,
            bool holdingGrab, bool controlsActive, double secondsToContact, double dt,
            out SkateBodyFlipResult result, out string error)
        {
            result = default(SkateBodyFlipResult); error = null;
            if (!Valid(state) || requested < SkateBodyFlipKind.None || requested > SkateBodyFlipKind.Back ||
                !Finite(secondsToContact) || secondsToContact < 0 || secondsToContact > 60 || !Step(dt))
            { error = "Invalid body flip request or step."; return false; }
            if (!airborne)
            {
                result = new SkateBodyFlipResult(default(SkateBodyFlipState), false, false, false, state.Known && !state.Qualified);
                return true;
            }
            var began = !state.Known && requested != SkateBodyFlipKind.None && controlsActive && holdingGrab;
            if (!state.Known && !began) { result = new SkateBodyFlipResult(state, false, false, false, false); return true; }
            var kind = began ? requested : state.Kind;
            var progress = Math.Min(1, state.Progress + dt / BodyFlipDuration);
            var grip = Math.Min(BodyFlipGrabSeconds, state.GrabSeconds + (holdingGrab && controlsActive && state.Progress < 1 ? dt : 0));
            var next = new SkateBodyFlipState(kind, progress, grip);
            result = new SkateBodyFlipResult(next, began, !state.Complete && next.Complete,
                began && secondsToContact < BodyFlipDuration + 0.03, false); return true;
        }

        // The same rigid map is applied to every solved joint and to the canonical board frame.
        // It preserves all bone/contact distances and never rotates the movement capsule/camera.
        public static bool TryFlipFrame(SkateBodyFlipState state, SkateBoardPose frame, SkateVector pivot,
            SkateVector axis, out SkateBoardPose transformed, out string error)
        {
            transformed = default(SkateBoardPose); error = null;
            if (!Valid(state) || !frame.Initialized || !SkateBoardPath.Valid(frame) || !Position(pivot) ||
                !Vector(axis) || Math.Abs(axis.LengthSquared - 1) > 0.0001)
            { error = "Invalid body flip frame."; return false; }
            transformed = new SkateBoardPose(FlipPoint(state, pivot, axis, frame.Origin),
                Turn(frame.Forward, axis, state.Degrees), Turn(frame.Up, axis, state.Degrees)); return true;
        }

        public static SkateVector FlipPoint(SkateBodyFlipState state, SkateVector pivot, SkateVector unitAxis, SkateVector point)
        { return pivot + Turn(point - pivot, unitAxis, state.Degrees); }

        public static bool Valid(SkateWallRideState state)
        {
            return state.Active ? state.Cooldown == 0 && Finite(state.Seconds) && state.Seconds > 0 && state.Seconds <= RideMaximumSeconds &&
                Vector(state.Normal) && Math.Abs(state.Normal.LengthSquared - 1) <= 0.0001 && Math.Abs(state.Normal.Y) <= 0.3 &&
                Vector(state.Travel) && Math.Abs(state.Travel.LengthSquared - 1) <= 0.0001 && Math.Abs(SkateVector.Dot(state.Normal, state.Travel)) <= 0.0001 :
                !state.Frontside && state.Seconds == 0 && state.Normal.LengthSquared == 0 && state.Travel.LengthSquared == 0 &&
                Finite(state.Cooldown) && state.Cooldown >= 0 && state.Cooldown <= RideRecaptureDelay;
        }

        public static bool Valid(SkateBodyFlipState state)
        {
            return state.Kind == SkateBodyFlipKind.None ? state.Progress == 0 && state.GrabSeconds == 0 :
                state.Kind >= SkateBodyFlipKind.Front && state.Kind <= SkateBodyFlipKind.Back &&
                Finite(state.Progress) && state.Progress >= 0 && state.Progress <= 1 && Finite(state.GrabSeconds) &&
                state.GrabSeconds >= 0 && state.GrabSeconds <= BodyFlipGrabSeconds;
        }

        private static bool Valid(SkateSurfaceContact c)
        { return !c.Found ? c.Point.LengthSquared == 0 && c.Normal.LengthSquared == 0 && !c.Ramp && !c.Coping :
            Position(c.Point) && Vector(c.Normal) && Math.Abs(c.Normal.LengthSquared - 1) <= 0.0001; }
        private static bool Wall(SkateVector position, SkateSurfaceContact contact, out double distance)
        {
            distance = contact.Found ? SkateVector.Dot(position - contact.Point, contact.Normal) : 0;
            return contact.Found && !contact.Ramp && !contact.Coping && Math.Abs(contact.Normal.Y) <= 0.3 &&
                distance >= 0.02 && distance <= RideReach && (position - contact.Point).Length <= 1.6;
        }
        private static SkateVector Horizontal(SkateVector value) { return new SkateVector(value.X, 0, value.Z); }
        private static SkateVector Project(SkateVector value, SkateVector normal) { return value - normal * SkateVector.Dot(value, normal); }
        private static SkateVector Unit(SkateVector value) { var length = value.Length; return length > 0.000001 ? value * (1 / length) : default(SkateVector); }
        private static SkateVector Cross(SkateVector a, SkateVector b) { return new SkateVector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
        private static SkateVector Turn(SkateVector value, SkateVector axis, double degrees)
        { var a = degrees * Math.PI / 180; var c = Math.Cos(a); var s = Math.Sin(a); return value * c + Cross(axis, value) * s + axis * (SkateVector.Dot(axis, value) * (1 - c)); }
        private static double Clamp(double value, double low, double high) { return Math.Max(low, Math.Min(high, value)); }
        private static bool Vector(SkateVector value) { return Finite(value.X) && Finite(value.Y) && Finite(value.Z); }
        private static bool Position(SkateVector value) { return Vector(value) && Math.Abs(value.X) <= 100000 && Math.Abs(value.Y) <= 100000 && Math.Abs(value.Z) <= 100000; }
        private static bool Velocity(SkateVector value) { return Vector(value) && value.Length <= 50; }
        private static bool Step(double dt) { return Finite(dt) && dt > 0 && dt <= 0.05; }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
