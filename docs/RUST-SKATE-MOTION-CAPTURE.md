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
- `body.headAnchor`: optional `"nose"` (the unchanged default) or `"ears"`.
  The ear option uses the midpoint of model landmarks 7 and 8, rather than the
  nose, for the shoulder-midpoint-to-head direction. It requires a nonempty
  `body.headAnchorProvenance` explanation and records that choice in clip
  metadata. The measured neck-to-head length, other joints, `HeadForward` and
  existing confidence rules remain unchanged. This is an estimated head-center
  reconstruction; it does not make occluded ears measured or replace whole-clip
  visual review. A degenerate ear direction fails the job.
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
- `sampleHz`: evaluation rate. Interpolation at 120 Hz does not make a 30 fps
  source into 120 fps measured motion.

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
trick or interruption relinquishes capture presentation.

Presentation events may repeat a phase to describe individual foot contacts.
The initial load has both feet attached; load/pop may release them separately,
and air has neither foot attached. Catch contacts may accumulate separately;
land and recovery require both feet. Reattachment before catch, contact loss
after catch, reversed phases and simultaneous conflicting events are refused.
The first event of each phase remains its timing anchor.
The normal air clock starts at the recorded air phase, after both source feet
have released. The first live pop frame retains the procedural pose.

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
