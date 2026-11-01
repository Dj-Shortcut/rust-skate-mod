# Plain normal BS180 source controls (unreviewed)

The own bounded reference is Wes Kremer's black SK8MAFIA hoodie/cap take in
[The Berrics, Ishod Wair Vs Wes Kremer: BATB6 – Round 1](https://www.youtube.com/watch?v=i8jPt6jHk98&t=44).
He is filmed Goofy, pops on level concrete, turns his body and board together
without a deck flip, lands Switch and rolls away. This is a plain BS180,
not a BS180 flip or a shove-it relabelled as a body turn.

The private source is the 43–58-second bounded cut, 30000/1001 fps with
1/30000 time base. Selected local frames18–78 have exact bounded-file PTS
18018–78078. The original video's global frame/PTS is not known. The selected
window ends before the hard cut to the other skater around local frame80.
Pop47, Catch58 and Land60 are visual estimates, not measured contact times.
The official attempt table's BATB6 opening BS180 pair is zero-based
rows10211/10212; that table has no video URLs or timestamps. The actual take
was separately checked in the official video. Source footage, pixels,
detector binaries and raw landmark arrays remain private and are not shipped
or relicensed with these original animation controls.

## Reconstruction and one-turn authority

`captured-body.json` contains this take's own compact torso-local arm-direction
estimates, selected identity61/61 frames. Filmed Goofy is reflected and its
anatomical sides are swapped exactly once before storing these directions.
The controls contain no world or board heading. Unreliable wrist frames are
listed and filled from bounded trustworthy neighbors; the declared authored
balance prior supplies bent asymmetric arms during the cropped interval.

The top of the head and raised hands partly leave the image around frames51–57.
Head/gaze, body windup, depth, root, hips, fixed-length legs, shoes and contacts
are explicitly reconstructed. A panning three-quarter/side camera and foot
overlap prevent a claim of calibrated or fully observed 3D motion. Source
presence is not visual approval.

The baker reconstructs full source-world positions, directions, sole frames
and board rotation, including the declared source heading. It then removes
that same board heading once from **all** joints (including toes), directions,
sole frames, board origin and board quaternion. This preserves relative
shoulder/hip windup. The actual native `NoseYaw`, including `SpinYaw`, owns the
one live body-and-board half-turn; the source cannot add another turn or
cancel it like a shove. The exact native signature is normal Ollie,
Flip0/Shove0/Spin−180/Body0.

The rear shoe starts beyond the rear bolts on the tail. Front sole release47.5
precedes the noseward outer-upper griptape guide; that guide is not a sole
contact. Right/rear release50 is followed by a distinct free-Air/press-end52.5.
A modest authored nose-up carry persists through54 and levels byCatch58;
free shoes follow that pitch with a small clearance. An original curved deck
and actual tapered shoe geometry supply the contact
frames. There is no off-edge flick or airborne flip. A compact, bent pressure
endpoint preserves the strict fixed-chain reach and floor checks. Reference
ballistic height is removed once; live physics owns flight, turning, landing
and scoring. Ordinary recovery remains the normal route after actual Land.

## Reproduce

```sh
python3 scripts/author_skate_bs180.py --output context/artifacts/bs180-own-source
```

NumPy is required. Inputs and original geometry are hash-pinned. Generated
JSON and SKMC stay outside the committed source directory; pack publication
is the final atomic replacement. Raw metadata remains `reviewed=false`.

The controlled actual Pad/full32Hz Step/Land/scorer replay selects Backside180
and the exact source route in normal and low flights, Regular and Goofy.
The 60Hz body replay retains every airborne frame and reaches ordinary
Ground at four seconds. Those managed checks do not constitute native Rust,
Shadow or Claude's offline visual approval.
