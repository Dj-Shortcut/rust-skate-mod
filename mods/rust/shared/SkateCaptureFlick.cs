using System;
using System.Collections.Generic;

namespace Shortcut.RustMod
{
    public enum SkateCaptureFlickPhase { None, Setup, Slide, Edge, Return, Complete }
    // Authored geometry-inferred labels, not proof of a native material boundary.
    public enum SkateFlickSurfaceRegion { None, SoleSlide, UpperForefoot, Heel, Other }

    // Actual support feature of an AUTHORED presentation path, not a source event.
    public readonly struct SkateCaptureFlickContact
    {
        public readonly SkateCaptureFlickPhase Phase;
        public readonly SkateFlickSurfaceRegion Region;
        public readonly bool Heel, Left, HasSupport;
        public readonly int FootTriangle, DeckTriangle;
        public readonly SkateVector ShoeLocal, DeckLocal, ShoeWorld, TargetWorld;
        internal SkateCaptureFlickContact(SkateCaptureFlickPhase phase, bool heel, bool left,
            SkateFlickSurfaceRegion region, SkateDeckContactWitness witness, SkateBoardPose foot, SkateBoardPose deck)
        {
            Phase = phase; Heel = heel; Left = left; Region = region; HasSupport = witness.Initialized;
            FootTriangle = witness.Initialized ? witness.FootTriangle : -1;
            DeckTriangle = witness.Initialized ? witness.DeckTriangle : -1;
            ShoeWorld = TargetWorld = witness.Initialized ? witness.DeckPoint : default(SkateVector);
            ShoeLocal = witness.Initialized ? Local(witness.DeckPoint - foot.Origin, foot) : default(SkateVector);
            DeckLocal = witness.Initialized ? Local(witness.DeckPoint - deck.Origin, deck) : default(SkateVector);
        }
        private static SkateVector Local(SkateVector value, SkateBoardPose frame)
        { return new SkateVector(SkateVector.Dot(value, frame.Right), SkateVector.Dot(value, frame.Up), SkateVector.Dot(value, frame.Forward)); }
    }

    // One instance per owner. Anatomy is authored; the contact feature comes from
    // the same full measured shoe/deck fit as clearance, not a forced instep point.
    // Caller gates normal Kickflip/Heelflip and supplies the actual board trajectory.
    public sealed class SkateCaptureFlick
    {
        public const string Provenance = "Authored normal flip path and geometry-inferred regions; actual mesh support witness and upright release, not measured source contact. Frozen-model surfaces checked; native upper/outsole boundary unverified.";
        private readonly Shoe left, right;
        private readonly SkateDeckContactSurface surface;
        private readonly SkateVector heelSideRim, toeSideRim, centre;
        private bool released, ownerHeel;
        private SkateStance ownerStance;
        private double previousSeconds, ownerDuration;
        private SkateBoardPose releaseLocal;
        public SkateCaptureFlickContact Contact { get; private set; }

        public SkateCaptureFlick(SkateDeckContactSurface surface, SkateFootContactProfile leftProfile,
            SkateFootContactProfile rightProfile)
        {
            if (surface == null) throw new ArgumentNullException("surface");
            this.surface = surface;
            left = new Shoe(surface, leftProfile, -1); right = new Shoe(surface, rightProfile, 1);
            var rim = Boundary(surface);
            heelSideRim = Rim(surface, rim, -1); toeSideRim = Rim(surface, rim, 1);
            SkateVector point; string error;
            if (!TrySurfacePoint(surface, 0, surface.MaximumZ * .42, out point, out error)) throw new ArgumentException(error);
            centre = point;
        }

        public void Reset()
        { released = false; previousSeconds = ownerDuration = 0; releaseLocal = default(SkateBoardPose);
          Contact = default(SkateCaptureFlickContact); }

