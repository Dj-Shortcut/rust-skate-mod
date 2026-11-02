# Fakie Ollie: independent own source and original contacts

[The Berrics P4C reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=73) shows the actual landed Fakie Ollie by the black-cap/gray-shorts rider on flat concrete, from load to upright roll-away. No different trick body or stationary/curb take is loaded. Source footage, pixels, audio, raw detector arrays and model weights stay private; no source-video redistribution rights are asserted.

FilmedRegular anatomy stays unchanged: canonicalRight presses the leading tail while velocity points backward. Left/nose guides toward board+noseZ, which trails the travel. The source is never turned into the travel frame.

The own input is bounded preview decoder frames274..347, at actual30000/1001. The preview startsPTS990 with timebase1/30000 and increments1001. Public-time estimates are approximately73.142467–75.578233seconds. These are bounded decoder indices, not original global frames. Original full-video hash/globalPTS remain unknown. The next actor cut is excluded.

The fixed-ROI detector selects73/74 frames. Raw327 is rejected by the original feet-distance identity guard; it remains missing. Compact controls omit327, and the sampler interpolation between own326/328 is explicitly authored reconstruction. No raw body, confidence or actor mask was filled.

Own source torso and upper/lower arm vectors provide the base under one explicit reconstructed panning-camera basis. Coherent canonical hip/shoulder heading and knee planes replace unreliable monocular depth sign reversals. Neutral head roll and gaze point toward the board/landing. An owner-requested authored balance correction keeps the canonical Left/nose-side Pop arm bent at shoulder height or below, from decoder304.5..314 only. It preserves the other arm and all accepted free-Air/catch/recovery geometry. Pre-Pop Load is the ordinary procedural charge pose; its independent gaze belongs to shared charge presentation, not this future source clip. Fixed canonical lengths, exact root height, shoe roll, upper-guide and sole contacts are authored, not measured3D. Raw confidence is not inflated.

| Candidate phase | Bounded decoder frame |
| --- | ---: |
| Extension Pop | 304 |
| CanonicalLeft sole releases into the outer-upper guide | 306 |
| Actual tail strike reference | 309 |
| CanonicalRight tail sole releases | 311.5 |
| Distinct later free Air | 314 |
| Both-foot settling catch | 319 |
| Source landing estimate | 321 |
| Upright recovery | 337..347 |

The tail foot stands beyond the bolts. The front dark outer upper guides noseward along the original griptape; the outsole does not replace that feature and no off-edge flip is added. The board stays grip-up, with no flip/shove/body spin. Pressure cancellation uses the same native0.6second reference arc; actual native translation, NoseYaw, flight and observed landing own playback. There is no second jump/carrier, travel-axis rotation or scoring authority in the source.

```sh
python scripts/author_skate_fakie_ollie.py --output context/fakie-ollie-source
```

The baker reuses already-main normal-Ollie contact mathematics only, with independently pinned own body, phases, geometry and contact controls. It checks hashes and strictly ordered clocks before atomically writing JSON and one SKMCAP pack. Dense interpolation is reconstruction, not additional capture. The raw pack remains `reviewed=false`.

Current gate: the owner corrections pass all four actual managed input/32Hz Step/Land/scorer→60Hz body cases (normal36/36, low34/34 active Air frames), with no fallback/bail, observed landing and ordinary ground at4s. The real shared charge presentation preserves independent gaze and an upright neck-to-head segment before future source selection; the baker authors only the bounded shoulder-height Pop arm. All four original-skin/13-part-board Load/Pop/Air/Catch/Land/End comparisons were inspected, then all four decoded-reference viewer cases completed1×/¼× through recovery with0 browser errors. [Eight updated rig-only phase/contact sheets and exact public source/product pins](../../../../docs/review-media/a3-fakie-ollie/README.md); [local corrected viewer](http://127.0.0.1:63761/a3-fakie-ollie-owner-fixes/). Raw data remain unapproved. Integrated CI and native Unity/IL2CPP/Shadow acceptance remain separate; Claude owns the library/offline record.
