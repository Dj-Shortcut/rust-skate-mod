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
| 132–137 | Low approach |
| 138 | Visible release/body-rise onset; physics/input is immediate |
| 140.6–141.0 | Front sole rolls to a short instep exit before the steep nose |
| 142 | Rear-tail support |
| 142.4–144 | Rear sole releases between the supported 142 and free 143; free Air |
| 144–148 | Steep frontside shove/kickflip and asymmetric pickup |
| 150–152 | Grip returns before the feet descend for Catch |
| 158 | Both soles catch the completed board |
| 160 | Landing |
| 164–184 | Absorption, gradual recovery and rolling away |

Cache time is frame / (30000/1001); the original video window begins at 188 s.
The source is [the owner-selected Berrics video](https://www.youtube.com/watch?v=_oRoo1x1uzM&t=192).
Reference pixels remain private. The public inputs contain source-derived
retarget directions and separately authored fixed-chain/contact controls.

The source's compact front knee and lower rear chain guide this reconstruction.
Its foot depth, full board 3D orientation, frontside tail pocket and hidden
instep contact are estimates. Earlier detector gaps 140/141/152 are explicitly
retained as reconstruction uncertainty. Hands briefly leave the image near 144.
None of these controls are labelled calibrated 3D motion capture.

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

The steep board has a complete 13-part model-floor envelope. Authored pressure
release first stays above its full support hull. During an observed ordinary
flatground flight, an exact-Hardflip-only runtime bound can translate the whole
coupled pose and board together above the cached takeoff plane. Its maximum
correction is reported; over 12 cm refuses the capture instead of changing any
physics, landing, foot/reach guard or other family. This is labelled authored
presentation, not a new collision solver or a terrain query. Missing or stale
observed ground-entry data refuses this Hardflip route; it does not silently
continue without its required floor bound.

The observed ground-entry root joins the source body at the actual tail strike
(frame 142), before the rear sole leaves. Joining the much higher Air148 root
earlier had anticipated the tuck while that rear foot still supported the
board; the unchanged reach guard correctly rejected it. Upper entry remains
short and the later asymmetric Air follows this take.

The low release at 142 connects directly to the collected front pickup at
145. Early body and free-front-foot heights have their own source-led world
curves; they do not inherit the changing height of the steeper board. The
existing asymmetric rear chain and Catch/landing endpoints remain intact.
The visible early torso fold is reconstructed from this take; camera depth
and the original detector gaps remain uncertain.

The rear separation is authored within the observed 142–143 interval. Holding
that foot to the spinning tail until 143 had pulled its ankle downward after
the wood strike, creating another reach-limited body-height kink. Its later
Air shape, and the existing Catch and landing endpoints, stay unchanged.
