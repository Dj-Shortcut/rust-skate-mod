# Switch BS Pop Shove-it — original rig review candidate

[Viewer and source comparison](http://127.0.0.1:63761/a4-switch-bs-pop-shove/)
for `switch-bs-pop-shove-it-source-keyed-v1`.

The body comes from its own white-shirt Switch take in
[BATB13 Jamie Griffin vs Zach Doelling](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=202),
bounded frames 657–736 of the existing 181–219 s excerpt. This is a grip-up
board-only 180, with a tail-ball scoop, lifting front foot, no flip or body turn,
swapped-deck-end catch and upright recovery. The body is not borrowed from
another trick. Only original generic board/shoe geometry and fixed-chain math
are reused.

The source is manually read as Regular-base/Switch-Goofy, Right leading and
Left popping the tail; the bounded movie lacks a complete normal stance
witness. One anatomical reflection normalizes it to canonical Regular.
Sparse camera yaw, monocular depth, root, feet/contact timing, weak crouch
knee planes and the occluded leading-arm suffix are explicitly reconstructed.
The original pixels remain private and unchanged; no calibrated 3D claim.

The phase sheets contain the same original skin and complete 13-part board
used in the viewer, including both physical stances and normal/low profiles.
Actual animated GLB foot bones establish physical stance separately from the
requested native binding: requested Goofy is physically Regular on this frozen
proxy, and requested Regular is physically Goofy. This is a measured preview
annotation, not a global game-handedness assumption. The one source-facing
camera estimate stays fixed across phases; the source/native pose and heading
are unchanged.

Four controlled actual Pad/full 32 Hz Step/Land/scorer cases select the exact
Switch Pop Shove-it: Ollie, switch=true, tailLeads=false, Flip 0, Shove −180,
Spin/Body 0. The right stick charges toward the tail, settles on the correct
pure side for 0.10 s, then returns neutral; the left stick stays neutral.
All Air samples use the keyed route, followed by ordinary grounded recovery
to 4 s. The existing support, floor, clearance and reach guards stay strict.

The author compared the own source and original mesh at Load, Pop, Air,
catch, landing and rollaway in all four cases. Both 1× and ¼× viewer playback
reach ordinary 4-second recovery; all 28 phase seeks decode actual source
frames. Twenty focused feet/side captures include the released scoop,
swapped-end catch and stable rollaway. No browser/runtime errors occurred.
The eight sheets below contain four whole-phase grids and four feet/side
grids; no source pixels are published.

Raw pack SHA-256:
`87046067b5df79745638db41ad94f43e7997a1dc7de3393e4fdc2a2b74b649ca`.
Runtime/source checkpoint:
[`8c2ca81f98ea61a2e84426b931344d5b6bcec640`](https://github.com/Dj-Shortcut/rust-skate-mod/commit/8c2ca81f98ea61a2e84426b931344d5b6bcec640).

Raw `reviewed=false` remains intact. Private controlled fixture approval,
source presence, offline visual judgement and native Shadow acceptance are
separate gates. These public JPEGs contain only the original rig; source
pixels, detector/model outputs, private test approvals and generated packs
stay outside the repository. Claude retains the library/offline verdict;
native Shadow remains pending.
