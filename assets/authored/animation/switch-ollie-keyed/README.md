# Switch Ollie: independent own source and original contacts

[The Berrics P4C reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=80) shows the actual landed Switch Ollie by the black-cap/gray-shorts rider on flat concrete, from load to upright roll-away. No different trick body or stationary/curb take is loaded. Source footage, pixels, audio, raw detector arrays and model weights stay private; no source-video redistribution rights are asserted.

The naturally Regular rider performs in the opposite filmed Goofy stance. Exactly one anatomical Left/Right swap and X reflection canonicalizes the source to Regular: filmed Left/trailing-tail pop becomes canonical Right/tail pop; filmed Right/nose guide becomes canonical Left/nose guide. Signature Switch does not trigger another reflection or turn.

The own input is bounded preview decoder frames75..158, at actual30000/1001. The preview startsPTS0 with timebase1/30000 and increments1001. Public-time estimates are approximately80.502500–83.271933seconds. These are bounded decoder indices, not original global frames. Original full-video hash/globalPTS remain unknown. The next actor cut is excluded.

The full-frame detector selects84/84 frames with the same declared foreground actor. Original low-confidence Right heel/toe/ankle and Left-wrist estimates remain listed; source presence does not make depth or contact measured.

Own source torso and upper/lower arm vectors are retained under one explicit reconstructed panning-camera basis. Coherent canonical hip/shoulder heading and knee planes replace unreliable monocular depth sign reversals. Neutral head roll and gaze point toward the board/landing. Fixed canonical lengths, exact root height, shoe roll, upper-guide and sole contacts are authored, not measured 3D. Raw confidence is not inflated.

| Candidate phase | Bounded decoder frame |
| --- | ---: |
| Extension Pop | 103 |
| CanonicalLeft sole releases into the outer-upper guide | 105 |
| Actual tail strike reference | 108 |
| CanonicalRight tail sole releases | 110 |
| Distinct later free Air | 113 |
| Both-foot settling catch | 116 |
| Source landing estimate | 118 |
| Upright recovery | 140..158 |

The tail foot stands beyond the bolts. The front dark outer upper guides noseward along the original griptape; the outsole does not replace that feature and no off-edge flip is added. The board stays grip-up, with no flip/shove/body spin. Pressure cancellation uses the same native 0.6 second reference arc; actual native translation, NoseYaw, flight and observed landing own playback. There is no second jump/carrier, travel-axis rotation or scoring authority in the source.

```sh
python scripts/author_skate_switch_ollie.py --output context/switch-ollie-source
```

The baker reuses already-main normal-Ollie contact mathematics only, with independently pinned own body, phases, geometry and contact controls. It checks hashes and strictly ordered clocks before atomically writing JSON and one SKMCAP pack. Dense interpolation is reconstruction, not additional capture. The raw pack remains `reviewed=false`.

Current gate: four actual managed Pad/32Hz Step/Land/scorer→60Hz body cases pass (normal36/36, low34/34 keyed Air frames), with no fallback/bail, observed landing and ordinary ground at4 s. The real product includes the independent-gaze/upright-head-segment ordinary-charge fix. All four normal/low physical Regular/Goofy original-skin/full13-part-board Load/Pop/Air/Catch/Land/End comparisons were inspected beside decoded source; all four1×/¼× viewer runs finish4 s with0 browser errors. The source body/contact pack is unchanged. [Eight rig-only phase/contact sheets and exact published source/product pins](../../../../docs/review-media/a3-switch-ollie/README.md); [local final viewer](http://127.0.0.1:63761/a3-switch-ollie-final/). Raw data remain unapproved; Claude owns the library/offline record. Integrated CI and native Unity/IL2CPP/Shadow acceptance remain separate gates.
