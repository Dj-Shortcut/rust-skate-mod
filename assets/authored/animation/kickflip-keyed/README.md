# Original source-keyed Kickflip (unreviewed development data)

`keys.json` contains eleven original coupled rider/board poses authored against
the owner-selected Kickflip reference, plus one explicitly authored soft
rollaway key after the photographed interval. There are no source pixels, video,
detector output or earlier presentation-correction layers in this asset. The
monocular view constrains silhouettes; hidden depth, metric pitch, exact upper
shoe contact and Catch at source273 within the blurred272–274 interval are
authored choices, not measured motion. This is not a finished animation.

Use the actual reference frames, not the withdrawn early-Catch table: source
Pop259, upper-forefoot slide/release261–262, underside still visible266–269,
authored Catch273, Land275, early rollaway278. The source rate is30000/1001.
The reference filmed stance is uncertain. Canonical Regular and the anatomical
Goofy mirror are explicitly authored mappings, not a proven source stance.

## One coordinate and time contract

- Physical support anchor is `(0,0,0)`; +Y is up, +Z points at the nose and +X
  points toward Regular toes. All body points and the whole board share it.
- Flat-ground `board.origin.y` is exactly0.092m (deck top). It includes authored
  local pitch/lift, but no ballistic flight arc or horizontal player travel.
- Runtime adds the real physical carrier once: `Contact(player) - up * Pop`.
  Do not add a second DeckTop or subtract each sample's board centre.
- First source254 is t0; Pop259=0.1668333333s, authored Catch273=0.6339666667s,
  Land275=0.7007s. Uniform Pop→Land warping preserves that late-Catch fraction.
- One monotone cubic Hermite curve carries unwrapped full board-roll winding;
  dense quaternion samples preserve the revolution. Body targets use bounded
  monotone cubic interpolation and fixed-length anatomical chains.
- Toe endpoints include the existing0.07m physical sole drop exactly once.
- After real source278, sourceFrame/video mapping clamps at278 and
  authoredRecovery=true. A0.4s authored transition retains the same foot/bolt
  anchors while the torso returns gently toward ordinary soft-knee riding.

Bake with Python and NumPy into a private output directory:

```sh
python scripts/author_skate_kickflip.py --output /absolute/private/output
```

The existing SKMCAP01 writer validates83 dense samples, including canonical
reach at interpolation midpoints, and writes Reviewed=false. The JSON includes
sourceRootMotion/provenance and evaluated original keys for private comparison.
The private display adds exactly one separate physical carrier arc; that arc
is absent from the shipped control data. Native skin/contact, full movement,
interruption and final visual acceptance remain separate gates.
