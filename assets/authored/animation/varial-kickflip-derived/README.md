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

Free-foot clearance enters with each foot’s existing release weight and fades
through the existing return-to-catch interval. It does not step on at the upper
release or disappear in one sample at Catch. The shared source’s filmed stance
and pose hash are retained as provenance, not a claim of filmed Varial motion.

Generate the shared Tre source with its existing authoring workflow, then run:

```sh
python3 scripts/author_skate_varial_kickflip.py \
  --input /private/path/360-flip-source-keyed-v1.json \
  --output context/artifacts/varial-kickflip-derived/output
```

The input hash is pinned. Footage, detector outputs, evaluation approvals and
generated packs remain private. Original and generated source packs stay
`Reviewed=false`; a local test-only approval does not approve a game pack.

The Varial now has its own explicitly authored compact Air and bent-elbow
balance. A stable toe-side chest frame replaces the Tre reconstruction's
crosswise heading; the torso folds forward rather than arching. Both hands
settle near the sides during Catch instead of extending into a straight T.
The free feet collect closer to the rotating deck and the pelvis follows into
a compact tuck and bent catch. The existing local shoe/deck-clearance and
fixed-leg reach checks still apply. Grounded press, final catch sole targets,
board rotation and physical carrier remain unchanged. These are authored
Varial choices, not captured Varial observations or new source confidence.

## Free-foot Air gap

The higher collecting foot now targets slightly nearer the rotating deck
during its authored Air window. The already-near rear foot retains its
accepted target. The same full-shoe clearance still lifts
a foot where the deck passes underneath; its minimum separation and fixed-leg
reach checks are unchanged. Board keys, carrier flight, grounded press, final
catch and recovery stay fixed. This is a small authored refinement of the
shared Tre derivation, with no filmed Varial claim or raw approval.

The refreshed original-rig comparisons use the same runtime and camera for
the prior compact source and this refinement. Air close-ups show the smaller
collecting-foot gap; the adjacent Catch views retain the accepted pose:

- [Normal Regular](../../../../docs/review-media/2026-11-01-varial-compact/normal-regular-air-catch-before-after.jpg) and [full phases](../../../../docs/review-media/2026-11-01-varial-compact/normal-regular-phases.jpg).
- [Normal Goofy](../../../../docs/review-media/2026-11-01-varial-compact/normal-goofy-air-catch-before-after.jpg) and [full phases](../../../../docs/review-media/2026-11-01-varial-compact/normal-goofy-phases.jpg).
- [Lower Regular](../../../../docs/review-media/2026-11-01-varial-compact/low-regular-air-catch-before-after.jpg) and [full phases](../../../../docs/review-media/2026-11-01-varial-compact/low-regular-phases.jpg).
- [Lower Goofy](../../../../docs/review-media/2026-11-01-varial-compact/low-goofy-air-catch-before-after.jpg) and [full phases](../../../../docs/review-media/2026-11-01-varial-compact/low-goofy-phases.jpg).

[Review receipt](../../../../docs/review-media/2026-11-01-varial-compact/review-manifest.json).
Private viewer: `http://127.0.0.1:63761/varial-air-gap/`. Four actual managed
cases reached ordinary riding at 4 s and replayed at 1×/¼× without browser
errors. These are offline checks with controlled bindings; native Rust/Shadow
and Claude’s independent library verdict remain separate.
