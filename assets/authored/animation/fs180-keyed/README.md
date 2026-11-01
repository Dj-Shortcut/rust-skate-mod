# Own-source plain Frontside 180

This is the independent red-beanie/94-jersey **plain FS180** in
[The Berrics reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=99).
The earlier black-shirt attempt around 94.9 seconds is a failed FS shove and is
not this source. A visible left-foot-leading/right-foot push identifies this
rider as Regular. One continuous take includes load, tail pop, a whole
rider-and-board half turn, bent landing and upright rollaway.

The private bounded download requested public 98–105 seconds. The retained
single-actor window ends at estimated public 101.5035 seconds. Actual
bounded-container frame 0 starts at PTS 990 (0.033 seconds), time base 1/30000;
frame 105 ends at PTS 106095 (3.5365 seconds). The next actor cut at frame 106 is
excluded. Animation controls use local 42–105, with Pop 54, tail strike 56.4,
front sole release 55.5, Right-tail release 58.4, later free-Air pressure end 60,
Catch 70, Land 75 and Recovery 84. These contacts/phases are visual estimates.
Public full-video times are requested-cut estimates; original global video
frame numbers/PTS were not recovered from this transcoded bounded cache.

## What is captured and what is authored

The actual detector selected one actor in 102/106 retained frames. Ambiguous
post-land frames 86, 93, 94, 95 stay gaps in the raw data. The compact controls
explicitly mark their neighboring interpolation as authored reconstruction.
Monocular depth and absolute camera/body yaw are not calibrated. The right
wrist is unreliable in many frames; hidden arm depth, bent Catch balance,
fixed-chain legs, pelvis, shoe/grip contacts and recovery are authored and
labelled. The head uses an anatomical skull-up axis with bounded source-led
gaze, rather than placing the head along the gaze vector.

`captured-body.json` retains this own take's relative torso/arm gesture and a
labelled complete scene hip-heading reconstruction. It does not borrow another
trick's body. Reference pixels/raw detections stay private; this file contains
original project reconstruction controls and asserts no footage relicensing.
The geometry reference reads only the existing original board/shoe surface,
sole points and instep patch, with a checksum of that geometry subset.

## One real turn

The canonical source scene turns 0→−180 degrees. The baker removes its complete
board-heading yaw from **all joints and physical toes, every anatomical
direction, both sole frames and board origin/orientation**. Source-relative
hip/shoulder windup and catch remain. The game's actual `NoseYaw` supplies the
one real turn and preserves the native half-turn landing into Switch; source
time never commands a second 180 or flips the anatomical stance.

The exact requested signature is Ollie, switch=false, tailLeads=false, flip 0,
shove 0, spin +180 and body 0. It is separate from FS pop shove-its, body varials,
360s, late changes and other stances. Rotation/scoring/landing stay native.
The front outer-upper shoe patch guides the griptape without an off-edge
kickflip flick or a board roll.

## Regenerate

From the repository root with NumPy installed:

```sh
python scripts/author_skate_fs180.py --output context/private/fs180
```

The converter pins its own body and original geometry subset, uses the strict
capture writer, and replaces its generated pack last. Generated JSON/packs and
private diagnostic approval files remain ignored. The raw pack remains
`reviewed=false`; a successful conversion or managed replay is not an offline
quality verdict or a native Shadow test. Claude owns the library verdict.

## Managed replay and visual material

The full managed route uses real held-side Spin and Pad input through the
actual 32 Hz controller Step/Land/scorer, then the current 60 Hz measured
body fitter and presentation. The scorer reports **Frontside 180**. All Air
frames select this source and coupled board keys: 36/36 normal and 34/34
lower, in both stances. The real half-turn landing enters Switch; the replay
continues through source recovery and returns to ordinary riding by 4 seconds.
The controlled bindings and flat-ground fixture are not native Rust.

The frozen original skin and complete 13-part board were exported and viewed
at 1× and ¼ speed in all four cases, beside the decoded genuine source.
Physical Regular/Goofy labels come from the animated GLB foot bones, rather
than assuming native-request and proxy labels coincide. The camera/depth is
approximate: the source camera tracks and pans, so this is not a calibrated
pixel match. Own checks show coupled rider/board rotation, compact airborne
knees, shoes near the grip, bent landing and stable planted recovery.

- [Normal Regular](../../../../docs/review-media/2026-11-01-a2-fs180/normal-regular-phases.jpg)
- [Normal Goofy](../../../../docs/review-media/2026-11-01-a2-fs180/normal-goofy-phases.jpg)
- [Lower Regular](../../../../docs/review-media/2026-11-01-a2-fs180/low-regular-phases.jpg)
- [Lower Goofy](../../../../docs/review-media/2026-11-01-a2-fs180/low-goofy-phases.jpg)
- [Normal Catch/Land hand before/after, Regular](../../../../docs/review-media/2026-11-01-a2-fs180/grip-guide-regular.jpg)
- [Normal Catch/Land hand before/after, Goofy](../../../../docs/review-media/2026-11-01-a2-fs180/grip-guide-goofy.jpg)
- [Replay and provenance receipt](../../../../docs/review-media/2026-11-01-a2-fs180/manifest.json)

Private viewer: `http://127.0.0.1:63761/a2-fs180-catch-balance/`. Original footage
remains private. Claude owns the family/library verdict; this bounded follow-up
and native Shadow acceptance remain separate. No raw pack is marked approved
by these checks.

Owner Catch/Land refinement: the canonical Left/nose-side hand opens slightly outward and forward with a bent elbow instead of folding into the chest. This is explicitly authored in the existing decoder 67..84 balance window. The upper arm/elbow, other arm, head/torso, feet, board, source clock and accepted earlier/later source geometry are unchanged. The [same-camera before/after and phase sheets](../../../../docs/review-media/2026-11-01-a2-fs180/README.md) use the current product freeze in both physical stances and heights. All four actual routes and decoded-source 1×/¼× replays reach ordinary 4 s recovery. Source controls remain unreviewed; native Shadow remains pending.
