# Separate body and board frames in halfpipe airs

In ordinary free air the rider's body now turns gradually from the departure
normal toward world-up by the estimated apex. Near a sensed landing it turns
back toward the observed ramp normal. An actual grind retains its separate grind
alignment. This changes the previously incorrect rule that held the whole body
on its side throughout a steep air.

`SkateRide.RiderViewNormal` supplies the body frame. `ViewNormal` retains the
separate board/support frame, including the existing board pitch and landing
alignment. The rig still builds its foot and grab contacts against the actual
`DeckFrame`; leveling the body does not substitute a level board or move the
physical player. Planted feet, handplants, wall presentation and body flips retain
their existing frame owners.

The free-air timing uses elapsed release time, release rise and gravity. Rotation
is continuous and bounded. A landing approach keeps a fixed starting body-up and
a monotone share toward the observed plane; a missed probe cannot turn it back
toward world-up. The board helper preserves the previous support-frame behavior.

At most three `AIRFRAME` lines accompany a steep departure: departure, first
descent and first sensed landing approach. They report actual mode, whether
release came from a grind, body/board up, nose/deck yaw and existing display
angles. Flat departures stay quiet. No world position or player identity is
recorded. The logs are physics-step observations rather than rendered pixels.

The same four controlled air cases were regenerated before/after on the
reference skin and real board: two free departures, a grab and an explicit grind
control. The free body becomes upright; board/movement rows and the grind control
remain unchanged. These controls use no sensed landing plane. A separate bounded
landing example exercises the return to a sensed ramp despite a missed probe.
The existing grab reach solver can still fold the chest strongly even with the
new upright body basis; that grab silhouette is not approved.

The combined grab review also includes a moving transition return with a medium
three-quarter camera. It uses explicitly labeled offline capsule/mesh contact
emulation. A visible steep-support display gap and incomplete finger geometry
remain open; this is not native Unity collision or full-grip acceptance. Retain source compilation, CI, visible motion and native acceptance as
separate gates. The former outward top-deck transfer does not establish normal
halfpipe reentry. Native Shadow validation remains open.