        // releaseDeck is an explicit unflipped release endpoint, transported to
        // this frame's world carrier. A first call after D computes that same
        // endpoint without retrospectively claiming that contact was displayed.
        public bool TryPose(SkateRiderRig rig, SkateCaptureFrame current, SkateRiderPose ordinary,
            SkateVector ordinaryLeftSoleUp, SkateVector ordinaryRightSoleUp,
            SkateBoardPose uprightCarrier, SkateBoardPose releaseDeck,
            double elapsedSeconds, double releaseSeconds, SkateStance stance, bool heel,
            out SkateCaptureFrame result, out string error)
        {
            result = current; error = null; Contact = default(SkateCaptureFlickContact);
            if (!Finite(elapsedSeconds) || elapsedSeconds < 0 || !Finite(releaseSeconds) || releaseSeconds <= 0 || releaseSeconds > .25 ||
                (stance != SkateStance.Regular && stance != SkateStance.Goofy) ||
                !Frame(uprightCarrier) || !Frame(releaseDeck) || !Frame(current.Board))
            { Reset(); error = "Invalid authored flick clock, stance or explicit carrier/deck frame."; return false; }
            if (elapsedSeconds < previousSeconds || ownerStance != stance || ownerHeel != heel || ownerDuration != releaseSeconds) Reset();
            ownerStance = stance; ownerHeel = heel; ownerDuration = releaseSeconds; previousSeconds = elapsedSeconds;
            double u = elapsedSeconds / releaseSeconds;
            if (u >= 4) return true;
            bool isLeft = stance == SkateStance.Regular;
            var shoe = isLeft ? left : right;
            SkateBoardPose ordinaryFoot;
            var oa = isLeft ? ordinary.LeftAnkle : ordinary.RightAnkle;
            var ot = isLeft ? ordinary.LeftToe : ordinary.RightToe;
            if (!SkateCaptureFootSupport.TryFootFrame(oa, ot, isLeft ? ordinaryLeftSoleUp : ordinaryRightSoleUp, out ordinaryFoot))
            { error = "Authored flick requires a measured ordinary front-foot frame."; return false; }
            double down = SkateVector.Dot(ot - oa, ordinaryFoot.Up), along = SkateVector.Dot(ot - oa, ordinaryFoot.Forward);
            if (!Finite(rig.FootLength) || rig.FootLength <= 0 || Math.Abs((ot - oa).Length - rig.FootLength) > .00001)
            { error = "Authored flick ordinary ankle/toe length does not match the rig."; return false; }
            var rim = (stance == SkateStance.Regular) == heel ? toeSideRim : heelSideRim;
            SkateBoardPose foot;
            var witness = default(SkateDeckContactWitness);
            var contactDeck = current.Board;
            SkateCaptureFlickPhase phase;
            if (u <= 1)
            {
                phase = u < .22 ? SkateCaptureFlickPhase.Setup : u < .55 ? SkateCaptureFlickPhase.Slide : SkateCaptureFlickPhase.Edge;
                contactDeck = u == 1 ? releaseDeck : current.Board;
                if (!TryFoot(contactDeck, uprightCarrier, shoe, stance, heel, u, rim, out foot, out error)) return false;
                if (u < .22) foot = Blend(ordinaryFoot, foot, Ease(u / .22));
                if (!TrySupport(contactDeck, shoe, stance, foot, out foot, out witness, out error)) return false;
                if (u == 1)
                {
                    if (!EdgeRegion(shoe, witness, contactDeck, stance, heel, out error)) return false;
                    releaseLocal = InFrame(foot, uprightCarrier); released = true;
                }
            }
            else
            {
                phase = SkateCaptureFlickPhase.Return;
                if (!released)
                {
                    SkateBoardPose endpoint; SkateDeckContactWitness endpointWitness;
                    if (!TryFoot(releaseDeck, uprightCarrier, shoe, stance, heel, 1, rim, out endpoint, out error) ||
                        !TrySupport(releaseDeck, shoe, stance, endpoint, out endpoint, out endpointWitness, out error) ||
                        !EdgeRegion(shoe, endpointWitness, releaseDeck, stance, heel, out error)) return false;
                    releaseLocal = InFrame(endpoint, uprightCarrier); released = true;
                }
                SkateBoardPose free;
                if (!SkateCaptureFootSupport.TryFootFrame(isLeft ? current.Pose.LeftAnkle : current.Pose.RightAnkle,
                    isLeft ? current.Pose.LeftToe : current.Pose.RightToe, isLeft ? current.LeftSoleUp : current.RightSoleUp, out free))
                { error = "Authored flick requires the current free-foot frame for its return."; return false; }
                foot = Blend(FromFrame(releaseLocal, uprightCarrier), free, Ease((u - 1) / 3));
                // Return has no support witness. It never follows the flipping deck.
            }
            var toe = foot.Origin + foot.Forward * along + foot.Up * down;
            SkateRiderPose posed;
            if (!SkateCaptureFootSupport.TrySetFeet(rig, current.Pose,
                isLeft ? foot.Origin : current.Pose.LeftAnkle, isLeft ? toe : current.Pose.LeftToe,
                isLeft ? current.Pose.RightAnkle : foot.Origin, isLeft ? current.Pose.RightToe : toe, false, out posed, out error)) return false;
            result = new SkateCaptureFrame(posed, current.Board, current.Rotation,
                isLeft ? foot.Up : current.LeftSoleUp, isLeft ? current.RightSoleUp : foot.Up,
                current.HasEvent, current.Observation, current.PelvisCorrection, current.LeftHandCorrection, current.RightHandCorrection, current.RiderUp);
            Contact = new SkateCaptureFlickContact(phase, heel, isLeft, shoe.Region(witness.FootTriangle, witness.Initialized), witness, foot, contactDeck);
            return true;
        }

