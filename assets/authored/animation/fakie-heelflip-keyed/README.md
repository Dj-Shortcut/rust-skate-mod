# Authored Fakie Heelflip from a shared Fakie body

This is an explicitly **authored derivation**, not an independently filmed
Fakie Heelflip. It reuses the reviewed Fakie Kickflip body reference from
Paul Rodriguez's landed backwards take in `PqJZcwTY1QE`, with original heel
contact, negative board roll, lower-body/contact and recovery controls. The
raw generated pack remains `reviewed=false`. Claude owns the separate offline
quality decision; native Rust and Shadow are separate gates.

## Source search before choosing the derivation

The [Swinburne attempt table](https://doi.org/10.25916/sut.31043779.v1)
contains 340 exact `Fakie Heelflip` attempts, of which 295 are marked `Land`.
It contains season and anonymous skateboarder IDs, but no video IDs or
seekable timestamps. Its CC BY-NC 4.0 table was used privately as an index and
is not redistributed or claimed as CC0 here.

The three already cached battles were matched by complete trick sequences:

| Cached video | Matching table rows, zero based | Plain Fakie Heelflip |
|---|---:|---|
| `PqJZcwTY1QE` | 21828–21914 | None in this battle |
| `YcmPDIj2BFo` | 23938–23983 | None in this battle |
| `_oRoo1x1uzM` | 23984–24052 | None in this battle |

The bounded search did not verify a seekable independent take. That does not
mean the dataset lacks the trick. The owner-authorized shared-body fallback
is used and labelled explicitly, as for the authored Varial.

## Coordinate and contact contract

Canonical Regular anatomy is preserved while travelling backwards: nose is
local +Z; the anatomical Right foot pops the tail at -Z and the Left heel
flicks the toe-side nose corner. The actual game route supplies backwards
travel. Turning the body/feet by the travel vector would exchange the roles.
The expected scorer signature is Ollie, switch=true, tailLeads=true, flip=-360,
shove=0 and body spin=0.

The front setup is straighter than a kickflip, with toes over the toe-side
edge. The heel feature is local `[0,-0.075,-0.07]`, carried against the declared
nose corner; it is distinct from a sole or side-instep contact. Both knees
collect above the board; both soles attach for catch, stay planted through a
bent landing and the continuous recovery. Head position is reconstructed from
an anatomical up axis and a bounded gaze, rather than treating gaze as the
neck-to-skull axis.

`shared-fakie-body.json` contains the project's original retargeted body
controls, not source pixels or raw third-party motion data. Monocular depth,
heel/board contact, pelvis height, fixed-chain knees and recovery are explicitly
authored. No calibrated contact or independent heel-source fidelity is claimed.

## Regenerate

From the repository root, with NumPy installed:

```sh
python scripts/author_skate_fakie_heelflip.py \
  --output context/private/fakie-heelflip
```

The converter pins the body checksum and exact Fakie signature, validates the
strict capture writer, and publishes its generated raw pack last. Generated
JSON, packs, footage and private diagnostic approval fixtures remain in ignored
`context/`; they are not production-approved assets.

## Offline review material

The complete managed input/replay uses a real mirrored Pad gesture and an
already established Fakie state. The scorer reports **Fakie Heelflip**, with
switch=true and tailLeads=true. All airborne frames select this exact source
and coupled board keys: 36/36 for normal and 34/34 for lower, in both stances.
The four-second runs include the observed landing and return to ordinary
riding; no source approval flag is changed in the delivered raw pack.

The original rig and complete 13-part board were exported and replayed at
1× and ¼ speed, with all four cases reaching 4.0 seconds without browser
errors. Physical labels come from the actual animated GLB anatomy, rather
than assuming the native requested label equals the frozen proxy label.

- [Normal Regular](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/normal-regular-phases.jpg)
- [Normal Goofy](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/normal-goofy-phases.jpg)
- [Lower Regular](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/low-regular-phases.jpg)
- [Lower Goofy](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/low-goofy-phases.jpg)
- [Review receipt](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/manifest.json)

Private viewer: `http://127.0.0.1:63761/a1-fakie-heelflip/`. It explicitly
labels the adjoining footage as the **shared Fakie Kickflip body reference**.
Original footage stays private. These are managed offline checks with controlled
bindings and geometry; no native Rust or Shadow play is claimed. Claude still
owns the independent visual verdict and library status.

The original-rig heel-exit close-ups are also retained:
[Regular](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/heel-flick-regular.jpg)
and [Goofy](../../../../docs/review-media/2026-11-01-a1-fakie-heelflip/heel-flick-goofy.jpg).
They show the authored heel guide, rather than claiming the shared kickflip
video proves a heel flick.

## Pop/Flick lead-arm balance

The canonical Left lead arm now opens lower and forward with a bent elbow
during Pop and the heel flick, instead of joining the other arm in a high V.
This short authored correction rejoins the shared body before source frame
2721. Backwards travel does not change which anatomical arm is adjusted.
The other arm, torso, feet, board, contacts, timing and accepted later poses
are unchanged. The pinned shared body remains intact; the derivation and raw
`reviewed=false` status remain explicit.
