# Source-led normal Impossible (unreviewed)

Generate the original controls and bounded pack with:

```sh
python scripts/author_skate_impossible.py --output context/impossible-candidate
```

Requires NumPy. No video, detector binary, game file or private path is needed.
The original output always has `reviewed=false`; generating it does not approve
or install it.

The reference is Jamie Griffin's Impossible in
[The Berrics footage](https://www.youtube.com/watch?v=_oRoo1x1uzM), verified from
the original cached frames477–493, rather than the preceding Pressure Flip
label. Frame numbers refer to the existing29.970fps cache, not an independently
verified absolute YouTube timestamp. The video is reference only and is not
included or relicensed. Captured directions are reconstructed monocular
estimates, with original authored board, foot and body constraints.

`captured-body.json` preserves43 source-frame direction controls472–514. The
estimated source board trajectory and world root are deliberately discarded.
`keys.json` specifies the visibly delayed/accelerating full vertical wrap,
free-front-foot tuck, rear-foot curl and continuing gesture through rollaway.
Hidden depth and precise angles remain authored estimates. This is a separate
Impossible recipe, not a renamed free flip or a yaw animation.

The canonical Regular source starts from the actual common charged stance:
rear/right footprint at tail−.32m, front/left footprint near+.06m. Goofy uses
the reader's anatomical mirror. Rear ownership remains present through Pop
and Air; the front foot is entirely free. At Catch both feet return to the
grip. During the wrap, the rear ankle follows source-led body-relative X/Z controls
forward under the collected knees; it does not remain frozen behind the hips
at the starting tail position. Its true outside deck edge contact selects the
matching bevel side as the board inverts, keeping the leg out of the deck.
The hook follows the original frozen shoe's real toe-cap surface near
`[0,-.0642,.1629]` in ankle/flat-toe/sole-up coordinates. Its earlier guessed
feature lay outside the visible shoe and was rejected. Mesh, bind pose,
materials and weights are unchanged.

The board and feet subtract one T=.6s/g8 reference rise; playback adds the
actual upright physics carrier once. The held44.16° tail strike follows the
same original13-part board support geometry, rather than rotating around a
flat axle box. The runtime pressure correction retains the observed ground
carrier. A shorter physical jump can collect the hook foot slightly higher
with the board to keep the same complete vertical winding clear of its real
floor. That correction is bounded to.12m and uses the existing strict leg
solver; an unsupported jump or rig refuses playback instead of relaxing reach
or moving the player's physics body.

The special adapter accepts only exact ID `impossible-source-keyed-v1`, a
reviewed and digest-approved pack, and the normal rear-foot Impossible action:
Direction+1, no nollie/reversed/switch/fakie/extra rotation. It checks the six
ordered phases, continuous rear contact and one complete vertical winding.
Ordinary flip contracts are unchanged. New actions, input/player/stance or
geometry changes release ownership. Physical landing starts the recovery;
a short existing body blend returns to ordinary riding without extending the
source clock.

Offline exported Regular/Goofy meshes and the normal callsite are separate
from native Rust/Shadow acceptance. The preview uses the unchanged original
frozen rider and real13-part skateboard. For Impossible, the ordinary free-flip
checks that demand both feet be free or close to a flat deck are intentional
family exceptions; they must not erase its rear hook or high free-front-foot
knee. Native skin/contact, interruption and final visual approval remain
required before an installation pack is approved.