        private bool EdgeRegion(Shoe shoe, SkateDeckContactWitness witness, SkateBoardPose deck, SkateStance stance, bool heel, out string error)
        {
            error = null;
            var region = shoe.Region(witness.FootTriangle, witness.Initialized);
            var point = witness.Initialized ? Local(witness.DeckPoint - deck.Origin, deck) : default(SkateVector);
            double side = stance == SkateStance.Regular ? 1 : -1;
            if (!witness.Initialized || region != (heel ? SkateFlickSurfaceRegion.Heel : SkateFlickSurfaceRegion.UpperForefoot))
            { error = "Authored flick endpoint is supported by another shoe region, not its upper-forefoot/heel patch."; return false; }
            if (point.Z < surface.MaximumZ * .65 || (heel ? side * point.X < 0 : side * point.X > 0))
            { error = "Authored flick patch contact leaves the matching nose-side region."; return false; }
            return true;
        }

        private bool TryFoot(SkateBoardPose deck, SkateBoardPose carrier, Shoe shoe, SkateStance stance, bool heel,
            double u, SkateVector rim, out SkateBoardPose foot, out string error)
        {
            double side = stance == SkateStance.Regular ? 1 : -1, roll = Ease((u - .55) / .45);
            var orientation = Blend(new SkateBoardPose(default(SkateVector), deck.Forward, deck.Up),
                new SkateBoardPose(default(SkateVector), carrier.Forward, carrier.Up), roll);
            var forward = SkateBoardPath.Turn(orientation.Forward, orientation.Up, side * (heel ? 90 : 45 + 30 * roll));
            var up = heel ? SkateBoardPath.Turn(orientation.Up, SkateBoardPath.Cross(orientation.Up, forward), -55 * roll) :
                SkateBoardPath.Turn(orientation.Up, forward, side * 40 * roll);
            if (heel) forward = SkateBoardPath.Turn(forward, SkateBoardPath.Cross(orientation.Up, forward), -55 * roll);
            else
            { var across = SkateBoardPath.Cross(up, forward); forward = SkateBoardPath.Turn(forward, across, -25 * roll);
              up = SkateBoardPath.Turn(up, across, -25 * roll); }
            SkateVector contact; foot = default(SkateBoardPose);
            if (!TrySlidePoint(stance, heel, u, rim, out contact, out error)) return false;
            var guide = Lerp(shoe.SoleGuide, heel ? shoe.HeelGuide : shoe.UpperGuide, roll);
            var basis = new SkateBoardPose(default(SkateVector), forward, up);
            // Region centroids describe the authored sweep only. They are never
            // presented as a pinned contact: the full-mesh witness supplies that.
            foot = new SkateBoardPose(deck.Point(contact) - basis.Rotate(guide), forward, up);
            return true;
        }

