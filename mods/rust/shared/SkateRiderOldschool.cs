using System;

namespace Shortcut.RustMod
{
    public static partial class SkateRider
    {
        // input is the real rider/carrier frame, independent of state.Deck. In a Hippie
        // jump the body rises while that deck keeps rolling on its observed floor.
        public static bool TryCreateIndependentDeck(SkateRiderRig rig, SkateRiderInput input, SkateOldschoolState state,
            SkateVector handTarget, double handFollow, bool leftHand, out SkateRiderPose pose, out SkateBoardPose frame, out string error)
        {
            pose = default(SkateRiderPose); frame = state.Deck; error = null;
            if (!Valid(rig) || !Valid(input) || input.Bailed || input.Pushing || input.Handplant > 0 || input.BoardPath.Known ||
                !input.DeckFrame.Initialized || !SkateOldschool.Valid(state) || state.Phase == SkateOldschoolPhase.Failed ||
                !frame.Initialized || !SkateBoardPath.Valid(frame) || !Vector(handTarget) || !Fraction(handFollow))
            { error = "Invalid independent skateboard pose."; return false; }

            SkateRiderPose basis;
            if (!TryCreate(rig, WithGrab(input, SkateGrabKind.None, 0), out basis, out error)) return false;
            double contact = state.FootContact;
            double release;
            if (state.Kind == SkateOldschoolKind.HippieJump) release = 1 - contact;
            else if (state.Phase == SkateOldschoolPhase.Released)
                release = Smooth(Math.Max(0, Math.Min(1, state.PhaseSeconds / SkateOldschool.FootReleaseSeconds)));
            else release = state.Phase == SkateOldschoolPhase.Catching ? 1 - contact : 0;
            double tuck = input.Airborne ? .13 * (rig.ThighLength + rig.ShinLength) * release : 0;
            var up = Unit(input.BoardUp);
            double barefoot = 0;
            if (state.Kind == SkateOldschoolKind.Caveman)
            {
                if (state.Phase == SkateOldschoolPhase.Pickup || state.Phase == SkateOldschoolPhase.Carrying)
                    barefoot = input.Airborne ? SkateOldschool.DeckClearance : input.GroundDrop;
                else if (state.Phase == SkateOldschoolPhase.Released)
                    barefoot = SkateOldschool.DeckClearance * (1 - release);
            }
            // A grounded Caveman is on his feet while the board is in his hand. Retain
            // that barefoot offset through take-off and ease it out with the hand release.
            var freeLeft = basis.LeftAnkle + up * (tuck - barefoot);
            var freeRight = basis.RightAnkle + up * (tuck - barefoot);
            double ankle = SkatePose.FootLift - input.DeckOffset;

            // Start at the world-body foot locations, then clip to the actual footprint.
            // A shove must not rotate or exchange the anatomical feet with the deck.
            var attachedLeft = IndependentFootOnDeck(basis.LeftAnkle, frame, ankle);
            var attachedRight = IndependentFootOnDeck(basis.RightAnkle, frame, ankle);
            var la = Blend(freeLeft, attachedLeft, contact);
            var ra = Blend(freeRight, attachedRight, contact);
            var sole = Unit(Blend(up, frame.Up, contact));
            var leftToe = Unit(Project(basis.LeftToe - basis.LeftAnkle, sole));
            var rightToe = Unit(Project(basis.RightToe - basis.RightAnkle, sole));
            if (sole.LengthSquared < .5 || leftToe.LengthSquared < .5 || rightToe.LengthSquared < .5)
            { error = "Independent skateboard has no valid catch orientation."; return false; }
            double leftDrop = Math.Max(-rig.FootLength * .8, Math.Min(rig.FootLength * .8, SkateVector.Dot(basis.LeftToe - basis.LeftAnkle, up)));
            double rightDrop = Math.Max(-rig.FootLength * .8, Math.Min(rig.FootLength * .8, SkateVector.Dot(basis.RightToe - basis.RightAnkle, up)));
            var lt = la + leftToe * Math.Sqrt(rig.FootLength * rig.FootLength - leftDrop * leftDrop) + sole * leftDrop;
            var rt = ra + rightToe * Math.Sqrt(rig.FootLength * rig.FootLength - rightDrop * rightDrop) + sole * rightDrop;
            var targets = new SkateOllieTargets(frame, la, ra, lt, rt, sole, sole, contact, contact, 0);
            SkateVector shift, lk, rk;
            if (!SkateOllieStyles.TryLegs(rig, input.Stance, basis, targets, out shift, out lk, out rk, out error)) return false;
            var ls = basis.LeftShoulder + shift; var rs = basis.RightShoulder + shift;
            var lh = basis.LeftHand + shift; var rh = basis.RightHand + shift;
            SkateVector le, re;
            if (!SkateOllieStyles.TryLimb(ls, lh, rig.UpperArmLength, rig.ForearmLength, basis.LeftElbow - basis.LeftShoulder, out le, out error) ||
                !SkateOllieStyles.TryLimb(rs, rh, rig.UpperArmLength, rig.ForearmLength, basis.RightElbow - basis.RightShoulder, out re, out error)) return false;
            pose = new SkateRiderPose(basis.Pelvis + shift, basis.LeftHip + shift, basis.RightHip + shift, lk, rk, la, ra, lt, rt,
                basis.Chest + shift, basis.Neck + shift, basis.Head + shift, ls, rs, le, re, lh, rh,
                basis.PelvisForward, basis.PelvisUp, basis.ChestForward, basis.ChestUp, basis.HeadForward);

            bool gripping = state.HandContact > .99;
            if (gripping || handFollow > 0)
            {
                var goal = gripping ? state.Grip : Blend(leftHand ? pose.LeftHand : pose.RightHand, handTarget, handFollow);
                if (!ActionHandPose(rig, pose, goal, leftHand, out pose, out error)) return false;
            }
            // The independent flying/rolling deck is never mistaken for the rider's ground.
            // A grounded pickup/catch must respect actual body support; an airborne pose uses
            // only a conservative body-owned lower bound, like the other airborne actions.
            var floor = input.Airborne ? input.BoardPosition - up * (rig.ThighLength + rig.ShinLength + .5) :
                input.BoardPosition + up * (Math.Min(0, input.DeckOffset) - input.GroundDrop);
            if (!Finite(pose) || !AboveGround(pose, floor, up))
            { pose = default(SkateRiderPose); error = "Independent skateboard contacts intersect rider support."; return false; }
            return true;
        }

        private static SkateVector IndependentFootOnDeck(SkateVector foot, SkateBoardPose deck, double ankle)
        {
            var offset = foot - deck.Origin;
            // Catch reach is a body-centre envelope, not extra deck width. Bring each
            // anatomical foot onto the real top footprint through the contact blend.
            double across = Math.Max(-.09, Math.Min(.09, SkateVector.Dot(offset, deck.Right)));
            double along = Math.Max(-.36, Math.Min(.36, SkateVector.Dot(offset, deck.Forward)));
            return deck.Point(new SkateVector(across, ankle, along));
        }
    }
}
