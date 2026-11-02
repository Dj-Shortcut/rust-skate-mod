# Nollie Hardflip — own Rodriguez source reconstruction

Reference: [The Berrics, Rodriguez attack](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=514), existing full-file frames 15420–15534. Actual PTS is frame index ×1001 in timebase 1/30000. The continuous take shows a nose pop, full hardflip under an unspun body, catch, bent landing and uninterrupted rollaway. The chapter label at 517 seconds starts after the actual load/pop; it is not the animation clock.

The filmed rider is physical Goofy: anatomical **Right front/nose pops, Left rear gives the instep flick**. One local X reflection plus complete anatomical side relabelling makes the canonical Regular controls: **Left nose sole pop, Right rear outer-upper/instep flick**. The source-matching runtime view is physical Goofy; the other stance must be labelled mirrored. Reference pixels remain unmirrored.

Own strict VIDEO detection accepts 107/115 frames. Raw frames 15484–15486 and 15489–15493 remain rejected. Only bracketed control rows bridge these gaps, explicitly as authored reconstruction, followed by two symmetric smoothing passes. Camera-axis depth, sole/instep contact, full combined board rotation, pelvis and compact lower chains are authored against this take. Late source Right-wrist uncertainty maps to canonical Left and has a bounded soft-arm recovery. No other trick body is read.

Run `scripts/author_skate_nollie_hardflip.py --output <private-directory>`. It verifies its own body and the pinned original geometry/surface hashes, then atomically writes the JSON and raw `reviewed=false` pack. Generic rotation, shaped-shoe fit, interpolation, fixed-chain and whole-shoe clearance mathematics are shared. Normal Hardflip foot roles or source-specific timing/root/tuck overrides are not imported.

First extension 15474.5 (bracketed by dense source frames 15474–15475), nose pressure 15481, Left nose-sole release 15482.75, Right instep exit 15483.25, distinct free Air 15485.5, catch 15494.5/15495.5 and Land 15498 are authored visual estimates. Board winding is full +360 roll with −180 local FS yaw; the body has no halfturn. Actual native input/flight/landing/scoring remain authoritative. Source presence and a successful bake do not establish route, original-rig, offline or native acceptance; those gates remain separate.

The first bounded actual-rig candidate was rejected: Pop stayed in Load, the direct Air board was too flat, and Catch/recovery too extended. One combined authored correction moves the first-rise marker earlier, gives the full board a steep intrinsic Air pass, and lowers Catch/recovery pelvis while preserving nose strike, sole/instep exits, roll/yaw winding, camera, source body and strict guards. Corrected actual-route/mesh review is still pending.

The body starts extending before the board snaps. A flat-board hold through 15478.5 preserves the visible source setup while retaining the earlier first-rise marker; nose pressure and both release clocks remain unchanged.

## Authored arm balance

The preserved actual-rig views exposed a long near-T at extension15478 and a high/crowded elbow at Catch/Land. An arm-only authored window now uses this take's own hip/facing basis for asymmetric forward/outward balance with bent elbows and hands below the shoulders. First-rise, pressure, steep Air and Catch/Land have their own targets; the overlay fades completely into the original arm recovery by15512. This reconstructs plausible arm depth and elevation instead of claiming measured monocular motion.

The own body file, source anatomy, head/torso, pelvis/knees/feet, board pitch/roll/yaw, contacts and event clocks remain untouched. In particular, firstPop15474.5, the flat-board hold15478.5, genuine nose/instep release, steep full flip, soft Catch and recovery are retained. Raw remains reviewed=false; fresh actual-runtime and visual review are still required for the new arm window.
