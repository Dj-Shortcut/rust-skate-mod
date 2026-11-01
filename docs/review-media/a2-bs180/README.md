# A2 normal BS180 — original rig review media

[Private interactive comparison, 1× and ¼×](http://127.0.0.1:63761/a2-bs180/)
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

| Physical mesh stance | Normal guide / catch / rollaway | Low guide / catch / rollaway |
|---|---|---|
| Regular | [Foot close-ups](bs180-normal-physical-regular-feet.jpg) | [Foot close-ups](bs180-low-physical-regular-feet.jpg) |
| Goofy | [Foot close-ups](bs180-normal-physical-goofy-feet.jpg) | [Foot close-ups](bs180-low-physical-goofy-feet.jpg) |

Physical stance is independently read from the animated GLB foot bones
against its board heading. This frozen proxy's requested native binding is
opposite to its physical mesh label; the viewer reports both explicitly.

The exact route is `bs-180-source-keyed-v1`: normal Ollie,
Flip0/Shove0/Spin−180/Body0. Source board heading is removed once from all
source joints, directions, toes, sole frames and board; live `NoseYaw`
retains the actual body-and-board turn. A modest nose-up carry persists
through source54, then levels before Catch58. Free shoes follow that pitch
near the grip. Tail release50 and a distinct free-Air/press-end52.5 preserve
the existing strict reach, floor and contact guards.

The controlled actual Pad/full32Hz Step/Land/scorer replay selects Backside180
in all four cases. The60Hz body retains36/36 normal-air frames and34/34 low-air
frames, then reaches ordinary Ground at4s. The32 individual render phases
come from those actual replay GLBs, with no render-only pose edits. Test-only
approval and generated models remain private; raw `reviewed=false` is retained.
The private browser review reaches4s at both1× and¼× in all four cases;
28 phase selections use live decoded source video, and12 guide/catch/rollaway
foot views have no browser runtime errors. Self-inspection confirms a loaded
ground pose, an airborne rider/board half-turn with the board near the shoes,
a bent-knee actual landing and ordinary upright recovery. These checks do
not promote the pack or replace Claude's independent visual judgement.

Source commits: `dd98d0e` and `017dcd9`.
Raw source pack SHA256:
`30c069b18e60db6c45eeeb96399409e81f5a6d88736f3f7a88d48a2f58fe4d13`.
Exact route freeze: `e3d34ec0436789073694b2ab6c5e03b994a15d78`.
Claude owns the offline judgement and library status. Native Rust/Shadow
selection, controls and gameplay remain a separate pending gate.