        private static bool TrySupport(SkateBoardPose deck, Shoe shoe, SkateStance stance, SkateBoardPose candidate,
            out SkateBoardPose fitted, out SkateDeckContactWitness witness, out string error)
        {
            double side = stance == SkateStance.Regular ? 1 : -1;
            bool sideways = SkateVector.Dot(candidate.Up, deck.Up) <= .2;
            var projection = sideways ? new SkateBoardPose(candidate.Origin, candidate.Forward,
                SkateBoardPath.Turn(candidate.Up, candidate.Forward, -side * 90)) : candidate;
            double offset; fitted = candidate;
            // Cached rigid basis conversion preserves every world vertex. The
            // original sole profile, source contact checks and guards are intact.
            if (!(sideways ? shoe.Sideways : shoe.Flat).TryOffset(deck, projection,
                SkateCaptureFootSupport.MaximumOffset, out offset, out witness, out error)) return false;
            fitted = new SkateBoardPose(candidate.Origin + deck.Up * offset, candidate.Forward, candidate.Up);
            return true;
        }

        private bool TrySlidePoint(SkateStance stance, bool heel, double u, SkateVector rim, out SkateVector point, out string error)
        {
            double side = stance == SkateStance.Regular ? 1 : -1;
            var p = Lerp(centre + new SkateVector(heel ? side * .035 : 0, 0, 0), rim, Ease((u - .22) / .78));
            if (u >= 1) { point = rim; error = null; return true; }
            return TrySurfacePoint(surface, p.X, p.Z, out point, out error);
        }

        private sealed class Shoe
        {
            internal readonly SkateVector SoleGuide, UpperGuide, HeelGuide;
            internal readonly SkateDeckFootFitter Flat, Sideways;
            private readonly SkateFlickSurfaceRegion[] regions;
            internal SkateFlickSurfaceRegion Region(int triangle, bool known)
            { return known && triangle >= 0 && triangle < regions.Length ? regions[triangle] : SkateFlickSurfaceRegion.None; }
            internal Shoe(SkateDeckContactSurface surface, SkateFootContactProfile profile, int outside)
            {
                if (profile == null) throw new ArgumentNullException("profile");
                double minY = double.PositiveInfinity, maxY = double.NegativeInfinity,
                    minZ = double.PositiveInfinity, maxZ = double.NegativeInfinity, width = 0;
                for (int n = 0; n < profile.VertexCount; n++)
                { var p = profile.Vertex(n); minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                  minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z); width = Math.Max(width, Math.Abs(p.X)); }
                double height = maxY - minY, length = maxZ - minZ;
                if (height <= 1e-6 || length <= 1e-6 || width <= 1e-6)
                    throw new ArgumentException("Authored flick requires volumetric measured shoe geometry.");
                regions = new SkateFlickSurfaceRegion[profile.TriangleCount];
                var sole = default(SkateVector); var upper = sole; var heel = sole;
                double soleArea = 0, upperArea = 0, heelArea = 0;
                for (int n = 0; n < profile.TriangleCount; n++)
                {
                    var a = profile.Vertex(profile.Index(n * 3)); var b = profile.Vertex(profile.Index(n * 3 + 1)); var c = profile.Vertex(profile.Index(n * 3 + 2));
                    var normal = SkateBoardPath.Cross(b - a, c - a); double area = normal.Length;
                    var mean = (a + b + c) * (1.0 / 3);
                    double z = (mean.Z - minZ) / length, low = Math.Min(a.Y, Math.Min(b.Y, c.Y)), high = Math.Max(a.Y, Math.Max(b.Y, c.Y));
                    bool isUpper = z >= .55 && low >= minY + height * .14 && high <= minY + height * .85 &&
                        outside * mean.X >= width * .25 && (normal.Y > 0 || outside * normal.X > 0);
                    bool isHeel = z <= .22 && low >= minY + height * .12 && high <= minY + height * .65 && normal.Z < 0;
                    bool isSole = high <= minY + height * .10;
                    regions[n] = isUpper ? SkateFlickSurfaceRegion.UpperForefoot : isHeel ? SkateFlickSurfaceRegion.Heel :
                        isSole ? SkateFlickSurfaceRegion.SoleSlide : SkateFlickSurfaceRegion.Other;
                    if (isUpper) { upper += mean * area; upperArea += area; }
                    if (isHeel) { heel += mean * area; heelArea += area; }
                    if (isSole && z >= .45 && z <= .80) { sole += mean * area; soleArea += area; }
                }
                if (soleArea <= 0 || upperArea <= 0 || heelArea <= 0)
                    throw new ArgumentException("Measured shoe lacks the required geometry-inferred lower, forefoot or rear-heel regions.");
                SoleGuide = sole * (1 / soleArea); UpperGuide = upper * (1 / upperArea); HeelGuide = heel * (1 / heelArea);
                Flat = new SkateDeckFootFitter(surface, profile);
                var xyz = new double[profile.VertexCount * 3]; var indices = new int[profile.TriangleCount * 3];
                for (int n = 0; n < profile.VertexCount; n++)
                { var p = SkateBoardPath.Turn(profile.Vertex(n), new SkateVector(0, 0, 1), -outside * 90);
                  xyz[n * 3] = p.X; xyz[n * 3 + 1] = p.Y; xyz[n * 3 + 2] = p.Z; }
                for (int n = 0; n < indices.Length; n++) indices[n] = profile.Index(n);
                SkateFootContactProfile rotated; string error;
                if (!SkateFootContactProfile.TryCreate(xyz, indices, out rotated, out error)) throw new ArgumentException(error);
                Sideways = new SkateDeckFootFitter(surface, rotated);
            }
        }

