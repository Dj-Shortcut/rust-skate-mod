# Genuine BS 360 Shove-it source-keyed route

`bs-360-shove-it` is an additional exact normal family, not a second turn added
to the BS180 source. It uses the existing landed Kroetkov white-shirt BATB12
take and its own body/foot observations. Source preparation and the previous
legacy prototype remain preserved privately.

| Contract | Value |
| --- | --- |
| New keyed ID | `bs-360-shove-it-source-keyed-v1` |
| Native signature | Ollie, normal, tail not leading; Flip 0 / Shove −360 / Spin 0 / Body 0 |
| Canonical board | Pure yaw +360; no longitudinal flip or body180 |
| Source window | Cache frames 78–125 from the preserved 45–125 take, 30000/1001 fps |
| Source phases | Pop 85, rear release 87–88, press fade ends 90, collected Air 93, first catch 100 / both 101, Land 103, Roll 112 |

The actual board completes its full shove with grip up while the body does not
turn180. The original footage is Goofy: anatomical Right is front, Left is rear.
Canonical sourceLeft is front after normalization. Companion preview anatomy is
separately checked against actual animated GLB foot bones; requested/native labels
are not evidence of physical handedness in the real game.

The source-led baker replaces the old prototype's 15-degree pressure and guessed
height with the current whole-board tail support and upright physical carrier.
It removes the previous board translation once, preserves this take's body
and limb directions, and fits the unchanged fixed-length legs to authored foot
targets. The rear free-foot pickup follows the visible asymmetric collection.
The source's retained image landmarks guide the rear balance arm vertically,
while its original depth direction and both measured arm lengths remain fixed.
Low-visibility arm frames 94 and 96 are authored interpolation between actual
anchors, with no confidence increase. The source's own slight Air pitch remains
after the pressure phase. The rear shoe follows the rotating tail until it releases. The front
lifts clear without a kick/heel flick. Full360 restores the original nose/tail
order at catch, unlike a half-turn's reversed deck-local foot pins.

The exact keyed adapter derives its pressure window from this clip's events and
joins the world-C1 body root at source93, with the separate short upper entry.
The actually observed launch derivative is subtracted before adding the live
carrier. A real flatground takeoff plane is required. The existing complete
13-part hull bounds a coupled body/board floor correction to 0.12m; missing plane,
non-finite data, held hands, excess correction or measured reach fails capture.
Physics, scoring, contact/reach tolerances and other keyed IDs are unchanged.
Current shared input-owned charge and its native owner handoff are used without
predicting the trick while grounded.

Executable authoring uses the existing SHA-pinned private canonical input:

```sh
python scripts/author_skate_bs360_shove.py \
  --input "$PRIVATE_BS360_SOURCE_JSON" \
  --raw "$PRIVATE_BS360_RAW_POSE_GZIP" \
  --output context/artifacts/bs360-source-keyed/output
```

Strict parsing/encoding precedes output mutation. Temporary files are replaced
atomically, preserving the previous reader-valid pack on encode/write failure.
No footage, raw detections, private poses, generated pack or approval is published.
Source and reconstructed candidates remain `Reviewed=false`; an isolated
TEST-ONLY-NOT-FOR-INSTALL copy exercises the reader/normal route. A chosen gamepack
must select this ID instead of the old Kroetkov legacy ID for the same movement.

Monocular shoe depth/yaw, pressure pitch and exact contact subframes are authored
estimates. The source wheel-contact bracket is102–104; actual game collision owns
landing. Dense interpolation is not additional detector evidence. Normal/low and
both stances require source/mesh and whole-motion review. These offline gates do
not prove installation, production interop, mount behavior or Shadow gameplay.
