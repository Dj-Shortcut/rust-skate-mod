# Development candidate 0.18.63

This is source prepared for the next native test, not an accepted game release. The
owner reached the world with 0.18.62 and server 0.3.38 on 26 October. All 18 selected
WAVs loaded. Boarding then repeatedly failed at a Unity call inside the root-proxy
checks. Neither successful startup nor audio preparation proved a working ride.

## What changed and what remains open

| Area | Source change or finding | Remaining acceptance |
| --- | --- | --- |
| Boarding | Failure step, bounded stack trace and visible refusal; collision checks and rollback preserved. | Exact unavailable call, its repair, then a successful native mount. |
| Building wheel | X while holding Back retains controller confirmation; current gesture labels accept `legacyEnglish`. | Native Rust/Skate switching, fade, selection, placement and cancellation. |
| Icons | Twelve authored white-alpha silhouettes are tinted and drawn by the wheel; declared files follow the package. | Native readability and controller selection. |
| Held items | Preserve pre-hidden renderers; record active-object ownership before setters; restore independent objects and retry pending ones outside menus. | Visible empty hands, return of the item and controller operation after dismount. Named native belt-selection methods remain unavailable in the captured wrapper. |
| Compass/camera | Missing `SetViewVars` is reported; null input cannot report success; cached routes reset with the player. | A verified replacement for the missing native view route. This is not a compass or camera fix. |
| Drop-in (separate proposal) | After confirmed ramp contact, weight shifts forward and the knees absorb before returning to riding; feet keep the measured deck contacts. | Separate source integration, tail-on-coping preparation, native skin, motion and riding feel. |
| Placement | Existing twelve-kind mapping, shared snap frame and ten-Wood escrow were inspected; no new source defect was established. | All twelve pieces on ground/foundations, inventory route, successful payment and failure refund. |
| Round ramps | Four authored GLBs are catalogued separately; existing active models remain unchanged. | Versioned server/client geometry migration; see [the dimension differences](RUST-SKATE-RAMP-ASSETS.md#runtime-integration-boundary). |
| Starting and evidence | Preserve interrupted logs before cleanup, show bounded checks, and install an explicitly pinned candidate without changing the release pin. | Native Windows PowerShell installation, launch, cleanup and rollback. |

The supported-entry drop-in is a separate presentation change outside the base
recovery patch. It does not add a
tail/coping phase, move the physical player or replace the board's observed surface.
The source-pose preview is not evidence of realistic native skinning.

## Prepare the candidate

First preserve the existing client logs. `zz status` starts a scripted ride and
must not be used as a read-only diagnostic command.

The build requires an error-free interop set for the installed Rust build. If Rust
changed, regenerate that set before building. With the current `zz` helper loaded,
set `$pcandidate` to the full reviewed commit SHA, then use that same SHA for `$pref`:

```powershell
$prepo = 'Dj-Shortcut/rust-skate-mod'
# Set $pcandidate to the full reviewed candidate SHA first.
$pref = $pcandidate
zz launcher
```

An invalid candidate or a failed build must leave the installed launcher untouched.
Candidate installation does not promote `release.txt`. The resulting desktop
**Rust Skate** button uses local copies of the selected commit and the existing
Steam shortcut/controller reader.

Keep the eighteen selected WAVs private. If they already live outside the default
`sounds/`, set `$psoundpath` to that existing folder before `zz launcher`. Installation
validates the eighteen named files and saves a local `sounds-source.txt` pointer;
later starts and installs reuse it without a second permanent source copy. The
client still checks the WAV format. Do not substitute the historical generated sounds.
The friends' pack includes just the eighteen WAVs in its own `sounds/`, without
the pointer or the owner's private folder path.

After installing these tools, diagnostics can run without starting Rust:

```powershell
& "$env:USERPROFILE\Downloads\claude-loader-probe\launcher\skate.ps1" -Diagnostics
```

Diagnostics preserve private logs and report preparation, mount and refusal
signals. For each shown log they also report the latest mount outcome, the
recorded failure step, and the first Unity stack-frame method name if present.
If the step is outside the inspected log tail or absent, diagnostics say so.
A stack frame is a clue, not proof of the stripped native method; no
exception text, argument values or source paths are printed. Repeated identical
refusals retain their earlier recorded detail when the client deduplicates traces.
They do not start or stop the game. A report from an older saved run is not
evidence that the newly installed candidate works.

For an offline check, dot-source `diagnostics.ps1` against disposable folders
and put `SKATE MOUNT yaw=90`, a `SKATE ROOTSYNC mount refused:` line, and
`SKATE MOUNT failure step=root Joint children type=NotSupportedException` in
that order in a synthetic `run-P-*/skate.log`. `Show-SkateChecks` must report
the last attempt as refused at `root Joint children`, even though the log also
contains an earlier successful mount. A following `SKATE MOUNT yaw=...` must
clear the old failure detail. This checks log interpretation only.

## One focused native attempt

1. Start the installed candidate with **Rust Skate**, connect to the owner's
   server, and wait for preparation. Check `skate.status` as the player.
2. Press and release Y on the controller, or K, once. If boarding fails, preserve
   this run's step/stack evidence before further attempts or rebuilding. Repair
   that exact API failure without relaxing proxy ownership or collision checks.
3. Once boarding succeeds, push, steer, brake and dismount with the controller.
   Verify first/third person, empty hands, held-item recovery, on-foot controls,
   compass and camera separately. The missing view binding remains a known failure.
4. With a normal building plan, open Rust's construction wheel once before
   switching. Verify X/Back switching, confirmation, cancellation and placement,
   then all twelve pieces, snapping and ten-Wood payment/refund.
5. At the existing halfpipe, check three climbs and fakie returns without put-backs.
   Evaluate audible sounds in that real setting. Evaluate the supported drop-in
   entry separately if that proposal is included in the selected candidate.

Each result records the actual client/server versions. A passing compile, a pure
solver probe, a rendered source pose or the server's collider-health report does
not close any of the native acceptance items above.
