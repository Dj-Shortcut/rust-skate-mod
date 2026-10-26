# Original skateboard sound pack

54 original synthesized CC0 mono44.1kHz16-bit PCM variations are in `skate_sounds/`; `demo.wav` is the30-second listening sequence. No game recordings, commercial packs or external sounds were used. The source generator and pinned Python requirements are included; synthesis uses filtered noise, original modal wood/metal resonances and seeded transients.

The pack manifest records measurements from actual post-PCM bytes: LUFS, peak, DC, loop points, onset and hashes. Quiet grit/whirr layers use the declared lower targets. Loops are3seconds and zero-ended; ten repeats and+40% pitch join checks pass. One-shots are dry and end-faded.

Generate into a separate directory with `python scripts/generate_skate_sounds.py --output <dir>/skate_sounds`; `--verify-only` rereads and measures the delivered files and exits nonzero on failure. Rebuild the repository audit manifest only after review; `skate_sounds/manifest.json` is the pack's detailed manifest. The installed-runtime audio binding is separate from this authored pack. Timbral realism, audible repetition and in-game balance require listening.
