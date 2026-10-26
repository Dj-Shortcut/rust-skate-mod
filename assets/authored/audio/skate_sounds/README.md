# Original skateboard soundpack

54 original synthesized WAVs: 18 exact stems, 3 deterministic variations each (`_01` to `_03`). All assets are 44,100 Hz, mono, signed 16-bit little-endian PCM. All seven loop stems are exactly 3 seconds. Measured WAV metadata, seeds, per-file SHA-256, actual LUFS/peak/DC/onset/tail metrics and loop points are in manifest.json. License: CC0-1.0; see LICENSE.txt and SOURCES.md.

| Stem | Type | Target integrated LUFS | Intended source |
|---|---|---:|---|
| skate_rolling_smooth | Loop | -20 | Smooth concrete, urethane wheels/bearings |
| skate_rolling_rough | Loop | -20 | Asphalt grain, wheel bumps, tiny stones |
| skate_rolling_wood | Loop | -20 | Hollow wooden platform and wheel seams |
| skate_grit | Loop | -28 | Quiet additive stone layer |
| skate_whirr | Loop | -32 | Very quiet additive free-bearing layer |
| skate_grind | Loop | -20 | Fine-grain steel truck friction |
| skate_deckslide | Loop | -20 | Waxed wood deck scrape |
| skate_push | One shot | -22 | Dry shoe contact/scuff |
| skate_ollie | One shot | -12 | Tail crack and grip scrape |
| skate_landing | One shot | -12 | Four-wheel flex, wood and bolt rattle |
| skate_drop | One shot | -9 | Heavy landing |
| skate_seam | One shot | -23 | Paired axle wheel clicks |
| skate_grind_in | One shot | -16 | Steel truck engagement |
| skate_grind_out | One shot | -19 | Short metal release/ring |
| skate_slide_in | One shot | -17 | Wood impact and wax scrape |
| skate_scrape | One shot | -19 | Tail stop scrape |
| skate_slam | One shot | -13 | Clothes/shoe/hand impacts, no voice |
| skate_bail | One shot | -17 | Rolling-away board and clatter |

The normal ollie and landing are -12 LUFS; drop is 3 LU louder, slam is 1 LU below landing, push is 10 LU below ollie. All targets are actual integrated post-PCM measurements with standard BS.1770 K weighting and 400ms gating blocks. They are not dBFS peak values. Grit/whirr intentionally sit below -20 LUFS so additive layers remain quiet instead of overpowering the wheels.

Every file has peak <=-1 dBFS, zero integer sample-sum DC and endpoint fades. One-shot energy begins within 20ms at the -60dBFS threshold and finishes with a natural faded tail. Loop points are sample0 through sample132300 exclusive; exact-zero endpoints and 3ms seam fades avoid joining clicks. Ten concatenated cycles and +40% playback-rate numerical join checks are recorded. Seeded irregular micro-events avoid a metronomic texture; use the three variations and crossfade surface changes. The normal authored loop rate corresponds to a medium 4m/s ride; a pitch/rate factor up to1.4 is the supplied numerical check. Fine-grain grind/deckslide loops can cover0.2-5s with independent engagement/release events and a short runtime fade.

`../Demo.wav` is a dry 30-second push/roll/ollie/landing/drop/grind/deckslide/scrape/slam/bail sequence using the delivered files. `../Demo.metrics.json` gives its timeline, exact duration, master attenuation and measurements. The demo applies one master attenuation to prevent mix clipping while preserving clip relationships.

Reproduction (from the parent soundpack folder, using the local venv):

```
.venv/bin/python generate_skate_sounds.py --output skate_sounds
.venv/bin/python generate_skate_sounds.py --output skate_sounds --verify-only
```

A failed format, loudness, peak, DC, onset, tail, seam, asset set, hash or demo check exits1. `../verification.json` is a fresh readback of the delivered PCM files. This is a standalone soundpack; no game/runtime integration or Shadow listening test is claimed. Timbral realism, perceived repetition and final in-game balance need listening review.
