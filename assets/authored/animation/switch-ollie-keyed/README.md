# Switch Ollie: independent own source and original contacts

[The Berrics P4C reference](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=80) shows the actual landed Switch Ollie by the black-cap/gray-shorts rider on flat concrete, from load to upright roll-away. No different trick body or stationary/curb take is loaded. Source footage, pixels, audio, raw detector arrays and model weights stay private; no source-video redistribution rights are asserted.

The naturallyRegular rider performs in the opposite filmedGoofy stance. Exactly one anatomical Left/Right swap andX reflection canonicalizes the source toRegular: filmedLeft/trailing-tail pop becomes canonicalRight/tail pop; filmedRight/nose guide becomes canonicalLeft/nose guide. SignatureSwitch does not trigger another reflection or turn.

The own input is bounded preview decoder frames75..158, at actual30000/1001. The preview startsPTS0 with timebase1/30000 and increments1001. Public-time estimates are approximately80.502500–83.271933seconds. These are bounded decoder indices, not original global frames. Original full-video hash/globalPTS remain unknown. The next actor cut is excluded.

The full-frame detector selects84/84 frames with the same declared foreground actor. Original low-confidence Right heel/toe/ankle and Left-wrist estimates remain listed; source presence does not make depth or contact measured.

Own source torso and upper/lower arm vectors are retained under one explicit reconstructed panning-camera basis. Coherent canonical hip/shoulder heading and knee planes replace unreliable monocular depth sign reversals. Neutral head roll and gaze point toward the board/landing. Fixed canonical lengths, exact root height, shoe roll, upper-guide and sole contacts are authored, not measured3D. Raw confidence is not inflated.

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

The tail foot stands beyond the bolts. The front dark outer upper guides noseward along the original griptape; the outsole does not replace that feature and no off-edge flip is added. The board stays grip-up, with no flip/shove/body spin. Pressure cancellation uses the same native0.6second reference arc; actual native translation, NoseYaw, flight and observed landing own playback. There is no second jump/carrier, travel-axis rotation or scoring authority in the source.

```sh
python scripts/author_skate_switch_ollie.py --output context/switch-ollie-source
```

The baker reuses already-main normal-Ollie contact mathematics only, with independently pinned own body, phases, geometry and contact controls. It checks hashes and strictly ordered clocks before atomically writing JSON and one SKMCAP pack. Dense interpolation is reconstruction, not additional capture. The raw pack remains `reviewed=false`.

Current gate: source executable and own reference classified. Actual normal/low controller/Step/Land/body replay, rigged viewer review and CI integration are pending. Claude owns the offline/library verdict. Unity/IL2CPP and Shadow have not been verified here.
