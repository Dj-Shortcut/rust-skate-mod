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

Pop 339, tail strike 341.5, Right release 343.5, distinct later free-Air
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
