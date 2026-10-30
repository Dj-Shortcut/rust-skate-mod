# Source-led Switch Heelflip Air

The existing Switch Heelflip recipe keeps an almost standing pelvis above its
independently authored free-foot paths. Normal playback preserves that shape;
adding another jump or capping individual knee angles would not repair it.
This family reconstruction fits the pelvis and both free-ankle heights together
around the actual striped-shirt reference at source frame 2239. It retains this
take's wide heel-side gesture and source bend planes. The free-Air window adds the visibly missing forward
fold as one coherent upper-body rotation, retaining its relative arm gesture
and gaze. It does not turn the body to fit the camera.

The input is the private original unreviewed capture. Its encoded SHA256 is
`33097aca96337fe757a4bb35dd1696b07517e02c6328ad7cf62552d238b6db69`.
The [reference attempt](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=74)
remains private research media; no footage or raw detector files are shipped.
Depth and meter controls are explicitly authored, not calibrated 3D motion.

```sh
python scripts/author_skate_switch_heelflip_air.py \
  --input /absolute/path/to/original/source.json \
  --output context/artifacts/switch-heelflip-air/output
```

The lower-body window remains source 2235–2244.75. A separate upper-body window
keeps the forward fold through Catch 2245, Land 2248 and absorption 2253, then
fades smoothly back to the source by 2263. Ending this fold before Catch made
the otherwise bent landing look upright. The arm gesture and gaze turn with
the upper group; no independent arm, neck or body-yaw curve is added.

The exact clip ID, complete board motion, events, contacts, foot orientations
and all lower-body/foot data outside the original Air window stay unchanged.
Samples outside the combined windows remain source-identical. The existing
supported runtime recovery and PR #615 head alignment remain in charge; the
whole action still needs inspection through the ordinary route. Fixed chains
and interpolated samples pass through
the existing strict writer before prior output is replaced. Output is always
`Reviewed=false`; a separate private evaluation copy grants no installation or
Shadow approval. Regular/goofy and normal/low physical flights require their
own mesh review. Physics, scoring, contact fitting and landing remain in charge.
