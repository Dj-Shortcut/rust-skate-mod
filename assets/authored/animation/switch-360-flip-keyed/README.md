# Switch 360 Flip — own BATB12 reconstruction

Source present and executable; **the prior1fdf Catch/Land preview was held for stiffness, and this source correction is not yet visually reviewed, offline approved or tested in game**. The raw pack keeps `reviewed=false`. Root owns the normal-route integration and final PR; Claude owns the library status.

## Source and anatomy

The genuine striped-shirt Paul Rodriguez take is in [Chris Joslin vs Paul Rodriguez, BATB12](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=202). The original, unmirrored cached full video has SHA-256 `531eb85fd1d292c229836f6eedc4adec422aaf6dd644040c49c7480b2beadad7`. Its decoder uses 30000/1001 fps, first PTS 0 and PTS = frame × 1001 with time base 1/30000. The selected approach starts at frame 6060 (202.202 s), after the previous actor's cut between 6050 and 6055; the next cut is between 6150 and 6153.

Dense original 6081–6104 frames show the board's complete 360 shove and kickflip, an unspun rider, catch, flat landing and rollaway. The take is physical **Regular: original Left at the nose/front instep, Right on the tail**. Those chains are checked on the setup and scoop frames, rather than inferred from the switch signature or an on-screen label. Canonical controls retain the same anatomy with **zero reflection or anatomical swap**. Original source pixels remain unmirrored and private. Actual physical labels in future GLBs must still be audited from their foot bones.

Own strict MediaPipe VIDEO selection accepted the same actor in all 76 frames 6060–6135. The animation uses only 6060–6117: collapsed 6097 and swapped-leg 6098 are rejected as complete poses and interpolated from this take's own 6096/6099, leaving **56 accepted samples from 58 take frames**. Left forearm/wrist visibility and depth are unreliable around 6091–6092, 6097–6098 and parts of 6104–6117. Actor selection does not certify anatomy or measured 3D. Raw detector data, original video/audio and model weights remain private.

## Authored execution

| Source frame | Meaning |
|---|---|
| 6082 | First visible upward extension; the real Pop marker |
| 6086.6 / 6087.4 | Left sole releases / upper-outer shoe contact becomes full |
| 6087.8 / 6088.2 | Tail strike / Right tail sole releases |
| 6088.8 / 6089.4 | Left instep releases / distinct later free Air |
| 6099 / 6100 | First Left catch / both feet caught |
| 6102.7 | Flat landing |
| 6117 | Last genuine source pose; subsequent heel-reset motion is excluded |
| 6117–6134 | Authored upright stabilization from this take's own 6060 body |
| 6135 | Stable authored endpoint; original reference held at 6117 |

The original Left outer-upper shoe feature and its normal fit the shaped grip surface for the small flick; the sole is not substituted. The Right foot is beyond the rear bolts in the tail scoop pocket. After one complete canonical +360 roll/+360 yaw, the feet catch the same physical deck ends and toe headings. The native signature is Ollie, switch true, fakie false, flip +360, shove −360, body/spin 0. Source yaw and the actual native carrier must each be applied once.

Only generic original rig, shoe, curved-deck, interpolation and clearance mathematics are reused. No normal Tre, Hardflip, Halfcab or other take's body, foot paths or source clocks are read. Camera yaw/depth, pelvis, knee plane, gaze, contact timing and winding interpolation are consciously authored estimates. A smooth pressure **world-pelvis** envelope removes the reference rise once, retaining knee reserve during upward extension. Original fixed lengths and reach/clearance guards remain unchanged.

Source-real high arms are deliberately replaced with bent asymmetric shoulder-height balance under the owner's rule. Hidden Left forearm depth is reconstructed and labelled. After genuine frame 6117 the upper body returns smoothly to this take's own upright 6060 pose while the caught/landed shoe anchors stay fixed; no later heel reset or leg swing is inherited. This is an **authored source suffix**, not measured recovery.

## Catch and landing correction

Root held the initial1fdf preview: both knees were nearly straight at Catch/Land, with the visible absorption delayed until after touchdown. The authored pelvis now lowers before first catch6099 and both-feet catch6100, stays bent through landing6102.7, reaches one absorption trough6106 and recovers smoothly through6110/6113 into the existing own6117–6135 upright suffix. The hands stay low and open on bent, asymmetric arms while that torso lowers. These controls are consciously authored for a believable landing, not additional captured motion.

Board winding, all source clocks/events, original foot paths, contacted ankle/toe/sole targets, the zero-reflection anatomical binding and the6117 genuine-source cutoff remain unchanged. The old preview is an explicit rejected baseline; unchanged exports and prior route checks do not certify these new controls. The fresh458-sample raw bake and four actual managed Pad/Step/Land/scorer/body cases pass. Complete physics/scoring traces remain byte-identical to the held2520 route, and all source board clocks/events, source foot targets and runtime contacted shoe/toe targets remain unchanged. Root original-mesh comparison/replay is still required before publication; Claude retains the independent offline verdict.

## Reproduce and remaining gates

```sh
python scripts/author_skate_switch_360_flip.py --output context/switch-tre-baked
```

The initial local bake produces 458 samples. Source, geometry and helper hashes are validated before output; output is written atomically. This proves executable source data. The fresh four actual Pad/Step/scorer/body routes pass (normal36/36, low34/34 captured Air frames), return to ordinary Ground by4s and preserve all prior physics/scoring traces exactly. Original-skin/13-part-board GLBs, same-source normal/low physical Regular/Goofy phase sheets, 1×/¼× viewer review, Claude's independent offline verdict and the separate Shadow test remain pending for this correction.
