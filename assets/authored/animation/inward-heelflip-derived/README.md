# Inward Heelflip: explicit shared-Heel derivation

Batch A1, [#684](https://github.com/Dj-Shortcut/rust-skate-mod/issues/684).
This is an authored normal Inward Heelflip, using the existing normal
Heelflip body/timing plus an authored backside half-shove. It is **not a filmed
Inward capture**. The raw output stays `reviewed=false`; source presence,
Claude's offline judgement and Shadow acceptance are separate states.

## Source search before deriving

The official [Swinburne attempt annotations](https://doi.org/10.25916/sut.31043779.v1)
were read first: 24,053 rows, including **113 landed exact normal Inward
Heelflips**, 14 in BATB12. These exist; the table supplies anonymous skater IDs,
season, trick, variant and outcome, but no video IDs or timestamps.

The three already cached games were matched by their ordered attempt sequences.
Ranges below are zero-based data rows, excluding the CSV header.

| Cached video | Matched annotation rows | Distinguishing sequence | Normal Inward take |
| --- | --- | --- | --- |
| `_oRoo1x1uzM`, Griffin/Peterson | 23984–24052 | Kickflip → Heel → BS Bigspin → Varial Heel → FS Bigspin → Pressure → Hardflip → Impossible → Double Heel → Tre | None in this matched game |
| `PqJZcwTY1QE`, Huston/Rodriguez | 21828–21914 | Switch Heel → Nollie Heel → Nollie BS180 Heel → Nollie Tre → Switch Kick → Nollie Kick | Its Inward labels are Nollie/Switch, not the normal slot |
| `YcmPDIj2BFo`, Cole/Kroetkov | 23938–23983 | BS360 → Cole Kickflip bail → Kroetkov Pop Shuvit → FS Shuvit → Kickflip → 360 Shuvit → Tre | None in this matched game |

No seekable normal take was verified in those cached games. The owner explicitly
allows this Heel + BS180 derivation after that bounded scan. This is not a claim
that no normal Inward footage exists elsewhere. The annotations are a search
index, not motion or footage; no Berrics footage or source frames are committed.

## Motion and contacts

`constraints.json` pins the existing normal Heel body, Heel controls and geometry
helper by SHA-256. The baker reuses their original shaped-deck and shoe fitting;
there is no straight board proxy or renderer pose correction.

- The rear foot loads the tail beyond the bolts. The front foot makes the
  Heel's heel-led toe-side exit, not a Kickflip sole/instep flick.
- The contacted feet follow the initial backside scoop. After each release,
  the anatomical feet remain on their world sides while the board completes
  **−360° flip and +180° canonical yaw**. The native signature is normal Ollie,
  **−360° flip, −180° shove, zero body spin**.
- The catch transforms both foot coordinates **and toe heading** by the final
  deck rotation's transpose. A reversed deck does not swap the rider's feet or
  turn the body through 180°.
- Bent, asymmetric arms are an authored Inward balance prior; shared Heel
  footage is not presented as an Inward arm capture. The head/gaze and ordinary
  recovery follow the shared Heel source reconstruction.
- The body stays compact through the pressure endpoint. This prevents the
  normal route's entry interpolation from raising the pelvis toward a future
  free-Air pose while the rear foot is still held on the tail. Measured limb
  limits, floor guards and physics remain unchanged.
- Reference carrier height is removed once. Actual flight, landing, scoring
  and ordinary recovery remain the game's responsibility.

| Event | Shared source frame |
| --- | ---: |
| Window start | 333 |
| Pop / load departure | 348 |
| Front sole releases | 349.5 |
| Tail strike | 350 |
| Heel guide releases | 350.5 |
| Rear sole releases | 350.75 |
| Distinct free-Air endpoint | 352.75 |
| Reversed-deck catch | 362 |
| Land | 366 |
| Recovery | 372 |
| Window end | 385 |

The shared clock is 30000/1001 fps. Free-Air is a separate event even after the
sole contact bits have cleared: it defines the pressure-release endpoint used
by the normal presentation route.

## Rebuild and review boundary

```sh
python scripts/author_skate_inward_heelflip.py \
  --output context/artifacts/inward-heelflip-derived
```

Requires NumPy. It writes `inward-heelflip-derived-heel-v1.json` and
`skate_captures.skmc`. Both are prepared and validated before replacing output;
the pack is replaced last. Committed source folders cannot be output targets.
The existing pack validator checks sampled endpoints and interval midpoints.

The controlled managed replay exercises real Pad composition, the complete
32 Hz riding Step and Land/scorer, then the current 60 Hz body presentation on
a 40 m flat. Normal/low × regular/goofy all select this exact ID, score Inward
Heelflip, land, and return to uncaptured ordinary Ground through four seconds.
Normal captures all 36 Air frames; low captures all 34. Approval is confined to
a private `TEST-ONLY-NOT-FOR-INSTALL` copy; the original raw pack is unchanged
and unreviewed. The native engine, runtime bindings and Shadow remain untested.

Mesh sheets and 1×/¼× replay are the visual gate; passing the exporter or managed
replay alone does not approve the animation. Claude owns the library's offline
judgement. Source footage, packs, detector output and model binaries stay private.

The [rig-only phase and contact sheets](../../../../docs/review-media/a1-inward-heelflip-derived/README.md)
show both physical stances at normal/low height, including ordinary rollaway.
