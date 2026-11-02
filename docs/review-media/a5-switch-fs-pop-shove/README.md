# A5: Switch Frontside Pop Shove-it

[Open the viewer](http://127.0.0.1:63761/a5-switch-fs-pop-shove/).
The source execution is Goofy: Right guides the nose, Left pops the tail.
Goofy is shown beside that source; Regular is explicitly labelled as its
mirror. The original source pixels are never mirrored. The frozen original
skinned rider and complete13-part skateboard are used throughout.

| Flight | Physical Regular | Physical Goofy |
| --- | --- | --- |
| Normal | [Phases](switch-fs-pop-shove-normal-physical-regular.jpg) · [Contacts](switch-fs-pop-shove-normal-physical-regular-contacts.jpg) | [Phases](switch-fs-pop-shove-normal-physical-goofy.jpg) · [Contacts](switch-fs-pop-shove-normal-physical-goofy-contacts.jpg) |
| Low | [Phases](switch-fs-pop-shove-low-physical-regular.jpg) · [Contacts](switch-fs-pop-shove-low-physical-regular-contacts.jpg) | [Phases](switch-fs-pop-shove-low-physical-goofy.jpg) · [Contacts](switch-fs-pop-shove-low-physical-goofy-contacts.jpg) |

The phase sheets show Load, Pop, Scoop, Air, Catch, Land, Rollaway and the
ordinary4-second endpoint. Contact sheets add the side view, tail pressure,
release, reversed-end catch and planted rollout. Physical captions come from
the animated GLB foot bones against its initial nose; requested/native binding
is retained separately in the manifest.

This is the genuine black-shirt/cap attempt in
[The Berrics video](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=205),
exact attempt-table row1091. Own80 foreground VIDEO selections cover local
frames738–817; the next actor cut827 is excluded. Actual bounded PTS is
1001×frame at1/30000, FPS30000/1001. The original global frame/PTS is unknown;
the requested181-second cut offset only estimates public video seconds.

The own filmed-Goofy body is normalized once into canonicalRegular using its
initial nose and one X reflection/anatomical swap. Model pose774 incorrectly
reverses anatomy, so it is explicitly rejected and reconstructed only from
this take's own773/775. No other trick's body is borrowed. Camera pan/depth,
contacts/root, fixed-chain knees and cropped/hidden leading-arm depth remain
labelled authored reconstruction. Source baseRegular is inferred from the
Switch label, without a complete plain-normal witness.

SourcePop768 starts the actual first leg/body rise; strike773.5, front release
771.5, tail release775.5, later freeAir778, catch782/784 and Land787 remain
separate. The production presentation root entry is shortened to0.125flight
**only for this new Switch FS clip**, preserving independent tail-foot/board
paths and all reach/contact/floor guards. The source pelvis, feet and board
curves were retained; physics and scoring were unchanged. The same-camera
[Goofy](entry-before-after-goofy.jpg) and [Regular](entry-before-after-regular.jpg)
rig-only comparisons show the earlier rise and less perched front knee; both
source and native times are labelled.

The exact
[render/runtime freeze](https://github.com/Dj-Shortcut/rust-skate-mod/commit/4f2b7c2be24c7a1e734d15daca1e751805bd2fe4)
selects Ollie, Switch=true, TailLeads=false, flip0, shove+180, spin0 and body0.
Four actual Pad→production Step/Land/scorer cases produce
`Switch Frontside Pop Shove-it`, with36/36 normal and34/34 low Air samples
using the capture. Native trace bytes match the pre-entry-change baseline.
All four cases were exported and checked at1×/¼× through ordinary recovery;
28 decoded-source phase seeks and24 source774/side/feet views reported no
browser errors. The actual model and source comparisons were viewed in both
stances/heights, including the labelled mirror.

Raw pack SHA-256:
`3803675fdaf0fc0752e9cd01903838efc62c83099bf705c6b32c55283e3c1b4f`.
It remains `reviewed=false`. These are author self-review and controlled
managed checks; Claude owns the independent offline verdict and library
status. Native Rust/Shadow, multiplayer and installed-package acceptance are
separate and unverified here. Source video/audio/pixels and raw model arrays
remain private; committed images show only the original rig.

After main gained Nollie FS (#722), integration `2676f9c8` retained its index36 and appended Switch FS at37. All four complete body files and four native traces are byte-identical to the rendered `4f2b7c2b` freeze, including metadata. The original GLBs/replays stay explicitly pinned to4f2; no new render or broader runtime claim is made. The complete81-source client compile passes against356 cached references.
