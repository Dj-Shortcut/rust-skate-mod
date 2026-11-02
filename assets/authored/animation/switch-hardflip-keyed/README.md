# Switch Hardflip — own Rodriguez source

Current review candidate: raw
`85aa60e815f4ff2dfb70b0c58354146e3266072497d30f27461d6052791abb4e`,
`reviewed=false`. Original-mesh runtime freeze:
`13d34285290bdc55b2e3ee4144a8dc976132d1eb`.

Viewer: http://127.0.0.1:63761/a6-switch-hardflip-landing-repair/

Review images: `docs/review-media/a6-switch-hardflip-landing-repair/`.
Eight current phase/contact sheets and four same-time original-to-repaired
Catch/Land comparisons contain only the original rider and shaped skateboard;
reference video pixels remain private.

```sh
python scripts/author_skate_switch_hardflip.py --output <private-output-directory>
```

The inputs bind Rodriguez's genuine landed Switch Hardflip in
[PqJZcwTY1QE](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=260), full-cache
frames 7804–7932, to this take's own VIDEO extraction. Filmed physical Regular
has anatomical Left nose/front outer-upper instep flick and Right trailing
tail/scoop. Initial-nose normalization applies no anatomical reflection.
Physical Goofy is a labelled mirror; original reference pixels stay unmirrored.
The 129-frame extraction accepted 124 selections: missing 7882/7895 and rejected
complete-side reversals 7830/7853/7854 are reconstructed only from accepted
neighbours in the same take. Camera/depth/root/contact/board and clipped wrist
depth are authored estimates, not calibrated or exact 3D capture. No other
trick's body or motion is borrowed. Original rig/deck/shoe and interpolation
mathematics is reused read-only.

First rise is 7846; separate tail strike is 7854.3. Front sole release 7852.75
precedes the outer-upper instep release 7854.15; Right tail release 7855.3
precedes distinct free Air 7857.5. Full +360 roll/−180 canonical yaw has no body
180. Catch 7867.5/7868.5 swaps deck ends while preserving world/anatomical feet
and toe headings. The tail-ball rest unwinds toward actual world-up before the
snap; the normal Hardflip's timed restore and extra client tuck are not used.
The independent world pelvis/pickup curves remove the reference carrier once;
actual controlled physics supplies travel, flight and landing.

The [A6 review](https://github.com/Dj-Shortcut/rust-skate-mod/issues/725#issuecomment-6575418317)
accepted Load/Pop/Flick/Air but rejected tall Catch/Land and fists tucked under
the chin/against the belly. The authored 7864.5–7890 repair brings absorption
into touchdown relative to the descending board and opens asymmetric bent
arms low/outward. It continues into the same take's existing absorption and
smoothly rejoins recovery. The first repair improved Catch but still stood up
at Land before dipping later; its same-camera evidence is preserved privately.
The final targets remove that visible bob rather than relying on a world-height
minimum alone. Both pelvis curves remain world heights before the existing
single carrier subtraction. The immutable body
`06b1bc7a1659a902f3b2f2ef2e9eb61c1a79af2873e6a6f11d2ae265e05113a0`,
all shoes/toes/soles, board, directions and event/contact clocks are unchanged.
All source samples outside 7864.5–7890 remain exact. Existing reach, floor,
source-pressure, skin/inverse-bind and fixed-bone guards are retained.

Four controlled managed Pad/full 32Hz Step/Land/scorer/body cases pass through
4s: normal/low keep 36/34 coupled Air samples and end on ordinary Ground with
capture off. Managed physics/scoring traces are byte-identical to the original
under controlled bindings/ground; this is not native-gameplay evidence. Root
verified all 85 product C# files unchanged from the already production-SDK-
compiled main. Four original-skin/13-part-board GLBs were audited from actual
animated foot bones: physical Regular is the direct default and Goofy the
labelled mirror, independently of requested enum/file names. The eight current
sheets and four before/after witnesses were visually inspected.

All four variants completed full 1× and ¼× playback through 4 seconds, with
36 decoded source phase visits and 48 Side/Feet/impact visits, zero browser
errors. An independent browser worker reviewed 80 staged impact/recovery
frames: bent knees continue through touchdown into one absorption and upright
rollaway, without a renewed stand/bob, leg swing or chin/belly fist clamp.
Caught shoes remain held. The review manifest pins the private run and bounded
visual judgment. Claude's independent offline verdict and native Shadow
gameplay remain open; raw approval has not been promoted.
