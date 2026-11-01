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
