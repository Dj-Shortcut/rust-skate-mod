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
- Runtime adds its actual physical upright carrier once. A short authored
  ground-press window subtracts its T=.6/g8 reference rise from board-local Y;
  runtime compensates the reference/actual rise difference during that window.
  This keeps tail pressure on the ground without adding a second jump arc.
- The minimum-charge press-fit fallback belongs to the ordinary
  `kickflip-source-keyed-v1` entry, including single Kickflips without a held
  extension. It retries only a failed press-foot fit during that entry with the
  same measured pelvis/reach/foot guards. Successful solves, foot targets and
  pop/entry/source clocks remain unchanged.
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
group around its current pelvis and reconstructs the fixed-length arm/head
segments. Hidden depth and these poses are authored source-led estimates.
Outside its C2 window the captured upper motion is retained exactly. Changed
foot targets use measured fixed-length chains. During258–278 a C2
blend keeps the knees in a continuous stance/body-facing bend plane, with a
small toe contribution. The flicking shoe does not turn the whole thigh;
outside that interval the captured plane is retained. There is no knee-angle cap or legacy runtime stack.
Upper-shoe contact is not represented as sole support. The conservative contact-intent markers remain unchanged; they do not assert
that the whole sole stays flat during the late slide/roll. Source-frame uncertainty and every local constraint are recorded
in the generated metadata.

The pop uses the current curved deck frame for both soles. Pop259 starts from the measured flat native Load frames: front footprint+.06,
rear footprint−.32. The front sole starts at the middle and
slides to the nose over259–260, rolls onto an upper/outside shoe feature at the
heel-side nose corner261, then releases outward262. The rear sole stays on the moving tail through the reference strike260.
Only afterwards does it rise along the tail normal into a vertical pickup;
it does not kick sideways to evade the board. Compact sole/deck shape controls reuse the original
project geometry, including the nose kick and concavity. One
free-Air target curve lifts the rear foot above the rotating tail before returning
both feet to the compact source-led tuck. The Catch foot pin completes at273;
the hip impact envelope starts there and reaches its minimum after landing,
rather than shortening the legs before Catch. These are local reconstructions,
not measured hidden depth.

A separate restrained arm arc258–278 replaces the rapid two-hands-up,
forward, back sequence. Bent elbows carry the near hand up to roughly shoulder
height at261, then down through the tuck/Catch. The far arm stays in a calm
lateral balance pose. Torso/head source-led keys are unchanged by this repair.
After the tail strike the rear pickup moves vertically into its compact path;
it does not revisit the old pitched-tail ankle target or add a backwards kick.

One source-led coupled envelope after261 replaces incompatible independent
Air-foot and captured-hip outputs. The free feet collect close above the real
board support envelope while it keeps its full winding. The hips collect into
the same tuck, Catch is already bent, then absorption continues into recovery.
Fixed chain lengths determine the knees; the stable body-facing bend plane
continues through rollaway instead of returning to ambiguous nearly straight
captured poles. Captured gestures remain the basis, and captured-body.json is
unchanged. This is an explicit authored reconstruction, not measured monocular
depth or a runtime correction stack.

The instep contact uses the original dark outer upper panel between shoe rows
+.04 and+.121. The former+.155 toe-side feature is only about5.6mm from the frozen white
band. Matching that black point alone did not ensure the whole upper side
led contact or keep nearby white faces clear. The patch is projected onto the real dark outer-side mesh near
[-.04220,-.04040,+.08993]. The real upper-side surface normal is aligned opposite the analytic curved
nose/grip normal; the anatomical toe direction is projected tangentially.
The roll starts during the late grip-slide after259.25, and after261 the
contact foot orientation is held during release. The nose
rim, mesh, weights and materials are unchanged.

The far arm around273 is occluded and blurred in the reference. Its restrained
balance reconstruction keeps the wrist below the head crown; the data does not
claim an observed overhead gesture.

The reference-ground press is authored over258–262. Compact support hulls of
the original project board mesh (184 whole-board,72 tail,104 wheel points)
replace private geometry dependencies. The baker uses only NumPy to solve a
44.1617196858° strike with0.005m wheel clearance, and computes support height
as `-min((support @ rotation.T).y)`. Reference flight isT=.6/g8 with
`f=clamp((sourceFrame-259)/16,0,1)` and `H=.5*g*T*T*f*(1-f)`. Board-local Y
blends to support height + post-strike lift −H; there is no extra world arc.
The authored board window is `[258,259,260.25,262]`; pitch remains0° at259,
then rises smoothly to the retained44° strike. It holds actual reference-ground
pressure from259.75 through260.25, with two explicit extra samples, before
post-strike lift begins. This17ms hold survives60Hz playback instead of relying
on one instantaneous source pose. The native reference-compensation window
and physics remain unchanged. The early authored X/Z
lag also joins from the same native flat-board origin. Exact259–262 samples
and two additional hold endpoints are retained in an85-frame pack so the Pop endpoint is not only an
interpolated approximation. Runtime adds
`(H-actualRise)*boardWeight`, with the adjoining front-foot weight
held through261/faded to262 and rear-foot weight held through260/faded to261.5.

Authored pressure moves the captured hip/upper group toward near-straight rear
leg extension at260, then restores its existing captured/contact basis by264.5.
Thigh/shin lengths remain0.44/0.43m; there is no reach clamp or marker relaxation.
The later compact tuck, upper gesture, Catch, landing and recovery remain the
previous basis. Pop/contact event markers retain their existing semantic clock;
they are not assertions of measured tail impact. The nose and upper-shoe
contact, whole motion, live charge entry and game transition still need visual
review. These source-led controls and hidden depth remain unreviewed.

```sh
python scripts/author_skate_kickflip_hybrid.py --output /absolute/private/output
```

Python+NumPy and the existing bounded SKMCAP01 writer bake85 samples with
Reviewed=false. A valid pack is not a finished animation: compare with the best
previous captured viewer and source, then assess contact, anatomy, shocks and
native gameplay separately.
