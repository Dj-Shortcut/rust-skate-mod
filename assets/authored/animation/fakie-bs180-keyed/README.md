# Own-source Fakie Backside 180 / Halfcab

This is the genuine plain **Halfcab** in the official
[Moose vs Bastien BATB6 game](https://www.youtube.com/watch?v=H4MEulcgUTc&t=32),
using one plaid-shirt / black-knitcap foreground actor. The source has a
backward approach, one coupled rider-and-board half-turn, no flip or separate
shove, and a landed forward roll. The independently inspected opening sequence
matches the dataset's Nollie BS180 / FS Halfcab / Halfcab pairs; the caption
is supporting evidence, not the only trick check.

The shared private 22–35 s download is 1.78 MiB. This take uses exact bounded
frames **324–379**; frame 380 cuts to the next actor and is excluded. The
stream is 30000/1001 fps, time base 1/30000, first PTS 990; selected PTS are
325314–380369. Estimated public times are 32.8108–34.64597 s. Original global
video frames/PTS are unknown after bounded transcoding.

## Anatomy and source limits

Canonical Regular anatomy stays in the **nose frame**: +Z nose, anatomical
Left toward the nose, Right on the -Z tail. Actual velocity is backwards and
the Right tail is travel-leading. The source is never rotated to the velocity
or relabelled as a normal BS180. Exact selection is Ollie, switch=true,
tailLeads=true, flip=0, shove=0, spin=-180, body=0.

The actual detector selected this actor in all 56 frames under unchanged
hip/height/foot-hint and continuity guards. No gaps were filled and no actor
fallback occurred. Low-confidence point lists remain explicit. In particular,
LeftWrist is unreliable in 30/56 frames and visibly collapses near the pelvis
in Air while the filmed arm is raised. Both balance-arm depth and directions
are reconstructed from the pixels with bent, outward arms. Head placement
uses anatomical skull-up and bounded gaze toward board/landing.

Own torso gesture remains source-derived. Camera/depth, complete scene hip
and board heading, fixed-chain legs, pelvis, original shoe/grip guide and
contacts are authored visual estimates. Full scene heading is removed once
from **every joint/toe, direction, sole frame and board transform**; the
actual `NoseYaw` supplies the only real half-turn and the normal-stance landing.
The outer upper front shoe guides the grip; there is no flip flick.

Pop 339, authored early tail strike 339.5, Right release 340.5, distinct later free-Air
pressure endpoint 345.5, Catch 350 and Land 356 are visual phase estimates.
The genuine footage ends before a long upright recovery. Timeline 379–406
is explicitly **authored planted upright recovery**, not future video frames;
the private comparison holds the last genuine source image during that suffix.
The board-height curve includes the original 0.092 m deck-origin ground
datum, so landed wheels remain above the riding floor through source recovery
and the ordinary-body handoff.
Reference pixels, raw detections and private fixtures remain untracked.

## Regenerate

```sh
python scripts/author_skate_fakie_bs180.py --output context/private/fakie-bs180
```

NumPy is required. The converter pins the own body and original geometry
subset, validates with the strict capture writer and replaces the raw pack
last. Raw output stays `reviewed=false`. Four full managed controller/body
routes pass normal and lower profiles in both stances, with the actual
**Fakie Backside 180** scorer. This checkpoint is not a visual verdict or a
native Rust/Shadow result. Claude owns the library and offline verdict.

## Original-rig review media

[Eight rig-only phase/contact sheets](../../../../docs/review-media/a3-fakie-bs180/README.md) and the private [source-comparison viewer](http://127.0.0.1:63761/a3-bs-halfcab-balance/) cover normal/lower × physical Regular/Goofy,1×/¼×, through actual Land and ordinary4 s recovery. Original skin, weights and board hierarchy are unchanged. Reference footage is held at the last genuine frame during the explicitly authored upright suffix. All raw output remains reviewed=false; Claude verdict and native Shadow validation remain separate.

## Early snap and level guide

The brief snap now precedes the main body turn and drops into a level front-bolt upper-shoe guide, with the tail sole already released. The front knee bends forward/down instead of perching over the nose. A small authored pressure extension rejoins the original accepted Air exactly; the own captured body, clock, ground datum, complete native heading authority and later Air/Catch/Land/recovery are retained. This is a source-presentation correction following the reviewed normal-BS control pattern, not a shared physics or camera change. The previous steep Pop/Guide media is superseded by the corrected source checkpoint and final runtime world-gaze fix.


Claude707 balance refinement: both arms use a bounded authored Guide–Catch correction in anatomical body axes, with bent elbows and hands at/below shoulder height. Only elbow/hand points change inside decoder 339.5..355.5; shoulders, head/torso/pelvis, shoes/board, source/native turn clock, snap/guide and later Land/recovery retain their accepted source paths. Current original-rig phase/contact sheets include all four same-camera Guide/Air/Catch before/after comparisons. All four actual routes and decoded-source 1×/¼× replays reach ordinary 4 s recovery; raw remains reviewed=false and Claude owns the offline verdict.
