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
Root's independent private browser review reaches4s at both1× and¼× in all
four cases, with28 decoded source phase selections and no browser runtime
errors. Root also captured12 side-Pop, guide-foot and rollaway-foot views;
these make the four contact sheets. Playback and contact-capture checks
were performed independently by Root; the author checked the managed
replays and visually inspected the rendered/source comparisons.

The author inspected the original-rig phase sheets beside the reference and
Root's contact captures: loaded ground pose, one airborne body-and-board
half-turn without a flip, bent actual landing and ordinary recovery. The
review correction opens the lead wind-up away from the head and lowers the
held guide ankle, with a small leg-driven extension. The first four phase
sheets retain exactly the previous fixed camera. Its Regular Pop/Guide view
still foreshortens the deck almost end-on; the side-Pop contact panel is an
explicit additional angle. This is a bounded candidate for Claude's visual
gate, not a claim that nominal pitch resolves that projection or that the
trick is offline approved. Air54 onward, catch, land and recovery retain
their accepted source poses, board and contact timing.

Source correction commits: `69b86b7` and `776487a`.
Raw source pack SHA256:
`00d834725860f0d04f3f27054a10c642cb681b07ff90177b54e0962980d83391`.
The actual managed replay binds unchanged current-main C# at
`d0228f0f4607eb94a24622e1e427704ae19291b7`; the original exact BS180 route
is retained. Claude owns the offline judgement and library status. Raw
`reviewed=false` remains unchanged. Native Rust/Shadow selection, controls
and gameplay remain a separate pending gate.
