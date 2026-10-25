Standing project authorization and agent coordination: [docs/AUTONOMY.md](docs/AUTONOMY.md).
Open work, in the owner's order: [TODO.md](TODO.md). One line per item; details go in issues and PRs.

All player-facing game text must be English. Keep HUDs, menus, item names,
controls, interaction feedback and displayed errors in English in every change.
Use readable glyphs supported by the shipped UI font. This is a user requirement.

What is published is the mod source and the documentation needed to use and
develop it. Game files, server data, logs, credentials, research dumps and
third-party checkouts are not; keep them in the ignored `context/` folder.
Credits for projects that were studied belong in `README.md` and `NOTICE`.

`make publish-check` walks the tracked tree for leaks (`context/`, `.env`,
keys, game files) and tests outside `crates/approved_tests`. It is a grep over
a fixed set of shapes, not a proof of origin or licensing.
