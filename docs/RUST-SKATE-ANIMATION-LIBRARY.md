# Full skate-animation library

The target is the whole requested game library in [TODO](../TODO.md), including
all applicable stances, directions, entry/exit paths and recovery. The 25 Berrics
selections are reference material, not a limit or a count of finished animations.

## Delivery order and current gates

| Batch | Coverage | Current gate |
| --- | --- | --- |
| 1 | Switch Heelflip, Nollie Heelflip, Fakie Kickflip, Nollie Backside 180 Heelflip, Nollie 360 Flip | Offline viewer review completed on 30 October at main `5cbbf2f7`: twenty full normal/low, Regular/Goofy replays on the frozen skin and thirteen-part board. Deep Ground preparation, pop/release, Air, catch, landing and roll-away were inspected beside the source, including slow motion. Original pack approval, native controller binding and Shadow remain open. |
| 2 | Riding, steering, crouch, normal/mongo push, ollie/nollie, braking, powerslide, revert, kickturn, both manuals, landing and upright roll-away | Procedural source on `main`, visual review open: grounded low-speed axle kickturns with tail-foot and rear-axle loading before the lift (#647, #655), grounded reverts kept on the wheel plane (#638) and powerslide (speed-check) ground contact (#635) are source present with native acceptance open. A dedicated manual-revert action is still missing. |
| 3 | All requested flip/shove families and stance/spin variants, composed from shared source families; separate references for exceptional foot/body actions | The registry contains 21 signatures: 14 base (the five batch-1 kinds, normal kickflip/heelflip/360 flip, BS/FS pop shove-it, Hardflip, Varial Kickflip, BS and FS 360 shove-it) and 7 double/triple derivatives (the held normal Double Kickflip among them). Impossible has a separate board-path adapter. Source-keyed Nollie360 and BS Pop Shove-it replace older sources within their existing kinds. Code/source presence is not a finished-animation count; the remaining 40-reference coverage, per-trick review, exact pack approval and Shadow stay open. |
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

### Reviewed batch-1 snapshot, 30 October

| Trick | Selected clip ID | Offline viewer | Native Rust/Shadow |
| --- | --- | --- | --- |
| Switch Heelflip | `switch-heelflip` | Reviewed, normal/low and both physical stances | Pending |
| Nollie Heelflip | `nollie-heelflip` | Reviewed, normal/low and both physical stances | Pending |
| Fakie Kickflip | `fakie-kickflip` | Reviewed, normal/low and both physical stances | Pending |
| Nollie BS180 Heelflip | `nollie-backside-180-heelflip` | Reviewed, normal/low and both physical stances | Pending |
| Nollie 360 Flip | `nollie-360-flip-source-keyed-v1` | Reviewed, normal/low and both physical stances | Pending |

The integrated replay uses actual Pad/Ground, Flicking and TakePop transitions.
The ordinary charged pose persists across recognition, with deeper authored
Ground preparation and the same measured shoe anchors. Review covered the new
preparation-to-pop transition and post-landing stability, not just isolated Air
poses. The separate truck trace shows the actual opposed hanger/axle steering
and bushing deformation; it uses controlled source-fixture inputs and does not
establish native grip forces or a Rust ride.

This is a bounded offline review at the pinned source revision. The original
packs remain `Reviewed=false`; their production approval, installation and game
test are separate. Raw diagnostic failures remain visible and no thresholds
were weakened. Physical Regular/Goofy labels follow the actual companion skin
feet: this exporter swaps native left/right names, so native requested stance
is recorded separately until the controller binding can be checked on Shadow.

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

### Coverage record: the 40 shared-source slots

Kept by Claude as the quality gate (owner direction of 31 October 2026, #337);
Codex builds and merges. One row per source family, one cell per stance. A cell
names the state and, where there is one, the clip on `main`: **native accepted**
(seen in the game), **offline reviewed** (viewer, normal/low, both physical stances,
30 October snapshot above), **source present** (registry kind, keyed route and
authored controls on `main`, `reviewed=false`, not yet viewed as a whole), **kind
only** (registry signature without a source route), **open PR** (draft, not on
`main`), or **none**. Source video ids are the Berrics references the authoring
docs name; the footage itself stays private. State as of `main` `4cc6eec`.

| Source family | Normal | Fakie | Nollie | Switch |
| --- | --- | --- | --- | --- |
| Ollie | none for normal riding (the CC BY-SA video ollie in `ollie-video/` is sampled by the rigging showcase only) | none | none | none |
| Kickflip | source present: `kickflip-source-keyed-v1` (`kickflip-keyed/`, authored from reference images); held Double on the same pop: `double-kickflip-source-keyed-v1` source present (#662, #663), bound to #659's native held-flip clock | **offline reviewed**: `fakie-kickflip` (`fakie-kickflip-air/`), double/triple derivatives registered | none | none |
| Heelflip | source present: `heelflip-source-keyed-v1` (`heelflip-keyed/`, `_oRoo1x1uzM` 136–158 s) | none | **offline reviewed**: `nollie-heelflip`, double/triple registered | **offline reviewed**: `switch-heelflip` (`switch-heelflip-air/`, `PqJZcwTY1QE` t=74), double/triple registered |
| BS pop shove-it | source present: `bs-pop-shove-it-source-keyed-v1` (`bs-pop-shove-keyed/`, Cole `YcmPDIj2BFo` t=128); BS 360: `bs-360-shove-it-source-keyed-v1` source present (#602), the genuine BATB12 Kroetkov take with authored pickup, fold and arm depth | none | none | none |
| FS pop shove-it | kind only (`fs-pop-shove-it` registered, no keyed route or authored controls on `main`); FS 360: `fs-360-shove-it-source-keyed-v1` source present (#653), an authored derivation from the unreviewed FS 180 reconstruction, no filmed FS 360 take | none | none | none |
| 360 flip | source present: `360-flip-source-keyed-v1` (`treflip-keyed/`, `_oRoo1x1uzM` ~253 s, filmed goofy); Varial Kickflip: `varial-kickflip-derived-tre-v1` source present (#640), an authored Tre derivation with its own 180 scoop and half-turn catch, no filmed take | none | **offline reviewed**: `nollie-360-flip-source-keyed-v1` (`nollie360-keyed/`, `PqJZcwTY1QE` t=153) | none |
| Hardflip | source present: `hardflip-source-keyed-v1` (`hardflip-keyed/`, `_oRoo1x1uzM` t=192) | none | none | none |
| Inward heelflip | none | none | none | none |
| BS 180 | none (procedural spin only) | none | combined only: `nollie-backside-180-heelflip` (`nollie-bs180-heelflip-air/`, `PqJZcwTY1QE` t=115), **offline reviewed**; not a standalone BS 180 reference | none |
| FS 180 | none (procedural spin only) | none | none | none |

Count on `main`: 4 slots offline reviewed (plus the combined nollie BS 180
heelflip, which is reviewed but not a standalone BS 180 reference), 5 source
present, 1 kind only, 30 without a source. Nothing is native accepted. Outside the 40 slots, the normal Impossible has its own
board-path adapter and controls (`impossible-keyed/`, `_oRoo1x1uzM`), source
present. Batches 2 and 4–7 (riding, grabs, grinds, oldschool, transition) have
procedural source and no capture record; their state is in the gate table at the
top of this document. A merged PR changes a cell only after this record is
updated in the same or the next Claude library PR; a cell never moves to
"offline reviewed" or "native accepted" on the strength of a compile or an export.

## Conversion and integration

Use the executable [capture pipeline](RUST-SKATE-MOTION-CAPTURE.md). Retain the
source window, trick identity, full board quaternion, phase/contact observations,
authoring provenance and uncertainty for every clip. Keep videos, model outputs,
private recipes and review evidence outside the published repository.

Use Berrics material for further flat-ground references and the owner's Xbox
footage as a grab reference. Failed body detections are not motion data; explicit
reconstruction must not inflate confidence or evade actor/gap rejection.
Occluded head authoring is opt-in and leaves its uncertainty in the JSON receipts.

The normal capture registry covers 21 exact flat-ground signatures: 14 base
families (the five batch-1 kinds, normal kickflip/heelflip/360 flip, BS/FS pop
shove-it, Hardflip, the authored Varial Kickflip and the BS and FS 360 shove-its)
plus 7 separately reviewable double/triple derivatives, the held normal Double
Kickflip among them (its keyed clip id differs from its movement kind, the one
exemption the loader makes). Impossible uses its own board-path adapter and is
not a registry signature. The source-keyed Nollie360 candidate replaces its older source within
the existing kind; it does not add another signature or approve the original
pack. These are implemented selection paths, not a count of finished animations.
Batch 1 has the pinned offline review above; its exact pack approval and native
Shadow gates remain open. Every other result retains independent viewer, exact
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
