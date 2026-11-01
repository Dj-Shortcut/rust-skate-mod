# Own source-keyed FS Pop Shove-it

`fs-pop-shove-it-source-keyed-v1` uses the real normal frontside180 pop shove
by Sewa Kroetkov in The Berrics `YcmPDIj2BFo`. It is not FS360 slowed down,
a backside body, a body spin or a flip. Canonical yaw is negative180;
the existing native signature is normal Ollie / flip0 / shove+180 / spin0.

The immutable private source reconstruction is local frames190–276 of the
124 s cache (130.340–133.209 s), physically Goofy/right-leading. Pop211,
rear release212, catch220, Land226 and recovery241 follow this own take.
The actual source was inspected from setup to the landed roll-away; a panning
three-quarter camera, blur and deck/ankle overlap leave depth uncertain.

The baker reads only that SHA-bound own body. The BS-named imported helper
provides generic fixed-chain reconstruction; it never reads another clip.
Source upper, head and limb directions come from the FS take. Authored controls
supply the heel-side tail pressure, single180 deck sweep and released foot
pickup where old monocular ankle/deck depth is unusable. Anatomical catch
positions and toe headings are transformed into the final180 deck basis;
the rider's feet do not exchange places with the board's nose and tail.

The original source contact-prior root step is bridged explicitly at218–228,
with bent landing. Later pelvis balance is centred over the actual sole
footprints; the rear knee follows its anatomical toe direction as in this
source's steep-shin recovery. These are declared authored corrections, not
measured3D or extra source detections. The physical carrier owns airtime and
travel; the baker removes the old reconstructed board/root translation once.

```sh
python scripts/author_skate_fs_pop_shove.py --input <private-original-fs.json> --output <private-output>
```

The controls pin the own source JSON, generic reconstruction helper and the
original shoe/shaped-deck geometry. Output JSON and SKMC are encoded before
replacement. The generated source stays `reviewed=false`; no footage, detector
output, generated pack or new approval ships in this folder.

Validation so far is source selection and strict managed reader/sampler checks.
The runtime integration owner separately runs the actual Pad/Step/body route,
normal/low and both physical stances, then exports the frozen mesh and real
board for comparison. A bake is not a viewer approval; Claude's offline quality
gate, original pack approval and native Shadow acceptance remain separate.
