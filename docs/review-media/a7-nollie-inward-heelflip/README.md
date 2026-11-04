# Nollie Inward Heelflip — own-source review candidate

Twelve rig-only JPGs show normal/low × physical Regular/Goofy: eight body phases,
four contact closeups, and Land/Rollaway/End recovery. Each image uses the
original skinned rider and complete shaped 13-part board. No source film pixels,
raw detector data, game files or private review fixtures are published here.

The preserved source is the genuine landed striped-shirt Rodriguez take in
[The Berrics reference at 159s](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=159),
original frames 4770–4870 inside the uncut shot 4759–4881 at 30000/1001 fps.
Filmed Goofy has Right nose pop / Left rear heel. Original private film stays
unmirrored; physical Goofy is the direct anatomical reconstruction and physical
Regular its mirror. Detector gaps 4817/4818 and ankle/heel occlusion 4817–4821
remain explicitly reconstructed from the same take, without an anatomical swap.
Root/foot depth, full board angles, contacts and low balance arms are authored;
they are not measured 3D. See the [source notes](../../../assets/authored/animation/nollie-inward-heelflip-keyed/README.md).

The images come from actual product SkatePad input and full 32 Hz Step/Land/scoring,
replayed through the 60 Hz capture/body route at frozen main `610b69eb`, with
three proposed family adapters compiled only as private copies. Normal cases
capture 36/36 Air frames with a 0.59375 s observed flight; low cases capture
34/34 with 0.5625 s. All four score **Nollie Inward Heelflip**, receive one actual
Pad pop, have no held continuation or body spin, observe Land and return to
ordinary Ground with capture off at 4 s. Native geometry and foot acquisition
are controlled in this offline replay; Rust/Shadow runtime is unverified.

Physical labels are checked from the animated GLB's anatomical foot bones
against its initial deck nose. In this frozen proxy, requested enum Regular
renders physical Goofy and requested enum Goofy renders physical Regular. This
is an explicit review mapping, not a change to native stance semantics. The same
exporter already gives this reversal in unchanged ordinary Ground at 0 s with
capture off, and retains it before pop. Actual GLB board-forward and recorded
initial nose agree (dot 1.0); the candidate does not introduce the reversal.

Bare-eye inspection of all four phase/contact/recovery sequences confirms bent
Catch and Land, low open arms, planted catch/landing shoes and soft knees through
Rollaway/End. The preserved 0.94 m source pelvis endpoint was checked visibly;
no motion or source reconstruction was changed for these sheets. Heel selects
the toe-up rear-heel guide before its authored release at 4818.9; that contact is
occluded in the original. Catch follows both-foot acquisition around 4829,
Land follows actual physics, and End shows ordinary recovery at 4 s.

Raw pack `reviewed=false` is unchanged (`8f835c405dda176bbb72e98263d34eceec7b900cf49f516abbec8f244257e88c`).
The private viewer supplies original unmirrored film, 1×/¼× controls, source/body
clock, physical stance/profile selection and explicit selected GLB download.
The full continuous comparison of all four cases remains pending. These images
do not grant Claude's independent offline verdict, native acceptance or a
library count. Root owns final integration and verification on merged main.
