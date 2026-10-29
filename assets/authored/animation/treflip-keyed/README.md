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
opposite coordinate sign. The free-turn winding is preserved in the dense combined
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
Foot roles and strict fixed limb lengths remain intact. Captured segment
and foot gestures retain the Tre's own execution. One authored local-height
curve removes monocular board-root drift; that same translation reaches the
body and free feet. The late X/Z catch segment regularizes the estimated
actor root from source416–420 instead of snapping only the body at419–420.
That root repair preserves the source408/412 gestures and pre-416 poses.
The separate front-chain reconstruction follows the noseward, nearly horizontal
front thigh visible in source408–416. During the free-foot window it raises
the front ankle coherently with that knee plane; it changes neither the
collected rear chain nor the source420 Catch and landing endpoints.

A small explicit rear depth reconstruction follows the estimated source-camera
ray. Only the original board surface underneath each shoe can require
clearance; a remote high nose cannot force both feet into a generic tuck.
These are authored occluded-depth/contact choices, not measured3D tracks or
angle caps. Native shared charge owns Load before trick selection. The source
Load controls remain reference material. The hidden head look is authored
toward the board. Air-only clearance returns tozero before Land. The compact
own-board surface contains original model geometry only, without textures or
third-party data. The Tre's source-camera estimate is separate from Kickflip.

The exact Tre runtime opt-in joins its source408 collected-Air pose at5/21
of the flight and uses the observed world-entry velocity. This retains the
other families' separate entry rules. Physics and the native scorer remain
authoritative; the viewer adds no jump, root or pose correction.

## Source-specific review

The [original source around253–255s](https://www.youtube.com/watch?v=_oRoo1x1uzM&t=253)
shows raised V-arms and an open front/collected rear scissor around source412.
Source420 (about254.01s) has a relatively extended late Catch, followed by
knee preparation before contact and impact absorption. The authored landing
profile follows that execution; it does not substitute an early Kickflip squat.

Keep generic diagnostic FAILs visible. A hand-above-head limit, both-soles
proximity throughout the middle of the flip, or a bent-Catch limit starting
before this clip's actual Catch can conflict with those visible source poses.
This is a source-specific review point, not permission to weaken the checks.

```sh
python scripts/author_skate_treflip.py --output /absolute/private/output
```

Python+NumPy and the existing SKMCAP01 writer produce the original
Reviewed=false pack. The normal-route fixture uses a separate TEST-ONLY
review-bit copy and never installs it in the game. Exact-ID dispatch is a
separate shared change owned alongside the Heelflip selector; existing
Kickflip behavior remains separately guarded. Source builds, headless media,
whole-motion visual review and native Shadow approval are separate gates.
