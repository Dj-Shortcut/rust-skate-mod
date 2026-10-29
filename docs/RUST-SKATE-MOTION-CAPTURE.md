# Video motion capture pipeline

The pipeline converts local video or retained MediaPipe pose detections into
board-local rider animation and an external capture pack. It is code and data:
Python performs extraction, reconstruction and batch export; the C# sampler
retargets the exported samples to measured rider dimensions. The guarded rigging
showcase remains available for unreviewed references. Normal riding has a
separate, explicitly approved presentation path; physics and scoring retain
their existing owners.

A monocular pose detector estimates a skeleton. It does not directly measure
hidden hands, fingers, board rotation, root height or ground slope. Each job
declares its camera basis, root/board keys, contacts and corrections. Reports
retain these estimates; successful conversion does not automatically mark a
clip as reviewed.

## Run a batch

Use Python 3.12 with NumPy. Video extraction additionally needs OpenCV,
MediaPipe and a local Pose Landmarker task model; already-extracted pose JSON
does not require the detector. See the official
[Pose Landmarker guide](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker/python)
for the model/API. Keep dependencies, models, source videos and output in ignored
`context/`, outside the published runtime.

```sh
python scripts/capture_pipeline.py batch /absolute/private/jobs.json \
  --output /absolute/private/capture-output \
  --model /absolute/private/pose_landmarker_heavy.task --resume
```

All jobs use one command. A source video is referenced in place, rather than
copied once per trick. If a job supplies `source.poseJson`, its retained actual
detections are used. Otherwise extraction uses the local video and model. A job
id is a filename-safe identifier, not a path. Manifests do not execute shell
commands or fetch URLs.

The output contains an external `.skmc` pack plus per-job samples, source and
quality information. Cache keys include source content, effective job settings,
detector model when used, and converter code. Resume reuses matching work;
changing calibration invalidates the affected job. Failed jobs are reported
individually and the command exits nonzero. A partial batch is not a completed
library.

## Compose shared-source variants

