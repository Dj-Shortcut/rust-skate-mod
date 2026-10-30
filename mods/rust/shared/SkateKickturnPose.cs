using System;

namespace Shortcut.RustMod
{
    // Authored weight transfer over a measured axle, with the real shoe and
    // formed deck supplying its support. This does not move the physics body.
    public static class SkateKickturnPose
    {
        public static bool TryApply(SkateRiderRig rig, SkateRiderPose body, SkateBoardPose deck,
            SkateVector groundUp, SkateVector axle, int side, double weight,
            SkateVector leftUp, SkateVector rightUp, SkateDeckFootFitter leftFitter, SkateDeckFootFitter rightFitter,
            out SkateRiderPose posed, out SkateVector shownLeftUp, out SkateVector shownRightUp, out string error)
        {
            posed = body; shownLeftUp = leftUp; shownRightUp = rightUp; error = null;
            if (!SkateBoardPath.Valid(deck) || !deck.Initialized || (side != -1 && side != 1) ||
                !Finite(weight) || weight < 0 || weight > 1 || !Finite(axle.LengthSquared) ||
                Math.Abs(groundUp.LengthSquared - 1) > .001 || groundUp.Y < .9995 ||
                leftFitter == null || rightFitter == null || rig.ThighLength <= 0 || rig.ShinLength <= 0)
            { error = "Invalid measured kickturn preparation."; return false; }
            if (weight == 0) return true;
            SkateBoardPose lf, rf;
            if (!SkateCaptureFootSupport.TryFootFrame(body.LeftAnkle, body.LeftToe, leftUp, out lf) ||
                !SkateCaptureFootSupport.TryFootFrame(body.RightAnkle, body.RightToe, rightUp, out rf))
            { error = "Kickturn sole frame is degenerate."; return false; }
            double order = SkateVector.Dot(body.LeftAnkle - body.RightAnkle, deck.Forward);
            if (Math.Abs(order) < .05) { error = "Kickturn loaded foot is ambiguous."; return false; }
            bool loadedLeft = order * side > 0;
            SkateBoardPose target;
            if (!TailFrame(deck, loadedLeft ? lf : rf, loadedLeft ? leftFitter : rightFitter, side, out target, out error)) return false;
            var targetLeft = loadedLeft ? target.Origin : lf.Origin;
            var targetRight = loadedLeft ? rf.Origin : target.Origin;
            // Carry the complete trunk over the actual loaded axle. Choose a
            // height within both fixed leg chains, without moving that axle goal.
            var shift = axle - body.Pelvis; shift -= groundUp * SkateVector.Dot(shift, groundUp);
            double height = SkateVector.Dot(body.Pelvis, groundUp);
            double reach = (rig.ThighLength + rig.ShinLength) * .95;
            if (!Height(body.LeftHip + shift, targetLeft, groundUp, reach, ref height, body.LeftHip - body.Pelvis) ||
                !Height(body.RightHip + shift, targetRight, groundUp, reach, ref height, body.RightHip - body.Pelvis))
            { error = "Kickturn axle weight exceeds measured leg reach."; return false; }
            shift += groundUp * (height - SkateVector.Dot(body.Pelvis, groundUp));
            var world = new SkateBoardPose(default(SkateVector), new SkateVector(0, 0, 1), new SkateVector(0, 1, 0));
            SkateRiderPose moved;
            if (!SkateRider.TryTransformPose(body, world, new SkateBoardPose(shift * weight, world.Forward, world.Up), out moved))
            { error = "Kickturn body transfer is invalid."; return false; }
            var la = body.LeftAnkle * (1 - weight) + targetLeft * weight;
            var ra = body.RightAnkle * (1 - weight) + targetRight * weight;
            var lu = loadedLeft ? Unit(leftUp * (1 - weight) + target.Up * weight) : leftUp;
            var ru = loadedLeft ? rightUp : Unit(rightUp * (1 - weight) + target.Up * weight);
            var ltoe = Unit(Project(lf.Forward, lu)); var rtoe = Unit(Project(rf.Forward, ru));
            double lo, ro;
            if (!leftFitter.TryOffset(deck, new SkateBoardPose(la, ltoe, lu), SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out lo, out error) ||
                !rightFitter.TryOffset(deck, new SkateBoardPose(ra, rtoe, ru), SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out ro, out error)) return false;
            la += deck.Up * (lo + SkateCaptureFootSupport.ContactBand); ra += deck.Up * (ro + SkateCaptureFootSupport.ContactBand);
            if (!SkateCaptureFootSupport.TrySetFeet(rig, moved, la, la + ltoe * rig.FootLength,
                ra, ra + rtoe * rig.FootLength, false, out posed, out error)) return false;
            shownLeftUp = lu; shownRightUp = ru; return true;
        }

        private static bool Height(SkateVector hip, SkateVector foot, SkateVector up, double reach,
            ref double height, SkateVector hipOffset)
        {
            var flat = Project(hip - foot, up); double vertical2 = reach * reach - flat.LengthSquared;
            if (!(vertical2 > 0)) return false;
            height = Math.Min(height, SkateVector.Dot(foot, up) + Math.Sqrt(vertical2) - SkateVector.Dot(hipOffset, up)); return true;
        }

        private static bool TailFrame(SkateBoardPose deck, SkateBoardPose foot, SkateDeckFootFitter fitter,
            int side, out SkateBoardPose placed, out string error)
        {
            placed = foot; error = null; double z = side * .32;
            var surface = fitter.Surface; SkateVector normal = default(SkateVector); double highest = double.NegativeInfinity;
            for (int i = 0; i < surface.TriangleCount; i++)
            {
                var t = surface.Triangle(i);
                double a = (t.B.X - t.A.X) * (z - t.A.Z) + (t.B.Z - t.A.Z) * t.A.X;
                double b = (t.C.X - t.B.X) * (z - t.B.Z) + (t.C.Z - t.B.Z) * t.B.X;
                double c = (t.A.X - t.C.X) * (z - t.C.Z) + (t.A.Z - t.C.Z) * t.C.X;
                if (!((a >= -1e-10 && b >= -1e-10 && c >= -1e-10) || (a <= 1e-10 && b <= 1e-10 && c <= 1e-10))) continue;
                double y = t.A.Y + t.HeightX * -t.A.X + t.HeightZ * (z - t.A.Z);
                if (y <= highest) continue;
                highest = y; normal = Unit(deck.Rotate(new SkateVector(-t.HeightX, 1, -t.HeightZ)));
            }
            if (normal.LengthSquared < .9) { error = "Kickturn tail has no measured support face."; return false; }
            var oriented = new SkateBoardPose(foot.Origin, Unit(Project(foot.Forward, normal)), normal);
            if (!fitter.TryChargePlacement(deck, oriented, z, out placed, out error)) return false;
            double offset;
            if (!fitter.TryOffset(deck, placed, SkateCaptureFootSupport.MaximumOffset - SkateCaptureFootSupport.ContactBand, out offset, out error)) return false;
            placed = new SkateBoardPose(placed.Origin + deck.Up * (offset + SkateCaptureFootSupport.ContactBand), placed.Forward, placed.Up); return true;
        }
        private static SkateVector Project(SkateVector value, SkateVector normal) { return value - normal * SkateVector.Dot(value, normal); }
        private static SkateVector Unit(SkateVector value) { return value.LengthSquared > 1e-12 ? value * (1 / value.Length) : default(SkateVector); }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