        private readonly struct Edge : IEquatable<Edge>
        {
            internal readonly SkateVector A, B;
            internal Edge(SkateVector a, SkateVector b)
            { bool swap = a.X > b.X || (a.X == b.X && (a.Y > b.Y || (a.Y == b.Y && a.Z > b.Z)));
              A = swap ? b : a; B = swap ? a : b; }
            public bool Equals(Edge other) { return (A - other.A).LengthSquared == 0 && (B - other.B).LengthSquared == 0; }
            public override bool Equals(object value) { return value is Edge && Equals((Edge)value); }
            public override int GetHashCode()
            { unchecked { return A.X.GetHashCode() ^ A.Y.GetHashCode() * 31 ^ A.Z.GetHashCode() * 97 ^
                  B.X.GetHashCode() * 193 ^ B.Y.GetHashCode() * 389 ^ B.Z.GetHashCode() * 769; } }
        }
        private static List<SkateVector> Boundary(SkateDeckContactSurface surface)
        {
            var edges = new Dictionary<Edge, int>();
            for (int n = 0; n < surface.TriangleCount; n++)
            { var t = surface.Triangle(n); Count(edges, new Edge(t.A, t.B)); Count(edges, new Edge(t.B, t.C)); Count(edges, new Edge(t.C, t.A)); }
            var result = new List<SkateVector>();
            foreach (var edge in edges) if (edge.Value == 1) { result.Add(edge.Key.A); result.Add(edge.Key.B); }
            return result;
        }
        private static void Count(Dictionary<Edge, int> edges, Edge edge)
        { int count; edges.TryGetValue(edge, out count); edges[edge] = count + 1; }
        private static SkateVector Rim(SkateDeckContactSurface surface, List<SkateVector> boundary, int side)
        {
            double width = side < 0 ? -surface.MinimumX : surface.MaximumX;
            double best = double.PositiveInfinity; var result = default(SkateVector);
            for (int n = 0; n < boundary.Count; n++)
            { var p = boundary[n]; if (p.Z < surface.MaximumZ * .65 || side * p.X <= 0) continue;
              double score = Square(p.Z - surface.MaximumZ * .82) + Square(p.X - side * width * .8);
              if (score < best) { best = score; result = p; } }
            if (!Finite(best)) throw new ArgumentException("Measured deck has no unambiguous nose-side top rim.");
            return result;
        }
        private static bool TrySurfacePoint(SkateDeckContactSurface surface, double x, double z,
            out SkateVector point, out string error)
        {
            for (int n = 0; n < surface.TriangleCount; n++)
            {
                var t = surface.Triangle(n); var p = new SkateVector(x, 0, z);
                double a = Cross2(t.B - t.A, p - t.A), b = Cross2(t.C - t.B, p - t.B), c = Cross2(t.A - t.C, p - t.C);
                if ((a >= -1e-12 && b >= -1e-12 && c >= -1e-12) || (a <= 1e-12 && b <= 1e-12 && c <= 1e-12))
                { point = new SkateVector(x, t.Height(new SkateDeckContactMath.Point(x, z)), z); error = null; return true; }
            }
            point = default(SkateVector); error = "Measured deck has no support triangle at the authored slide location."; return false;
        }
        private static double Cross2(SkateVector a, SkateVector b) { return a.X * b.Z - a.Z * b.X; }
        private static SkateBoardPose InFrame(SkateBoardPose foot, SkateBoardPose carrier)
        { return new SkateBoardPose(Local(foot.Origin - carrier.Origin, carrier), Local(foot.Forward, carrier), Local(foot.Up, carrier)); }
        private static SkateBoardPose FromFrame(SkateBoardPose foot, SkateBoardPose carrier)
        { return new SkateBoardPose(carrier.Point(foot.Origin), carrier.Rotate(foot.Forward), carrier.Rotate(foot.Up)); }
        private static SkateVector Local(SkateVector value, SkateBoardPose frame)
        { return new SkateVector(SkateVector.Dot(value, frame.Right), SkateVector.Dot(value, frame.Up), SkateVector.Dot(value, frame.Forward)); }
        private static SkateBoardPose Blend(SkateBoardPose a, SkateBoardPose b, double u)
        { var q = SkateCaptureRotation.Slerp(Rotation(a), Rotation(b), u);
          return new SkateBoardPose(Lerp(a.Origin, b.Origin, u), q.Rotate(new SkateVector(0, 0, 1)), q.Rotate(new SkateVector(0, 1, 0))); }
        private static SkateCaptureRotation Rotation(SkateBoardPose frame)
        {
            var r = frame.Right; var u = frame.Up; var f = frame.Forward; double x, y, z, w, s, trace = r.X + u.Y + f.Z;
            if (trace > 0) { s = Math.Sqrt(trace + 1) * 2; w = .25 * s; x = (u.Z - f.Y) / s; y = (f.X - r.Z) / s; z = (r.Y - u.X) / s; }
            else if (r.X > u.Y && r.X > f.Z) { s = Math.Sqrt(1 + r.X - u.Y - f.Z) * 2; w = (u.Z - f.Y) / s; x = .25 * s; y = (u.X + r.Y) / s; z = (f.X + r.Z) / s; }
            else if (u.Y > f.Z) { s = Math.Sqrt(1 + u.Y - r.X - f.Z) * 2; w = (f.X - r.Z) / s; x = (u.X + r.Y) / s; y = .25 * s; z = (f.Y + u.Z) / s; }
            else { s = Math.Sqrt(1 + f.Z - r.X - u.Y) * 2; w = (r.Y - u.X) / s; x = (f.X + r.Z) / s; y = (f.Y + u.Z) / s; z = .25 * s; }
            return new SkateCaptureRotation(x, y, z, w).Unit();
        }
        private static bool Frame(SkateBoardPose f) { return f.Initialized && SkateBoardPath.Valid(f); }
        private static bool Finite(double v) { return SkateDeckContactMath.Finite(v); }
        private static double Square(double v) { return v * v; }
        private static double Ease(double v) { v = Math.Max(0, Math.Min(1, v)); return v * v * v * (10 + v * (-15 + 6 * v)); }
        private static SkateVector Lerp(SkateVector a, SkateVector b, double u) { return a + (b - a) * u; }
    }
}
