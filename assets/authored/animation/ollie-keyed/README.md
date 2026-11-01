# Rolling Ollie: independent source and original rig reconstruction

This is the normal, rolling Ollie from [The Berrics: Spencer Barton vs. Tony Mangucci](https://www.youtube.com/watch?v=P4CflPS6YwQ&t=60). The black-cap, gray-shorts rider rolls on the same level concrete from setup to an upright roll-away. There is no curb, gap or stationary take. The filmed stance is regular; anatomical reflection supplies the opposite stance. No other trick's body capture is loaded.

Only original fixed-rig controls are published. Footage, audio, raw landmark arrays and third-party model weights remain private. No permission to redistribute or relicense The Berrics footage is asserted.

## Timing and reconstruction

The selected 70 frames are decoder frames 158 through 227 of a bounded, transcoded 55–64-second preview. Its SHA256 is in the controls. These are **not original global video frame numbers**. Integer frame PTS were read back: time base 1/30000, first container PTS 990, subsequent frame increments 1001. Selected PTS 159148 through 228217 give 2.3023 seconds; requested-cut-plus-normalized-PTS estimates the public window as 60.271933–62.574233 seconds. Original full-video hash and global PTS are unknown. The following actor cut starts at bounded frame 229 and is excluded.

All 70 frames retain the same explicitly selected foreground person. Low-visibility model estimates remain listed in `captured-body.json`; confidence was not increased. A declared, authored camera-heading basis transforms all own source joint and direction vectors together. It removes the camera's changing heading once. There is no authored board yaw, shove, flip or extra native carrier rotation.

The source supplies its own torso and balance-arm gesture. Camera depth, neutral-roll head gaze toward the board/landing, fixed canonical lengths, pelvis height, leg planes and all shoe/deck contacts are explicitly reconstructed. Brief foot/deck overlap and motion blur prevent measured shoe-roll claims.

| Phase | Bounded source frame | Contact |
| --- | ---: | --- |
| Load | 158–170 | Front foot near board middle; rear foot beyond the tail bolts |
| Pop | 171 | Extension; both soles initially supported |
| Front sole release | 173 | Front outer upper begins guiding along griptape toward the nose |
| Tail strike | 176 | Rear sole presses the actual kicked tail |
| Rear sole release | 178.4 | Tail foot unambiguously released |
| Free Air | 180 | Separate event after release; both feet free, compact knees |
| Settling catch | 187–188 | Grip stays up; feet settle over both trucks |
| Land | 191 | Source estimate only; actual game landing leads |
| Upright recovery | 213–227 | Same take's quiet upright roll-away |

The front guide uses the frozen original shoe's dark outer-upper surface, not the outsole, and follows the original concave/kicked deck. No kickflip flick is added. A short comfortably bent pressure endpoint lets the native entry reach the still-supported tail foot without relaxing the measured fitter. The native carrier supplies translation and flight once; the reference pressure arc is subtracted only for contact reconstruction.

## Reproduce and status

```sh
python scripts/author_skate_ollie.py --output context/ollie-source
```

The baker verifies its body and original board/shoe hashes before writing one SKMCAP pack. It supersamples interpolation; this does not create additional observed frames. Its pack metadata remains `reviewed=false`.

Offline managed replay: pure Ollie controller input, full 32 Hz Step/Land/scoring, 60 Hz body sampling, original shaped-board contact fitting. Both stances and normal/low charge complete without capture fallback: 36/36 active Air frames for normal, 34/34 for low; observed flights are 0.59375 and 0.5625 seconds. This is controlled managed execution, not Unity/IL2CPP or Shadow verification. Source presence and successful export do not grant visual approval. Claude owns the library's offline verdict; Shadow remains separate.
