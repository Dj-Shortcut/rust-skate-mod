using System;

namespace Shortcut.RustMod
{
    // The five initial, explicitly selected flatground sources predate authored
    // board keys. Their ordinary air pitch never reaches the ground. Presentation
    // only: use the last real charged stance, then return to that clip's own Air.
    public sealed class SkateCaptureLegacyPop
    {
        public const string Provenance = "Authored initial-five pop from the observed charged stance and ground; real board-hull support and measured shoe fitting; physics and scoring remain authoritative.";
        private readonly SkateRiderPose groundPose;
        private readonly SkateBoardPose groundDeck;
        private readonly SkateVector groundLeftUp, groundRightUp, groundOrigin, groundNormal, groundVelocity;
        private readonly bool nosePop, leftPops, kickFlick, sourceCoupledRoot;
        private readonly double deckTop, sourcePop, sourceAir;
        private double rootSourceCursor = double.NaN, rootBlendAge;
        private SkateDeckFootFitter sideSource, sidePositive, sideNegative;
        private static readonly SkateBoardMesh Mesh = SkateBoardMesh.CreateDetailed();
        private const double StrikeAngle = 44.2, RootJoinSeconds = .30;
        private SkateCaptureLegacyPop(SkateRiderPose pose, SkateBoardPose deck, SkateVector lu, SkateVector ru,
            SkateVector origin, SkateVector normal, SkateVector velocity, double top, bool nose, double pop, double air, bool kick, bool coupleRoot)
        {
            groundPose = pose; groundDeck = deck; groundLeftUp = lu; groundRightUp = ru;
            groundOrigin = origin; groundNormal = Unit(normal); groundVelocity = velocity; deckTop = top; nosePop = nose; kickFlick = kick; sourcePop = pop; sourceAir = air;
            sourceCoupledRoot = coupleRoot;
            double l = SkateVector.Dot(pose.LeftAnkle - deck.Origin, deck.Forward);
            double r = SkateVector.Dot(pose.RightAnkle - deck.Origin, deck.Forward);
            leftPops = nose ? l > r : l < r;
        }
        public static bool Accepts(SkateCaptureClip clip, SkateCaptureTrickSignature signature)
        {
            if (clip == null || clip.Id != clip.MovementKind) return false;
            switch (clip.Id)
            {
                case "switch-heelflip": case "nollie-heelflip": case "fakie-kickflip":
                case "nollie-backside-180-heelflip": case "nollie-360-flip":
                    int n = SkateCaptureVariants.IndexOf(clip.MovementKind);
                    return n >= 0 && SkateCaptureVariants.Get(n).Signature.Same(signature);
                default: return false;
            }
        }
        public static bool TryCreate(SkateCaptureClip clip, SkateCaptureTrickSignature signature, SkateRiderPose pose, SkateBoardPose deck, SkateVector lu, SkateVector ru,
            SkateVector origin, SkateVector normal, SkateVector velocity, double top, bool nose,
            out SkateCaptureLegacyPop playback, out string error)
        {
            playback = null; error = null;
            if (!Accepts(clip, signature) || nose != (signature.Pop == SkatePopKind.Nollie) || !SkateBoardPath.Valid(deck) || !deck.Initialized || deck.Up.Y < .98 ||
                !Finite(top) || top <= 0 || top > .2 || !Finite(origin.LengthSquared) || !Finite(velocity.LengthSquared) ||
                !Finite(normal.LengthSquared) || normal.LengthSquared < .5 || normal.Y < .98 ||
                !Finite(lu.LengthSquared) || !Finite(ru.LengthSquared) || lu.LengthSquared < .5 || ru.LengthSquared < .5 ||
                !Finite(pose.Pelvis.LengthSquared) || Mesh.Parts.Length != 13)
            { error = "Observed legacy pop stance or board is invalid."; return false; }
            SkateBoardPose l, r;
            if (!SkateCaptureFootSupport.TryFootFrame(pose.LeftAnkle, pose.LeftToe, lu, out l) ||
                !SkateCaptureFootSupport.TryFootFrame(pose.RightAnkle, pose.RightToe, ru, out r))
            { error = "Observed legacy pop shoes have no measured frame."; return false; }
            double pop = -1, air = -1;
            for (int n = 0; n < clip.EventCount; n++)
            {
                var e = clip.GetEvent(n);
                if (pop < 0 && e.Phase == SkateCapturePhase.Pop) pop = e.Time;
                if (air < 0 && e.Phase == SkateCapturePhase.Air) air = e.Time;
            }
            if (pop < 0 || air <= pop)
            { error = "Initial-five pop needs its own ordered Pop/Air phases."; return false; }
            playback = new SkateCaptureLegacyPop(pose, deck, lu, ru, origin, normal, velocity, top, nose, pop, air, signature.Flip > 0,
                clip.Id == "fakie-kickflip" || clip.Id == "switch-heelflip"); return true;
        }
        // A held zero-degree scorer must not freeze the filmed Pop at its
        // first pose. The actual flick gate owns this short source Pop sequence;
        // once the observed rotation clock catches up it resumes unchanged.
        public void AdvancePop(SkateCapturePlayback playback, double elapsed, ref SkateCapturePhase phase, ref double value)
        {
            if (!Finite(elapsed) || elapsed < 0 || elapsed >= RootJoinSeconds) return;
            double current = phase == SkateCapturePhase.Air ? playback.FlightSourceTime(value) : playback.SourceTime(phase, value);
            double progress = Math.Min(.999999, elapsed / SkateFlipFlick.EdgeReleaseSeconds);
            if (current < sourcePop + progress * (sourceAir - sourcePop))
            { phase = SkateCapturePhase.Pop; value = progress; }
        }
        public bool TryApply(SkateRiderRig rig, SkateCaptureFrame input, SkateBoardPose carrier, double elapsed,
            SkateDeckFootFitter leftFitter, SkateDeckFootFitter rightFitter, out SkateCaptureFrame output, out string error,
            double? sourceSeconds = null)
        {
            output = input; error = null;
            if (!Finite(elapsed) || elapsed < 0 || !SkateBoardPath.Valid(input.Board) || !input.Board.Initialized ||
                !Finite(input.Pose.Pelvis.LengthSquared) || !SkateBoardPath.Valid(carrier) || !carrier.Initialized || carrier.Up.Y < .98 ||
                leftFitter == null || rightFitter == null)
            { error = "Invalid observed legacy pop carrier."; return false; }
            if (elapsed >= RootJoinSeconds) return true;
            var up = groundNormal; double gate = SkateFlipFlick.EdgeReleaseSeconds;
            double rootAge = elapsed;
            if (sourceCoupledRoot && sourceSeconds.HasValue)
            {
                if (!Finite(sourceSeconds.Value) || sourceSeconds.Value < 0)
                { error = "Observed legacy pop source cursor is invalid."; return false; }
                // After the real foot release, a held source pose must not keep
                // climbing through a different root-blend age. Physics, pressure
                // and flick still use elapsed; resume this age on observed progress.
                if (elapsed > gate * 2.4)
                {
                    // Seed at the release transition itself, not from the last
                    // pressure frame: absolute float time can straddle the gate.
                    if (!Finite(rootSourceCursor) || sourceSeconds.Value > rootSourceCursor + 1e-9)
                    { rootSourceCursor = sourceSeconds.Value; rootBlendAge = elapsed; }
                    rootAge = rootBlendAge;
                }
            }
            if (rootAge >= RootJoinSeconds) return true;
            double press = elapsed <= gate ? Smooth(elapsed / (gate * .7)) : 1 - Smooth((elapsed - gate) / gate);
            var board = input.Board; var rotation = input.Rotation;
            if (press > 0)
            {
                double pitch = Math.Asin(Math.Max(-1, Math.Min(1, SkateVector.Dot(board.Forward, up))));
                double wanted = (nosePop ? -1 : 1) * StrikeAngle * Math.PI / 180;
                var axis = board.Right; double half = -(wanted - pitch) * .5;
                var turn = new SkateCaptureRotation(axis.X * Math.Sin(half), axis.Y * Math.Sin(half), axis.Z * Math.Sin(half), Math.Cos(half));
                rotation = SkateCaptureRotation.Slerp(rotation, Multiply(turn, rotation), press);
                var forward = rotation.Rotate(new SkateVector(0, 0, 1)); var boardUp = rotation.Rotate(new SkateVector(0, 1, 0));
                // The ground is observed from the last real, supported board. The
                // complete original mesh supplies the lowest point, not a box.
                double floor = SkateVector.Dot(groundOrigin, up) + groundDeck.Origin.Y + Minimum(groundDeck.Forward, groundDeck.Up, new SkateVector(0, 1, 0));
                double support = Minimum(forward, boardUp, up);
                double required = floor - support - SkateVector.Dot(board.Origin, up);
                // Easing may approach the floor from above, but must apply the
                // full required lift once the newly pitched hull reaches it.
                var origin = board.Origin + up * Math.Max(required, required * press);
                board = new SkateBoardPose(origin, forward, boardUp);
            }
            var la = input.Pose.LeftAnkle; var lt = input.Pose.LeftToe; var ra = input.Pose.RightAnkle; var rt = input.Pose.RightToe;
            var lu = input.LeftSoleUp; var ru = input.RightSoleUp;
            double rear = 1 - Smooth((elapsed - gate) / (gate * .65));
            double front = kickFlick ? 1 - Smooth((elapsed - gate * 1.4) / gate) :
                1 - Smooth((elapsed - gate * 1.25) / (gate * 1.15));
            if (!Foot(rig, board, true, leftPops ? rear : front, elapsed, leftFitter, ref la, ref lt, ref lu, out error) ||
                !Foot(rig, board, false, leftPops ? front : rear, elapsed, rightFitter, ref ra, ref rt, ref ru, out error)) return false;
            var floorOrigin = carrier.Origin + up * SkateVector.Dot(groundOrigin - carrier.Origin, up);
            var heading = carrier.Forward - up * SkateVector.Dot(carrier.Forward, up);
            if (heading.LengthSquared < .5) { error = "Legacy pop ground heading is degenerate."; return false; }
            var groundedCarrier = new SkateBoardPose(floorOrigin, Unit(heading), up);
            var from = groundedCarrier.Point(groundPose.Pelvis + groundVelocity * elapsed);
            var root = from * (1 - Smooth(rootAge / RootJoinSeconds)) + input.Pose.Pelvis * Smooth(rootAge / RootJoinSeconds);
            var shift = root - input.Pose.Pelvis; var p = input.Pose;
            var moved = new SkateRiderPose(p.Pelvis + shift, p.LeftHip + shift, p.RightHip + shift,
                p.LeftKnee + shift, p.RightKnee + shift, p.LeftAnkle + shift, p.RightAnkle + shift,
                p.LeftToe + shift, p.RightToe + shift, p.Chest + shift, p.Neck + shift, p.Head + shift,
                p.LeftShoulder + shift, p.RightShoulder + shift, p.LeftElbow + shift, p.RightElbow + shift,
                p.LeftHand + shift, p.RightHand + shift, p.PelvisForward, p.PelvisUp, p.ChestForward, p.ChestUp, p.HeadForward);
            SkateRiderPose posed;
            if (!SkateCaptureFootSupport.TrySetFeet(rig, moved, la, lt, ra, rt, false, out posed, out error)) return false;
            var contacts = input.Observation.Contacts;
            if (elapsed < gate)
            {
                if ((leftPops ? rear : front) >= 1) contacts |= SkateCaptureContacts.LeftFoot;
                if ((leftPops ? front : rear) >= 1) contacts |= SkateCaptureContacts.RightFoot;
            }
            else if (elapsed < gate * 2.4)
                contacts &= ~(SkateCaptureContacts.LeftFoot | SkateCaptureContacts.RightFoot);
            var observation = new SkateCaptureEvent(input.Observation.Time, input.Observation.Phase, contacts);
            output = new SkateCaptureFrame(posed, board, rotation, lu, ru, input.HasEvent, observation,
                input.PelvisCorrection + shift, input.LeftHandCorrection, input.RightHandCorrection, input.RiderUp);
            return true;
        }
        private bool Foot(SkateRiderRig rig, SkateBoardPose board, bool left, double weight, double elapsed,
            SkateDeckFootFitter fitter, ref SkateVector ankle, ref SkateVector toe, ref SkateVector sole, out string error)
        {
            error = null; if (weight <= 0) return true;
            var a = left ? groundPose.LeftAnkle : groundPose.RightAnkle;
            var t = left ? groundPose.LeftToe : groundPose.RightToe;
            var u = left ? groundLeftUp : groundRightUp;
            var localA = Local(a - groundDeck.Origin, groundDeck);
            var flat = Local(t - a, groundDeck); var footUp = Local(u, groundDeck);
            var wantedA = board.Point(localA); var wantedToe = wantedA + board.Rotate(flat); var wantedUp = board.Rotate(footUp);
            SkateBoardPose foot;
            if (!SkateCaptureFootSupport.TryFootFrame(wantedA, wantedToe, wantedUp, out foot))
            { error = "Legacy press shoe frame is degenerate."; return false; }
            if (left != leftPops)
            {
                SkateBoardPose slid;
                double z = (nosePop ? -1 : 1) * (.06 + .24 * Smooth(elapsed / SkateFlipFlick.EdgeReleaseSeconds));
                if (!fitter.TryChargePlacement(board, foot, z, out slid, out error)) return false;
                foot = slid;
                if (kickFlick && !Instep(board, fitter, elapsed, ref foot, out error)) return false;
            }
            double offset;
            if (!Support(fitter, board, foot, out offset, out error)) return false;
            wantedA = foot.Origin + board.Up * (offset + SkateCaptureFootSupport.ContactBand);
            wantedToe = wantedA + foot.Forward * rig.FootLength;
            var oldForward = toe - ankle;
            ankle = ankle * (1 - weight) + wantedA * weight;
            sole = Unit(sole * (1 - weight) + foot.Up * weight);
            var heading = oldForward * (1 - weight) + (wantedToe - wantedA) * weight;
            heading -= sole * SkateVector.Dot(heading, sole);
            if (heading.LengthSquared < 1e-12 || sole.LengthSquared < .5)
            { error = "Legacy pop foot blend is degenerate."; return false; }
            toe = ankle + Unit(heading) * rig.FootLength; return true;
        }
        // Use a measured upper-side face, not a nominal sole roll. Mapping its
        // real normal against the grip also keeps the forefoot toe tangent up.
        private bool Instep(SkateBoardPose board, SkateDeckFootFitter fitter, double elapsed,
            ref SkateBoardPose foot, out string error)
        {
            error = null; double gate = SkateFlipFlick.EdgeReleaseSeconds;
            double roll = Smooth((elapsed - gate * .7) / (gate * .55));
            if (roll <= 0) return true;
            var release = board.Forward * (nosePop ? -1 : 1);
            double toeSide = SkateVector.Dot(foot.Forward, board.Right) >= 0 ? 1 : -1;
            var toe = foot.Forward;
            var flat = new SkateBoardPose(foot.Origin, toe, foot.Up);
            double side = SkateVector.Dot(flat.Right, release) >= 0 ? 1 : -1;
            var geometry = fitter.Profile;
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity, minZ = minY, maxZ = maxY, width = 0;
            for (int n = 0; n < geometry.VertexCount; n++)
            {
                var v = geometry.Vertex(n); minY = Math.Min(minY, v.Y); maxY = Math.Max(maxY, v.Y);
                minZ = Math.Min(minZ, v.Z); maxZ = Math.Max(maxZ, v.Z); width = Math.Max(width, Math.Abs(v.X));
            }
            double height = maxY - minY, length = maxZ - minZ, best = double.PositiveInfinity;
            var normal = default(SkateVector); var feature = normal;
            var wanted = new SkateVector(side * width * .90, minY + height * .34, minZ + length * .63);
            for (int n = 0; n < geometry.TriangleCount; n++)
            {
                var a = geometry.Vertex(geometry.Index(n * 3)); var b = geometry.Vertex(geometry.Index(n * 3 + 1));
                var c = geometry.Vertex(geometry.Index(n * 3 + 2)); var mean = (a + b + c) * (1.0 / 3);
                var face = Unit(SkateBoardPath.Cross(b - a, c - a));
                double z = (mean.Z - minZ) / length, y = (mean.Y - minY) / height;
                if (z < .5 || z > .78 || y < .20 || y > .68 || side * mean.X < width * .55 ||
                    side * face.X < .5 || Math.Abs(face.Z) > .2) continue;
                double cost = (mean - wanted).LengthSquared;
                if (cost < best) { best = cost; normal = face; feature = mean; }
            }
            if (normal.LengthSquared < .5)
            { error = "Measured flick shoe has no supported upper-side forefoot face."; return false; }
            // Close the measured side patch on the actual curved nose/tail
            // corner, whose grip normal differs from the nominal deck-up axis.
            double x = -toeSide * (fitter.Surface.MaximumX - fitter.Surface.MinimumX) * .395;
            double zTarget = (nosePop ? fitter.Surface.MinimumZ : fitter.Surface.MaximumZ) * .9;
            double closest = double.PositiveInfinity; var contact = default(SkateVector); var grip = contact;
            for (int n = 0; n < fitter.Surface.TriangleCount; n++)
            {
                var tri = fitter.Surface.Triangle(n);
                var centre = (tri.A + tri.B + tri.C) * (1.0 / 3);
                var nrm = Unit(SkateBoardPath.Cross(tri.B - tri.A, tri.C - tri.A));
                // The contact surface also contains bevels with a tiny upward
                // component. A flick closes on the riding skin, not that sidewall.
                if (nrm.Y < .7) continue;
                double cost = (centre.X - x) * (centre.X - x) + (centre.Z - zTarget) * (centre.Z - zTarget);
                if (cost < closest) { closest = cost; contact = centre; grip = nrm.Y < 0 ? nrm * -1 : nrm; }
            }
            if (grip.LengthSquared < .5)
            { error = "The measured flick corner has no riding surface."; return false; }
            var targetNormal = board.Rotate(grip) * -1;
            var localToe = Unit(new SkateVector(0, 0, 1) - normal * normal.Z);
            var localSide = Unit(SkateBoardPath.Cross(normal, localToe));
            var targetToe = Unit(toe - targetNormal * SkateVector.Dot(toe, targetNormal));
            var targetSide = Unit(SkateBoardPath.Cross(targetNormal, targetToe));
            var mappedUp = SurfaceMap(new SkateVector(0, 1, 0), localSide, normal, localToe, targetSide, targetNormal, targetToe);
            var mappedToe = SurfaceMap(new SkateVector(0, 0, 1), localSide, normal, localToe, targetSide, targetNormal, targetToe);
            var up = Unit(flat.Up * (1 - roll) + mappedUp * roll);
            var forward = flat.Forward * (1 - roll) + mappedToe * roll;
            forward = Unit(forward - up * SkateVector.Dot(forward, up));
            if (forward.LengthSquared < .5 || up.LengthSquared < .5)
            { error = "Measured upper-side flick orientation is degenerate."; return false; }
            var targetOrigin = board.Point(contact) -
                (SkateBoardPath.Cross(mappedUp, mappedToe) * feature.X + mappedUp * feature.Y + mappedToe * feature.Z);
            foot = new SkateBoardPose(flat.Origin * (1 - roll) + targetOrigin * roll, forward, up);
            return true;
        }
        private static SkateVector SurfaceMap(SkateVector v, SkateVector x, SkateVector y, SkateVector z,
            SkateVector toX, SkateVector toY, SkateVector toZ)
        { return toX * SkateVector.Dot(v, x) + toY * SkateVector.Dot(v, y) + toZ * SkateVector.Dot(v, z); }
        // Same rigid Sideways query used by the existing flick component.
        // Rotate the query basis AND every profile vertex inversely: the world
        // shoe volume and support/reach limits are unchanged. This is not a sole
        // contact assertion for a foot which is deliberately on its upper side.
        private bool Support(SkateDeckFootFitter fitter, SkateBoardPose board, SkateBoardPose foot,
            out double offset, out string error)
        {
            if (SkateVector.Dot(foot.Up, board.Up) > .2)
                return fitter.TryOffset(board, foot, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out offset, out error);
            offset = 0; error = null;
            var positive = SkateBoardPath.Turn(foot.Up, foot.Forward, 90);
            var negative = SkateBoardPath.Turn(foot.Up, foot.Forward, -90);
            double angle = SkateVector.Dot(positive, board.Up) >= SkateVector.Dot(negative, board.Up) ? 90 : -90;
            var projectedUp = angle > 0 ? positive : negative;
            if (SkateVector.Dot(projectedUp, board.Up) <= .2)
            { error = "The measured side-contact query has no upright projection."; return false; }
            if (!object.ReferenceEquals(sideSource, fitter))
            { sideSource = fitter; sidePositive = sideNegative = null; }
            var query = angle > 0 ? sidePositive : sideNegative;
            if (query == null)
            {
                var profile = fitter.Profile; var xyz = new double[profile.VertexCount * 3]; var indices = new int[profile.TriangleCount * 3];
                for (int n = 0; n < profile.VertexCount; n++)
                {
                    var v = SkateBoardPath.Turn(profile.Vertex(n), new SkateVector(0, 0, 1), -angle);
                    xyz[n * 3] = v.X; xyz[n * 3 + 1] = v.Y; xyz[n * 3 + 2] = v.Z;
                }
                for (int n = 0; n < indices.Length; n++) indices[n] = profile.Index(n);
                SkateFootContactProfile rotated;
                if (!SkateFootContactProfile.TryCreate(xyz, indices, out rotated, out error)) return false;
                query = new SkateDeckFootFitter(fitter.Surface, rotated);
                if (angle > 0) sidePositive = query; else sideNegative = query;
            }
            return query.TryOffset(board, new SkateBoardPose(foot.Origin, foot.Forward, projectedUp),
                SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out offset, out error);
        }
        private double Minimum(SkateVector forward, SkateVector up, SkateVector normal)
        {
            var right = SkateBoardPath.Cross(up, forward); double low = double.PositiveInfinity;
            for (int n = 0; n < Mesh.Positions.Length; n += 3)
            {
                var v = right * Mesh.Positions[n] + up * (Mesh.Positions[n + 1] - deckTop) + forward * Mesh.Positions[n + 2];
                low = Math.Min(low, SkateVector.Dot(v, normal));
            }
            return low;
        }
        private static SkateVector Local(SkateVector v, SkateBoardPose b) { return new SkateVector(SkateVector.Dot(v, b.Right), SkateVector.Dot(v, b.Up), SkateVector.Dot(v, b.Forward)); }
        private static SkateVector Unit(SkateVector v) { return v.LengthSquared > 1e-24 ? v * (1 / v.Length) : default(SkateVector); }
        private static double Smooth(double u) { u = Math.Max(0, Math.Min(1, u)); return u * u * u * (10 + u * (-15 + 6 * u)); }
        private static bool Finite(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }
        private static SkateCaptureRotation Multiply(SkateCaptureRotation a, SkateCaptureRotation b) { return new SkateCaptureRotation(a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y, a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X, a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W, a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z); }
    }
}
