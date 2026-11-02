# Next shared-source candidates

Codex source search for #584, 1 November 2026. Claude owns the
[animation-library record](RUST-SKATE-ANIMATION-LIBRARY.md); this is a candidate
index, not a coverage update, motion pack or approval. No `reviewed` flag changes.

## What is actually cached

| Video | Local availability inspected | Search limit |
| --- | --- | --- |
| [`_oRoo1x1uzM`, Griffin/Peterson final](https://www.youtube.com/watch?v=_oRoo1x1uzM) | Short windows 136–158, 160–176, 188–212, 240–260, 318–344 and 480–492 s; metadata for the 712 s video | The full match is **not** cached. The available windows cannot establish that a missing trick never occurs elsewhere. |
| [`PqJZcwTY1QE`, Huston/Rodriguez](https://www.youtube.com/watch?v=PqJZcwTY1QE) | Full 635.068 s, 1280×720, 30000/1001 fps. Chapter index plus a manual two-second on-screen-label scan from 70 through 634 s; denser full-rider frames around the selected attempts below | A label scan locates candidates; only the dense attempt windows below were checked for board action, landing and feet. |
| [`YcmPDIj2BFo`, Cole/Kroetkov](https://www.youtube.com/watch?v=YcmPDIj2BFo) | Overlapping windows 114–130, 124–140, 136–152 and 148–158 s; metadata for the 423 s video | The full match is **not** cached. No further footage was downloaded. |

The reference windows remain private. Reusing the same file avoids another
copy of it. Times below use the cached file's frame clock; cut-window offsets
are explicit, and frame numbers are zero-based. The 30000/1001 clock is not an
integer 30 fps clock.

## Candidates in the owner's order

| Requested slot | Attempt window / filmed stance | Landed and actual board action | Camera and feet at pop / catch | Decision |
| --- | --- | --- | --- | --- |
| Normal FS pop shove-it | [Kroetkov, 130.340–133.209 s](https://www.youtube.com/watch?v=YcmPDIj2BFo&t=130), local frames **190–276** of the **124 s** window; physical **Goofy**, right foot leading | **Yes**: rear pop, frontside half-turn of the deck with no deck flip or matching body half-turn, both feet return and continuous roll-away | Tracking front three-quarter into side/rear three-quarter. Both shoes visible around pop 209–211; the turning deck edge, pan blur and changing perspective limit sole/depth measurement. Both shoes visible approaching catch around 220–225; no fully hidden flick foot substituted | **Preferred next source.** Own continuous take and existing raw detections; not FS360 played at half rotation. Reconstruct uncertain depth explicitly. |
| Normal inward heelflip | No verified normal take in the cached windows | The `PqJZcwTY1QE` labels around 158 s and 556 s are **Nollie Inward Heelflip** and **Switch Inward Heelflip**; they do not fill the normal slot | Normal pop/catch visibility not established | **Still needs a normal take.** Do not silently remove the stance modifier or substitute the final's Varial Heelflip. |
| Standalone BS 180 body | No verified standalone take in the cached windows | The 96–100 s `PqJZcwTY1QE` label is **Backside 180 Kickflip**; the final's 153 s sequence is **Backside Bigspin** | Pure BS180 pop/catch visibility not established | **Still needs a standalone body reference.** Existing combined Nollie BS180 Heel is not this slot. |
| Standalone FS 180 body | No verified standalone take in the cached windows | `PqJZcwTY1QE` around 242–246 s says **Frontside 180 Kickflip**; final 174 s says **Frontside Bigspin** | Pure FS180 pop/catch visibility not established | **Still needs a standalone body reference.** The unreviewed reconstruction under FS360 does not verify a filmed FS180. |
| Nollie kickflip | [Rodriguez, 189.456–191.591 s](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=189), full-file frames **5678–5742**; physical **Goofy**, right foot leading/nose-pop | **Yes**: nose presses down around 5702–5706, rear foot releases the single longitudinal deck flip, both feet catch and roll away; no body half-turn | Tracking three-quarter view with both shoes in frame. Near nose-pop foot is visible; brief ankle/deck overlap limits contact depth. Both shoes and the returning grip are visible around 5718–5722 | **Selected candidate.** Full setup, pop, flip, catch and recovery available. No raw detection extracted for this new selection yet. |
| Switch kickflip | [Rodriguez, 182.516–184.651 s](https://www.youtube.com/watch?v=PqJZcwTY1QE&t=182), full-file frames **5470–5534**; physical **Regular**, left foot leading, Switch execution | **Yes**: rear/tail pop around 5484–5486, single longitudinal deck flip and stable landing around 5502–5506, followed by uninterrupted roll-away | Tracking three-quarter, not a calibrated orthographic side. Front/flick shoe visible leaving the deck; rear ankle partly overlaps the rising deck. Both shoes visible on return; fine sole/deck separation remains uncertain | **Selected candidate.** Stance-specific pop/flick, not a mirrored ordinary Kickflip. No raw detection extracted for this new selection yet. |
| Fakie heelflip | No verified pure take in the cached windows | `PqJZcwTY1QE` around 176 s and 372–390 s says **Fakie Bigspin Heelflip**, not pure Fakie Heelflip | Pure Fakie Heel pop/catch visibility not established | **Still needs a pure take.** Combination timing and scoop cannot be passed off as a standalone flip. |

The selected stance is the visible physical leading foot plus the trick's
pop direction, not a request to mirror the source video. These are manually
inspected monocular references, not measured 3D motion. Side-to-side ankle,
hand and board depth still need declared reconstruction and mesh review.

### Alternatives and rejected leads

- **Nollie Kickflip, Huston:** `PqJZcwTY1QE` full-file frames **5774–5834**
  (192.659–194.661 s), physical Regular/left-leading. A genuine nose pop,
  single flip and landed roll-away are visible. Both feet are visible at pop
  and catch; the three-quarter camera pans and briefly crops the approach
  before 5774. Keep as an independent alternate, not an automatic stance mirror.
- **Switch Kickflip, Huston:** the following attempt around 185–188 s is
  visible in the same cached file. It remains an alternative lead; its
  frame-by-frame contact/stance calibration was not completed in this pass.
- **Nollie Kickflip, Peterson:** `_oRoo1x1uzM` local frames 260–360 of the
  318 s window (326.675–330.012 s) show a real attempt but **not a successful
  recovery**: brief board contact, then the front foot steps off/run-out around
  328.9–329.5 s. Rejected as a complete landed source, even though the label is
  correct. Camera changes to the opponent after the bail.
- **Varial Heelflip:** `_oRoo1x1uzM` around 164–169 s shows a shove-plus-heel
  combination. It is neither a normal Inward Heel nor a Varial Kickflip.
  The previous suggestion associating this 2:47 take with `PqJZcwTY1QE` used
  the wrong video ID; keep it attached to the final.

## Chapter labels are search hints

The full `PqJZcwTY1QE` cache exposes two useful gaps in its chapter metadata:

- The metadata **Switch Kickflip, 183–203 s** interval also contains the
  separate **Nollie Kickflip** attempts at about 190 and 193 s, then a normal
  **360 Flip** around 197–201 s. The dense source frames, rather than the chapter
  title, establish the selected families above.
- The broad metadata intervals also omit the on-screen **BS180 Kickflip**
  around 96 s, **Bigspin Kickflip** around 142 s, **Varial Heelflip** around
  222 s, **FS180 Kickflip** around 242 s and **Nollie Varial Kickflip** around
  410 s. None has been promoted to a completed source by this index.

A two-second label scan can miss an unlabelled or brief attempt. A missing
candidate here therefore means **not verified in the inspected cache**, not
proof that the public match contains no such trick. The next search should
resolve the remaining normal Inward Heel, pure BS/FS180 and Fakie Heel windows;
no combination or different stance fills those cells by name alone.

## Private reproducibility and remaining gates

- `PqJZcwTY1QE` full-cache SHA-256:
  `531eb85fd1d292c229836f6eedc4adec422aaf6dd644040c49c7480b2beadad7`.
- `YcmPDIj2BFo` 124–140 s cache SHA-256:
  `038fa40ab62d32d05967971e600a3e9b6ce5075593f66f3969a8b0c9fab6e70d`.
- FS Pop selected raw-pose SHA-256:
  `3fea1426df9b4c428bee838891bbd2a2ce4f7cffc8b2d0e0d6de06e2d697e26a`.
  Existing raw job uses frames 190–276 and retains its original detections;
  the source pack remains unreviewed.
- Private review sheets include the continuous source-window overview and
  denser Pop/Air/Catch/Land samples for the three selected/alternate takes.
  No video, frame image, detector dump or generated pack is committed here.
- These are The Berrics references used privately under the owner's chosen
  workflow; no CC0 licence or redistribution permission is asserted. The
  Swinburne annotations are a search index, not footage, timestamps or motion.
- Next: extract the two new kickflip windows, author each family's own contacts
  and timing, replay normal/low in both physical stances on the actual board,
  then obtain Claude's visual decision. Source selection does not change an
  animation-library cell to offline reviewed or native accepted.
