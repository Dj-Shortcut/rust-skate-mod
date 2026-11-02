# Selected WAV skateboard audio

Since client source `0.18.61`, the loader uses **18 selected WAV sounds**, one `_01.wav` per
sound type, from `Path.Combine(BepInEx.Paths.PluginPath, "skate_sounds")`. In an
active Rust loader this is `BepInEx/plugins/skate_sounds/`. The public source does
not include the replacement sounds. The historical `SkateAudio` and
`SkateFoley` synthesis APIs remain available, but the current `SkateSfx` playback
path does not call them and has no synthetic fallback.

The [sound brief](RUST-SKATE-SOUND-BRIEF.md) defines the intended sounds and
mix. Their presence, installation and heard quality are separate gates from the
WAV loader source. A source compile is not a successful Shadow installation or
playback test.

## Files and event routing

Every stem below requires the exact suffix `_01.wav`; `_02` and `_03` are not
loaded. There are seven continuous channels and eleven one-shot types.

| File stem | Playback and trigger |
| --- | --- |
| `skate_rolling_smooth` | Loop: hard ground and construction surfaces |
| `skate_rolling_rough` | Loop: terrain/dirt rolling bed |
| `skate_rolling_wood` | Loop: recognized wooden riding panels |
| `skate_grit` | Loop: extra terrain/dirt layer |
| `skate_whirr` | Loop: airborne wheels, fading as they slow |
| `skate_push` | One-shot: push counter advances |
| `skate_ollie` | One-shot: pop counter advances |
| `skate_landing` | One-shot: ordinary landing |
| `skate_drop` | One-shot: landing with `LandImpact >= 7.5` |
| `skate_seam` | One-shot: step-up, or periodic construction seams |
| `skate_grind_in` | One-shot: truck-grind entry |
| `skate_grind` | Loop: moving truck grind |
| `skate_grind_out` | One-shot: grind/slide ends without a simultaneous pop |
| `skate_slide_in` | One-shot: deck-slide entry |
| `skate_deckslide` | Loop: moving deck slide or ground powerslide |
| `skate_scrape` | One-shot: revert or beginning of a powerslide |
| `skate_slam` | One-shot: wall slam or bail |
| `skate_bail` | One-shot: escaping-board sound when a bail begins |

A bail emits one body-impact and one escaping-board clip; a simultaneous wall
slam does not add a duplicate body-impact clip. The heavier drop replaces the
ordinary landing clip for that event. Mounting or ordinary dismounting does not
play a drop sound.

Surface selection retains the existing nearest-contact ray and recognized
park-collider lookup. Hard/construction uses the smooth bed, wood uses its own
bed, and terrain/dirt combines rough rolling with grit. The rough recording does
not prove native asphalt recognition. Unclassified objects remain hard ground;
a default-layer hit alone does not imply wood. Speed affects gain and pitch,
and loop gains crossfade. This remains a coarse substrate mapping, not native
per-triangle material recognition.

Playback uses local **3D sources** (`spatialBlend = 1`), with linear attenuation
from 1 to 25 metres. Seven loop channels follow the current board position.
One-shots use eight cached voices and retain their event positions as the rider
moves away: board contacts originate at the board, push at the foot, and body
impact near the rider. An idle voice is reused first; a full pool replaces its
oldest tail. Source creation stays in off-board preparation, with one source per
preparation unit. Native source settings are read back before readiness is logged.

Doppler is disabled because the existing speed/airborne logic already controls
pitch. The listener/mute state is unchanged. These are local emitters; observer
replication, positional multiplayer events and server audio are separate work.
Native attenuation, event timing and mix balance still need in-game listening.

## Loading, failure and lifecycle

The runtime reader accepts little-endian RIFF/WAVE, PCM16 mono at 44,100 Hz. It
checks chunk boundaries, format, duration and sample counts. Durations must be
at least 20 ms and at most four seconds for a loop or six seconds for a one-shot.
The production brief asks for two-to-four-second loops; the reader's wider
structural bound does not certify loudness, loop joins or recording quality.

Off-board setup separates file opening, clip creation, bounded PCM decoding and
sample upload into preparation units. Files are cached before a ride; events do
not reread WAVs. Native clip creation/upload remains an uninterruptible engine
call, so cooperative preparation is not a hard frame-time guarantee.

A missing, corrupt or unsupported required file, upload error or playback error
latches the optional sound subsystem unavailable. It stops its sources and attempts to release owned clips and file readers. An
English HUD notice points to `skate.log`, and the rider can continue without
skateboard audio. There is no partial-pack playback or automatic synthetic
fallback. Fixing files does not hot-reload a failed cache; restart the client or
reinitialize the rig after correcting the pack.

Disabling the rig stops/mutes its sources and synchronizes event counters. A
complete cache can resume; unfinished decoding is disposed and restarted during
later preparation. Destruction releases sources, clips and readers. Audio
cleanup failure remains an explicit diagnostic rather than a successful-cleanup
claim.

