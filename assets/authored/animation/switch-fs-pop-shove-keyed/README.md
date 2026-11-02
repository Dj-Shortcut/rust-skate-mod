# Switch FS Pop Shove-it: own BATB13 source

This source reconstructs the black-shirt/black-cap Switch Frontside Shuvit
attempt in [The Berrics video](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=205),
exact attempt-table row1091. The existing bounded38-second cache is used;
no new video is downloaded. Source frames738–817 at30000/1001 fps cover setup,
pop, grip-up board-only FS180, catch, landing and upright rollaway. The next
actor cut827 is excluded. Bounded PTS is1001×frame at timebase1/30000; the
original video's global frame/PTS is unknown. The requested-cut181s offset is
an estimate for public seconds, not an original frame index.

The actual771/773/774 images identify filmed Goofy: anatomical Right guides
the rising nose and Left pops the trailing tail. The Switch label implies a
Regular-base rider, but that base inference has no full plain-normal witness.
The filmed initial nose is aligned once, then exactly one X reflection plus
anatomical Left/Right swap normalizes to canonicalRegular Leftnose/Righttail.
The earlier no-reflection assumption was rejected. This fixes source
normalization; native heading, the authored canonical foot/catch headings and
board timing remain unchanged. The board turns; the torso and foot headings
do not turn180 with it. At catch, anatomical feet retain their world-side
roles while deck-local nose/tail positions are reversed.

`captured-body.json` contains this attempt's own80 foreground VIDEO selections,
retargeted into the original fixed rig. No BS, normal FS, Halfcab or other
trick's body motion is borrowed. The detector incorrectly reverses the whole
anatomical shoulder/hip chain on774 alone; that model pose is explicitly
rejected and reconstructed only from this take's neighbouring773/775 before
smoothing. Original confidence limits and low-visibility frames are retained.
Camera pan/depth is authored; no rotating-board de-yaw or travel-axis rotation
is used. Source video/audio/pixels and raw model arrays remain private.

`constraints.json` specifies authored tail-ball pressure, heel-side tail pocket,
front-foot pickup without a flick, signed board yaw−180, grip-up pitch,
free-foot clearance, reversed-end catch, fixed-chain knees, root height and
recovery. Pop768 marks the first leg/body rise (earlier770 marker rejected); front sole release771.5, strike773.5, tail sole release775.5,
and distinct later freeAir778 are separate markers. Catch782/784 and Land787
retain both anatomical feet. Partly cropped leading-arm depth at the snap/Air and
the hidden rollout suffix are explicitly authored in torso anatomical axes,
with a bent forward/outward balance arm (filmedRight becomes canonicalLeft). Generic board/shoe/rig math is reused
read-only; all production guards remain intact.

Claude's [A5 gate](https://github.com/Dj-Shortcut/rust-skate-mod/issues/716#issuecomment-6567110165)
rejects the overhead V at Scoop and the sustained straight high arm at Catch/Land.
The repair explicitly authors **both** elbow/hand targets as bent forward/outward
balance at or below shoulder height. It blends in over source762–768, owns
Scoop/Catch/Land through797 and rejoins the original rollout by808. This uses
horizontal anatomical body axes, not the backward Switch travel vector.
The own captured body, shoulders/head/torso/pelvis, feet/soles, board curves,
contacts and all source/event times remain unchanged. This is an authored arm
reconstruction, not new measured mocap or an independent offline approval.

Bake using the repository's NumPy environment:

```sh
python scripts/author_skate_switch_fs_pop_shove.py --output context/switch-fs-shove
```

The exact intended route is Ollie, Switch=true, TailLeads=false, flip0,
shove+180, spin0 and body0. The new clip alone uses a0.125flight presentation
root entry so its first rise is visible before the tail strike; source feet,
pelvis/board curves, physics/scoring and all existing guards remain intact.

[Viewer](http://127.0.0.1:63761/a5-switch-fs-pop-shove/) ·
[Original-rig phase/contact sheets](../../../../docs/review-media/a5-switch-fs-pop-shove/README.md).
Four actual Pad/production Step/Land/scorer cases select Switch Frontside
Pop Shove-it, normal/low and both requested bindings. All36normal/34low Air
samples per stance retain the capture. All four complete native traces
match the earlier4f2 baseline byte-for-byte. The475 source samples and each
241-frame actual body replay differ only in the four elbow/hand positions;
all non-arm body/foot/board/sole/time/contact/physics fields are exact.

Four original-skin/full13-part-board exports and32 phase frames were
self-checked at1×/¼× through4-second ordinary recovery, with32 decoded-source
phase visits and24 Source/Side/Feet arm/contact details, without browser
errors. The new arm transitions settle through Catch/Land into the original
rollout without a high-hand rebound. The true first-rise768 is the main Pop
button; the exact old rejected strike773.5 remains explicit as TailStrike
and in same-camera before/after figures. Source-matching Goofy and its
Regular mirror follow the actual GLB bone audit, including default/dropdowns;
source pixels remain unchanged.

The [render/runtime freeze](https://github.com/Dj-Shortcut/rust-skate-mod/commit/05745de05687ec43e61a782bf075539288679c93)
uses raw pack SHA-256
`4e0e2868db2aec3a037e1afcabcf8b3532d5be195c6e1cdf5c87c207ae55cde9`. All C# remains unchanged from main at
this data-only repair. Raw packs stay `reviewed=false`. These are controlled
managed/graphical author checks; Claude owns the independent offline
verdict and library status. Native Rust/Shadow and installed-package
acceptance remain separate and pending.
