# Held extra flips

A released pop flick requests one turn. On an ordinary flat pure-flip pop,
keeping the right stick on the same rim direction confirms one extra at each
0.10 seconds of observed air hold, at most two extras. The existing trigger,
direction, suspend and landing cancellation remain. A fresh air flick is still
a separate late-flip request. A spin/shove/body-varial already active or pending
before the first confirmation keeps the ordinary continuation route.

Only an accepted held extra adds 360 degrees and selects a finite flip rate
from the actual remaining degrees and the existing estimated landing budget,
less the flick delay and 0.03 seconds. The rate is latched until another genuine
confirmation or owner/flight reset and is capped at 4.5. Small upward numeric
rounding makes that budget conservative; neither angles nor landing deadlines
are rounded or forced. Invalid or insufficient budgets retain the existing
late/landing behavior.

Only flip motion and its measured scorer axis use this rate. Spin, shove, body
varial, board paths and finger actions keep their existing rate. The old scorer
overloads retain the 1..2 validation range. The explicit flip factor permits at
most 162 degrees per maximum 0.05-second step.

Landing still scores the rotation actually performed. An early flat 360 may
score a single even if more was requested; an invalid fractional rotation keeps
the existing bail rule. No intent alone awards a Double or Triple.

Controlled managed Pad/full-Step/landing/scorer replays cover both stances with
normal charge and the minimum real 30ms setup. That minimum yields 0.5352s from
the unchanged strength formula, rather than forcing the theoretical 0.528s
zero-charge limit. Short release and ordinary late input retain their previous
physical/scoring frames. Native Unity/Rust remains a separate acceptance gate.
Approved capture signature changes still refuse until a coherent same-pop,
monotone continuation and dedicated body/board data are integrated. This source
change does not reset capture progress or claim completed preview animation.
