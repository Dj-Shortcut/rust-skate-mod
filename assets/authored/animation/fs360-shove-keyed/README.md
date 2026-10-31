# Authored FS360 shove controls

The input is the pinned, unreviewed FS180 Kroetkov reconstruction. The baker
keeps that take's body directions and balance gesture, removes its carrier
translation, and authors the larger frontside scoop, free rear pickup and
later catch. The reference footage is FS180, not filmed FS360.

The board has full negative360 canonical yaw with no flip/body spin. Catch
transforms the complete anatomical position and toe heading into its final
basis. Fixed shoe/deck geometry and strict measured-leg fitting remain active.
The published controls and code do not distribute footage or grant approval.

Canonical Left is the front foot; Right is the rear foot. These are authored
contact estimates on the pinned FS180 source window, frames 190–252 at
29.97002997 fps, with an authored full-turn board and later catch:

| Event | Source frame | Contact estimate |
| --- | ---: | --- |
| Front release / Pop | 211 | Rear sole only |
| Rear release | 212 | Neither sole |
| Ground press ends | 214 | Neither sole |
| Full rotation ends | 223 | Neither sole |
| Front catch | 224 | Left sole |
| Rear catch | 224 | Right sole; both catches coincide |
| Land | 226 | Both soles; actual game collision remains authoritative |

`catchFeet` states each foot's catch frame, canonical world-space footprint
relative to the board origin, and toe heading: Left `[-0.04, 0, 0.24]` m at
65°, Right `[-0.04, 0, -0.24]` m at 85°. Heading runs from +Z toward +X.
The XYZ target uses Y=0 as its deck-plane datum; the pinned curved-deck/shoe
fit determines the actual ankle height. Both the footprint and heading are
transformed into the final −360° deck basis. The baker requires the explicit
front release to match Pop and both foot catches to match the shared catch event.

The controls SHA-pin the original FS JSON, measured-chain helper and Tre
geometry JSON; that geometry separately pins the shaped deck surface. Thigh,
shin, foot and toe-drop lengths come from the source's `metadata.rig`, as does
its hip width. The pinned input currently uses 0.44/0.43/0.14/0.07 m lengths.
Normal/low references describe the existing offline replay flight durations;
they do not establish a real FS360 controller gesture or native-game acceptance.
