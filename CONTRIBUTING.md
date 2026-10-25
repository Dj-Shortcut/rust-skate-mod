# Contributing

This repository is a skateboard mod for the existing Rust PC game: a BepInEx
client mod and an Oxide server plugin, both C#, under [mods/rust](mods/rust).
Read [README.md](README.md), [TODO.md](TODO.md) and [docs/INDEX.md](docs/INDEX.md) first.

- Start in an issue, or comment on the existing one with your scope, branch and
  files, so two people do not change the same file.
- One focused change per PR. Describe what was seen in the game and what only
  compiles; a green CI run is not a ride.
- Permanent tests live only in [crates/approved_tests](crates/approved_tests/README.md)
  and need the owner's approval by name.
- No game files, extracted assets, server data, logs or keys. Run
  `make check` before you push.
- Player-facing text is English.

Report bugs with the client and server versions, what you did, what you
expected and what happened, plus the relevant lines from `skate.log`.
