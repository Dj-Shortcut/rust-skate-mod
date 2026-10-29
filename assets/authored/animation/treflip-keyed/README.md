# Source-led 360 Flip / Tre (unreviewed)

This is a Tre-specific reconstruction of the white-shirt defense in The
Berrics video `_oRoo1x1uzM`, around253–255s. The source was checked through
Load, rear scoop, the visible underside during the combined turn, Catch and
rollaway. The filmed skater is Goofy. The compact retarget controls are
canonical Regular; Goofy playback uses the existing anatomical mirror.
Source footage, detector outputs and pixels remain private and are not
included or relicensed.

`author_skate_treflip.py` uses this clip's own captured-body controls and
combined board quaternion sequence. It does not relabel a Kickflip. The back
foot starts at the tail/toe-side scoop pocket and pulls the tail behind the
rider while the front foot gives a small upper-instep flick. Its airborne
scissor and raised arms follow this particular source, rather than borrowing
the Kickflip's balanced-arms/two-feet-tuck pose. Hidden depth, contact and root
placement are explicitly authored estimates.

The native exact signature is Ollie, normal stance/travel, Flip+360,
Shove−360, Spin0. The authored visual shove uses the existing source's
opposite coordinate sign. All rotations are preserved in the dense combined
quaternion path; the physical scorer remains authoritative.

## Shared physical conventions

Meters, +Y up, +Z nose, physical floor origin0 and resting deck top0.092m.
Import removes exactly one upright source-board translation from each joint;
it never inverse-rotates a body with the flipping/spinning board. Runtime
adds its physical carrier once. Own-board support hulls and actual own shoe
geometry provide the ground press and clearance. No game mesh, new skin or
exporter-only pose correction is used.

Flat Pop403 begins with the native grounded footframes. The real tail strike
holds around404 with a small rear-foot scoop, then the airborne source winding
continues. The events are Pop403, rear-release Air404, an explicit authored
press-fade-end Air406, Catch420, Land424 and recovery430. Repeated Air406 is a
presentation boundary, not an extra detected event. The shared exact-ID
selector derives this interval from the clip; no Kickflip16-frame constants
are imported into the Tre timing.

The same full reference flightT=.6/g8 is used only to cancel the source press
against the physical carrier. A bounded source-side floor clearance during
the combined turn accounts for the real board envelope. Catch is already
bent and impact absorption flows into a stable body-facing recovery plane.
Foot roles and strict fixed limb lengths remain intact.

```sh
python scripts/author_skate_treflip.py --output /absolute/private/output
```

Python+NumPy and the existing SKMCAP01 writer produce the original
Reviewed=false pack. The normal-route fixture uses a separate TEST-ONLY
review-bit copy and never installs it in the game. Exact-ID dispatch is a
separate shared change owned alongside the Heelflip selector; existing
Kickflip behavior remains separately guarded. Source builds, headless media,
whole-motion visual review and native Shadow approval are separate gates.
