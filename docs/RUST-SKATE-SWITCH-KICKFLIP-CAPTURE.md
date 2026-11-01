# Own Switch Kickflip source and review

The new source-keyed reconstruction uses Paul Rodriguez's independent landed
Switch Kickflip in [the existing Berrics reference](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=182),
frames 5470–5534 at 30000/1001 fps. It does not borrow the normal Kickflip's body.
Its baker and pinned body controls live in
`assets/authored/animation/switch-kickflip-keyed/` and
`scripts/author_skate_switch_kickflip.py`.

The load places the back foot on the tail beyond the bolts and the front foot
near the deck middle. Extension starts before the tail-pressure witness. The
front ankle banks so the side/upper instep meets the curved heel-side nose; the
sole-support bit clears before this side contact. The air uses the own source
fold, a consciously reconstructed hidden arm/knee and explicit free-foot pickup
to clear the rotating board. Catch and landing bend before the ordinary finish.

All camera-axis depth, root and contact edits are labelled authored. The original
65-frame identity selection has no actor gaps. The far left elbow/wrist and knee
remain mostly untrusted model estimates; their confidence is never increased.
The two earlier generic root-fit failures are retained privately. The shared
root-registration guard and gameplay physics were not relaxed.

## Validation and limits

- The unreviewed pack passes the actual C# reader and fixed-chain sampler for
  both stances at every source row and midpoint.
- Four controlled actual Pad/full 32 Hz Step/Land/scorer traces start from an
  established Switch state: signed body travel is reversed while the deck's nose
  physically leads. The source scorer reports **Switch Kickflip**, with one
  Ollie/+360-flip packet, zero shove, no held extra flip and no bail.
- The final shared body presentation is evaluated at 60 Hz for normal/low and
  both requested stances. Capture stays active through every Air frame and ends
  in ordinary Ground after a full four-second replay.
- Managed source replay and original rig export do not prove callable Unity
  bindings, avatar behavior, entering Switch through another trick, controller
  transport or native Shadow acceptance. Raw packs stay `reviewed=false`.

The review uses the existing original rider skin and the actual 13-part
skateboard/truck hierarchy. Physical stance labels are audited from exported
foot bones against deck forward, not assumed from a fixture's requested label.
Reference frames and complete source footage remain private. Only original
rig-only comparison images may be published for Claude's separate quality gate.
