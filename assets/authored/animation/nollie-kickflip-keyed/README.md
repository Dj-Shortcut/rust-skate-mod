# Own-source Nollie Kickflip

The own landed Paul Rodriguez take is `PqJZcwTY1QE`, original full-video
frames5678–5742 at30000/1001fps. It is filmed physical Goofy/right foot
leading and reconstructed in canonical Regular; the native sampler supplies
the anatomical counterpart. No other trick's body or motion is loaded.

`captured-body.json` contains65 compact fixed-rig reconstruction rows from
that take. It contains neither footage nor raw detector arrays. Source video,
raw-pose and retarget hashes remain explicit. Hidden arm/foot depth stays
estimated; `reviewed=false` is retained throughout.

`constraints.json` authors the source-led nose pressure, the anatomical rear
Right foot's **outer instep**, full board pitch/roll, compact free feet, coherent
pelvis, bent catch/landing and stable recovery. Geometry and generic math/sole
fitting helpers are reused; the Tre or normal Kickflip body is not reused.
Original board/shoe shapes and their hashes are unchanged.

The A1 head/lead-arm correction is explicitly authored. The compact source
head-to-neck vector points downward through Pop/Land; normalizing its length
alone puts the head into the chest. `upperBalance` replaces that axis with an
upright orthogonal head frame and a gaze toward the board, then the landing.
Rodriguez really holds a high diagonal balance arm in5720–5722, but its
monocular proxy reconstruction reads as a straight pole above the head. The
canonical Left arm is bent and settled through Catch/Land, then blended back
to the existing recovery; the stance reflection supplies the counterpart.
Pelvis, torso, shoulders, legs, feet, board and all contact/event clocks are
unchanged. This is a visible-quality reconstruction, not measured head/arm
depth or automatic approval.

The phase clock is explicit:

| Source frame | Stage | Foot contacts |
| --- | --- | --- |
|5702 | Leg-extension Pop begins | Both |
|5708 | Nose strike witness | Nose/Left plus rear/Right flick |
|5709 | Nose sole releases | Rear/Right instep |
|5710 | Rear instep released; pressure/flick stage finishing | None (`Pop`) |
|5712 | Free Air; actual native entry endpoint | None (`Air`) |
|5718 /5720 | Nose-foot / both-foot catch | Left / both |
|5722 | Landing witness | Both |
|5733 | Settled recovery | Both |

The5708 witness is an authored subframe/contact estimate, not a physics event.
During the instep roll, foot-contact flags do not claim a plantar sole contact.
The distinct first `Air` event at5712 is deliberate: the family parser uses it
as the pressure/entry endpoint after the popping sole's release. An earlier
free-foot contact fact at5710 must not silently change that endpoint.

The original source carrier/flight arc is removed once. The live physical
carrier is added once. Nose pressure uses the own complete shaped-board hull,
with the reference0.6s flight/gravity8 subtraction. Actual landing and scoring
remain authoritative. Native route integration is a separate focused change.

```bash
python3 scripts/author_skate_nollie_kickflip.py --output /absolute/private/output
```

Requires NumPy. The script validates its pinned inputs and canonical reader
contract before writing output. Repeating the same build is deterministic.

Current verification: the candidate passes canonical pack validation and the
controlled real-Pad/full32Hz Step/Land replay through the proposed native route:
normal/low × Regular/Goofy, with the source capture active for every Air frame
and ordinary Ground recovery by4s. This is managed source execution with
controlled geometry; it is neither a Rust client test nor visual approval.
The actual exported rigged GLBs were inspected against private own source frames
at Load/Pop/Air/Catch/Land for both physical stances and both launch heights:
visible nose pressure, compact free Air, bent catch/landing and upright stable
recovery. Rear-instep close-ups show the dark upper at the tail rim with the
sole band turned away. Rig-only review frames and source/route hashes are in
[`docs/review-media/2026-11-01-nollie-kickflip-source-keyed/`](../../../../docs/review-media/2026-11-01-nollie-kickflip-source-keyed/).
The frozen proxy's actual lead-foot audit is explicit: its anatomical bone-label
map reverses the requested stance, so viewer selectors use measured physical
stance, not the request label. This does not establish the Rust avatar mapping.
Whole-motion viewer review, the Claude quality gate and Shadow acceptance
remain open. Original/generated
source packs remain `reviewed=false`; private replay approval is test-only.
