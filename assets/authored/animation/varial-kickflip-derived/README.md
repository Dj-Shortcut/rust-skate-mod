# Authored Varial Kickflip from a shared Tre body

These controls derive a new BS180 + kickflip board path from the existing
unreviewed Tre reconstruction. There is no claimed filmed Varial attempt.
The reference images and video show the Tre used for the body reference.

The rear toe-side scoop is smaller. The front upper-shoe release precedes
the longitudinal flip. The free rear-foot depth is reduced, then both feet
return to anatomical world-side targets. A half-turn swaps board-local nose
and tail, so the complete catch point and toe heading are transformed through
the final deck rotation; the old Tre sole pins are not retained.

The measured shoe/deck fitting and fixed-limb reach checks remain strict.
Source frames 403/404/406/420/424 retain their reference phase roles; the
intermediate 180-degree rotation, release timing and catch are authored.
Dense samples add no detections or measured contact confidence.

Generate the shared Tre source with its existing authoring workflow, then run:

```sh
python3 scripts/author_skate_varial_kickflip.py \
  --input /private/path/360-flip-source-keyed-v1.json \
  --output context/artifacts/varial-kickflip-derived/output
```

The input hash is pinned. Footage, detector outputs, evaluation approvals and
generated packs remain private. Original and generated source packs stay
`Reviewed=false`; a local test-only approval does not approve a game pack.
