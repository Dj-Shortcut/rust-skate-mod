# Authored normal Kickflip upper body

`SkateCaptureKickflipUpper.Begin` binds the actual sampled Kickflip Catch pose
for the active stance. Its torso-local upper-arm and bend-plane directions are
retained; both elbows are relaxed to an authored 130 degrees with measured arm
IK. The selected source Catch has both hands below the shoulders, rather than
at the chin. An unusable reference refuses instead of substituting another clip.

`TryApply` uses the current upright carrier and real elapsed setup/owner time:
a C2 0.08-second entry targets an absolute 32-degree over-nose chest lean, then
returns over 0.18 seconds from the actual Catch/Land events. Negative Catch/Land
ages mean those events have not occurred. There is no future source cursor or
trick prediction during charging. The caller owns the ordinary Kickflip route;
a genuinely available neutral Load posture can use real charge age, while a
newly selected Pop owner starts from zero. Other tricks are not opted in.

The swing is around the unchanged pelvis. Hip, knee, ankle and toe coordinates,
pelvis facing/lean, board, sole frames and contacts stay exact. Chest/spine and
shoulder lengths are retained. The head follows the bent neck at its measured
length with source world gaze retained. Held-hand masks retain their exact world
hand targets; unreachable arms refuse through measured IK. This is authored
presentation, not measured airborne torso/arm motion or visual acceptance.

`TryChargeFold(pose, uprightCarrier, chargeWeight, out pose, out error)` supplies
the same absolute 32-degree swing with C2 of the real charge amount, without
any Catch-arm target or future family selection. Its upper body and arms move
rigidly, retaining all lengths; the head/gaze follows that ordinary rigid fold.
The ordinary-ground caller excludes action/held-hand owners and may carry the
actual charge through its short Air tail. The Kickflip owner seeds its real
setup age with that actual charge so a charged Pop does not restart upright.
