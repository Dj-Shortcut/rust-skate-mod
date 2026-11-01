# Own-source plain Frontside 180

This is the independent red-beanie/94-jersey **plain FS180** in
[The Berrics reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=99).
The earlier black-shirt attempt around94.9seconds is a failed FS shove and is
not this source. A visible left-foot-leading/right-foot push identifies this
rider as Regular. One continuous take includes load, tail pop, a whole
rider-and-board half turn, bent landing and upright rollaway.

The retained private cache covers estimated public98–105seconds. Actual
bounded-container frame0 starts at PTS990 (0.033seconds), time base1/30000;
frame105 ends at PTS106095 (3.5365seconds). The next actor cut at frame106 is
excluded. Animation controls use local42–105, with Pop54, tail strike56.4,
front sole release55.5, Right-tail release58.4, later free-Air pressure end60,
Catch70, Land75 and Recovery84. These contacts/phases are visual estimates.
Public full-video times are requested-cut estimates; original global video
frame numbers/PTS were not recovered from this transcoded bounded cache.

## What is captured and what is authored

The actual detector selected one actor in102/106 retained frames. Ambiguous
post-land frames86,93,94,95 stay gaps in the raw data. The compact controls
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

The canonical source scene turns0→−180degrees. The baker removes its complete
board-heading yaw from **all joints and physical toes, every anatomical
direction, both sole frames and board origin/orientation**. Source-relative
hip/shoulder windup and catch remain. The game's actual `NoseYaw` supplies the
one real turn and preserves the native half-turn landing into Switch; source
time never commands a second180 or flips the anatomical stance.

The exact requested signature is Ollie, switch=false, tailLeads=false, flip0,
shove0, spin+180 and body0. It is separate from FS pop shove-its, body varials,
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
