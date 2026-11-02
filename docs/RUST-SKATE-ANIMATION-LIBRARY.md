# Full skate-animation library

The target is the whole requested game library in [TODO](../TODO.md), including
all applicable stances, directions, entry/exit paths and recovery. The 25 Berrics
selections are reference material, not a limit or a count of finished animations.

## Delivery order and current gates

| Batch | Coverage | Current gate |
| --- | --- | --- |
| 1 | Switch Heelflip, Nollie Heelflip, Fakie Kickflip, Nollie Backside 180 Heelflip, Nollie 360 Flip | Offline viewer review completed on 30 October at main `5cbbf2f7`: twenty full normal/low, Regular/Goofy replays on the frozen skin and thirteen-part board. Deep Ground preparation, pop/release, Air, catch, landing and roll-away were inspected beside the source, including slow motion. Original pack approval, native controller binding and Shadow remain open. |
| 2 | Riding, steering, crouch, normal/mongo push, ollie/nollie, braking, powerslide, revert, kickturn, both manuals, landing and upright roll-away | Procedural source on `main`, visual review open: grounded low-speed axle kickturns with tail-foot and rear-axle loading before the lift (#647, #655), grounded reverts kept on the wheel plane (#638) and powerslide (speed-check) ground contact (#635) are source present with native acceptance open. A dedicated manual-revert action is still missing. |
| 3 | All requested flip/shove families and stance/spin variants, composed from shared source families; separate references for exceptional foot/body actions | The registry contains 23 signatures: 16 base (the five batch-1 kinds, normal kickflip/heelflip/360 flip, BS/FS pop shove-it, Hardflip, Varial Kickflip, BS and FS 360 shove-it, nollie kickflip, switch kickflip) and 7 double/triple derivatives (the held normal Double Kickflip among them). Impossible has a separate board-path adapter. Source-keyed Nollie360 and BS Pop Shove-it replace older sources within their existing kinds. Code/source presence is not a finished-animation count; the remaining 40-reference coverage, per-trick review, exact pack approval and Shadow stay open. |
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
docs name; the footage itself stays private. State as of `main` after #724; the gate's 1 and 2 November stills reviews are below the table.

| Source family | Normal | Fakie | Nollie | Switch |
| --- | --- | --- | --- | --- |
| Ollie | **offline reviewed** 1 Nov: own rolling Ollie (#690, `docs/review-media/a2-ollie-own-source/`; the CC BY-SA `ollie-video/` stays showcase-only) | **offline reviewed** 1 Nov: own Fakie Ollie (#699, `docs/review-media/a3-fakie-ollie/`) | **offline reviewed** 1 Nov: own Nollie (#695, `docs/review-media/a3-nollie-ollie/`) | **offline reviewed** 1 Nov: own Switch Ollie (#701, `docs/review-media/a3-switch-ollie/`) |
| Kickflip | source present: `kickflip-source-keyed-v1` (`kickflip-keyed/`, authored from reference images); held Double on the same pop: `double-kickflip-source-keyed-v1` (`double-kickflip-derived/`, #662, #663, bound to #659's native held-flip clock), **offline reviewed** 1 Nov (gate stills + Codex replays; side-view check of the pop lean open) | **offline reviewed**: `fakie-kickflip` (`fakie-kickflip-air/`), double/triple derivatives registered | **offline reviewed** 1 Nov after #685: `nollie-kickflip-source-keyed-v1` (`nollie-kickflip-keyed/`, #679, #681, Rodriguez `PqJZcwTY1QE` frames 5678–5742, filmed goofy); nose pop, rear-instep flick, compact air; #685's authored head/gaze and bent lead arm replace the overhead arm and bowed head, both stances | **offline reviewed** 1 Nov: `switch-kickflip-source-keyed-v1` (`switch-kickflip-keyed/`, #680, #681, Rodriguez `PqJZcwTY1QE` frames 5470–5534, filmed regular); compact air, bent-knee catch, both stances |
| Heelflip | source present: `heelflip-source-keyed-v1` (`heelflip-keyed/`, `_oRoo1x1uzM` 136–158 s) | **offline reviewed** 1 Nov: `fakie-heelflip-keyed` (`fakie-heelflip-keyed/`, #686), an authored derivation (reviewed Fakie Kickflip body + heel flick, no filmed take; caches only have Fakie Bigspin Heelflip labels, #672) | **offline reviewed**: `nollie-heelflip`, double/triple registered | **offline reviewed**: `switch-heelflip` (`switch-heelflip-air/`, `PqJZcwTY1QE` t=74), double/triple registered |
| BS pop shove-it | source present: `bs-pop-shove-it-source-keyed-v1` (`bs-pop-shove-keyed/`, Cole `YcmPDIj2BFo` t=128); BS 360: `bs-360-shove-it-source-keyed-v1` (`bs360-shove-keyed/`, #602, the genuine BATB12 Kroetkov take with authored pickup, fold and arm depth), **offline reviewed** 1 Nov (catch and land upright and stiff, acceptable) | **offline reviewed** 2 Nov: own-source Fakie BS Pop Shove-it (#715 with the anatomy correction of #721: physical regular take, no reflection, source pack `08c631…`, `docs/review-media/a4-fakie-bs-pop-shove/`; the earlier `a899…` pack is superseded) | **offline reviewed** 2 Nov: own-source Nollie BS Pop Shove-it (#711, `docs/review-media/a4-nollie-bs-pop-shove/`) | **offline reviewed** 2 Nov: own-source Switch BS Pop Shove-it (#712, `docs/review-media/a4-switch-bs-pop-shove/`) |
| FS pop shove-it | `fs-pop-shove-it-source-keyed-v1` (`fs-pop-shove-keyed/`, #675, Kroetkov `YcmPDIj2BFo` 130.3–133.2 s, filmed goofy), **offline reviewed** 1 Nov (air frame 216 shows the deck end-on and steeply pitched; confirm against the source frame); FS 360: `fs-360-shove-it-source-keyed-v1` (`fs360-shove-keyed/`, #653 with the pop extension of #676, an authored derivation from the FS 180 reconstruction, no filmed FS 360 take), **offline reviewed** 1 Nov (mid-air carrier tilt shows the wheels from behind; confirm it stays under the scoop's 40°) | **offline reviewed** 2 Nov after #730: own-source Fakie FS Pop Shove-it (#724, `docs/review-media/a5-fakie-fs-pop-shove/`) | **offline reviewed** 2 Nov after #727: own-source Nollie FS Pop Shove-it (#722, `docs/review-media/a5-nollie-fs-pop-shove/`) | **offline reviewed** 2 Nov after #728: own-source Switch FS Pop Shove-it (#723, `docs/review-media/a5-switch-fs-pop-shove/`) |
| 360 flip | source present: `360-flip-source-keyed-v1` (`treflip-keyed/`, `_oRoo1x1uzM` ~253 s, filmed goofy); Varial Kickflip: `varial-kickflip-derived-tre-v1` (`varial-kickflip-derived/`, #640 with the compact authored upper body of #678, a Tre derivation, no filmed take), **offline reviewed** 1 Nov after #678 (no arch, bent asymmetric arms through the catch, feet close to the deck; the normal/regular air deck gap is still the largest of the family) | none | **offline reviewed**: `nollie-360-flip-source-keyed-v1` (`nollie360-keyed/`, `PqJZcwTY1QE` t=153) | none |
| Hardflip | source present: `hardflip-source-keyed-v1` (`hardflip-keyed/`, `_oRoo1x1uzM` t=192) | none | none | none |
| Inward heelflip | **offline reviewed** 1 Nov: authored Heel + BS 180 derivation (#687, `docs/review-media/a1-inward-heelflip-derived/`; no verified filmed take in the cached Berrics windows, #672), reversed-deck catch | none | none (a Nollie Inward Heelflip label near 158 s of `PqJZcwTY1QE`, unverified) | none (a Switch Inward Heelflip label near 556 s, unverified) |
| BS 180 | **offline reviewed** 1 Nov after #697: own-source BS 180 (#693, `docs/review-media/a2-bs180/`) | **offline reviewed** 2 Nov after #710: own-source BS Halfcab (#704, `docs/review-media/a3-fakie-bs180/`) | combined only: `nollie-backside-180-heelflip` (`nollie-bs180-heelflip-air/`, `PqJZcwTY1QE` t=115), **offline reviewed**; not a standalone BS 180 reference | none |
| FS 180 | **offline reviewed** 1 Nov: own-source FS 180 with one coupled rider-and-board half-turn (#692, `docs/review-media/2026-11-01-a2-fs180/`) | **offline reviewed** 1 Nov: own-source FS Halfcab (#702, `docs/review-media/a3-fakie-fs180/`) | none | none |

Count on `main`: 23 slots offline reviewed (fakie kickflip, nollie heelflip,
switch heelflip, nollie 360 flip, normal FS pop shove-it, switch kickflip, nollie
kickflip after #685, fakie heelflip from #686 and inward heelflip from #687 as
authored derivations, normal ollie #690, normal FS 180 #692, normal BS 180 after #697, nollie #695,
fakie ollie #699, switch ollie #701, FS halfcab #702, BS halfcab after #710,
nollie BS pop shove-it #711 (levelled in #718), switch BS pop shove-it #712 and
fakie BS pop shove-it #715, nollie FS pop shove-it after #727 and switch FS pop shove-it
after #728 and fakie FS pop shove-it after #730; the
combined nollie BS 180 heelflip is reviewed but is not a standalone BS 180
reference and is not counted; the Varial Kickflip is a reviewed derivative inside
the 360-flip slot, as are the held Double Kickflip, BS 360 and FS 360 inside
theirs), 5 source present (normal kickflip, heelflip, BS pop shove-it, 360 flip,
hardflip), 12 without a source.
Nothing is native accepted. Outside the 40 slots, the normal Impossible has its own
board-path adapter and controls (`impossible-keyed/`, `_oRoo1x1uzM`), source
present. Batches 2 and 4–7 (riding, grabs, grinds, oldschool, transition) have
procedural source and no capture record; their state is in the gate table at the
top of this document. A merged PR changes a cell only after this record is
updated in the same or the next Claude library PR; a cell never moves to
"offline reviewed" or "native accepted" on the strength of a compile or an export.

### Gate review of the 1 November families

Reviewed by Claude from the rig-only contact sheets Codex committed under
`docs/review-media/2026-11-01-*` (#674, #675, #676: six phases per sheet, normal
and low charge, physical regular and goofy, `review-manifest.json` with GLB and
pack hashes) together with Codex's own normal- and quarter-speed replays. Stills
cannot show contact continuity between frames; that part rests on the replays.
No source pixels are in the repository, so source fidelity was not re-judged
here. Nothing below is native acceptance; raw packs stay `reviewed=false`.

| Family | Decision | What was seen | Open point for the next viewer pass |
| --- | --- | --- | --- |
| Held Double Kickflip | offline reviewed | Compact air with the deck inverted directly under the feet, bent-knee catch, both stances | The normal/regular pop frame reads as a strong sit-back from the rear camera; check from the side (the low/goofy side view is upright) |
| BS 360 shove-it | offline reviewed | Scoop with the rear foot riding the tail, feet clear of the full turn, catch after 360° in both stances | Catch and land are upright with the arms low: stiff but plausible; a little knee and arm would read better |
| FS 360 shove-it (after #676) | offline reviewed | Pop now extends upward with the tail strike at 211.6 instead of the earlier sit-back; catch 224 kept | At 1.150 s the rear camera sees the wheels: confirm the carrier tilt stays within the scoop's ~40° and flattens before catch |
| Normal FS pop shove-it (#675) | offline reviewed | Extended pop, half-turn catch with the feet on the swapped ends, upright roll-away, both stances | Air frame 216 shows the deck end-on and steeply pitched; confirm against the Kroetkov frame |
| Varial Kickflip (#640, then #678) | offline reviewed after #678 | #674's delivery was rejected (arms thrown up with an arched back in the air, straight T-arm catch, deck ~0.4 m below the feet). #678's before/after sheets in the same camera show a compact authored upper body: no arch, bent asymmetric arms settling through the catch, feet brought to the deck | The normal/regular air gap between feet and deck is still the largest of the family; keep an eye on it in the viewer |
| Nollie Kickflip (#679, #681, then #685) | offline reviewed after #685 (batch A1, #684) | #685's same-camera before/after sheets: head now up and looking toward the board and landing from Pop on, the lead arm bent and low from Catch through Land in both stances and both heights; lower body, board path and nose pop unchanged | The air keeps one arm raised above the shoulder, plausible for a flip; nothing else open |
| Fakie Heelflip (#686) | offline reviewed (batch A1, #684) | Authored from the reviewed Fakie Kickflip body with a heel-guide flick against the toe-side nose corner: deep Load, tail-end fakie pop, heel flick at 0.95 s, deck turning under the feet, bent-knee catch and land, upright roll-away to 4 s, normal/low and physical regular/goofy | The pop/flick frames throw both arms up in a wide V, inherited from the fakie kickflip body; acceptable, but a lower lead arm would read calmer |
| Inward Heelflip (#687) | offline reviewed (batch A1, #684) | Authored from the Heelflip source with a BS 180 scoop: deep Load, pop with the heel exit off the toe-side edge, deck turning heel-wise and 180° under the feet, reversed-deck catch with both feet over the bolts, bent-knee land and upright roll-away to 4 s; normal/low, physical regular/goofy | None from the stills; the low/goofy air frame catches the deck end-on mid-rotation, which reads correctly in the replays Codex reports |
| Ollie (#690) | offline reviewed (batch A2, #689) | Own rolling take: deep Load, tail pop with the front foot dragging up, level deck under the feet at the apex, bent-knee catch and land, upright roll-away; normal/low, physical regular/goofy | None from the stills |
| FS 180 (#692) | offline reviewed (batch A2, #689) | One frontside half-turn of rider and board together: wound-up Load, pop, level air with the deck under the feet, catch facing the new direction, bent-knee land and roll-away; all four sheets | At catch/land the normal/regular lead hand comes in to the chest; plausible as a spin check, a little wider would read calmer |
| BS 180 (#693), first gate | superseded by the #697 row below | Load, air, catch, land and roll-away are good in all four sheets | Physical regular, both heights: at Pop and Guide the deck stands almost vertical with the front knee folded up as if the rider sits on the nose; a BS 180 pop stays near ollie pitch (~35–45°). Physical goofy, both heights: at Pop and Guide the lead hand is clamped against the side of the head. Fix both, then resubmit the same sheets |
| BS 180 (#697) | offline reviewed (batch A2, #689) | The snap now happens at 0.867 s from a crouch with the deck at about 60°, and the guide at 0.967 s has the front foot on the front bolts with the deck nearly level; the goofy lead hand is off the head and opens forward. Air, catch, land and roll-away unchanged | At the guide frame the rider stands tall with nearly straight legs; a little more knee would read better |
| Nollie (#695) | offline reviewed (batch A3, #694) | Nose pop with the rear foot dragging up, level deck at the apex, bent-knee catch and land, upright roll-away, both stances and heights | None |
| Fakie Ollie (#699) | offline reviewed (batch A3, #694) | Fakie tail pop, level deck at the apex, bent-knee catch and land; Load gaze now forward/down and the Pop arm at shoulder height after the first-pass notes | None |
| Switch Ollie (#701) | offline reviewed (batch A3, #694) | Switch tail pop, level deck at the apex, bent-knee catch and land, upright roll-away, both stances and heights | Arms held wide and low through Air and Catch; fine |
| FS Halfcab (#702) | offline reviewed (batch A3, #694) | Fakie FS half-turn of rider and board together: early snap at 0.867 s, level front-bolt guide, level air, catch facing forward, bent-knee land and roll-away | T-arms in the air read a little stiff; a softer elbow would read better |
| BS Halfcab (#704), first gate | superseded by the #710 row below | Snap, guide, level deck and landing are good in all four sheets | From Guide through Catch both arms are thrown straight up above the head in a V, in both stances; it reads as a cheer, not balance. Keep them at or below shoulder height, open to the sides like the FS Halfcab |
| BS Halfcab (#710) | offline reviewed (batch A3, #694) | Guide through Catch now keep both arms bent and below the shoulders, open forward and to the sides, in both stances and heights; snap, level guide, landing and roll-away unchanged | Physical regular Rollaway: the arms hang straight and stiff at the sides; a slight elbow bend would read better |
| Nollie BS Pop Shove-it (#711) | offline reviewed (batch A4, #706) | Nose scoop with the front foot, free air with the feet lifted off the deck, board-only half-turn with no body turn, both feet back over the bolts at Catch, bent-knee land and roll-away; normal/low, physical regular/goofy | Physical regular normal Air: from this camera the deck reads almost vertical (grip face end-on); likely foreshortening of the ~32° nose-down pitch while it is turned 90°, but confirm the pitch stays at or below pop pitch at mid-turn. Balance arms are high in Air; fine |
| Switch BS Pop Shove-it (#712) | offline reviewed (batch A4, #706) | Switch tail scoop, board-only half-turn under the rider, level deck in Air, both feet over the bolts at Catch, bent-knee land and roll-away, both stances and heights | Pop through Air the arms stand straight out at shoulder height (T-arms); a softer elbow, as on #710, would read better |
| Varial collecting foot (#708) and held Double pop side view (#709) | improvements accepted | #708: the collecting foot is a little closer in Air, near foot and Catch unchanged. #709: the side camera confirms the Load crouch and Pop tail strike; documentation only | #708: in Air the front foot is still clearly above the deck; the gap could close a little more before Catch |
| Fakie BS Pop Shove-it (#715) | offline reviewed (batch A4, #706) | Fakie tail scoop with the rear foot, front foot lifting clear, board-only half-turn with no body turn, level deck at Catch with both feet over the bolts, bent-knee land and upright roll-away; normal/low, physical regular/goofy | From Scoop release through Air the trailing hand rises to head height with the elbow up; lower it to shoulder height |
| Nollie BS shove levelling (#718), FS Halfcab softer arms (#717), FS Pop Shove-it flatter scoop (#714) | improvements accepted | #718: the deck levels sooner after the nose release. #717: a small elbow bend in Air and Catch, as asked. #714: the scoop and early Air are flatter than the historical take | #718: the regular normal Air frame still shows the deck almost end-on and steep; from the side or contact sheets it reads as foreshortening, so no further fix asked. #714: the Pop deck is still steep (~60°); fine for a scoop |
| Fakie BS Pop Shove-it anatomy (#721) | correction accepted; slot stays offline reviewed | The source is now the verified regular take with no reflection, so the raised balance arm and the scoop leg belong to the right sides; legs, board and timing unchanged | The raised hand at Scoop and Air is still above the head (now the other arm), and at Catch the lead hand is tucked against the chest; lower the raised hand to shoulder height and open the Catch hand |
| Nollie FS Pop Shove-it (#722), first gate | superseded by the #727 row below | Load, Air, Catch, Land and roll-away read well; level deck from Air on | At the nose press (0.950 s), all four sheets: both arms go up beside or above the head, the rider stands tall on a nearly straight leg, and the deck is close to vertical. Keep the arms at or below shoulder height, keep the knees bent through the press, and keep the nose-down pitch near pop pitch (~35–45°) |
| Switch FS Pop Shove-it (#723), first gate | superseded by the #728 row below | Switch tail pop, board-only half-turn, level Air, bolts at Catch, bent-knee land | At Scoop both arms are thrown up in a V above the head in both stances (the cheer pose the owner rejects), and at Catch and Land one arm stays straight overhead. Keep both arms at or below shoulder height, bent, as on #710 |
| Fakie FS Pop Shove-it (#724), first gate | superseded by the #730 row below | Fakie tail pop with the earlier rise, board-only half-turn, level Catch over the bolts, bent-knee land and roll-away | At Pop and Scoop release both arms form a V with the trailing hand well above the head, and the rider stands on nearly straight legs; lower the arms to shoulder height and keep more knee through the pop |
| Nollie FS Pop Shove-it (#727) | offline reviewed (batch A5, #716) | At the nose press both arms are now bent and forward at shoulder height and both knees stay bent, in all four sheets; Air, Catch, Land and roll-away unchanged | The deck at the nose press still shows its underside almost face-on (board path unchanged by design); if the source allows, bring the press pitch toward ~45° |
| Switch FS Pop Shove-it (#728) | offline reviewed (batch A5, #716) | Scoop, Air, Catch and Land keep both arms bent and at or below shoulder height in both stances; the pop panel now shows the crouch before the snap | At Catch and Land the lead arm is long and straight at shoulder height; a little elbow would match the rest |
| Fakie FS Pop Shove-it (#730) | offline reviewed (batch A5, #716) | Pop and Scoop now keep both arms bent at shoulder height and the knees bent through the press, in all four sheets; the side view confirms the overhead arms are gone. Rise, Air, Catch, Land and roll-away unchanged | At Scoop the tail leg is almost straight; a touch more knee would read softer |
| Fakie Heelflip arm (#703) and FS 180 hand (#705) | improvements accepted | #703: the lead arm at Pop/Flick is now lower and forward. #705: the lead hand opens at Land | FS 180: at Catch the lead hand is still close to the chest; minor |
| Nollie Kickflip, earlier gate (#679, #681) | superseded by the row above | Own Rodriguez take: genuine nose pop with the front foot, rear outer-instep flick, compact air with the deck inverted below | In both stances the lead arm stands straight above the head from Catch through Land, and the head is bowed into the chest from Pop to Land. Check source frames 5720–5742: if Rodriguez's arm is not overhead at landing this is the monocular depth artifact seen on the heelflip; either way the owner's 28 October rule applies (a believable head and arm win over an unreliable source) |
| Switch Kickflip (#680, #681) | offline reviewed | Own Rodriguez take, physical regular body with switch signature: upright pop, compact air with the deck inverted under the feet, bent-knee catch, settled roll-away, both stances | None from the stills |

Crossbone (#673, batch 4) is outside the 40 slots: the pelvis height is now part
of the reach feasibility and the stance frames are consistent; Codex's six
regular/goofy × normal/switch/fakie replays were not re-reviewed from stills.

## Conversion and integration

Use the executable [capture pipeline](RUST-SKATE-MOTION-CAPTURE.md). Retain the
source window, trick identity, full board quaternion, phase/contact observations,
authoring provenance and uncertainty for every clip. Keep videos, model outputs,
private recipes and review evidence outside the published repository.

Use Berrics material for further flat-ground references and the owner's Xbox
footage as a grab reference. Failed body detections are not motion data; explicit
reconstruction must not inflate confidence or evade actor/gap rejection.
Occluded head authoring is opt-in and leaves its uncertainty in the JSON receipts.

The normal capture registry covers 23 exact flat-ground signatures: 16 base
families (the five batch-1 kinds, normal kickflip/heelflip/360 flip, BS/FS pop
shove-it, Hardflip, the authored Varial Kickflip, the BS and FS 360 shove-its and
the own-source nollie and switch kickflips) plus 7 separately reviewable
double/triple derivatives, the held normal Double
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
