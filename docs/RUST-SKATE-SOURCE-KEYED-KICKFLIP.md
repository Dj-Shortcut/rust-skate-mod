# Source-keyed Kickflip

The exact `kickflip-source-keyed-v1` clip has a carrier-local body, board,
sole frames and contact events. It is authored from reference images, rather
than measured 3D motion. The normal route still requires the bounded pack,
reviewed clip and matching local owner approval. Old Kickflip packs retain
their existing interpretation.

`SkateCaptureKeyedPlayback` samples the body and board together and transforms
both into one upright world carrier. Local zero is physical support; a flat
deck-top is at +0.092 m. Source ballistic motion is removed once during
authoring. Runtime adds actual player/support translation and removes the
ordinary visual `SkateBoard.Pop`, which was added along world-up. It adds no
second deck height, procedural board pitch or visual flip rotation.

The clock uniformly maps the actual launch span onto the clip's Pop-to-Land
interval. `FlatgroundFlightSeconds` freezes `2 * outgoing.y / gravity` from
the actual charged launch before its first-step gravity compensation. The
clip's own Catch event keeps its original relative timing. The selected
reference has a late catch with a blurred contact window; a 50% catch must
not be invented. Actual Ground and a new landing counter start recovery.
A longer flight holds before source Land.

The exact normal signature opts into this route before legacy inverse clocks,
flick/tuck adapters and torso/landing priors. Those layers do not run afterward.
The existing measured shoe fit and leg-reach refusals remain active. A changed
signature, stance, action owner or foot geometry falls back to ordinary riding.
The renderer uses the resulting captured board frame; input, player forces,
collision, native rotation accounting and scoring retain their authority.

Source authoring, pack validation, comparison on the rig, native installation
and Shadow approval are separate gates. The rejected v2 checkpoint is retained
in PR #611; this source path alone does not approve the new animation.
