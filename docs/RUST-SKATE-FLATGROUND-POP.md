# Flatground pop profile

Ordinary level-ground ollies and nollies use a lower physical arc and a smaller
generic board lift. The previous 6 m/s carrier with gravity 16 reached 1.125 m
before adding up to 0.22 m of visual knee lift.

| Parameter | Ordinary flatground pop | Existing transition/action launch |
| --- | ---: | ---: |
| Full physical pop | 2.4 m/s | 6 m/s reference |
| Gravity | 8 m/s² | 16 m/s² |
| Equal-height flight | 0.6 s | Existing trajectory |
| Generic visual board lift | Up to 0.08 m | Up to 0.22 m |
| Flip/shove rate | 900°/s | 720°/s |
| Body-spin rate | 450°/s | 360°/s |
| Rear Impossible wrap | 0.4 s | 0.5 s |

`SkateFlatgroundPop` supplies the profile to the live grounded input path and
presentation fixtures. The selected launch class is frozen for its flight. Only
an ordinary ollie/nollie on nearly horizontal ground, without appreciable
inherited vertical motion or a separate action request, selects it. Ramp, edge,
grind-exit, planted/oldschool, explicit ollie-style and grab-held launches retain
the existing profile. An Impossible requested through the ordinary flatground
pop path uses its existing geometry with the shorter wrap clock.

Air integration, the landing forecast, air-frame presentation and a late body
flip's collision forecast share the flight gravity. Board pitch, generic lift
and foot slide normalize against its launch reference speed. Reducing the
flatground visual lift avoids adding the old 0.22 m again on top of the selected
physical arc; it does not scale the mesh or rewrite a captured source pose.

Rotation uses an explicit 1.25 rate factor, not a larger physics step. Remaining
rotation checks, flip/shove/body increments, spin completion, native wrap
progress and scoring share that factor. `SkateTricks.TryStep` keeps its existing
API/default rate; its bounded overload accepts the explicit flight rate and
checks native path progression against it. An independent finger-flip action
still supplies its own actual angular delta, which is counted once.

A grab started later inherits the current flight, including its smaller generic
lift; the separate grab-rise contribution is unchanged. A new wall action
returns control to the wall profile. The profile also remains in force when a
flatground pop falls to lower ground. Charge scales the impulse as before; a
weak or genuinely late request can still run out of air.

## Verification and remaining gates

The complete client source compiles against the cached reference set. Temporary
headless probes exercise the actual live launch/forecast bodies, actual shared
scorer, native path sampling and shared landing rules with real 32/60 Hz steps.
They verify completed flip/shove/spin/wrap motion before the clearance landing,
unchanged excluded launch classes, default scorer parity and rejection of invalid
rate inputs. Host stubs do not prove Unity or current-game execution.

The required visual check is fresh full-figure playback at 1× beside the source,
with a fixed-height camera and visible ground. Re-exported clips must be checked
with the unchanged inspection page: “wheel max” is the maximum over time of the
lowest wheel bounding-box point, targeted at 40–60 cm. Individual upper wheels
and the rear-foot Impossible geometry require their own interpretation.

The earlier 11 clips are not newly approved by this source correction. Fresh
combined contact/height exports, owner visual review and the Shadow playtest
remain separate gates; passing compilation or a carrier-budget prediction does
not replace them.

## Longitudinal flick release

An ordinary flatground launch with an initial longitudinal flip keeps the
vertical pop and input immediate, then waits an authored 0.0625 seconds for
the grip slide and instep/heel edge release before turning longitudinally.
`SkateFlipFlick.EdgeReleaseSeconds` is the shared phase boundary. It is a
presentation and gameplay timing choice, not a measured source-contact time.

`SkateRide.FlipFlickKnown` identifies that initial flight;
`FlipFlickElapsed` is its real elapsed flight time and `FlipFlickActive` means
the edge release is still pending. The foot presentation can use that same
clock without predicting a family during the neutral charge. This source
change supplies the timing contract; it does not itself author the foot slide
or prove an instep/heel contact.

A physics step crossing the boundary spends only its post-release fraction
on the real scorer's flip axis. Collision, gravity, shove and body dt remain
unchanged; requested flip degrees and capture signature remain complete.
The remaining-time warning accounts for the unspent release interval. Late
or held extra rotations do not start another interval. Pure shoves, special
board paths, ramps and action-owned rotations keep their existing clocks.
Land, bail, mount, a new take-off or an acquired action owner clears the gate.

This is a draft source checkpoint. Compilation and headless scorer/callsite
checks are separate from the coupled foot/board visual review and Shadow test.
