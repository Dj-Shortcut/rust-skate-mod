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

The phase sheets show eight moments: Load, **Pop / first rise768**, Scoop, Air, Catch,
Land, Rollaway and the ordinary4-second endpoint. The main Pop button now
selects that genuine event at0.850s. The old rejected strike773.5 is retained
at the exact same1.017s normal-flight instant as **TailStrike**, separately
from Pop. Contact sheets show that instant, Scoop from two angles, Catch and
Land; the associated foot-detail checks remain in the private receipt.
Physical captions/default/dropdowns come from the actual animated GLB foot
bones; requested/native bindings are retained separately in the manifest.

The [Regular](arms-before-after-regular.jpg) and
[Goofy](arms-before-after-goofy.jpg) arm comparisons preserve the original
camera, strike/Scoop/Catch/Land times and original skinned model. Both arms
are explicitly reconstructed into bent forward/outward balance at or below
the shoulders through those moments. The prior overhead V and sustained
straight high hand are removed; this is an authored correction for
[Claude's A5 gate](https://github.com/Dj-Shortcut/rust-skate-mod/issues/716#issuecomment-6567110165).
Source762–768 blends into the new arm controls;797–808 settles back into the
original accepted rollout. Shoulders/head/torso/pelvis, feet/soles, board,
contacts and all source/event clocks remain unchanged.

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
[render/runtime freeze](https://github.com/Dj-Shortcut/rust-skate-mod/commit/05745de05687ec43e61a782bf075539288679c93)
uses the existing Ollie, Switch=true, TailLeads=false, flip0, shove+180,
spin0/body0 route. This repair changes no C# or gameplay code.
Four actual Pad→production Step/Land/scorer cases produce
`Switch Frontside Pop Shove-it`, with36/36 normal and34/34 low Air samples
using the capture. All four complete native traces are byte-identical to
the prior4f2 baseline. Each241-frame body replay differs only in the four
elbow/hand positions: all other motion/physics/contact fields are exact.

Four original-skin/full13-part GLBs and32 phase frames were checked at1×/¼×
through4-second ordinary recovery. All32 decoded-source phase visits and
24 Source/Side/Feet arm/contact details completed without browser errors.
The author inspected both stances/heights, arm-window transitions and
Catch/Land through rollout; no high-hand rebound was observed. Root also
inspected the same-time before/after originals. These are controlled
managed/graphical self-review results; Claude retains the independent
offline verdict and library status.

Raw pack SHA-256:
`4e0e2868db2aec3a037e1afcabcf8b3532d5be195c6e1cdf5c87c207ae55cde9`.
It stays `reviewed=false`. Native Rust/Shadow, multiplayer and installed
package acceptance remain separate and unverified. Source video/audio/pixels
and raw model arrays remain private; committed images show only the
original rig. The earlier root-entry comparisons above are historical;
the new arm comparisons and eight current sheets use the freeze linked here.

After composing Nollie balance#727 from main, integration `a4232184d4f52edc4c5cb983fe4e55ac617ea2ed` retains every C# byte from the rendered freeze. The current baker also reproduces the exact same raw4e0e pack. Existing actual-case/GLB/replay evidence therefore retains its explicit original freeze; no rerender or additional native claim is made.
