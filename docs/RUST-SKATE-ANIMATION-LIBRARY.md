# Full skate-animation library

The target is the whole requested game library in [TODO](../TODO.md), including
all applicable stances, directions, entry/exit paths and recovery. The 25 Berrics
selections are reference material, not a limit or a count of finished animations.

## Delivery order and current gates

| Batch | Coverage | Current gate |
| --- | --- | --- |
| 1 | Switch Heelflip, Nollie Heelflip, Fakie Kickflip, Nollie Backside 180 Heelflip, Nollie 360 Flip | Viewer revision includes corrected head/neck frames, quicker recovery, relaxed cruise arms, targeted free-foot return and coupled trucks on the detailed board. Normal and slow visual review uses the reference skin; Rust/Shadow acceptance remains open. This source change does not approve or install a capture pack. |
| 2 | Riding, steering, crouch, normal/mongo push, ollie/nollie, braking, powerslide, revert, kickturn, both manuals, landing and upright roll-away | Existing procedural routes need visual review. Dedicated manual-revert, speed-check and kickturn actions are still missing. |
| 3 | All requested flip/shove families and stance/spin variants, composed from shared source families; separate references for exceptional foot/body actions | The registry contains 17 signatures: 11 base and 6 double/triple derivatives, including Hardflip. Impossible has a separate board-path adapter. A new source-led Nollie360 candidate replaces its older source within the same kind. Code/source presence is not a finished-animation count; per-trick visual review, exact pack approval and Shadow remain separate. Batch 1 and the remaining 40-reference coverage stay open. |
| 4 | All requested air grabs, tweaks and one-foot/no-foot variants | Existing grab catalogue and procedural presentation need hand/foot and motion review. Review airs above a halfpipe, including entry, hold, release, catch and landing. |
| 5 | All grinds, slides and stalls, darkslide, both exit directions | Existing action/pose source remains separate from approved capture playback. Rail, ledge and coping contact must be verified. |
| 6 | Ground grabs, coffin, boneless, beanplant, fastplant, no comply, caveman and hippie-jump variants | Existing source needs distinct per-action contact/motion review; shared generic poses do not count as individual finished animations. |
| 7 | Tail-on-coping drop-in, handplants, wallride/wallie, airs and lip transitions, front/backflips with grabs, bail and standing recovery | Supported-entry source does not supply the full drop-in. Contact routes, motion and native acceptance remain open. |

Finish the first five before processing the remaining families in batches.
Different stances can share or mirror data only after checking the resulting
contacts and timing. A source identifier or successful export does not establish
coverage. Keep failed candidates unreviewed and preserve the previous usable pack.

The owner's 28 October direction is visual delivery first: fix visible head/neck,
recovery, stiff cruise posture and shoe/deck errors in the viewer, spending at most
half a day per trick. Do not prolong this pass with further precision-measurement
rounds. Review normal and slow playback, compile, push the improvement, then add
new tricks through #584. Keep the later Shadow playtest as a separate gate.

## Batch 3: shared references, individual animations

The owner's [shared-source plan, #584](https://github.com/Dj-Shortcut/rust-skate-mod/issues/584)
targets ten source families in each of **normal, fakie, nollie and switch**:
40 reference slots, with a distinct generated animation and acceptance record for
every resulting trick. This count does not limit the full game library.

| Source family in each stance | Candidate reuse, subject to per-trick review |
| --- | --- |
| Ollie | Pop/body timing; one-foot variants need their own release execution |
| Kickflip | Toe-side flick, extra rotations and late kickflips |
| Heelflip | Heel-side flick, extra rotations and late heelflips |
| BS pop shove-it | Backside scoop, including 360 shove-it |
| FS pop shove-it | Frontside scoop, including 360 shove-it |
| 360 flip | Backside scoop plus toe flick; varial kickflip |
| Hardflip | Frontside scoop plus its distinct toe-flick path; 360 hardflip |
| Inward heelflip | Backside scoop plus heel flick; 360 inward heelflip |
| BS 180 | Backside body turn and candidate combined flip/spin motion |
| FS 180 | Frontside body turn and candidate combined flip/spin motion |

Retain stance-specific body/pop timing, each foot's flick/scoop and contact
trajectory, and game-owned full board rotation as separate inputs to composition.
Do not substitute a different stance's pop action. Mirror regular/goofy only
after checking both rendered executions, including toe/heel side and contacts.
Extra rotations, late flips and body spins must fit the active game phase; a
shared clip does not grant them the source trick's timing automatically.

