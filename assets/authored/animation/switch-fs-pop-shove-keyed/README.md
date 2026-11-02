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
samples per stance retain the capture; native traces remain byte-identical.
Four original-skin/full13-part-board exports,32 phase frames,1×/¼× playback,
28 decoded-source seeks and24source774/side/foot details were self-checked
through ordinary4-second recovery, without browser errors. Source matching
Goofy and its Regular mirror are explicitly labelled; source pixels never
mirror. Private camera fits reflect viewing side only, never native heading.

The [render/runtime freeze](https://github.com/Dj-Shortcut/rust-skate-mod/commit/4f2b7c2be24c7a1e734d15daca1e751805bd2fe4)
uses raw pack SHA-256
`3803675fdaf0fc0752e9cd01903838efc62c83099bf705c6b32c55283e3c1b4f`.
Raw packs stay `reviewed=false`. These are managed/graphical author checks,
not native Rust/Shadow or installed-package approval. Claude owns the
independent offline verdict and library status; the owner tests the native
client later on Shadow.
