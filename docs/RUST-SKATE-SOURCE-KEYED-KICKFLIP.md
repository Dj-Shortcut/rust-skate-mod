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

Entry retains the last observed riding pose in the upright carrier. Its root
position and velocity join the captured root, while the upper body joins over
the first short part of the physical flight. The current foot and board paths
remain fixed through that handoff. Neutral charging hinges toward the rider's
own facing direction in either stance; it does not require knowing the later
trick family or twist every charge along the board's nose axis.

Ordinary grounded charging now prepares the actual measured shoes before the
trick is selected: the rear footprint centres on the tail at -0.32 m and the
front footprint near the middle at +0.06 m. A known nose charge reverses those
ends. These are footprint centres, not guessed ankle positions. The early C2
setup reaches a deep, fixed-foot stance near a 75-degree internal knee angle,
with the ordinary bent arm shape, and holds before release. Foot preparation
settles first; the body finishes its crouch over about 0.2 seconds of the normal
0.3-second charge. The normal bone writer preserves
this coherent pose instead of adding ordinary Shape, foot refitting or HeadLift
again. Existing action/manual/grab/push exclusions and measured reach refusals
remain active.

The authored pressure stroke uses the real board's support envelope and a
0.6-second, gravity-8 reference carrier. It starts from the charged shoe frames,
strikes the tail, then releases into the flick and tuck. The runtime compensates
only the difference between reference rise and actual carrier rise during this
short stroke, including the attached-foot weights. Player motion is still added
once; the animation does not provide a second ballistic arc. Foot and arm
placements reconstructed from occluded reference views are labelled authored.

The outside-instep contact aligns the actual shoe surface normal with the
opposing curved nose surface, starting the ankle roll during the grip slide.
One authored local envelope couples Air feet and hip height around the unchanged
rotating board, then joins a bent Catch and impact absorption. Recovery keeps
stable body-facing knee planes while the measured feet remain planted. Natural
clip completion uses the existing 0.15-second body departure into ordinary riding,
with the floor-versus-deck carrier offset preserved.

Source authoring, pack validation, comparison on the rig, native installation
and Shadow approval are separate gates. The rejected v2 checkpoint is retained
in PR #611; this source path alone does not approve the new animation.