Batch 1 supplies candidate references for fakie kickflip, nollie heelflip,
switch heelflip and nollie 360 flip. Its combined nollie BS180 heelflip is a
candidate for separating spin and heel-flick layers, not an approved standalone
BS180 reference. Hardflip-derived frontside scoop with a heel flick for varial
heelflip/laser remains an explicit investigation, not an established mapping.
Impossibles/front-foot impossibles, underflips, dark catches, finger flips,
boned ollies and body varials require distinct references where execution differs.
Batches 2 and 4–7 remain outside this 40-reference set.

Use #584's BATB counts as a source-search index; they do not identify suitable
timestamps, establish clip quality or count completed animations. The linked
[Swinburne dataset](https://doi.org/10.25916/sut.31043779.v1) is a reference index;
retain actual source windows, provenance and uncertainties for selected footage.

The first family adapter covers switch/nollie double and triple heelflips and
fakie double and triple kickflips. It preserves the source flick/release on the
first live turn and catch on the final turn; intermediate turns share the free
body motion. Separately approved held extensions retain the displayed source
progress when the underlying samples and contact events match. This does not
add airtime, change scoring or complete an unfinished physical rotation.

Other families still need their own selection and presentation adapters before
entering normal gameplay. Each result records its source IDs, stance, composition,
reconstruction and release/catch timing. Keep `reviewed=false` until that exact
result passes visual review on the real rig and detailed board in both stances.
Check every composed trick's flick/scoop against its actual full board rotation,
and retain the existing batch acceptance and separate native-game gate below.

## Conversion and integration

Use the executable [capture pipeline](RUST-SKATE-MOTION-CAPTURE.md). Retain the
source window, trick identity, full board quaternion, phase/contact observations,
authoring provenance and uncertainty for every clip. Keep videos, model outputs,
private recipes and review evidence outside the published repository.

Use Berrics material for further flat-ground references and the owner's Xbox
footage as a grab reference. Failed body detections are not motion data; explicit
reconstruction must not inflate confidence or evade actor/gap rejection.
Occluded head authoring is opt-in and leaves its uncertainty in the JSON receipts.

The normal capture registry covers 17 exact flat-ground signatures: 11 base
families (the five batch-1 kinds, normal kickflip/heelflip/360 flip, BS/FS pop
shove-it and Hardflip) plus 6 separately reviewable double/triple derivatives.
Impossible uses its own board-path adapter and is not an eighteenth registry
signature. The source-keyed Nollie360 candidate replaces its older source within
the existing kind; it does not add another signature or approve the original
pack. These are implemented selection paths, not a count of finished animations.
Batch 1 remains open as a group. Every result retains independent viewer, exact
pack-approval and native Shadow gates. Later families need their own entry/hold/
exit and contact rules rather than relaxing the flat-ground exclusions. Ground, rail and coping
anchors must come from the active game action. Physics, board rotation, scoring,
catch and landing remain game-owned; animation does not move the player root.

For ordinary halfpipe airs, review the coupled rider and board through departure,
free flight and re-entry into the transition. Do not automatically straighten the
rider into a grind posture at the apex or merely because coping is nearby. Only
an actually entered grind may own that transition; existing grind source does
not establish a working in-game grind transition. Genuine landing preparation
must follow the actual landing surface. The translated ground-crouch previews,
including their later yaw-only correction, are rejected context examples, not
approved air poses. A lifted static pose cannot validate the flight orientation.

## Use the actual board for visual contact review

The primary runtime board is `SkateBoardMesh.CreateDetailed()`: its rounded
outline, concave and kicked nose/tail are part of the contact geometry.
`assets/authored/skate_board.glb` is a separate authored rig and is not loaded by
the current board builder. Do not silently substitute that model or a rectangular
plank when claiming runtime contact accuracy.

For the rigid detailed deck, transform each rest vertex with
`world = liveFrame.Origin + liveRotation * (vertex - (0, 0.092, 0))`.
The [truck rig](RUST-SKATE-BOARD-RIG.md) additionally requires the actual hanger
transforms, attached wheel steering/spin and bushing deformation. Preserve its
part hierarchy and fixed rest rotations; flattening it into the deck loses the
coupled steering and suspension even when the outline is correct.
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
An ankle must also turn in an anatomically plausible direction relative to its
lower-leg and bind frame. Check bend, axial twist and the deformed cuff; sole-up
angle or deck clearance alone cannot approve a shoe pose. Source-image depth
ambiguities must be resolved with anatomical evidence before collision ranking.

Exercise interruptions, a changed trick, bail, repeated actions and competing
presentation owners. Dark catch may deliberately hold an inverted deck; drop-in
must descend from coping. Record these exceptions per trick instead of forcing
the ordinary flat-ground rules onto them.

Offline checks and source compilation are prerequisites, not native acceptance.
The owner tests the combined result on Shadow after offline review. Keep the
states **source implemented**, **offline reviewed** and **native accepted**
separate; conversion never grants approval or promotes the release pin.
