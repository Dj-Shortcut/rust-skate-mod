# Grounded stick preparation

The rider keeps its measured ordinary charge setup while the controller gesture moves from Held into Flicking. The input module already keeps the held duration and popped end during that interval, although its public gameplay `Charging`/`Charge` output becomes false/zero. Previously this cleared the visible setup before a pop was actually emitted.

`SkateFlick.TryGroundPreparation` and the guarded `SkatePad` wrapper read only that existing Held/Flicking state. `RiderRig` uses it on the ground through the existing ordinary-footing/action exclusions. The successful measured foot/body pose remains owned by the same player, stance, input epoch and next-pop token. The capture observer remembers that displayed pose; it still selects no captured trick until the normal release/air route recognizes one.

The existing charge-entry handoff handles a real pop. Cancellation, input loss/reset, suspension, changed ownership and other action owners end preparation through the existing guards. The gameplay charging output, gesture state, pop composition, physics, scoring and source-animation data are unchanged. This preserves the current ordinary charge pose; it does not claim every filmed crouch has the same body proportions or lean.

Validation uses the real controller `Feed` path and `SkateFlick` state machine before Jumped, followed by the normal captured route on the frozen rider and shaped skateboard. A multi-frame Flicking interval must retain the measured preparation while gameplay Charging is false; a cancelled gesture must release it. Source compilation and publication checks remain separate from the Shadow playtest.
