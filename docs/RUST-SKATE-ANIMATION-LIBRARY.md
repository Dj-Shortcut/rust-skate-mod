# Full skate-animation library

The target is the whole requested game library in [TODO](../TODO.md), including
all applicable stances, directions, entry/exit paths and recovery. The 25 Berrics
selections are reference material, not a limit or a count of finished animations.

## Delivery order and current gates

| Batch | Coverage | Current gate |
| --- | --- | --- |
| 1 | Switch Heelflip, Nollie Heelflip, Fakie Kickflip, Nollie Backside 180 Heelflip, Nollie 360 Flip | Normal-route source exists. Inverse rotation timing and explicit head-authoring support added. Foot/contact quality, complete visual review and native acceptance remain open; no capture is approved by this change. |
| 2 | Riding, steering, crouch, normal/mongo push, ollie/nollie, braking, powerslide, revert, kickturn, both manuals, landing and upright roll-away | Existing procedural routes need visual review. Dedicated manual-revert, speed-check and kickturn actions are still missing. |
| 3 | All requested flip/shove families, fakie/switch/nollie variants, late/held flips, one-foot ollies, body spins, impossibles, underflip, dark catch and finger flip | Existing trick descriptors are not completed clips. Capture selection currently covers only batch 1. |
| 4 | All requested air grabs, tweaks and one-foot/no-foot variants | Existing grab catalogue and procedural presentation need hand/foot and motion review. Review airs above a halfpipe, including entry, hold, release, catch and landing. |
| 5 | All grinds, slides and stalls, darkslide, both exit directions | Existing action/pose source remains separate from approved capture playback. Rail, ledge and coping contact must be verified. |
| 6 | Ground grabs, coffin, boneless, beanplant, fastplant, no comply, caveman and hippie-jump variants | Existing source needs distinct per-action contact/motion review; shared generic poses do not count as individual finished animations. |
| 7 | Tail-on-coping drop-in, handplants, wallride/wallie, airs and lip transitions, front/backflips with grabs, bail and standing recovery | Supported-entry source does not supply the full drop-in. Contact routes, motion and native acceptance remain open. |

Finish the first five before processing the remaining families in batches.
Different stances can share or mirror data only after checking the resulting
contacts and timing. A source identifier or successful export does not establish
coverage. Keep failed candidates unreviewed and preserve the previous usable pack.

## Conversion and integration

Use the executable [capture pipeline](RUST-SKATE-MOTION-CAPTURE.md). Retain the
source window, trick identity, full board quaternion, phase/contact observations,
authoring provenance and uncertainty for every clip. Keep videos, model outputs,
private recipes and review evidence outside the published repository.

Use Berrics material for further flat-ground references and the owner's Xbox
footage as a grab reference. Failed body detections are not motion data; explicit
reconstruction must not inflate confidence or evade actor/gap rejection.
Occluded head authoring is opt-in and leaves its uncertainty in the JSON receipts.

The existing normal capture adapter remains limited to five exact flat-ground
variants. Later families need their own entry/hold/exit and contact rules rather
than relaxing that adapter's flat-ground exclusions. Ground, rail and coping
anchors must come from the active game action. Physics, board rotation, scoring,
catch and landing remain game-owned; animation does not move the player root.

## Use the actual board for visual contact review

The primary runtime board is `SkateBoardMesh.CreateDetailed()`: its rounded
outline, concave and kicked nose/tail are part of the contact geometry.
`assets/authored/skate_board.glb` is a separate authored rig and is not loaded by
the current board builder. Do not silently substitute that model or a rectangular
plank when claiming runtime contact accuracy.

For the detailed mesh, transform each rest vertex with
`world = liveFrame.Origin + liveRotation * (vertex - (0, 0.092, 0))`.
Preserve the rider's mesh, weights and bind pose when changing the review board.
Record the selected runtime board variant and geometry hash with the review.
Fallback board variants need their own check if they are used by the client.

Boxes and eight-corner floor envelopes are broad bounds, not the shaped deck.
A box overlap alone neither proves a shoe penetrates the real deck nor approves
clearance. Check the actual deck triangles and deformed shoe geometry, including
the nose, tail, side edge and between-key motion. Preserve earlier box results as
diagnostics, not as final contact evidence.

## Batch acceptance

Each batch supplies data, normal-play integration, comparison media and an
updated coverage record. Inspect normal-speed and slow playback from approach
through landing and recovery, on the rigged mesh. Check finite fixed-length
chains, contact continuity between source keys, visible penetration and pose
jumps, correct hands/feet, regular/goofy and applicable travel/trick variants.

Exercise interruptions, a changed trick, bail, repeated actions and competing
presentation owners. Dark catch may deliberately hold an inverted deck; drop-in
must descend from coping. Record these exceptions per trick instead of forcing
the ordinary flat-ground rules onto them.

Offline checks and source compilation are prerequisites, not native acceptance.
The owner tests the combined result on Shadow after offline review. Keep the
states **source implemented**, **offline reviewed** and **native accepted**
separate; conversion never grants approval or promotes the release pin.