The first composition step in [#584](https://github.com/Dj-Shortcut/rust-skate-mod/issues/584)
reuses the switch/nollie heel-flick and fakie toe-flick references for their
double and triple variants. These six presentations have independent identities;
they are not six additional filmed captures. The five existing source variants
remain available.

Use retained, converted clip JSON files as inputs:

```json
{
  "version": 1,
  "sources": {
    "switch-heelflip": "source-clips/switch-heelflip.json",
    "nollie-heelflip": "source-clips/nollie-heelflip.json",
    "fakie-kickflip": "source-clips/fakie-kickflip.json"
  },
  "variants": [
    "switch-double-heelflip", "switch-triple-heelflip",
    "nollie-double-heelflip", "nollie-triple-heelflip",
    "fakie-double-kickflip", "fakie-triple-kickflip"
  ]
}
```

```sh
python scripts/capture_pipeline.py compose /absolute/private/compose.json \
  --output /absolute/private/composed-output --include-base
```

Paths are relative to the manifest. `--include-base` also includes the declared
source clips; the two other batch-1 sources may be declared without deriving a
variant. The complete candidate is validated before replacing the previous pack.
Each derived JSON retains the source frames, contacts, board keys and provenance,
and records `sourceFamily`, `derivedFromClipId`, `requestedFlipDegrees` and
`derivation`. Its own `id` equals its movement kind and `reviewed` starts false.
Review and approval apply to that exact variant and pack hash; approving a source
does not approve its derived variants.

The game still owns every board turn and the available airtime. Presentation
keeps the source release/flick on the first turn and the return/catch on the last;
extra turns extend only the free body interval. A separately approved held-flip
extension may continue from the last shown source pose when both entries have
identical source samples and contact events. It cannot rewind the pop, change
families, extend after catch or acquire approval from its source. A longer review
flight does not establish that an ordinary flat-ground ollie has enough airtime.
Inspect both stances against the real board before approving any result.

While the controller's same-rim hold can still request an extra turn, presentation
reserves the last fully free source pose before the existing 0.08-second foot
return. A queued extra retains that reservation until physics consumes it.
An approved extension holds the last shown progress until its new family clock
catches up; it never stretches a near-catch foot pose through another board turn.
If source foot return already began, the extension is refused. With no pending
hold or extension, the original source clock remains unchanged. Physics, contact
events and the ordinary catch-approach guards retain authority.

## Job data

The manifest has `version: 1` and a `jobs` array. See the
[example recipe](examples/skate-captures.example.json). Its numbers are an
authored schema example, not calibration for an arbitrary source or a supplied
working capture. Replace the source paths and calibrate each actual attempt.

- `source`: local video/pose JSON, public reference URL, author and rights note.
  Optional `roi: [x,y,width,height]` declares a crop in original pixels. For
  multi-person video, `actorHint.hipAndHeightSourceKeys` declares source-frame,
  hip-X, hip-Y and body-height witnesses. The extractor tracks the unambiguous
  matching person; a missing or ambiguous actor fails instead of switching to a
  bystander. An actor hint is a selection constraint, not a measured skeleton.
- `window`: original inclusive frame indices and actual source fps. Do not join
  independent shots or treat a slow-motion replay as normal captured timing.
  Variable-frame-rate footage can opt into `frameTimes`, an inline array with
  exactly one finite timestamp per included source frame (at most 2401). Times
  are seconds relative to the first window frame: start at exactly 0, strictly
  increase, and end at the clip duration (at most 10 seconds). A nonempty
  `frameTimeProvenance` must explain the source presentation timestamps. For
  example, three frames can use `[0, 0.02, 0.07]`; do not derive these from
  average fps. `fps` remains nominal source identification in this mode.
  Camera keys, body/board/contact keys and event times use this actual duration;
  source-frame interpolation and detector timing follow the declared timestamps.
  The metadata retains the array and provenance, and cache keys include them.
  Without the array, the existing constant-frame-rate timing is unchanged.
  Resampling adds interpolated poses, not additional measured observations.
- `stance`: the recorded anatomical regular/goofy stance. The detector's body
  is normalized to canonical regular. Trick names such as switch and nollie
  remain explicit in `movementKind`.
- `camera`: an orthonormal source-to-authoring basis. Its rows are right, up and
  forward in MediaPipe coordinates; conversion is `world @ basis.T`. The
  camera coordinate conversion has determinant -1. A static basis preserves
  genuine body spins. Moving-camera corrections must be explicit; deriving a
  new camera heading from the hips every frame would erase the trick's turn.
- `body.pelvisKeys`: the declared root estimate in meters. Hip-relative
  detections alone cannot supply a measured world trajectory.
- `retarget.legPoleTransport`: optional `"source-plane"` transports the original
  hip/knee/ankle bend plane onto the target hip/ankle axis by its minimal
  rotation. It avoids projecting the raw thigh directly onto a different leg
  axis, which can amplify small depth errors into large knee turns. The default
  remains `"legacy-target-projection"` for existing packs. Degenerate axes,
  opposing axes and source bend components below 2 mm refuse conversion;
  uncertain straight-leg observations need explicit reconstruction. This does
  not improve source confidence or remove root/contact noise.
- `retarget.sourceContactRootFit`: optional explicit root reconstruction for
  planted feet. Set `mode: "source-knee-distance"`, a
  `maximumCorrectionM` greater than zero and at most 0.5, and nonempty
  `provenance` and `uncertainty` notes (at most 4096 characters each). The source
  knee angle determines each canonical hip-to-ankle distance. One coherent
  pelvis is fitted to the fixed ankle guide or the intersection of both guides'
  distance spheres, using `body.pelvisKeys` as the soft image/depth prior.
  Merely fitting inside the maximum leg reach can otherwise flatten an observed
  landing bend into an almost straight leg. Ankle guides, contact events, bone
  lengths and free-Air targets remain unchanged. Incompatible distances,
  underdetermined fits or correction beyond the declared bound refuse conversion.
  The existing canonical IK endpoint bounds still apply and any resulting
  distance adjustment is reported. Omission retains the previous behavior.
  Metadata and quality retain the reconstruction notes, source knee distances
  and additional root correction. This does not improve detector confidence or
  approve a clip. Producers must author coherent root/free-foot joins at release
  and catch; the core option does not silently smooth those boundaries.
- `board.floorEnvelope`: optional `{points, provenance, sourceSha256}` replaces
  the default eight-corner floor box with 4–4096 finite board-local points in
  meters, relative to the deck-top origin. Supply the complete geometry needed
  for floor clearance, including wheels and trucks. `provenance` must explain
  the envelope (at most 4096 characters); `sourceSha256` identifies its source
  bytes with a lowercase SHA256. All three extents must exceed 1 mm and point
  coordinates must remain within 2 m. Captured key-space reflection applies to
  this envelope as well. The digest records provenance; it does not authenticate
  an external file. A rest envelope does not simulate moving trucks or wheels.
- `feet.Left/Right.headingKeys`: optional covering `{t, value}` direction keys
  in board-local space, with a nonempty `headingProvenance` (at most 4096
  characters). They apply only while that foot has an observed contact. Free
  feet retain source-derived headings. Zero, opposing adjacent or sole-parallel
  headings refuse conversion. Explicit planted headings can prevent model
  heel/toe noise from rotating a planted shoe; they remain authored estimates.
- `feet.Left/Right.airHeadingKeys`: optional covering absolute authoring-frame
  directions used only while that foot is free. Supply a bounded nonempty
  `airHeadingProvenance`. Captured key-space reflection applies once. Zero or
  sole-parallel directions, or adjacent turns above 150 degrees, refuse
  conversion. These keys reconstruct a shoe frame; they do not establish
  anatomically valid ankle twist or clearance against the rotating board.
- `legs.Left/Right`: optional explicit bend-plane reconstruction containing
  `poleKeys`, `provenance` and `uncertainty`. Supply 2–4096 covering direction keys
  in absolute authoring space, before optional `body.authoredHeading`. Global
  captured key-space reflection and side exchange apply once. The existing
  measured-chain solve keeps hips and ankle targets unchanged. Zero, adjacent
  turns above 150 degrees or a pole parallel to the current hip-to-ankle axis
  refuse conversion. A declared valid pole can replace an underdetermined model
  bend plane; it does not bypass actor/confidence gates or add an observation.
  Reports retain this choice under `legBends`. Check visible knee location and
  anatomical foot/calf orientation together, including planted feet.
- `body.headAnchor`: optional `"nose"` (the unchanged default) or `"ears"`.
  The ear option uses the midpoint of model landmarks 7 and 8, rather than the
  nose, for the shoulder-midpoint-to-head direction. It requires a nonempty
  `body.headAnchorProvenance` explanation and records that choice in clip
  metadata. The measured neck-to-head length, other joints, `HeadForward` and
  existing confidence rules remain unchanged. This is an estimated head-center
  reconstruction; it does not make occluded ears measured or replace whole-clip
  visual review. A degenerate ear direction fails the job.
- `body.authoredHead`: optional, explicitly uncertain head reconstruction for an
  occluded interval. It requires `provenance` and `uncertainty` strings, each
  nonempty and at most 4096 characters. `blendTimes: [start, fullIn, fullOut, end]`
  are clip-relative seconds with `0 <= start < fullIn <= fullOut < end <= duration`.
  `orientation` must have `kind: "quaternion"` and exactly two `{t, value}` keys
  inside that window, with increasing times and unit XYZW quaternions at most
  150 degrees apart. These describe a chest-local right/up/forward frame, not
  world head positions. For example, neutral schematic anchors could use
  `"orientation": {"kind": "quaternion", "keys": [{"t": 0.9, "value": [0,0,0,1]},
  {"t": 1.7, "value": [0,0,0,1]}]}` within `blendTimes: [0.8, 1.0, 1.6, 1.8]`.
  Anchor progress and entry/exit correction use quintic C2 easing; the head aim
  shares the correction envelope. Only `Head` and `HeadForward` change inside
  the open window. Outside it both remain exact; neck position, fixed head
  length, all other joints, board, contacts and raw confidence remain unchanged.
  The existing source/actor/rejection gates run first: authoring cannot fill an
  otherwise refused gap or lower `maxGapFrames`. Degenerate or opposing head
  frames fail for review; entry/exit blends also reject a baseline-to-authored
  frame arc above 150 degrees, including ambiguous roll. The frame follows
  `keySpace`; captured goofy keys use
  the full axial quaternion reflection `(qx, -qy, -qz, qw)` into canonical space.
  Clip metadata and quality retain the authoring provenance and uncertainty;
  quality also lists affected sample times. Keep those JSON receipts with the
  unchanged-format pack; the binary does not add an authoring field. This option never promotes
  confidence or grants visual/native approval. Omitting it preserves the
  existing reconstruction path.
- `body.authoredHeading`: optional, uncertain correction of a common reconstructed
  camera-heading error. It requires `yawKeys` (`t`, `degrees`), `provenance` and
  `uncertainty`. Supply 2–32 increasing keys within the clip, bounded to ±90°,
  with zero at both window ends; adjacent changes must not exceed 90°.
  Quintic interpolation rotates the complete solved rider about the unchanged
  board origin, after the existing filters, IK and head authoring. All joint
  points, directions and sole normals share that one transform; relative body
  twist and measured chain lengths remain intact. Original camera/foot/root
  keys, board rotation, events and confidence remain unchanged. Outside the
  window the original output is exact. Captured Goofy key space mirrors the
  heading sign once. Metadata and quality retain the reconstruction and affected
  times. This is not measured camera motion, a body-spin gameplay action or an
  approval; compare the resulting motion with the source before using it.
- `board.positionKeys`: board deck-top origin, in meters.
- `board.orientation`: full quaternion XYZW or unwrapped Euler degree keys.
  `order` declares Euler order. Continuous 360/720-degree keys preserve complete
  rotations; two identical endpoint quaternions cannot establish a full flip.
- `events`: explicit load/pop/air/catch/land/recovery times and foot/hand
  contact masks. An airborne foot is not pinned to a flipping deck.
- `feet`: optional contact paths in board coordinates. Default placements and
  any corrections are reported as estimates. Optional `airPositionKeys` use
  absolute authoring coordinates, like `body.pelvisKeys`, while the foot is
  detached. They require an explicit `airPositionProvenance` string. Use actual
  source witnesses to reconstruct clearance and a continuous approach to a
  catch; keep the original model detections and mark reconstructed paths as
  estimates. Grounded contact keys and event times still define attachment.
- `retarget`: bounded missing-data and reach-correction policies. Low presence,
  visibility and source/ROI clipping are evidence of uncertainty, not proof of
  a measured limb. Unresolved gaps fail the job.
  The optional `occludedPolicy: "retain-model"` preserves finite, in-frame
  original model estimates when presence is at least 0.8 but visibility is low.
  These points remain explicitly untrusted and cannot become interpolation
  anchors. Crops, low presence and missing/ambiguous actors do not qualify.
  This avoids holding a future airborne hand over a long ground-phase gap;
  it does not make the hidden motion measured or visually correct.
  Source review can explicitly downgrade bad model points using
  `rejectedModelFrames: {"14": [2453, 2454]}` (raw landmark indices and original
  source frames), with a required `rejectedModelProvenance` explanation. Such
  points cannot remain trusted or retained estimates. The same bounded
  trusted-anchor interpolation and edge policy apply; actor identity gaps still
  fail. The report lists every rejected and reconstructed point. This is motion
  cleanup from reviewed source evidence, not another measured observation.
  Optional `armInterpolation: "spherical"` interpolates the normalized upper-arm
  and forearm directions between source frames. This preserves source-frame
  directions while avoiding angular bursts caused by normalizing interpolated,
  shrinking model segments. Opposing/degenerate directions fail for review.
  The default `"linear"` preserves earlier reconstruction behavior. Neither
  mode makes an incorrect or occluded source observation trustworthy.
  Optional `recoverySmoothing` contains `start`, `full`, `passes` (1–4),
  `provenance` and `uncertainty`. Its C2 window must start at or after Land and
  reach full before the clip ends. It adds symmetric model interpolation only
  inside that window. Raw detections/confidence and earlier airborne motion
  remain unchanged. The report records the largest model-point change; pelvis
  and contact curves remain separate reconstructions.
- `sampleHz`: evaluation rate. Interpolation at 120 Hz does not make a 30 fps
  source into 120 fps measured motion. Check release/catch timestamps against
  the actual output keys: interpolation across a free/contact boundary can
  change the measured shoe calibration. For constant-rate footage, an integer
  multiple of its exact frame rate aligns original-frame observations; fractional
  observations and variable-rate footage still require an explicit boundary check.

With `keySpace: "canonical"` (the default), body, board and contact keys are
already regular and are not mirrored again. `keySpace: "captured"` explicitly
transforms those keys together with the recorded body. Regular/goofy conversion
reflects X, exchanges anatomical limbs and reflects board rotation. World
placement and travel heading use the existing board-space transforms. Fakie,
switch and nollie need correct recorded pop/contact semantics; a label or
mirror alone does not create another authentic trick.

## Runtime pack

Place the generated pack at `BepInEx/plugins/skate_captures.skmc`, beside the
mod's plugin content, then start the existing `zz rigging` diagnostic. Building,
installing and starting a diagnostic are execution steps; this does not imply
that a native game session has been validated.

Restart the client after replacing the pack; the catalogue loads it once per
process. Capture entries show their recorded movement and review status. Missing or
invalid capture packs produce explicit feedback while the existing pose
catalogue remains available. Normal riding does not automatically consume
unreviewed reference captures. Stage bounds, stop/reset behavior and player-root
ownership stay in force.

The runtime-neutral binary reader validates sizes, timestamps, UTF-8 metadata,
sample shapes and finite values before sampling. The shared sampler handles
full board orientation, measured limb lengths, anatomical mirroring and contact
state. Bounded pelvis fitting adapts near-extension poses to the actual hip width
while preserving ankle targets and real bone lengths; sampling exposes that
correction. Unreachable fits fail. There is no dependency on a native Unity JSON
parser for the pack.

`SKMCAP01` packs are little-endian. The header is the eight-byte magic and a
uint32 clip count. Each clip stores five uint16-length-prefixed UTF-8 strings
(id, label, source URL, author, movement kind), a reviewed byte, float64 duration,
uint32 sample count, sample rows, and uint16 event count. An event is float64
time plus phase byte and contact-mask byte. Trailing or truncated data is invalid.

Each row has 83 float64 values: time; board origin XYZ; quaternion XYZW; 18
joint XYZ targets; five body-direction XYZ vectors; and two sole-up XYZ vectors.
The joint/direction order matches the existing capture retarget contract. Stored
toe points are physical endpoints; the native sampler returns the writer's flat
contact datum so the measured ankle-to-toe drop is applied once.

## Review and normal riding

Conversion alone does not enable a capture during normal skating. Inspect the
whole source/rig movement, including the load, free feet, catch, physical
landing and upright roll-away. Record the exact clip JSON hash and local review
evidence hashes using the [review receipt example](examples/skate-capture-review.example.json).
The receipt records an animation review, not native Rust-skin or gameplay
acceptance. Keep failed captures unapproved and preserve their source evidence.

```sh
python scripts/capture_approve.py /absolute/private/capture-output \
  --review /absolute/private/review.json --output /absolute/private/staging
```

This requires a complete successful batch whose clip files still reproduce its
pack. It verifies every named clip and evidence file, then writes a separate
`skate_captures.skmc` and `skate_captures.approved.txt`. Only the explicitly
reviewed clips receive reviewed metadata. The approval pins the exact staged
pack digest and selected ids. The original batch stays unchanged. Existing
approval does not carry over to a changed pack implicitly.

Place both files in `BepInEx/plugins/` and restart the client. For the owner
launcher and direct sessions, stage the pair in
`Downloads/claude-loader-probe/captures/`; `$pcaptures` overrides this directory
for a direct session. The launcher copies the pair into `play/`, and the friend
packer includes that installed pair. It does not include source videos, model
weights, raw detections or review working files. Missing, incomplete, malformed
or mismatched approval keeps normal riding on the existing poses.

The initial exact variants are Switch Heelflip, Nollie Heelflip, Fakie Kickflip,
Nollie Backside 180 Heelflip and Nollie 360 Flip. Matching uses the actual pop,
stance and requested rotation values, rather than a displayed trick name.
Unmatched variants keep the existing presentation. These are flat-ground
references; ramps, walltricks, grabs, grinds, manuals, oldschool actions and
other competing presentation states retain their existing routes.

The game supplies its current board frame, body heading, rotation progress and
actual landing. Capture data supplies relative body motion and contact targets.
The recorded board trajectory never drives the player's world position or
velocity. Recorded full board rotation transforms declared contact targets to
the full live deck frame; it is not added to the game's rotations a second time.
The existing charge pose remains authoritative before the requested trick is
known. Catch and recovery cannot cause an early gameplay landing. A changed
trick or interruption relinquishes capture presentation, except for the
separately approved, same-source held-flip extension described above.

The entry weight applies to the complete pose, including a foot still marked as
Pop contact. Apply its measured deck-support fit after blending; pinning it
directly to the source target would skip the entry weight and then jump back
when its release removes that pin. Physical landing anchors remain pinned
through recovery. During the initial crossfade, a released shoe with a supported
projection also receives any positive measured clearance offset: blending toward
a clear source trajectory can otherwise leave the shoe inside the kicked nose.
This clearance applies only while entry weight is below one, preserves the
observed contact mask and never pulls a free shoe down onto the deck. These
adapter rules do not approve the source foot trajectory.

Captured footing also requires measured profiles of the current visible shoes
and the primary detailed deck geometry. The client collects bounded post-pose
skin snapshots during ordinary flat-ground riding, separately for first- and
third-person views. Unsupported, unreadable, morphing or nonrigid foot patches
remain unavailable; skating retains its procedural route. View/model changes
invalidate the corresponding fit rather than reusing another shoe's dimensions.
No fixed assumed shoe thickness is substituted.

The support fit uses the actual concave/kicked deck triangles. It shifts declared
contacts along the deck normal and solves the measured leg chains again. The
same model calibration continues through free-foot intervals, and attached feet
are refitted after presentation blending. Bounded pelvis fitting leaves a small
reach reserve before the native writer's straight-knee clamp, preserving the
requested ankle contact instead of silently shortening it. This does not declare a free foot to
be attached, change a catch/landing event or move the player. The native collector
and fitting source require native verification; a passing companion mesh check
does not establish clearance for Rust's boots. Free-foot motion must still be
checked against the rotating board, not merely at attached contact samples.

Ordinary takeoff can already be in Air while both soles still have full support.
Those frames refit against the pitched deck rather than carrying the previous
flat-ground offset. Released feet retain model calibration without inventing a
contact. Capture recovery hands back to ordinary riding with a C2 fade over the
last 0.45 seconds, capped at half the post-landing interval so the original
landing impact remains intact. This avoids compressing the whole leg-pose
transition into a 0.12-second linear fade; it does not repair noisy source roots
or drifting authored foot anchors by itself.

The first observed landing records model-fitted board-local foot frames. They remain
fixed during recovery and ordinary roll-away, including after capture ownership
ends. A normal or mongo push releases only its moving foot. New actions, stance,
rig or shoe-profile changes invalidate the corresponding placement. UI loss can
retain ground support while the body leaves the captured pose with a short C2
transition.

Supported recovery explicitly reconstructs leg bend direction: it transports the
first displayed landing's bend planes onto each current hip-to-ankle axis, then
returns them to the ordinary supported planes with a C2 transition from 0.12 to
0.47 seconds after landing. This bend-direction step retains the existing foot,
pelvis and torso targets; the separate landing prior below may lower the body.
Neither is a new measured source observation. Gaze returns separately from 0.12
to 0.72 seconds using heading and elevation, avoiding a vertical shortcut between
opposite headings. Captured recovery limits displayed head yaw relative to the
current chest; camera look remains unrestricted. Idle
riding retains the measured rig's soft knees without adding an extra crouch.

Captured presentation has a shared **authored knee prior** for ambiguous
monocular depth. Once Catch has actually returned both feet to the board, it
prepares at least 35 degrees of knee flex (145-degree internal angle) over
0.04 seconds. Free feet,
source return, board rotation and physical contact timing are unchanged. The last
shown preparation continues at the actual landing; compression then reaches a
at least 52.5 degrees of flex over 0.065 seconds and releases over 0.30 seconds.
Without a completed catch, landing starts unprepared. The angular floor avoids
compressing the already prepared impact pose a second time. A deeper source is
not straightened.

Both hips and the upper body move together, capped at 0.14 m on the reference
0.87 m leg chain; ankle and toe anchors remain fixed and canonical IK stays in
force. This is presentation reconstruction, not measured capture, a changed
contact event or a physics/scoring change. Review replays must identify the
authored prior and use the same `SkateCaptureFootSupport.TryCatchPreparation` and
`TryLandingAbsorption` implementations as normal riding. Source packs and their
approval remain separate.

All captured flips share a physical Catch rise limit. The previous displayed
pelvis is transported to the current riding carrier; source stand-up cannot add
another upward pelvis motion as the feet return to the board. The capped coherent
body shift keeps the current foot positions and reconstructs canonical legs.
It starts on physical Catch even while source return is still labelled Air;
source contacts, rotation and the real flight remain authoritative. Land inherits
the reached final knee floor. Review metadata must identify `CatchRiseProvenance`.

The nollie BS180 heelflip uses a deeper authored supported profile: a 100-degree
internal knee target in Catch and at impact. Its deeper catch eases in over
0.12 seconds, retaining the same impact/release clock, drop cap and fixed feet.
Its knee planes ease toward the ordinary toe-directed
guides because retaining the ambiguous source planes still looked straight.
Normal 360 Flip uses a 135-degree catch target with the default landing target
and source knee planes. The other families keep the default angles and planes.
Review metadata must
include `SupportedKneeProvenance`.

The nollie BS180 heelflip also has an explicitly authored supported-torso
cleanup for its unreliable filmed lean. From Catch through recovery, a 0.12-second
entry eases the upper body toward a 30-degree lean, with at most 25 degrees of
correction. It rotates the chest/arm chain together around the pelvis; the helper
retains the pelvis heading, leg and foot points, and solved world gaze. Board
rotation, contacts, landing and approval gates are unchanged. Review metadata
must include `SupportedTorsoProvenance`; this is visual reconstruction, not a
source observation or acceptance of the entire BS180 clip.

Presentation events may repeat a phase to describe individual foot contacts.
The initial load has both feet attached; load/pop may release them separately,
and air has neither foot attached. Catch contacts may accumulate separately;
land and recovery require both feet. Reattachment before catch, contact loss
after catch, reversed phases and simultaneous conflicting events are refused.
The first event of each phase remains its timing anchor.
The first live pop frame retains the procedural pose. The normal route can use
the known variants' Pop-to-Catch rotation clock to retain their individual foot
releases. Callers without that option retain the Air-to-Catch clock.
For the registered source families, playback precomputes an inverse of the
source board's unwrapped Y*X*Z rotation progress. The pose follows the last
unfinished live rotation instead of assuming a linear source-time relationship.
Each active source axis must advance without reversing; a plateau in the limiting
progress, a large rotation step, a turn more than five degrees from its expected
endpoint or a near-gimbal pitch keeps
the previous linear clock. The startup status counts the Air-to-Catch inverse
clocks and the Pop-to-Catch inverse clocks separately. The arrays are built during
loading; lookup does not allocate.

The registered `bs-pop-shove-it` and `fs-pop-shove-it` presentations use an
ordinary ollie stance, zero flip/body spin, and live shove targets of -180 and
+180 degrees respectively. Their canonical source yaw is +180 and -180 degrees.
Only yaw drives these inverse clocks; the inactive roll stays within five
degrees and is never used as a divisor or limiting progress. A zero-flip source
has no held-flip expansion. Existing rotation/contact guards and pack approval
remain unchanged. This source route does not approve either new capture or
establish visual or native-game acceptance.

A source's first trusted Air pose can already be beyond half its rotation. Its
Air-to-Catch inverse then retains the existing guarded fallback, while a valid
Pop-to-Catch inverse can still drive the normal route. The startup field
`source pop-to-catch clocks` identifies that route; a zero Air-clock count alone
does not mean the normal route uses a linear source clock.

The normal `hardflip` presentation selects exactly an ollie with a +360-degree
flip and +180-degree frontside shove, with no switch, tail-leading or body/spin
modifier. This matches the existing gameplay classifier. Its canonical source
clock expects +360 roll and -180 yaw. Both active axes must pass the same inverse
guards; an opposite-yaw or otherwise ambiguous source retains the phase-clock
fallback. Registration and a readable pack do not establish correct free-foot
motion, catch or visual acceptance. The captured frontside scoop and toe flick
need their own reviewed source and both-stance normal-route inspection.

Normal Hardflip adds an explicitly authored early free-front-foot tuck. It lifts
the anatomical front ankle and toe together, up to a reference-scaled 0.28 m,
using the existing canonical leg solver without moving the pelvis or torso.
Only a foot without source contact is eligible. The C2 prior occupies the first
half of the trusted Air-to-Catch source interval, peaks at two sevenths, and
respects the existing capture-entry weight. It ends before late air/catch;
the shared catch and landing remain unchanged. Review metadata must retain
`HardflipEarlyTuckProvenance`; this is presentation authoring, not measured motion
or permission to approve the source pack.

The `impossible` capture uses a separate special-action adapter, not an ordinary
zero-flip/shove variant. The same reviewed-bit, explicit owner-id and exact pack
digest gates load its own slot; duplicate Impossible approvals disable loading.
Only the normal rear-foot Impossible, direction +1, without nollie, reversed,
switch, tail-leading or body/spin modifiers is supported in Regular and Goofy.
Other special actions retain their existing native presentation.

The existing `BoardPath` owns the wrap, pivot, physical progression and completion.
Normal rear-foot Impossible uses an authored reference-shoe clearance envelope:
the rear foot follows its native hook while contact travels around the shoe and onto the
real curved tail, including its kick height, to clear the shin during inversion.
It closes one vertical 360-degree turn with no added yaw. Its ordinary ollie AirPitch is suppressed
because the wrap already owns that rotation. Its authored native rear-foot pickup supplies up to 0.30 m
of descending-wrap clearance and returns for Catch; the separate ordinary ollie
visual lift is excluded for this family. Physics and scoring are unchanged.
Direction -1, nollie/reversed and other special trajectories retain their prior paths.
Its progress maps the source's first Pop to first Catch; the actual completion
starts source Catch and the observed physical landing starts source Land/recovery.
The source's estimated wrapping-board origin is removed about the rear-foot pivot
before transporting its body to the upright physical carrier. No source root,
board quaternion or timestamp changes movement, scoring or the live board.

The native hook ankle/toe targets and upright sole remain authoritative during the wrap. An
authored 0.26 m free-front-foot tuck follows native path progress with eased entry
and return, only while that source foot is released. The existing supported
knee prior uses the family-specific 100-degree limit and toe-directed bend plane
through wrap, catch and landing, preserving deeper source crouches. The
free front ankle can follow the source only while its source contact is absent
and the existing measured foot fitter accepts the live projection; its native
shoe heading remains upright. The final entry-blended free foot is fitted again;
an unsafe blend restores the native foot targets before the separately labelled
free-foot tuck. Steep, inverted or unsupported source projections retain native
foot orientation and clearance rather than weakening the sole/contact guard. Both
native feet own completed Catch and Land; the captured body uses the canonical
chain solver, a bounded entry fade, physical landing transition and recovery fade.
The existing shared knee prior continues through native completion and both
source Catch contacts over those fixed native feet. Its reached knee floor carries
into the same shared landing absorption, including the first physical Land frame.
These are the existing authored priors, not additional measured observations. Bail, another action, path release, changed stance
or changed visible-foot geometry relinquishes the adapter for that pop.

`SkateCapturedBoardPaths.Provenance` must remain visible in review metadata.
This is source-assisted special presentation, not measured hook contact. The
private source remains unreviewed; bounded reader and controlled normal-route
checks do not approve its estimated or low-visibility observations. Inspect both
stances with the real board before source approval; Unity/native and Shadow
acceptance remain separate open gates.

For the Air-to-Catch clock, the first trusted air sample can already be partway through its rotation. It is
held until live progress reaches that fraction, rather than moving back into a
foot-supported pop pose. An unfinished source endpoint is likewise held before
catch; it is not rescaled into a fictitious complete turn. Catch and landing
still follow live state. Matching the limiting component does not independently
match every source/live flip and yaw angle or prove foot clearance. Inspect the
actual skin and runtime board shape before approving a pack.

When live rotation stops, a stretched terminal source clock may still have a
short free-foot return left to play. Normal playback finishes that source return
at source speed, using the previous displayed source time and elapsed render
time, before introducing the source catch contacts. This lead is limited to
80 ms; a longer gap is refused. Physics, scoring and the live board remain
unchanged, and the existing new-contact approach-distance guard still applies.
Landing continues to use the observed physical landing time.
After impact, the normal adapter plays the filmed ground recovery at 1.35× speed
so the rider rises out of a deep landing sooner. The physical landing itself is
unchanged. The ordinary cruising pose keeps bent knees and more relaxed arms.

At free-foot release/catch boundaries, normal playback can transport the whole
shoe frame between its upright carrier and the live board frame. A C2 window is
limited to 80 ms or one quarter of the free interval at each edge; the middle of
flight remains independent of the flipping board. This avoids a coordinate-frame
jump, but neither repairs an incorrect source shoe pose nor guarantees that a
presentation crossfade stays outside the board. Check ankle bend/twist and the
actual skinned cuff as well as the sole and deck, including intermediate frames.
The measured positive-only free-shoe deck fit also applies during this catch
approach, as it does during entry. It preserves the source contact flags and
does not pull a free shoe down onto the deck. Its existing bounded/best-effort
fit is not a proof of clearance against the complete board or truck geometry.

For the existing nollie 360 flip, the free flick foot gets a short, smoothly
blended knee tuck during the final three source frames before catch; its shoe
orientation and upper body remain unchanged. The BS180/heelflip combination
uses the full source-to-live board frame for its free shoes so the remaining
pitch difference does not place a shoe through the deck. These are explicit
presentation adapters for those two tracks, not automatic rules for all families.

Recipes can opt into `body.anatomicalHeadFrame` with mode
`source-gaze-neutral-roll`, `up: [0,1,0]`, a `minimumUpProjection` between 0.1 and
0.5, plus explicit `provenance` and `uncertainty`. This reconstructs the skull's
up axis perpendicular to the retained source gaze instead of using the nose's
position as the neck/skull axis. It keeps the neck/head length, source gaze and
other joints unchanged. A near-vertical gaze refuses reconstruction because the
declared neutral roll is underdetermined. It cannot combine with `authoredHead`;
defaults remain unchanged. The four repaired exact clips also transport their
head axis with the settled recovery gaze; Switch retains its existing authoring.

The [full animation-library batches](RUST-SKATE-ANIMATION-LIBRARY.md) keep code,
capture quality and native gameplay acceptance separate.

The approval file is strict UTF-8 (at most 32 KiB), with these lines and one to
256 unique canonical clip ids:

```text
SKMCAP-APPROVAL-1
sha256=<64 lowercase hexadecimal characters>
clip=<exact clip id>
```

Both approval and pack are read once per client process, outside the per-frame
presentation path. A restart is required after replacement. Review metadata is
not a source licence and does not establish native gameplay acceptance.

## Source library and acceptance

The owner selected Skate IQ's
[stationary ollie](https://www.youtube.com/shorts/cyX5cdbqqLs) and
[Battle at the Berrics](https://www.youtube.com/watch?v=PqJZcwTY1QE)
as motion references. Berrics supplies many on-screen trick names. YouTube
chapter boundaries are selection hints: one chapter can contain several
different on-screen labels and attempts. Select a continuous successful attempt
and retain the visible label, performer, timing and occlusion limits.

Source footage, detector weights, raw detections and derived private motion
packs are not bundled in this repository. A source URL is attribution, not an
automatic licence grant; do not label source-derived captures CC0 without an
applicable grant. The earlier separately licensed ollie asset keeps its own
licence.

Check source/rig comparisons, contact changes, complete board rotations and
export fidelity per clip. A passing parser or compatibility compile does not
establish natural motion, the native Rust skin, normal-pop integration or
in-game acceptance. These remain separate verification gates.
