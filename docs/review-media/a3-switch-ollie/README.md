# A3 Switch Ollie — original rig review

Own landed [P4C reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=80): the same black-cap/gray-shorts rider performs a plain Switch Ollie on flat concrete. His filmed Goofy Right foot leads at the nose; his Left foot pops the trailing tail. One anatomical reflection normalizes that take to canonical Left/nose guide and Right/tail pop. The Switch signature does not add another reflection, body turn or travel-frame rotation.

Bounded78..92 s preview decoder75..158,30000/1001 fps, start PTS0/timebase1/30000; approximate full-video80.502500..83.271933 s. Next actor cut159 is excluded. Full-video global frames/PTS/hash are unknown. The actor is selected in84/84 raw frames; low-confidence original Right heel/toe/ankle and Left-wrist estimates remain explicit. Monocular camera/depth, canonical hip/shoulder heading, knee planes, gaze, root and shoe contacts are authored, not measured3D. Footage, audio, raw arrays and source-comparison pixels stay private.

[Private source-comparison viewer](http://127.0.0.1:63761/a3-switch-ollie-final/) · [published equivalent source](https://github.com/Dj-Shortcut/rust-skate-mod/commit/e9afedf6e66ec667700036f68230f8f5d9ae7442) · [exact product replay](https://github.com/Dj-Shortcut/rust-skate-mod/commit/10fbbc392d523044efed39e3efa16adcb4d43954).

| Physical preview | Normal | Low | Contact close views |
|---|---|---|---|
| Regular | [Load/Pop/Air/Catch/Land/End](normal-regular-phases.jpg) | [Load/Pop/Air/Catch/Land/End](low-regular-phases.jpg) | [normal](normal-regular-contacts.jpg), [low](low-regular-contacts.jpg) |
| Goofy | [Load/Pop/Air/Catch/Land/End](normal-goofy-phases.jpg) | [Load/Pop/Air/Catch/Land/End](low-goofy-phases.jpg) | [normal](normal-goofy-contacts.jpg), [low](low-goofy-contacts.jpg) |

Actual animated GLB foot bones determine physical preview stance; requested/native binding labels are separately recorded in [review-manifest.json](review-manifest.json). The actual signature is `Ollie,true,false,0,0,0,0`. Native translation, NoseYaw, flight and observed Land remain authoritative. No flip, shove, body spin or second jump is authored. The tail foot presses beyond the bolts; the front outer upper follows griptape, then the board stays grip-up beneath the compact Air and settling catch. Tail-sole release110 precedes distinct later free Air113; catch116, source Land118 and upright recovery140..158 are this take's own timing.

The final route preserves Nollie28/Fakie29 and adds Switch30 in this frozen replay. It inherits the real ordinary-charge fix that preserves independent gaze and the upright neck-to-head segment before future source selection. No additional Switch source pose correction was needed; the source pack remains byte-identical to the original own-source bake.

All four normal/low × physical Regular/Goofy cases run actual Pad/32Hz Step/Land/scorer followed by the measured fitter, ordinary charge, CapturedTricks and shared source sampler at60Hz. They select Switch Ollie with normal36/36 and low34/34 active Air frames, no fallback/bail, observed landing and an ordinary-ground endpoint at4 s. The original skin, inverse binds, full13-part skateboard and original support/ground/floor guards are retained.

All four source comparisons are inspected from Load through recovery, with decoded original video at all28 phase visits and full1×/¼× replays. [playback-summary.json](playback-summary.json) records the bounded result. The source is never mirrored; its fixed camera fit is explicitly estimated and does not repair a pose. Public images show only the original rig/board; stills alone do not establish continuity.

This is controlled managed and visual evidence. Raw metadata remain `reviewed=false`; only the private test copy is approved for replay. Claude owns the independent library/offline record. Integrated CI and native Unity/IL2CPP/Shadow acceptance remain separate gates. No physics/scoring, F5/F9, gameplay camera or placement code changed.
