# A3 normal Nollie — original rig review media

[Private interactive source comparison, 1× and ¼×](http://127.0.0.1:63761/a3-nollie-ollie/).
The source is the genuine normal Regular Nollie by the black-cap/tee,
grey-shorts rider in [The Berrics P4CflPS6YwQ](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=68).
Left/burgundy-sock presses the leading nose; Right/striped-sock guides the
tail up. Source pixels stay original and unmirrored, in the private viewer.
Checked-in images contain only our original rig, frozen skin and full shaped
thirteen-part skateboard.

| Physical preview stance | Normal flight | Low flight |
|---|---|---|
| Regular / anatomical Left foot leading | [Phase sheet](nollie-ollie-normal-physical-regular.jpg) | [Phase sheet](nollie-ollie-low-physical-regular.jpg) |
| Goofy / anatomical Right foot leading | [Phase sheet](nollie-ollie-normal-physical-goofy.jpg) | [Phase sheet](nollie-ollie-low-physical-goofy.jpg) |

| Physical preview stance | Normal guide / catch / rollaway | Low guide / catch / rollaway |
|---|---|---|
| Regular | [Foot close-ups](nollie-ollie-normal-physical-regular-feet.jpg) | [Foot close-ups](nollie-ollie-low-physical-regular-feet.jpg) |
| Goofy | [Foot close-ups](nollie-ollie-normal-physical-goofy-feet.jpg) | [Foot close-ups](nollie-ollie-low-physical-goofy-feet.jpg) |

Sheets show Load, nose pressure, Right upper guide, Air, both-foot Catch,
actual Land, Rollaway and ordinary End. The animated GLB foot bones establish
physical preview stance independently; this frozen proxy's requested/native
binding is opposite to its physical mesh label, and the viewer reports both.
This does not redefine native player handedness.

The bounded64–78s source file has actual30000/1001 fps and begins at PTS990
in time base1/30000. Selected decoder frames95–175 end before actorcut176;
original global source indices are unknown. Source clip and source-camera
depth are not claimed to be calibrated3D. Full-frame detector coverage is80/81:
raw142 stays an identity gap, its controls are explicitly reconstructed from
verified neighbors, and143 lower-body depth is separately reconstructed.
The anatomically swapped ROI extraction is rejected, not silently mixed.
Head/gaze, fixed chains, pelvis, contacts and settled recovery are authored.

The exact route `nollie-ollie-source-keyed-v1` is Nollie/normal/notFakie with
Flip0/Shove0/Spin0/Body0. Right sole release133 is followed by Left nose sole
release137.5, Right upper release138.5 and distinct later free-Air/pressEnd140.
Both-foot Catch144 does not invent an unresolved first-foot catch order.
Live physics owns forward travel, flight, actual landing and scoring; the
source provides no second jump or spin. Existing strict reach/floor checks
remain intact.

Four controlled actual Pad/full32Hz Step/Land/scorer and60Hz body replays
return Nollie, retaining36/36 normal-air frames and34/34 low-air frames in
both native stances, then ordinary Ground at4s. The32 render phases come
from those actual GLBs without render-only pose changes. Source presence,
export consistency, offline judgement and native gameplay are separate gates.
Own private browser review passes all four cases at1× and¼× through4s,
28 decoded-source phase selections and12 guide/catch/rollaway foot close-ups,
with no browser runtime errors. Self-inspection beside the source confirms
loaded knees before extension, Left nose pressure with Right pickup, compact
grip-up Air, both-foot Catch, bent Land and settled ordinary recovery. The
reference remains original, including its real panning camera and boundaries.

Source commit: `4898f05`. Route freeze:
`fa6778091cfadf2c5de2f93cd0dbf32c870e8443`.
Raw pack SHA256:
`014e972c36db6d5c51be60cfd64ef08141e8f215bd698649e8963ac9074b7de5`.
Raw `reviewed=false` is retained. Claude owns the offline judgement and library
status; native Rust/Shadow testing remains pending.
