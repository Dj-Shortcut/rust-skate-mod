# Five Xbox grab reference profiles

Development source for owner point 6 of #337. These profiles are ready for review;
full hand grip and native gameplay are not approved.

The owner's existing Xbox recording supplies visible airborne silhouettes and
reach/hold/release windows. The source SHA-256 is
`a30d5bbe471b188f760c0eef4dd6de510c7bd1c4d9d37b634f9aea25a922caf8`.
The recording, extracted frames, private preview meshes and capture data stay
outside GitHub. No footage was downloaded again.

| Profile | Original frame window | Visible reference used |
| --- | --- | --- |
| FS Grab | 4970–5154 | Compact board-side reach, opposite arm free |
| Method | 9449–9618 | Board drawn behind/beside the tuck, free arm raised |
| Crossbone | 6320–6466 | One leg extends across/out of the other, free arm raised |
| Weddle | 10016–10154 | Compact tuck, opposite arm outward |
| Crail | 8518–8667 | Cross-body board-end approach, opposite arm above the torso |

The selected windows include approach, air entry, hold, release and reentry.
Grip side, exact anatomical stance and individual fingers are partly occluded.
The catalogue keeps canonical trick semantics. The numerical targets are authored
reconstructions from visible evidence, not measured mocap or tracked fingers.

Only these five profiles add a raised free-arm target and a wrist approach outside
and above the selected board edge. Crail reaches toward the rounded nose end.
Existing contact entry/hold/release weights still own the pose. No input selection,
physics, scoring, catch or landing guard changes.

`SkateRider.TryGrabHandFrame` exposes an optional wrist frame for the selected
hand. `RiderRig` measures the actual avatar hand's finger direction and dorsal
basis from its middle/index/little proximal bones, including mirrored mapping.
It applies the frame through the existing grab contact weight, rebuilds from the
saved entry local rotation each frame, and restores it on release, another pose
owner, reset or unbind. Missing/degenerate finger bones disable this refinement
and retain animated hands; riding does not fail. Finger articulation is unchanged.

The updated private moving review combines these grab profiles with the separate
[point 5 body/board presentation change (#593)](https://github.com/Dj-Shortcut/rust-skate-mod/pull/593).
`RiderViewNormal` supplies the body basis;
`ViewNormal` still supplies the independent board/support basis. That change is
owned by point 5 and is not duplicated in this grab PR. The grab writer still uses
the actual `DeckFrame` for feet and hand approach.

All five revised clips have 151 frames at 60 Hz. The context comes from the actual
current `SkateParks.MegaProfile` and `SurfaceMesh`: 60 collider facets per wall,
R approximately 9.589 m, a 12 m flat and 12 m width. The native five metal carrier
chords are excluded backing, and the published R3.5 authored asset is different.
The earlier outgoing top-deck transfer remains a labelled comparison and is
superseded as the return review.

The controlled route leaves the last 89.25-degree face with the existing 3 m/s
surface velocity and 6 m/s pop. Literal `Sense`/`RampContact` query the finite mesh;
the existing landing guard meets that same transition and the projected velocity
rolls back fakie. A private continuous bottom-sphere sweep bounds displacement
against those actual mesh faces and edges. It uses computed contact times and
normals, without a target-position snap, forced landing event or added inward
steering. It emulates collision outside Unity and does not prove native physics.
The medium three-quarter moving camera follows departure, apex, return and roll.

The combined review makes the body basis world-up at the apex while retaining the
board's own steep pitch. Landing also exposes an unresolved support-display gap:
`SkateBoard.Contact` still follows the player root without compensating the steep
capsule rest offset; the board can visibly float away from the transition. This
is not hidden by relocating the preview. Native reentry and visible contact
remain open. Grab-specific chest folding also needs visual review; an upright
body basis does not approve the complete grab silhouette.

The reference hand mesh has a palm and thumb, without independent finger bones or
curled fingers. Its close-up cannot establish a convincing complete grip. Native
avatar hands, normal halfpipe reentry, contact appearance and controller feel
remain open for Shadow. The source is therefore submitted as a draft.

Validation: all 69 client sources compile as C# 7.3 against 356 verified cached
reference assemblies with zero warnings/errors. A temporary probe of the actual
wrist writer covers parent-local entry, full weight, repeated frames, fade,
release, other-owner restoration, missing basis and reset/unbind. The five revised moving
GLBs preserve the reference skin and real board hierarchy; these offline checks
do not execute Unity native bindings or establish visual acceptance.
