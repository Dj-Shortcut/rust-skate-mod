# Own-source Switch Kickflip reconstruction

`captured-body.json` contains 65 hip-relative direction-control rows from Paul
Rodriguez's landed Switch Kickflip in the existing Berrics reference
[PqJZcwTY1QE, frames 5470–5534](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=182).
His filmed switch setup is physical Regular, left foot leading. The mirrored
counterpart is generated anatomically; reference pixels are never mirrored.
This is an independent take, not the normal Kickflip mirrored or relabelled.

The controls are original project reconstruction data. Reference footage,
MediaPipe/model binaries and source pixels are not included or relicensed.
Monocular depth is estimated. In particular, the far left elbow/wrist have only
11/12 trusted frames and the far left knee 22 of 65. Retained model directions
remain estimates. The recipe explicitly reconstructs pelvis placement, knee
planes, the hidden arm, board trajectory, shoe contacts and side-instep flick.
It retains the source torso's full camera-axis fold rather than projecting it
onto one horizontal axis. These edits are authored, not measured 3D motion.

The earlier generic retarget's two root-registration failures remain in private
quarantine. Its shared 0.5 m reconstruction guard was not changed. This baker
uses its own declared pelvis/contact authoring and strict fixed-length IK; it
does not claim that the earlier failed retarget passed.

Generate with Python and NumPy:

```sh
python3 scripts/author_skate_switch_kickflip.py --output <private-output>
```

The body input is SHA-256 pinned by `constraints.json`. The baker writes one
unreviewed 267-pose, 9-event SKMCAP01 clip and an inspection JSON. The validated
pack is replaced last; input/geometry failures leave an earlier pack intact.
The default output contains no automatic approval marker. Source presence,
offline visual review and native game approval are separate gates.

| Source frame | Action / contact meaning |
|---|---|
| 5470–5482 | Own load: front foot near mid-board, rear foot beyond the bolts on the tail |
| 5482 | Pop clock begins with body extension; both soles initially supported |
| 5484.7 | Front sole banks and its **sole-support bit clears** |
| 5485.6 | Authored tail-pressure witness on the actual curved board support hull |
| 5486.2 | Front shoe's **side/upper instep**, not its sole, finishes the flick |
| 5487.8 / 5489 | Rear sole releases; later free-Air witness ends pressure compensation |
| 5493–5495 | Compact own-source air fold; free feet clear the rotating deck edge |
| 5498 / 5500 | Front catch, then both soles |
| 5503 | Observed landing window, bent knees |
| 5520–5534 | Recovery toward ordinary riding |

Subframe contacts and source root/depth are explicit estimates. One declared
reference upright ballistic carrier is removed from joints and board; the
runtime adds its actual physical carrier once. Native input, rotation debt,
scoring and landing remain authoritative.

The exact presentation identity is `switch-kickflip-source-keyed-v1`, movement
kind `switch-kickflip`, signature Ollie / Switch / not Fakie / +360 flip / no
shove or body spin. Shared runtime integration is reviewed separately.
