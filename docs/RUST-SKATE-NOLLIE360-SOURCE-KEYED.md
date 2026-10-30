# Source-led Nollie 360 Flip

This family uses the cached striped-shirt take from the owner's [Berrics reference](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=153). The take visibly shows the Nollie360 label, a nose pop, complete flip and shove, landing and roll-away. Its original footage and source packs are preserved. The generated pack remains `reviewed=false`; a separately hashed, explicitly test-only copy is required for offline normal-route evaluation. Native Shadow approval remains separate.

`author_skate_nollie360_hybrid.py` uses this take's own relative upper-body directions. It excludes the old standing pelvis/mean-ankle prior and the V5 free-foot heights that made the original preview too high and uneven. The reconstructed pelvis and both free ankles share one upright carrier and their own source-led pickup and knee planes. No ordinary Tre, Kickflip or Heelflip movement keys are copied.

The real input-owned Nollie charge places the nose/front footprint on the nose and the rear footprint around the middle. This family then uses a front-foot nose scoop, a rear-foot instep exit, independent complete +360 longitudinal roll and +360 canonical yaw, asymmetric collection, catch, later absorption and soft roll-away. Native signature remains Nollie / flip +360 / shove −360 / no body spin. Physics, scoring, collision timing and the real landing remain authoritative.

| Source frame | Visible phase and reconstruction |
| --- | --- |
| 4592 | Start of extension/Pop; vertical gameplay launch is immediate |
| 4594 | Nose wood strike, derived against the original full board support geometry |
| 4593.75–4595 | Rear sole releases into its instep contact and then leaves the tail |
| 4595.25–4597.25 | Front nose sole leaves; explicit short authored press fade |
| 4602 | Collected compact Air, own asymmetric feet and toe-facing knee planes |
| 4607 / 4608 | Authored front-first catch, then both soles on returning grip |
| 4609 | Source landing, mapped only after an actual native landing |
| 4612–4625 | Absorption and continuous roll-away |

The source's low nose shoe and already free rear shoe make front support plausible; the first lower returning shoe also supports front-first catch. Blur and occlusion do not prove the exact contact depth or subframe. These are authored annotations, not inherited measurements: the older Right-only masks are deliberately not treated as anatomical truth. Hidden depth, rear instep orientation and the preview/native stance mapping remain uncertain. No detection confidence is increased. Source pixels are never mirrored.

The exact new clip ID is `nollie-360-flip-source-keyed-v1`. The existing keyed selector accepts only that ID, movement kind and full signature. Its pressure window reads the front popping-foot event; existing families still read their original rear-foot event. Its root joins at this take’s first unambiguously free, broad-grip Air fraction 6/17 (source4598) with the existing world-velocity continuity; other family paths are unchanged. Missing observed ground refuses this exact nose-press clip and keeps the native fallback. The canonical carrier keeps actual native flight timing, with separate 0.600 s and minimum-charge 0.528 s evaluations.

The source envelope uses the unchanged original thirteen-part board support points. A bounded, coupled source translation protects the supported minimum flight while retaining the same body/feet/board relationship; it does not add a gameplay jump or an independent body arc. Fixed-chain reach and current contact guards remain strict. A successful bake or native-callsite fixture is not a visual or in-game approval.

Rebuild with a Python environment containing NumPy:

```sh
python scripts/author_skate_nollie360_hybrid.py --output /private/output/nollie360
```

Reference footage is not included in Git or relicensed. The source controls retain source-video, original pose and raw-detection hashes and distinguish upper-body estimates from authored lower-chain reconstruction.
