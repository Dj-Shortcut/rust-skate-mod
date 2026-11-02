# A5 Fakie FS Pop Shove-it — bent balance and pressure reserve

Viewer: [current source/rig comparison](http://127.0.0.1:63761/a5-fakie-fs-balance/). Frozen source/runtime: [98757ed135751383c5ec0aec82d339d83c0cb84e](https://github.com/Dj-Shortcut/rust-skate-mod/commit/98757ed135751383c5ec0aec82d339d83c0cb84e).

This repairs the [A5 rejection](https://github.com/Dj-Shortcut/rust-skate-mod/issues/716#issuecomment-6567110165): the Pop/Scoop V becomes bent shoulder-height balance, and one short height envelope keeps knee reserve through pressure. The last over-tall part of Rise necessarily changes with it, retaining upward extension instead of adding a second hip dip. This is an explicit authored choice; the real take briefly raises its arms diagonally. Feet, board, event clocks, source anatomy, guards and all C# remain unchanged. Lower body rejoins exactly at1118; the whole mainAir1122+, Catch, Land and upright recovery remain exact.

**Pop means first genuine rise1110.** The old rejected Rise1114, strike1115.8 and Scoop release1116.7 remain explicit same-time details and before/after witnesses. The fixed source view retains its foreshortening; a separately labelled, fixed body-side view makes knee reserve and elbow bend readable. No per-frame camera, pose scaling or native-heading change hides the repair.

| Height / physical stance | Full phases | Feet / balance | Same-time before → after | Fixed body side |
|---|---|---|---|---|
| Normal / Regular | [phases](normal-regular-phases.jpg) | [contacts](normal-regular-contacts.jpg) | [Rise / strike / Scoop](normal-regular-balance-before-after.jpg) | [strike / Scoop](normal-regular-body-side-balance-before-after.jpg) |
| Normal / Goofy | [phases](normal-goofy-phases.jpg) | [contacts](normal-goofy-contacts.jpg) | [Rise / strike / Scoop](normal-goofy-balance-before-after.jpg) | [strike / Scoop](normal-goofy-body-side-balance-before-after.jpg) |
| Low / Regular | [phases](low-regular-phases.jpg) | [contacts](low-regular-contacts.jpg) | [Rise / strike / Scoop](low-regular-balance-before-after.jpg) | [strike / Scoop](low-regular-body-side-balance-before-after.jpg) |
| Low / Goofy | [phases](low-goofy-phases.jpg) | [contacts](low-goofy-contacts.jpg) | [Rise / strike / Scoop](low-goofy-balance-before-after.jpg) | [strike / Scoop](low-goofy-body-side-balance-before-after.jpg) |

The own black-shirt/cap/tattooed rider in [The Berrics BATB13 Jamie Griffin vs Zach Doelling](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=217) supplies bounded1084–1138 of the existing181–219s cache. PTS is1001×frame /30000 at30000/1001fps; original full-video frames/hash are unknown. Original1114/1116/1118 chains and tattooRight identify physical Regular, Leftnose/Righttail, with zero reflection. Goofy is a labelled mirrored rig variation; actual animated GLB foot bones determine the dropdown, captions and manifest. Original reference pixels never mirror and remain private.

Own49/55 VIDEO selections include6explicit pre-pop gaps. Camera/depth/root, full shoes/contacts and gaze are reconstructed; detector confidence is not measured3D. Film ends1138 during absorption; the unchanged own upright suffix1138–1170 uses this take's upright1090 and holds genuine video1138. No future frames or another trick body are invented.

All four actual managed Pad/32Hz Step/Land/scorer and60Hz body cases pass: one real pop, normal36/36 or low34/34 Air samples, no fallback/bail, ordinary Ground/capture-off at4s. Native traces are byte-identical to the rejected baseline. Original skin/inverse binds and the complete13-part board use the same strict exporter. All four browser runs reach4s at1× and¼×;28 decoded-source phase checks,24 Source/body-Side same-time details and12 Feet-close checks pass with zero errors. The eight current phase/contact sheets and same-time before/afters were inspected; the adjacent manifest pins all16images and evidence hashes.

Raw remains `reviewed=false`. Claude owns the independent offline/library verdict. Native Shadow testing remains pending. Source presence, managed/export/browser checks and CI do not promote those gates. No source footage/pixels, raw detector output, generated models/packs, approval files or weights are published here.

After composing main#727/#728 and documentation#671/#729, all client/shared C# bytes remain exact to the rendered987 freeze and the current baker reproduces rawa145 exactly. A second agent independently inspected all8 current sheets and the source/body-side comparisons: scoped still PASS. This adds neither a separate playback review nor Claude/native approval.
