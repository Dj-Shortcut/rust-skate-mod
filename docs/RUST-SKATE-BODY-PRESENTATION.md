# Causal capture body height

`SkateCaptureBodyMotion` is an authored vertical presentation channel for the
normal Kickflip caller, after its flick and before final Catch/landing priors. One state
belongs to one presentation owner. Call `Begin` with the actual procedural
baseline and world-up before the first capture, then `TryStep` with render dt
through recovery. `Reset` on interruption, owner change, mount or teardown;
a new owner must begin again. Other tricks are not opted in by this helper.

The exact critically damped position/velocity step uses omega 40 rad/s
(about 0.12 s to 95% of a held target) and only the current target. It translates the body along world-up, preserves source
facing/lean and re-solves measured legs to the unchanged current ankle/toe
frames with `adjustPelvis=false`. Foot/sole, board, contact, physics and scoring
authority are unchanged. A zero-duration repeated query holds the analytic state without a made-up dt.
Finite render hitches use their real elapsed time; the analytic solver has no
100 ms cutoff. Exponential underflow takes the settled limit without overflowing
intermediate products. Negative/invalid dt or an impossible fixed-foot reach interval refuses and
clears the state.
Compilation and recorded-pose checks do not establish visible acceptance.

Ordinary ground charging gathers into a neutral crouch from 0.4 to 0.8 with the
existing spring. The trick family is still unknown here; pop input, physics,
charge duration and scorer timing are unchanged.

Before the unchanged leg solve, the predicted world-up height is projected onto
the analytic intersection of both current fixed-foot reach spheres, using the
existing `ReachShare` (0.998). Lateral hip-to-ankle distance stays unchanged.
If projection is required, its applied height becomes the filter state and the
discarded prediction velocity is cleared; unreachable lag cannot accumulate.
The feet/toes do not move, reach is not increased, and `TrySetFeet(false)` still
refuses any invalid bend plane or limb target. This body-height step belongs
before the existing Catch/landing knee priors so it cannot undo their flex.
