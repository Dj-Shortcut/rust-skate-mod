# Coupled rider and board frames in halfpipe airs

In ordinary free air the rider and skateboard turn together from the departure
normal toward world-up by the estimated apex. Near a sensed landing the same
assembly turns back toward the observed ramp normal. An actual grind retains its
own alignment. A steep departure no longer holds the rider sideways in free air.

`SkateRide.ViewNormal` supplies one common base for the board and rider's lower
body; `RiderViewNormal` reads that same value. Existing ollie pitch, grab tweaks,
flips and body flips remain relative rotations on top. Foot and grab contacts are
solved against the actual `DeckFrame`. Leveling only the torso while leaving the
base board vertical is explicitly rejected.

The free-air timing uses elapsed release time, release rise and gravity. Rotation
is continuous and bounded. A landing approach retains its starting up-vector and
a monotone share toward the observed plane; a missed probe cannot turn it away
from an approach already observed. Planted feet retain their support until
release; wall and handplant routes retain their own frame owners.

Ordinary board and rider placement also share the capsule-sphere display datum:
`radius * (worldUp - ViewNormal)` before Pop, Tilt and relative board rotations.
The radius comes from the player's collider. This rotates the displayed assembly
about the capsule sphere centre instead of leaving a visible gap at steep
support. The offset stays active through ordinary Ground and Air, and is exactly
zero on flat ground. Wall, handplant, independent action/oldschool, grind, bail,
scenario and showcase anchors are excluded. It does not change the physical
player, velocity, collision, landing detection or scoring.

At most three `AIRFRAME` lines accompany a steep departure: departure, first
descent and first sensed landing approach. They report mode, whether release
came from a grind, the common body/board base, nose/deck yaw and relative display
angles. Flat departures stay quiet. No world position or player identity is
recorded. Logs are physics-step observations rather than rendered pixels.

The four controlled cases cover two free departures, a grab and an explicit grind
control on the reference skin and real board. The original body-only comparison
is superseded. These controls have no sensed landing plane; separate transition
return previews use labeled offline capsule/mesh contact emulation. Source
compilation and bounded landing examples do not establish native Unity collision,
full hand-grip quality or Shadow gameplay acceptance. Native validation remains
open.
