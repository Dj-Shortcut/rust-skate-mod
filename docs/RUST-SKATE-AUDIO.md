# Selected WAV skateboard audio

Client source `0.18.61` loads **18 selected WAV sounds**, one `_01.wav` per
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

Playback is still local **2D audio** (`spatialBlend = 0`), with no observer
replication, positional multiplayer sound or server audio change. It respects
the existing listener/mute state. Event timing, transitions and mix balance still
need in-game listening.

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

The stock launcher, build, pack and install paths **do not stage the external
`skate_sounds` folder**. Their managed DLL/GLB inventory is unchanged. Do not treat
a stock friend pack as an installed replacement sound pack. A private companion
staging proposal exists, but its Windows/PowerShell session and cleanup flow have
not been executed; it is not a verified installer.

The chosen ElevenLabs-generated sounds and raw downloads stay outside public GitHub.
They are not relicensed as CC0. Keep provenance and applicable usage terms with
the private pack, as described in the sound brief. The historical procedural API
and its separately authored assets retain their own existing licences.

Offline reader/playback fixtures and a genuine cached-reference C# compile cover
their stated source scope. Historical procedural-audio tests remain API coverage;
they do not validate these sounds. Installed-build interop, native upload,
actual pack staging, teardown, performance, repeated-loop artifacts, output mix
and heard quality remain open until the replacement pack is tested on Shadow.
