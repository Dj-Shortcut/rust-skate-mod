# Nollie BS Pop Shove-it — own white-shirt BATB13 take

This unreviewed candidate uses its own continuous white-shirt/dark-trousers
actor from [The Berrics' BATB13](https://www.youtube.com/watch?v=tL8Ey9GS0xQ&t=195),
the landed Nollie Backside Shuvit at row1088 of the matched attempt sequence.
The board makes a half-turn with grip up; the body does not make a half-turn
and neither foot performs a flip flick. This is not a relabelled Halfcab or flip.

The cached181–219s excerpt has actual PTS1001×frame at timebase1/30000 and
30000/1001fps. The selected actor occupies bounded434–537; the next actor cut538
is excluded. The shorter library trim460–525 retains load, nose pop, Air,
catch, bent landing and the same filmed upright rollaway. Its public window is
approximately196.349–198.518s; original global frame numbers are unknown.

| Event | Bounded frame | Contact reconstruction |
| --- | ---: | --- |
| Load |473|Both soles, anatomical Left on nose |
| Rear-foot lift |478.5|Left nose sole remains |
| Pop |479|Left nose pressure/scoop; no instep flick |
| Nose release |480|Neither sole |
| Distinct free Air |482|Neither sole; pressure blend ends |
| First catch |491|Left; hidden sole contact estimated |
| Both catch |493|Both |
| Land |494|Bent legs; real game landing remains authoritative |
| Recovery |511–525|Same filmed take, settled ordinary stance |

Base Regular stance and anatomical Left/nose role are a **manual inference**
cross-checked against the same actor's Nollie/Switch and opposite Frontside
shoves. The excerpt lacks a complete plain-normal push/pop baseline. Camera
pan/depth and absolute board/body heading are not calibrated. Feet overlap
around486–490 and exact sole contact is hidden: reconstruction is labelled,
not increased confidence or measured contact.

Private strict VIDEO extraction selected104/104 frames with no gaps or actor
fallback. The checked-in body controls reconstruct this take's torso/arm
directions. They preserve explicit low-confidence point lists; LeftWrist474–489
and RightWrist504–529 are unreliable. Small body windup, bent balance arms,
Both balance elbows bend through Pop/Air and the hands settle by catch;
monocular arm depth is reconstructed even where a2D landmark is confident.
Skull-up/gaze, lower-body depth, nose pressure, board yaw/pitch and shaped-deck
contacts are authored from the visible take. No other trick's body is read.

Only geometry/interpolation/fixed-chain math is reused. Original shoe, curved
deck, skin and complete13-part support geometry remain unchanged. The free
shoes keep the body's heading while one local board-only yaw reverses deck-local
front/back; catch contact never rotates the rider180°. A single reference
carrier is removed from the own board/pose origin; the live carrier supplies
actual airtime/travel once. Ground deck-origin datum remains0.092m.

```sh
python scripts/author_skate_nollie_bs_pop_shove.py \
  --output context/artifacts/nollie-bs-pop-shove-source
```

The baker verifies body and geometry hashes, requires the exact Nollie /
normal / nose-leading / flip0 / shove−180 / spin0 / body0 identity, and refuses
a longitudinal board flip. Raw output stays `reviewed=false`. Strict encoding
finishes before output mutation; temporary files are atomically replaced.
No footage, raw detections, game assets or generated pack is redistributed.
Private test-only approval is not an installable or reviewed source pack.

Actual normal/low routes in physical Regular/Goofy, original-mesh phase/contact
sheets and1×/¼× decoded-source replay evidence are delivered with the integrated
PR. Claude owns the independent offline verdict. Shadow/native gameplay
verification remains separate.

## Mid-turn levelling candidate

Claude's #706 follow-up found that the authored mid-turn pitch became steeper than the nose pop. The nose-pressure stroke through release 480 stays unchanged; the released board now tapers monotonically from 32° toward 26° at 482, 20° at 486 and level at catch 491. These are authored visual-fit controls, not a source-camera or measured-angle correction. Board-only yaw, body, contact/event timing, native flight and strict reach/shoe/floor guards stay fixed. Updated paired Source/Side before/after, eight phase/contact sheets and four actual managed/viewer replays are in `docs/review-media/a4-nollie-bs-pop-shove/`. The preceding source-bearing #711 was Claude-offline-reviewed; this requested levelling follow-up awaits his verdict. The raw pack remains reviewed=false and native Shadow testing remains open.
