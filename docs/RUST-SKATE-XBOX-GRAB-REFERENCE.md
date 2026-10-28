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

Named reference grabs now keep the head above the neck in the common air basis
and turn its gaze toward descent, with bounded neck yaw. This runs after contact
and secondary solving, which otherwise rebuild the head from the rolled chest.
The native writer skips its generic head lift for these five grabs. Contact
endpoints, neck-to-head length and all other bone lengths stay unchanged. Existing
free-arm elevation and entry/held/release progress remain; no new timing model or
random movement is added.

The current private v3 review combines these profiles with the separate
[coupled air presentation change (#593)](https://github.com/Dj-Shortcut/rust-skate-mod/pull/593).
Rider and board use one common base before relative pitch, grab tweak and flips.
Its capsule display datum also removes the earlier steep support-display gap.
That source belongs to point 5 and is not duplicated in this PR.

Crail and FS Grab currently have full151-frame v3 clips at60Hz, including approach,
apex, transition landing and fakie roll. Context uses actual `MegaProfile` and
`SurfaceMesh`: sixty facets per wall, R approximately9.589m, flat12m and width12m.
A private continuous bottom-sphere sweep bounds displacement against the finite
mesh. Literal `Sense`/`RampContact` and the existing landing guard decide reentry;
there is no target-position snap, forced landing event or added inward steering.
This emulates collision outside Unity and does not prove native physics. The
popped-grab case correctly receives none of #593's free-air-only10cm tuck.

The v3 anatomy variant replaces only the original mitten hand surfaces with an
original palm, four three-segment fingers and an opposing two-segment thumb,
parented to the existing hand bones. Body vertices, weights and existing rest
bones remain unchanged; the frozen reference stays untouched. The private wrist
reach is recomputed from the actual rounded edge, concavity and nose kick using
a65mm authored wrist-to-MCP measurement. Fingers close/relax through the existing
grab weight. This is authored anatomy, not tracked fingers from the recording.

That prototype's new grip anchor and finger articulation are not yet native
runtime features. The current runtime preserves avatar finger animation. Measured
native MCP/reach calibration is the next separate API step; missing finger chains
must retain avatar animation and report the limitation. Full hand/contact quality,
grab-specific silhouette and Shadow gameplay remain open. This PR stays draft.
Some FS close-up angles are occluded by the knees; the complete medium route is
the primary review. Earlier body-only and outgoing deck-transfer cases are
preserved as superseded comparisons.

Validation: all69 client sources compile as C#7.3 against356 verified cached
reference assemblies with zero warnings/errors. Existing wrist restoration probes
cover entry, full weight, repeated frames, fade, release, other owners and reset.
The private v3 clips include the new post-solve head output and preserve the
reference body and actual13-part runtime board. Offline evidence does not execute
Unity native bindings or establish full trick acceptance.
