# A2 normal BS180 — original rig review media

[Private interactive comparison, 1× and ¼×](http://127.0.0.1:63761/a2-bs180-pop-arm/)
uses the original rig, frozen skin and complete shaped thirteen-part skateboard.
The source is the genuine plain normal Goofy BS180 by Wes Kremer in
[The Berrics BATB6 Round1](https://www.youtube.com/watch?v=i8jPt6jHk98&t=44),
bounded-file frames18–78 at30000/1001 fps, ending before the other-skater cut.
The board does not flip; rider and board make the same native half-turn.
The source remains original and unmirrored. Captured arm directions are
supplemented by explicitly reconstructed depth, contact, body windup, cropped
head/gaze and bent balance arms. These are not fully measured3D tracks.

The checked-in sheets contain only our original rig render, with no source
video pixels. Each contains Load, tail pressure, front outer-upper guide,
Air, Catch, actual Land, Rollaway and ordinary End.

| Physical mesh stance | Normal flight | Low flight |
|---|---|---|
| Regular / anatomical Left foot leading | [Phase sheet](bs180-normal-physical-regular.jpg) | [Phase sheet](bs180-low-physical-regular.jpg) |
| Goofy / anatomical Right foot leading | [Phase sheet](bs180-normal-physical-goofy.jpg) | [Phase sheet](bs180-low-physical-goofy.jpg) |

The contact sheets show a side-view Pop, then guide and rollaway foot details.

| Physical mesh stance | Normal side pop / guide / rollaway | Low side pop / guide / rollaway |
|---|---|---|
| Regular | [Foot close-ups](bs180-normal-physical-regular-feet.jpg) | [Foot close-ups](bs180-low-physical-regular-feet.jpg) |
| Goofy | [Foot close-ups](bs180-normal-physical-goofy-feet.jpg) | [Foot close-ups](bs180-low-physical-goofy-feet.jpg) |

Physical stance is independently read from the animated GLB foot bones
against its board heading. This frozen proxy's requested native binding is
opposite to its physical mesh label; the viewer reports both explicitly.

The exact route is `bs-180-source-keyed-v1`: normal Ollie,
Flip 0 / Shove 0 / Spin −180 / Body 0. Source board heading is removed once
from every joint, toe, direction, sole frame and board. Live `NoseYaw` owns
the single native body-and-board turn.

The board now snaps earlier and levels as the front upper slides toward the
front bolts. The leading knee bends forward and down, with the pelvis above
the board middle. Tail-sole release at source 48.5 follows snap 47.5; upper
contact continues to 50.5, with distinct free-Air/press-end 52.5. The board
rises while levelling. The accepted open Goofy wind-up and Air 54 onward,
catch, actual landing and roll-away remain intact. These are authored
contact/depth/timing corrections, not measured 3D motion.

[Before/after at identical times](POP-GUIDE-REVIEW.md) shows the rejected and
updated 0.933 s poses, the new actual snap at 0.867 s, then rejected/updated
Guide at the same time for each height. The fixed camera is identical. The
old rejected moment is retained explicitly; moving the phase button alone
would not satisfy this comparison.

The controlled actual Pad/full 32 Hz Step/Land/scorer replay selects
Backside 180 in all four cases. The 60 Hz body retains 36/36 normal-air frames
and 34/34 low-air frames, then reaches ordinary Ground at 4 s. The 32 phase
renders and four extra original-time renders come directly from those GLBs,
with the frozen original skin and shaped thirteen-part board. No render-only
pose edit is applied. Test-only approval and generated packs/models remain
private; raw `reviewed=false` is retained.

Root's independent browser replay reaches 4 s in all four cases at 1× and
¼×, with 28 decoded source phase selections and no runtime errors. Contact
views are captured independently from those final models. The author inspected the same-camera original-rig
frames: the early snap is diagonal, the original 0.933 s pose no longer
perches on an upright nose, the guiding board is level and rising, and the
later body-and-board half-turn, bent landing and settled recovery remain.
These checks do not replace Claude's independent offline judgement.

[Integrated source correction](https://github.com/Dj-Shortcut/rust-skate-mod/commit/8234e862cf5ae46d53e4cb9894e6a49b4194c0c4).
Raw source pack SHA256:
`0554c74a082cfbe494a7a225a3e240c5080da598ec45ec4734f0d162121434c4`.
The rebuilt managed replay binds the frozen integrated source at
`3f593c859c4be6afce08fad47ff28518ed6b4746`.
Claude owns the library status and offline judgement. Native Rust/Shadow
selection, controls and gameplay remain a separate pending gate.