## Packaging, rights and verification

The client tools default to `sounds\` in the owner's staging folder
(`Downloads\claude-loader-probe\sounds\`, never committed). For an existing private
folder elsewhere, set `$psoundpath` before `zz launcher`: it validates the eighteen
named files before applying the install and retains only a local `sounds-source.txt`
pointer. Later installs preserve that pointer when no override is requested.
`session.ps1` copies only those eighteen files into `BepInEx\plugins\skate_sounds`
for a session; the loader folder is removed with the rest during cleanup. Staging
checks readable, nonempty files; the client performs the WAV-format checks.
`pack.ps1` includes the same eighteen WAVs in a portable `sounds\`, without the
owner's pointer or private path. Applying a valid friends' pack selects its own
sounds instead of an old local pointer ([tools](../mods/rust/client/tools/README.md#settings),
[FRIEND.md](../mods/rust/client/FRIEND.md)). On 26 October the owner's private staging
adapter installed the selected pack and native client 0.18.62 reported all 18 WAVs
ready at 44100 Hz. The public session/launcher/pack pipeline still requires its own
Windows check; that earlier private installation does not validate the new scripts.

The chosen ElevenLabs-generated sounds and raw downloads stay outside public GitHub.
They are not relicensed as CC0. Keep provenance and applicable usage terms with
the private pack, as described in the sound brief. The historical procedural API
and its separately authored assets retain their own existing licences.

The owner authorized shortening the four late onsets and dynamics processing.
The current private preparation retains the original provider WAVs, all seven
validated loops and every one-shot's complete tail duration. Ollie, Scrape, Slam
and Bail lose only their measured quiet preroll, retaining five milliseconds
before the prior measured onset. Disclosed lookahead peak compression and a final
50 ms end fade provide safe PCM output; there is no voice or watermark processing.

The private completion candidate dated 29 October 2026 passes all 18 format,
peak/clipping, DC, onset/tail, repeated loop-join and absolute-LUFS targets within
the original 0.5 LU tolerance. Landing is −12 LUFS, Drop −9 and Slam −13 after a
bounded additional gain/lookahead-limiter stage. Their onset prefixes and last
50 ms remain PCM-identical. Ollie received only −1.084 dB uniform attenuation;
no duration, tail, target or tolerance was changed. These four replacement WAVs
and their provider provenance remain private; the other fourteen are unchanged.
Drop is 3 LU above Landing, Slam 1 LU below it and the unchanged Push is 10.396 LU
below the corrected Ollie. Processing and a technical pass do not establish
natural-sounding dynamics or an in-game mix.

### Reproduce the selected-pack loudness gate

[`measure_skate_sound_loudness.py`](../scripts/measure_skate_sound_loudness.py)
is a read-only validator for the eighteen fixed `_01.wav` game filenames. It
reuses the existing catalog's targets, reports missing/invalid/short/silent
inputs and fails on target misses; it never normalizes, pads, trims or writes
WAVs. A `demo.wav` in the folder is ignored and cannot replace a required game
sound. Dependencies are NumPy, SciPy and pyloudnorm (validated with 0.2.0).

```sh
python scripts/measure_skate_sound_loudness.py /path/to/skate_sounds --ffmpeg ffmpeg > loudness-report.json
```

The corrected meter analyzes only complete 400 ms windows with 100 ms hops.
Pinned pyloudnorm 0.2.0 rounds its block count and may include a truncated final
window while still dividing by 400 ms. The original 0.580-second Ollie therefore
read −12.397 LUFS in the raw meter but approximately −10.916 in complete-block
analysis and −10.90 in FFmpeg. The validator limits the **analysis view** to the
last complete window end; every audio sample remains in the file. It retains
raw pyloudnorm values as diagnostics, including any raw-meter disagreement.
Inputs shorter than a complete window are rejected rather than silently padded.

With `--ffmpeg`, independent input LUFS, true peak and agreement within 0.1 LU
are required gates; failure or a missing executable fails the run. On the private
completion candidate all eighteen targets pass both meters, with maximum
agreement error 0.041 LU, and all independent true peaks stay below −1 dBFS.
Without that argument, the report explicitly says `INDEPENDENT_NOT_RUN`; a local
LUFS pass does not claim independent validation. This command checks loudness,
format and sample peaks, not onset, DC, tails or loop seams; the candidate's
separate private preparation/reader checks cover those technical gates.

The actual client reader decoded the final pack with float samples matching the
PCM bytes. Private source fixtures cover moving loop origins, independent impact
tails, bounded voice reuse and lifecycle/failure handling. A cached-reference
compile covers the complete current client source. These checks do not prove
native spatial playback or subjective audio quality. Historical native startup
and WAV loading at version 0.18.62 do not validate these changes. Installation,
current-game event playback, public-tool staging and heard balance remain open.
