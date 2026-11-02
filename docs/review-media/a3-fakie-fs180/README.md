# FS Halfcab: softer Air elbows on the original rig

[Viewer and original source](http://127.0.0.1:63761/a3-fs-halfcab-soft-elbows/)
cover normal/low × physical Regular/Goofy through actual Land and ordinary
4-second recovery. The [same-camera Air/Catch comparison](air-catch-before-after.jpg)
shows the accepted baseline beside the forearm refinement.

| Flight | Physical stance | Full sequence | Arm/foot details |
|---|---|---|---|
| normal | Regular | [Phases](normal-regular-phases.jpg) | [Details](normal-regular-contacts.jpg) |
| normal | Goofy | [Phases](normal-goofy-phases.jpg) | [Details](normal-goofy-contacts.jpg) |
| low | Regular | [Phases](low-regular-phases.jpg) | [Details](low-regular-contacts.jpg) |
| low | Goofy | [Phases](low-goofy-phases.jpg) | [Details](low-goofy-contacts.jpg) |

Claude reviewed the base FS Halfcab in gate 6565046080/#707 and requested a
small elbow refinement. During source frames 242–254 the wrists bend slightly
forward/down in anatomical facing/up/hip axes, rather than backward travel.
Only source hand endpoints change; source shoulders/elbows, every non-arm
field, feet, board, contacts, source clock and outside-window poses stay
unchanged. The existing production fixed-chain sampler naturally refits
elbows with the changed wrist goals. Its source/controller/physics code and
safety guards stay intact; this is an authored visual correction.

These eight phase/detail JPEGs and the before/after contain the unchanged
original skin, weights and complete 13-part skateboard. Full phase grids
retain Load, early snap, level front-bolt guide, Air, Catch, bent Land,
upright rollaway and ordinary End. The source/camera settings match the
accepted #702 renderer and viewer. Actual animated GLB foot bones establish
physical stance; requested native binding is recorded separately. No source
pixels are published, and the preview changes no gameplay camera or pose.

The genuine [Berrics Moose vs Bastien BATB6 source](https://www.youtube.com/watch?v=H4MEulcgUTc&t=29)
uses one plaid/black-knitcap foreground actor, physically Regular. Bounded
frames 227–275 end before actor cut276; original global frames are unknown.
Exact bounded PTS=990+1001·frame, time base 1/30000, 30000/1001 fps. Backward
approach, pure rider+board half-turn, no flip and landed forward roll were
checked directly. Complete source heading is removed from every joint,
direction, sole and board; actual native NoseYaw supplies the sole real turn.

Camera/depth, hidden arm depth, fixed-chain contacts and cut recovery remain
explicit authored estimates. Timeline 275–302 is authored planted upright
recovery, with the genuine reference held at 275. Original deck ground datum,
accepted snap/guide, gaze, feet and native coupled rotation remain intact.
Four actual Pad/full 32 Hz Step/Land/scorer and 60 Hz body cases pass.
All four 1×/¼× viewer pairs reach ordinary 4-second recovery; 28 phase seeks
decode actual source frames and 20 arm/foot detail views finish with zero
browser/runtime errors. The author inspected normal/low physical Regular/
Goofy beside the original source through Load/Air/Catch/Land and upright
recovery, including forearm depth from Source and Side.

Source packs stay **reviewed=false**. Claude owns the offline/library verdict;
native Rust/Shadow remains pending.

[Integrated source/runtime freeze 8c04e0c5](https://github.com/Dj-Shortcut/rust-skate-mod/commit/8c04e0c563c9f61078e74b24a74aa9faa9cefdf7)
uses raw pack `dc04306f8008fc0a212be043ef136f6bffab100ed16b1bc62c3d0e630e4e041a`.
The accepted baseline raw SHA-256
`8545776186b856edd4b5eebbd5bcb52f836c3472e347596a396fe895e9659960`
is reproduced from its original controls and rerun through the same current
runtime for the comparison. [Manifest](manifest.json).
