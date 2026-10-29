# Causal capture body height

`SkateCaptureBodyMotion` is an authored vertical presentation channel for the
normal Kickflip caller, after its flick and existing body priors. One state
belongs to one presentation owner. Call `Begin` with the actual procedural
baseline and world-up before the first capture, then `TryStep` with render dt
through recovery. `Reset` on interruption, owner change, mount or teardown;
a new owner must begin again. Other tricks are not opted in by this helper.

The exact critically damped position/velocity step uses omega 40 rad/s
(about 0.12 s to 95% of a held target) and only the current target. It translates the body along world-up, preserves source
facing/lean and re-solves measured legs to the unchanged current ankle/toe
frames with `adjustPelvis=false`. Foot/sole, board, contact, physics and scoring
authority are unchanged. Invalid dt/state or unreachable fixed feet refuses
and clears the state; it never changes feet or silently fits the pelvis.
The slower 28 rad/s candidate was rejected because its landing lag exceeded
the unchanged fixed-foot leg reach in the recorded Kickflip.
Compilation and recorded-pose checks do not establish visible acceptance.
