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

Only these five profiles add a raised free-arm target and a measured wrist approach
to the selected board edge. Crail reaches toward the rounded nose end.
Existing contact entry/hold/release weights still own the pose. No input selection,
physics, scoring, catch or landing guard changes.

`SkateRider.TryGrabHandFrame` exposes an optional wrist frame for the selected
hand. `RiderRig` measures the actual avatar hand's finger direction and dorsal
basis from its middle/index/little proximal bones, including mirrored mapping.
It applies the frame through the existing grab contact weight, rebuilds from the
saved entry local rotation each frame, and restores it on release, another pose
owner, reset or unbind. Missing/degenerate finger bones disable the orientation refinement and retain
animated hands; riding does not fail.

For these same five profiles, the native writer now optionally articulates the
selected avatar's existing fingers. Bind caches all four three-bone finger chains
and the three native thumb bones, measures segment directions and lengths, and
requires unique, direct humanoid chains. Missing or ambiguous mappings keep the
avatar's finger animation and emit one binding diagnostic per affected hand.
The prototype's authored closure is adapted to those measured directions in the
same wrist basis. The thumb opposes the fingers. Humanoid mapping has no fingertip;
its final segment reach is an explicitly authored proportion, not a measured tip.
No hand mesh, bone, skin weight, body joint or gameplay state is added or changed.

Finger entry, hold and release consume the existing selected-hand contact weight
after the wrist writer. Each frame rebuilds from saved local rotations, without
accumulating rotation, allocations or bone searches. Release, another action owner,
reset, dismount and unbind restore the saved finger rotations. Actual humanoid
hand identity supplies the thumb side after the existing mirrored limb mapping;
the free hand remains avatar-driven. A finger-write failure disables only that
optional hand refinement and retains riding.

`SkateRiderRig.WithHands` adds optional measured wrist-to-middle-proximal lengths
and index-to-little knuckle spans without changing existing constructors. The
native binder supplies actual avatar measurements. `TryGrip` finds the rounded
deck edge with its authored concavity and kick, places the MCP outside the edge,
and derives the wrist through that measured length before solving torso reach.
The edge is not used as the wrist. Missing measurements use an explicit authored
65 mm reference reach; the binding log distinguishes this fallback. Both hand
axes use the same grip frame. This is not a calibrated native finger curl.

Named reference grabs now keep the head above the neck in the common air basis
and turn its gaze toward descent, with bounded neck yaw. This runs after contact
and secondary solving, which otherwise rebuild the head from the rolled chest.
For a procedural fakie transition return, actual velocity rather than the stored
nose direction supplies the gaze. A bounded blend owns both head tilt and yaw
through release and ground return. The native writer restores the independent
head after final easing/shaping and skips the competing generic head lift. Captured
poses, plants, walls, body flips and other action owners keep their own route. Contact
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

The native source now uses the same grip-anchor construction with measured avatar
MCP reach. The private anatomy mesh remains a reference, while the optional native
finger writer uses existing avatar bones only. Full hand/contact quality, grab-specific silhouette and
Shadow gameplay remain open. The v3 review identified upward-looking return heads
after release because the default gaze followed the nose while the rider rolled fakie. The new return
helper corrects that direction without moving other joints or the board. Knee
compression before contact, the visible prototype wrist seam, native hand contact
and Shadow gameplay remain open. This PR stays draft.
Some FS close-up angles are occluded by the knees; the complete medium route is
the primary review. Earlier body-only and outgoing deck-transfer cases are
preserved as superseded comparisons.

Validation: all69 client sources compile as C#7.3 against356 verified cached
reference assemblies with zero warnings/errors. Existing wrist restoration probes
cover entry, full weight, repeated frames, fade, release, other owners and reset.
The focused MCP probe checks old constructors, immutable measurements, invalid
input rejection, mirrored edge approaches, size-dependent wrist placement,
continuous weighted gaze and the actual post-easing head restoration slice. The two151-frame return
checks preserve every non-head joint exactly while looking down the return ramp.
The private v3 clips include the new post-solve head output and preserve the
reference body and actual13-part runtime board. Offline evidence does not execute
Unity native bindings or establish full trick acceptance.


Native finger-source validation: all69 client sources compile as C#7.3 against
the356 verified cached references. A private synthetic humanoid bone fixture runs
the actual extracted binder and writer for both anatomical hands, including entry,
hold, nonaccumulation, fade, release, another owner, reset and optional missing or
ambiguous chains. The warmed writer performs no measured managed allocations or
Animator bone lookups in that fixture. The real `RiderRig.Apply` callsite applies
fingers immediately after its existing selected-hand wrist writer. No permanent
scenario, new mesh, recording or preview asset is shipped.

The Xbox source windows and existing halfpipe approach/air/return comparisons
remain the visual references; they are not flat-ground grab demonstrations and
are not proof that Rust exposes these native finger bones. Actual avatar mapping,
visible finger closure, thumb/deck contact and Animator coexistence require the
Shadow test. This source is not an in-game or full-hand approval.
