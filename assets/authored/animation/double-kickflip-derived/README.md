# Double Kickflip: shared setup, distinct authored Double motion

`scripts/author_skate_double_kickflip.py` reuses the existing ordinary Kickflip
controls and exact early sample grid. The two takes are different: the body
basis is the ordinary `YcmPDIj2BFo` reconstruction, while the white-shirt attempt
in the official Berrics `_oRoo1x1uzM` final supplies the visible Double reference.
The latter shows two graphic/grip cycles followed by both feet returning and
landing. Its native frames 142–165 within the cached 480–492-second window are
qualitative motion witnesses; the edited video does not establish physical
flight duration or measured 3D. `referenceInspected` records that visual source
inspection, not approval of the reconstructed clip or its native playback.

The original control clock, Pop 259, Land 275 and duration remain unchanged.
Every original sample through the first original row after 264 (about 264.49)
retains its complete body, feet and board payload. This preserves the existing
entry endpoint and its neighbouring interpolation samples. The data does not
predict a second flip during Load or the initial flick. Native continuation
must require a real accepted held extension and retain the same pop/entry.

After 265 a separate winding interval reaches 720 rather than doubling or
replaying the ordinary 360 curve. Live native winding remains authoritative.
The free feet use the original whole-board support hull after its current
pitch/yaw: its all-roll radius leaves room for the thirteen-part board without
moving it or adding a world/root jump. A distinct free-return Air marker 272.75
precedes Catch 274.2; the old ordinary 273 Catch is removed. The native adapter
must withhold catch ownership while actual winding is unfinished.

The inspected Double reference has a more folded mid-Air torso. A short authored upper
rotation after 265 reconstructs a 70-degree anatomical forward fold from the
chest frame, retaining its heading, arm-chain lengths and a bounded downward
head/neck frame. Extending the old monocular lean alone would amplify side-bend.
In that same window, the arms use the reference's out/down balance shape with
the original fixed segment lengths. Carrying the different ordinary-take arm
gesture rigidly through this fold would incorrectly raise a hand above the head.
The same upper window retains a source-led forward Catch and absorption instead
of fading to the upright ordinary-take torso before touchdown. The authored
targets ease from 70-degree Air to 40-degree Catch, 50-degree absorption and a
45-degree early recovery, then fade before the original source exit. These are
coarse reconstructed silhouettes guided by White 160–168, not measured 3D angles.
Head/arms share that window; no separate gaze or arm clock is added.
The existing pelvic dip and knee absorption remain unchanged; the anatomical
profile already shows bent legs. The source difference is the upper body
standing up over them, so this correction retains the forward upper fold
without adding a second deeper squat. Source depth, the canonical stance
mapping and these local upper/foot controls are authored reconstruction,
not captured Double body motion. Feet, lower-body landing/root recovery,
board, events and source clock remain unchanged. Late procedural riding after
the capture exit stays with the native caller.

The neck allowance follows the same torso-fold value: the compact 70° Air
retains its existing 30° bound, while the more open Catch/absorption permits
less extension so the face keeps looking down. This shares the upper window;
it adds no separate gaze clock. These coarse silhouette angles are authored
controls, not measured head or torso motion from the footage.
The gaze keeps the original control's horizontal HeadForward heading rather
than aiming at board centre, which can move behind the folded head at Catch.
That avoids a horizontal direction reversal without a body/camera rotation.

The original pack remains `Reviewed=false`. A controlled test copy, real Pad
and Step continuation, scoring, normal/low and both anatomical stance previews,
whole-motion/grip inspection and native Shadow acceptance are separate gates.
Reference video/pixels, generated JSON/packs and private meshes stay outside Git.

```sh
python scripts/author_skate_double_kickflip.py --output /absolute/private/output
```

The default consumes only tracked reconstructed controls, NumPy and the existing
strict writer. Optional `--input` accepts the unreviewed ordinary JSON. Input
hashes, prefix preservation, reachable limbs and sample/midpoint validation run
before output replacement. A failed bake leaves the prior reader-valid pack
untouched. No physics, input, shared sampler or original Kick assets are edited.
