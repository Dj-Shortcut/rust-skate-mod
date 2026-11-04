# Fakie Inward Heelflip: authored shared-mechanics derivative

Batch A7, [#736](https://github.com/Dj-Shortcut/rust-skate-mod/issues/736).
This is an explicitly **authored derivative**, not an independently filmed
Fakie Inward capture. It combines the reviewed Fakie Heelflip's shared Fakie
Kickflip body/entry/recovery with the reviewed normal Inward's board-only
backside half-shove and reversed-deck catch mechanics. Raw generated packs
remain `reviewed=false`; Claude owns the separate offline verdict and library
document. Native Rust/Shadow acceptance is a separate gate.

## Source boundary

The additional cached Berrics lead is `8r83UjKi-L0`, nominal140–166 seconds.
The bounded audit saw five landed gray-shirt attempts but did not independently
resolve the pure Fakie Inward signature, anatomical stance and heel contact.
The partial-cache full-video offset is nominal. The earlier190–245-second
rejection is not reopened. This is not a claim that the trick is absent from
the whole match or other footage. No candidate is relabelled as verified mocap.

`captured-body.json` reuses the project's original fixed-rig Fakie body controls
from the reviewed authored Fakie Heelflip, with explicit derivation metadata.
The adjoining reference, when shown, is Rodriguez's shared **Fakie Kickflip body
reference**, not proof of a heel flick or a Fakie Inward. Source pixels, detector
arrays, third-party model data and private approval fixtures stay out of git.

## Coordinates, winding and contacts

- Canonical Regular: nose+Z, anatomical Right foot on the tail beyond the bolts
  at−Z, anatomical Left heel guiding the toe-side nose corner. Fakie travels
  backward; its leading tail still pops. The travel vector never swaps feet.
- Native signature: Ollie, switch=true, tailLeads=true, flip−360°, shove−180°,
  body spin0°. Source board yaw is+180° while the body remains unspun.
- Contacted heel/tail targets follow the initial scoop, then release gradually.
  Free anatomical feet stay on their world sides instead of orbiting the board.
- Both catch positions **and toe headings** are transformed by the final
  reversed-deck rotation's transpose. A nose/tail exchange is not a foot swap.
- Compact pressure/air, reduced free-shoe clearance, asymmetric bent balance
  arms, bent catch/landing and a soft-knee upright recovery are authored choices.
  The existing fixed limb chains, shaped deck/shoe fitting, reach guards and
  one reference-carrier subtraction remain in place.

The baker pins the shared Fakie Heel controls/body/helper and normal Inward
controls/helper. It does not change those accepted sources, physics, scoring,
camera, F5/F9, placement, native feet or an approved installation pack.

## Rebuild and current gate

```sh
python scripts/author_skate_fakie_inward_heelflip.py \
  --output context/artifacts/fakie-inward-heelflip
```

Requires NumPy. JSON and SKMC are prepared before replacement; SKMC is replaced
last, and the committed source folder cannot be an output target.

Initial candidate: 402 sampled frames with raw reviewed=false. Four controlled
actual Pad/full 32 Hz Step/Land/scorer executions report **Fakie Inward Heelflip**;
their 60 Hz presentation uses a private family-adapter preview against current
main 14f1. The same family adapters are now integrated in this branch against
main 610b. Normal cases select this ID on 36/36 Air frames, low on 34/34, in both
requested stances; all end Ground/capture-off at 4 seconds. Controlled geometry
and engine bindings are not native play.

[Twelve rig-only phase/contact/recovery sheets](../../../../docs/review-media/a7-fakie-inward-heelflip/)
cover normal/low physical Regular/Goofy. The exported anatomical foot bones,
not the requested binding name, determine those physical labels. Phase
inspection sees the crouch, heel guide, board near the feet, bent catch/landing
and return to an upright soft-knee stance. Four original-skin/13-part-board
GLBs support 1×/¼× replay in the private viewer. The integrated client at
`c0ecdb714caafa50dd7e143e518718ab7a7a570c` compiled all 81 sources against
356 hash-checked current Shadow SDK references using C# 7.3. This checks the
actual client source, but does not replay native skinning or install a pack.
Complete continuous replay review, Claude's offline verdict and native testing
remain separate pending gates.
