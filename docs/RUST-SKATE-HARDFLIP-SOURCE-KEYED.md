# Source-led ordinary Hardflip

This family uses its own black-shirt/red-shoe BATB12 take, not a relabelled
Kickflip or Heelflip pose. The private source sequence was checked through
pop, steep board passage between the legs, completed rotation and landing.

The exact runtime opt-in is `hardflip-source-keyed-v1`, movement `hardflip`,
with ordinary Ollie, flip +360, frontside shove +180 and no body spin. Canonical
board yaw is −180; longitudinal roll is +360. Once the board turns, its local
front/back foot coordinates exchange ends while the rider remains facing the
same direction. The selector does not opt other family IDs into this route.

| Source-cache frame | Phase |
| --- | --- |
| 132–140 | Low approach and rising preparation |
| 141 | Direct pop and front-foot release begins |
| 141–142 | Front instep exits before the strongly pitched free board |
| 142 | Rear-tail support |
| 143–144 | Rear sole releases; free Air |
| 146–152 | Steep frontside shove/kickflip, asymmetric pickup |
| 158 | Both soles catch the completed board |
| 160 | Landing |
| 164–184 | Absorption, gradual recovery and rolling away |

Cache time is frame / (30000/1001); the original video window begins at188s.
The source is [the owner-selected Berrics video](https://www.youtube.com/watch?v=_oRoo1x1uzM&t=192).
Reference pixels remain private. The public inputs contain source-derived
retarget directions and separately authored fixed-chain/contact controls.

The source's compact front knee and lower rear chain guide this reconstruction.
Its foot depth, full board3D orientation, frontside tail pocket and hidden
instep contact are estimates. Earlier detector gaps140/141/152 are explicitly
retained as reconstruction uncertainty. Hands briefly leave the image near144.
None of these controls are labelled calibrated3D motion capture.

The curved deck, measured sole footprint, fixed bone lengths, strict reach
checks, deep generic charge and live physical carrier are shared mathematics.
The Hardflip body, scoop, flick, free-foot path, board sequence and landing
controls belong to this take. Physics, scoring, action ownership and physical
landing remain authoritative. Playback uses this clip's actual Pop/Catch/Land
events; no Kickflip frame numbers are reused.

Rebuild locally with:

```sh
python scripts/author_skate_hardflip_hybrid.py --output context/artifacts/hardflip-source-keyed/output
```

The resulting pack is `Reviewed=false`. A separate private test-only clone
can exercise the ordinary route; it grants no installation approval. Viewer
review and the later Shadow/native test are separate gates.

The near-vertical passage is a real distinction in this take: the front shoe
rises substantially above the rear shoe while the front knee folds. The free
Air leg proportions, heel-side front-foot pickup, toe-side rear-foot pickup,
body balance and knee planes come from this Hardflip retarget. Scalar checks
written for a horizontal board can therefore miss their default hip/sole-gap
or equal-foot-height limits. Those results stay visible for family/source
review; no checker threshold or physics has been relaxed.
