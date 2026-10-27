# One video-derived ollie

Motion/data: **CC BY-SA 3.0**, derived from [Medvedev's Ollie 1](https://commons.wikimedia.org/wiki/File:Ollie_1.ogv), recorded 16 June 2012. The video is not bundled. Attribution, source and licence must follow redistributed motion/data; these samples are not CC0. The existing separately authored rider mesh retains its own licence.

`frames.json` contains 25 samples from source frames 24 through 48 at 24 fps, a one-second, non-looping crouch/pop/tuck/landing/recovery clip. These are actual detected source frames, filtered once with `[1,2,1]/4`, then retargeted to the existing reference skeleton. In-between display frames interpolate those samples; interpolation does not create extra measured detail.

`calibration.json` records manually observed board endpoints, perspective assumptions and contact corrections. MediaPipe's hip-relative body output supplies no global jump path or skateboard landmarks. The board rise/pitch are approximate image-calibrated estimates. Fixed-length leg IK, deck contact and a front-foot side/instep roll are explicit retarget corrections. Fingers are not captured. Missing anticipation before the source interval is not fabricated.

The generated `SkateOllieCaptureData.cs` embeds the same samples for the measured native Humanoid replay. It is sampled only by the guarded rigging showcase; it does not replace normal riding or alter board/player physics. The existing connected Blender companion can visualize the motion, but is not the native Rust skin.

Rebuild after producing the retained MediaPipe image/world33 JSON:

```sh
python scripts/retarget_ollie_video.py \
  --pose-json /absolute/private/ollie1-mediapipe.json \
  --calibration assets/authored/animation/ollie-video/calibration.json \
  --output assets/authored/animation/ollie-video/frames.json \
  --csharp mods/rust/shared/SkateOllieCaptureData.cs
```

The converter requires Python and NumPy. Raw source frames, detector/model packages and run evidence stay outside the tracked product under ignored `context/`. Native playback and naturalness still need verification on the actual game skin.
