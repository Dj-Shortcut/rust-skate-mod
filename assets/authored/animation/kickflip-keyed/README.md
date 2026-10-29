# Kickflip: captured body with local authored constraints (unreviewed)

The current generator is `scripts/author_skate_kickflip_hybrid.py`. It uses the
compact `captured-body.json` reconstruction controls and
`hybrid-constraints.json`; it needs no private path, game file, video or detector
binary. The original `keys.json` and pure-key generator remain as historical
comparison inputs, not the current body-motion basis.

The body controls retain the existing retargeted skeleton as the basis: torso,
hips, arms, head and source knee bend planes. They derive from the original canonical pose
JSON SHA256 `e7dd177099095caf3855fd0adc0e2c0701fc36fad9db2b2a0ebf181709883c82`
and pack SHA256 `05a6fbcf4fdcfe6a339600123a14078ea589c58079b81a84894b676b25860bc3`.
There are43 selected actor frames254–296 and170 dense retarget controls,
including an interpolation guard. Dense controls are not additional measured
motion. Monocular depth, source root and hidden limbs remain unreviewed
estimates. The source footage/pixels are neither included nor relicensed; no
licence to them is asserted. These files contain the project's reconstructed
control data, not a new detector result or an all-body hand animation.

## Coordinates and timing

- The shared output origin is physical support `(0,0,0)`, +Y up and +Z nose.
  Flat board deck top is0.092m. Regular and its anatomical Goofy mirror are
  canonical mappings; the filmed stance is uncertain.
- Import removes exactly one **upright translation** from every captured joint:
  `[sourceBoard.x, sourceBoard.y - .092, sourceBoard.z]`. Directions are retained;
  the body is never inverse-rotated with a flipping board. This is an explicit
  source-carrier normalization, not a claim of measured player-root motion.
- Old clip time is new clip time+1.0677333333s (`32 / (30000/1001)`). New source254
  is t0. Pop259, explicitly authored Catch273 within blurred272–274 and Land275
  remain the event clock; no old inverse normal-route clock is imported.
- Runtime adds its actual physical upright carrier once. Neither the old6/16
  flight nor a new ballistic arc is baked into the body or authored board.
- Body capture continues past278 through the available real source296.
  Any viewer source cursor must distinguish this from the historical held
  all-body-key recovery.

## What is authored locally

Board winding/pitch and the event timing are explicit source-led estimates.
Early pitch/relative board lag, front upper/outer-forefoot slide/edge/release,
a short rear-foot clearance, late free-foot clearance and the smooth landing
hip-height envelope are authored. A short, explicitly labelled upper-body
reconstruction259–278 repairs visible captured misses: arm height261,
forward torso/head direction267 and asymmetric arms273. It rotates the upper
group around the unchanged pelvis and reconstructs the fixed-length arm/head
segments. Hidden depth and these poses are authored source-led estimates.
Outside its C2 window the captured upper motion is retained exactly. Changed
foot targets use measured fixed-length chains and the transported captured knee
plane; there is no generic knee-angle cap or legacy runtime correction stack.
Upper-shoe contact is not represented as sole support. Sole markers end before
the ankle roll. Source-frame uncertainty and every local constraint are recorded
in the generated metadata.

The pop uses the current curved deck frame for both soles. The front sole
slides to the nose through260, rolls onto an upper/outside shoe feature at the
heel-side nose corner261, then releases outward262. The rear sole follows the
moving tail while leaving sideways; a frozen board pose at259 no longer
controls its release. Compact sole/deck shape controls reuse the original
project geometry, including the nose kick and concavity. One
free-Air target curve lifts the rear foot outside the rising tail before returning
both feet to the compact source-led tuck. The Catch foot pin completes at273;
the hip impact envelope starts there and reaches its minimum after landing,
rather than shortening the legs before Catch. These are local reconstructions,
not measured hidden depth.

At board keys261/262 a paired pitch and carrier-local X/Z translation reduces
the early relative lag while retaining the previously keyed nose-rim point.
This raises the tail clear of the floor during shorter supported launches;
it does not add a second physical jump arc. The nose and upper-shoe contact,
the entire motion and the game transition still need visual review.

```sh
python scripts/author_skate_kickflip_hybrid.py --output /absolute/private/output
```

Python+NumPy and the existing bounded SKMCAP01 writer bake83 samples with
Reviewed=false. A valid pack is not a finished animation: compare with the best
previous captured viewer and source, then assess contact, anatomy, shocks and
native gameplay separately.
