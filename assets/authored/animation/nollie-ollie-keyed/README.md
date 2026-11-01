# Own normal Nollie source controls (unreviewed)

The reference is the black-cap, black-shirt, grey-shorts Regular rider's
genuine plain Nollie in [The Berrics BATB12, Spencer Barton versus Tony Mangucci](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=68).
The Left burgundy-sock foot presses the leading nose; the Right striped-sock
foot guides the tail upward. The board rolls forward, does not flip or shove,
and lands on the same level concrete. This is neither Fakie nor a relabelled
tail-pop Ollie. The source was inspected through approach, extension, nose
pressure, Air, landing and upright rollaway.

The private bounded64–78s file is1280×720 at30000/1001 fps, time base1/30000,
first PTS990. The selected decoder frames95–175 have exact container PTS
96085–176165. Original global video frame indices/PTS are unknown. The next
actor shot begins at176 and is excluded. No source pixels, sound, raw model
arrays or detector binaries are published or relicensed.

## Actual observations and declared reconstruction

`captured-body.json` contains this take's compact hip-relative body controls,
source Regular with no reflection or anatomical swap. One explicitly authored
sparse panning-camera basis transforms every source joint/direction together;
there is no per-frame hip-heading cancellation or source trajectory. Monocular
depth, canonical fixed chains, pelvis, exact contacts, gaze and recovery are
reconstructed. Confidence is not proof of correct anatomy or visible contact.

The full-frame detector selected the declared foreground actor80/81 times.
Raw142 remains an ambiguous duplicate-hypothesis/no-pose gap. Its **authored
control row**, not raw detection, uses neighboring verified frames; the source
gap and reconstruction are listed in both controls and inspection metadata.
The narrow lower-body depth at143 is separately reconstructed from140/144.
The alternate ROI extraction is rejected because it swaps anatomical sides
at146/147; no ROI observations are silently merged into this take.

## Nose pressure and ordinary integration

Canonical +Z is the nose. Left starts beyond the nose bolts and supplies the
nose-down pressure; Right starts near the rear bolts and uses its outer upper
to drag tailward along the grip. That guide is neither a sole contact nor an
off-edge flick. Actual original tapered shoe/deck geometry supplies both sole
and upper contact frames. Source knee gestures remain the basis for fixed-chain
reconstruction, with individually authored free-foot paths and real surface
clearance. Head/gaze are reconstructed toward board/landing; the source's
settled recovery is blended toward one stable165 pose.

| Local source frame | Stage | Sole-support mask |
|---|---|---|
|95–129|Own approach and load|Both|
|131|Extension / Pop starts|Both|
|133|Right sole releases into its upper guide|Left only|
|134.5|Estimated nose strike|Left only|
|137.5|Left nose sole releases|None, still Pop|
|138.5|Right upper guide releases|None|
|140|Distinct later free-Air / pressure endpoint|None, Air|
|144|Both-foot catch; first-foot order unresolved|Both|
|146|Estimated actual landing witness|Both|
|165–175|Settled upright recovery|Both|

The strict keyed family parser must use **Left** as the popping sole and the
take's distinct release→free-Air interval. It must not substitute the earlier
Right sole release or copy another clip's frame constants. A compact pressure
endpoint preserves the existing reach checks. The whole original thirteen-part
board support hull preserves the floor envelope. There is one removed
reference ballistic carrier and one added live physical carrier; physics,
landing and scoring remain authoritative.

The exact route is `nollie-ollie-source-keyed-v1`, movement kind `nollie-ollie`,
signature Nollie / normal / not Fakie / Flip0 / Shove0 / Spin0 / Body0.
The controlled actual Pad/full32Hz Step/Land/scorer replay returns Nollie in
normal/low and both native stances;60Hz body capture retains every airborne
frame and reaches ordinary Ground at4s. Native Rust/Shadow and Claude's
offline judgement remain separate gates. Raw `reviewed=false` is retained.

Generate using Python and NumPy:

```sh
python3 scripts/author_skate_nollie_ollie.py --output context/artifacts/nollie-ollie
```

Inputs and original geometry are pinned. Generated JSON/pack belong outside
the source controls directory; the pack is replaced last. No approval marker
is generated automatically.
