# Flatground pop profile

An ordinary level-ground ollie or nollie now uses a lower carrier arc. The old
6 m/s launch with gravity 16 reached 1.125 m before the board's additional visual
knee lift (up to 0.22 m). Lowering only the preview would leave the game unchanged.

| Launch class | Full pop | Gravity | Return to equal height |
| --- | ---: | ---: | ---: |
| Ordinary level-ground pop | 1.35 m/s | 3.6 m/s² | 0.75 s |
| Existing transition/action launch | 6 m/s reference | 16 m/s² | Existing trajectory |

`SkateFlatgroundPop` supplies the shared flatground constants. The live grounded
input path selects that class only for an ollie/nollie on a nearly horizontal
surface, with no appreciable inherited vertical motion, ramp, held grab or
separate action owner. An Impossible requested through that ordinary pop path
uses the same carrier; its existing rear-foot wrap and rotation duration remain
owned by `SkateBoardPath`.

The launch freezes its gravity and presentation reference speed. The air step,
landing forecast and air-frame presentation use that same gravity. Board pitch,
visual knee lift and the front-foot slide normalize vertical velocity against
the selected reference speed. Flip, shove and body rotation rates are unchanged:
a full flip still has 0.5 s, followed by about 0.25 s before an equal-height landing.
Charge scales the impulse as before; an undercharged pop can still be too short
for a requested rotation.

Ramps, transition departures, grind exits, action launches and a grab held before
takeoff retain the existing profile. A grab started later inherits the flight
already in progress; it does not change gravity halfway through an arc. A late
body flip uses that same gravity in its collision-sweep forecast. Acquiring a wall
action hands control back to the wall action's existing gravity. The lower
flatground gravity is an authored game profile, not a measurement of real gravity.
It remains in force through that flight, including a fall to lower ground.

## Verification and remaining gates

- Complete client source compiles against the cached reference set.
- A temporary headless probe exercises the actual live launch and landing-forecast
  method bodies, actual shared launch/landing rules and the air-step gravity and
  clearance equations at 32 and 60 Hz. Full-charge ollie/nollie flights last about
  0.75 s and complete the existing flip before landing. Nine excluded launch
  cases retain the previous vertical velocity and gravity. This does not execute
  Unity or the current game's native movement code.
- An independent read of the four wheel meshes in each of the 22 frozen review
  GLBs predicts about 49–59 cm maximum wheel ground-clearance at 32 Hz and 48–58 cm
  at 60 Hz with this profile, including the existing visual lift, board orientation
  and first-step compensation. These are carrier-budget predictions from unchanged
  old poses, not measurements of newly exported animations.
- The inspection page's “wheel max” means the maximum over time of the *lowest*
  wheel bounding-box point. An individual upper wheel can be higher during a flip;
  the vertical rear-foot Impossible wrap needs its own wheel-height report.
- Re-exported full figures, contact sheets, the inspection-page measurements and
  the Shadow playtest remain separate visual/native gates. The 11 earlier clips
  are not newly approved by this source correction.
