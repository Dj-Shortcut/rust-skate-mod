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

The private moving check uses literal current native air/board methods, the shared
airborne/contact/secondary solver, the existing connected reference rider and
actual 13-part runtime board. Each of the five clips has 133 frames at 60 Hz.
Its supplied R3.5 transition and top-deck observations produce a perpendicular
outgoing deck-transfer, including surface approach, flight and top-deck landing.
This is not ordinary wall reentry or a native collision test. The authored Mega
asset differs from the constructed native Mega profile. Current departure tilt is
retained; this work does not resolve the separate halfpipe-air presentation issue.

The reference hand mesh has a palm and thumb, without independent finger bones or
curled fingers. Its close-up cannot establish a convincing complete grip. Native
avatar hands, normal halfpipe reentry, contact appearance and controller feel
remain open for Shadow. The source is therefore submitted as a draft.

Validation: all 69 client sources compile as C# 7.3 against 356 verified cached
reference assemblies with zero warnings/errors. A temporary probe of the actual
wrist writer covers parent-local entry, full weight, repeated frames, fade,
release, other-owner restoration, missing basis and reset/unbind. The five moving
GLBs preserve the reference skin and real board hierarchy; these offline checks
do not execute Unity native bindings or establish visual acceptance.
