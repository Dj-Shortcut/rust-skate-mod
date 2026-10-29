# Rust Skate for a friend

How someone without the development tools gets the skate mod on his own PC and rides on the
owner's server. The owner makes one zip from the launcher that is installed and played on his PC
([`zz pack`](tools/README.md#scripts)); the friend unpacks it and runs its installer. Nothing is
downloaded or built on the friend's PC, and the zip plays exactly the plugin the owner's launcher
is prepared with: the accepted release pinned in [`tools/release.txt`](tools/release.txt), or the
development candidate the owner installed with `$pcandidate` ([how](tools/README.md#after-a-rust-update)).

**Not yet done on any PC.** The pack and its installer exist as scripts in this repository, were
run through a dry run with stand-in files, and were parsed and fixture-checked by Codex with
PowerShell 7 outside the game. Making a real pack on the owner's PC, installing it on a second PC
and playing from it are still open; see the [status](#status).

## What the friend needs

- Windows, Rust bought on Steam and installed in Steam's own library on `C:` (the launcher knows
  only `C:\Program Files (x86)\Steam\steamapps\common\Rust`), Steam signed in.
- The same Rust build as the owner. The pack is made for one Steam build; after a Rust update
  the launcher refuses to start and the owner makes a new pack.
- An Xbox controller (XInput) for the controls as in the skate. games. The keyboard works too
  ([keys](README.md#keys)).
- Free space on `C:` for the unpacked pack, mostly the interop set for the game build; the
  installer measures it and refuses when it is short (it wants the pack's size plus 1 GB free).
- The owner's server address. The mod is for that server only: it runs without anti-cheat. Rust
  started by the launcher cannot join servers with anti-cheat, and Rust started the normal way from
  Steam is unchanged, because the mod is in the Rust folder only while the launcher's window is open.

## The owner: making the pack

On the PC that plays the mod, with the launcher installed and prepared for the installed Rust
build (`zz launcher` after every Rust update, or `zz launcher` with `$pcandidate` set to the full
commit of a development candidate):

```powershell
zz pack
```

It writes `Downloads\RustSkate-<commit>-build<build>.zip` from `Downloads\claude-loader-probe`:

- `bep788\`: the unpacked loader.
- `gen-<build>\out` and its `gen.log`: the interop set of the installed build.
- `rt\`: the changed runtime.
- `play\`: the installed plugin and its park models; with a launcher that has the diagnostics
  helper also the build-wheel icons declared in `artwork.txt`, and, when the owner staged a
  reviewed capture pair (`skate_captures.skmc` with `skate_captures.approved.txt`) and the
  launcher's scripts know captures, that pair.
- `launcher\`: the launcher's own scripts and `version.txt`; `diagnostics.ps1` when the scripts
  use it; `candidate.txt` when the launcher was prepared from a candidate rather than the pinned
  release, so that the pack says which commit it plays.
- `sounds\`: the 18 skateboard WAV files, the owner's private sound pack, taken from the source
  the launcher retained or from the staging folder `sounds\`. A set with files missing refuses the
  pack, since the plugin plays nothing then; no set at all packs a silent ride and says so;
  `sv psounds 0` leaves them out. The machine path of the owner's source is never packed.
- `server.txt` when it is there (`sv pserver 0` leaves it out).

Next to them: `install.ps1` from the branch the tools were loaded from, `Install Rust Skate.cmd`,
`pack.txt` (what is inside: pack name, date, release, Rust build, launcher version, whether the
server address, the sounds and how many icons went in) and `README.txt` with the steps below in
plain text. It refuses when the launcher is missing, was prepared for another build than the
installed one, or the interop set is incomplete.

A pack made from the pinned release is a **legacy pack** as long as `release.txt` pins 0.18.40:
that release's own scripts predate the sound pack, the icons and the diagnostics helper, so `zz pack`
says "Legacy launcher" and packs none of them. The friend then rides with the plugin's own
generated sounds, the ones the owner rejected. The 18 WAVs, the icons and the capture pair only
reach a friend through a pack made from a candidate launcher that stages them, and such a
candidate is not an accepted release ([status](#status)).

The pack holds files made from the owner's own installed game (the interop set), the loader and
the owner's sound pack, whose files are not relicensed for publication
([rights](../../../docs/RUST-SKATE-SOUND-BRIEF.md)). It is handed to the friends it was made for
and is not published; nothing of it belongs in this repository.

## The friend: installing and playing

1. Unpack the zip anywhere (right-click, Extract All) and open the unpacked folder.
2. In Steam: Games > Add a Non-Steam Game to My Library > Browse, pick
   `C:\Program Files (x86)\Steam\steamapps\common\Rust\RustClient.exe`, add it and keep its name,
   `RustClient`. On its page in the library, under the controller icon, choose the layout template
   "Keyboard (WASD) and Mouse". The launcher starts Rust through this shortcut so that the
   controller works on foot as well as on the board ([why](README.md#the-controller)); with the
   "Gamepad" layout the controller does nothing in Rust.
3. Double-click `Install Rust Skate.cmd`. It checks that the folder is a pack, that Rust is in
   Steam's own library and is the build the pack was made for, that Rust is not running, that no
   earlier session left the mod in the Rust folder, that the pack is complete (its loader, interop
   set, runtime, plugin, launcher scripts, declared icons and sound set), and the free space; then
   it copies the pack to `Downloads\claude-loader-probe` and puts two files on the desktop:
   **Rust Skate** and **Rust Skate - remove mod files**. It refuses with a plain sentence when
   something is missing, and installs nothing then. At the end it says the installed version, the
   number of icons, whether the pack brought the skateboard sounds (without them the ride is
   silent and the game shows "Skateboard sound pack is unavailable" once), whether the Steam
   shortcut was found, and the server address when it was packed in. A legacy pack says
   "Legacy launcher" instead of the icon and sound lines.
4. Start **Rust Skate** from the desktop and keep its window open while playing. Steam is started
   when it is not running. In Rust's main menu press `F1` and type `connect <address>` (the
   window shows the address when it was packed in), then `Enter`. The window's first lines are
   the launcher's own checks of the Rust folder and the staging folder; the same checks without
   starting Rust: `launcher\skate.ps1 -Diagnostics` in `Downloads\claude-loader-probe` (not in a
   legacy pack, whose launcher has no such switch).
5. On the server: `Y` on the controller, `K`, or two quick jumps gets you on the board; `Y` or `K`
   again gets you off. `A` pushes, the left stick steers, `B` brakes, the right stick held back and
   flicked forward is an ollie, flicked to a forward diagonal a kickflip or heelflip, `LT`/`RT` held
   in the air a grab. The first seconds on the board show the controls; hold `Back` on the
   controller while on the board for every controller trick (three pages, D-pad left/right), or
   `H` on the keyboard for every key (one page on the board, the line on how to get on when on
   foot). The whole table is in the [client README](README.md#controls).
6. Quit Rust the normal way. The window ends with "Done. The Rust folder is back to normal." If it
   does not, or the PC went off while playing, run **Rust Skate - remove mod files** before
   starting Rust normally: a loader left in the Rust folder would be loaded by a normal start too.

To remove everything: first run **Rust Skate - remove mod files** and read that it found no
loader files in the Rust folder (`clean.ps1` and its note in the staging folder are the only way a
loader left behind by a cut-off session is taken out again), then delete the two desktop files and
the folder `Downloads\claude-loader-probe`. A new pack from the owner is installed over the old one
the same way; it must be unpacked into a folder of its own, not straight into `Downloads` (the
installer refuses that, because the copy would first delete its own source). The server address
and the sounds follow the pack: a pack without them takes an older address or sound set away too.

## Status

- Code present: `tools/pack.ps1`, `tools/install.ps1`, the launcher's messages for a friend
  (`tools/skate.ps1`), the sound-pack, icon, capture-pair and candidate-receipt handling that
  #569 and #575 added to them.
- Checked outside the game: `pack.ps1` and `install.ps1` were run on Linux against a stand-in
  folder tree with the Windows paths swapped (the zip holds the expected files and nothing else
  from `play\`, `pack.txt` carries the build, the installer copies the tree, writes the desktop
  files, reports a missing Steam shortcut and refuses another Rust build); Codex later parsed the
  tool scripts with PowerShell 7.4.20 and ran the sound, icon, capture-pair and candidate paths
  against fixture folders with the build and network calls mocked (#337, #575). None of that is
  Windows PowerShell 5.1, a real Downloads folder or an installation.
- Not verified: no real pack has been made on the owner's PC; nothing has been installed on a
  second PC; no friend has started Rust through the installed launcher, joined the server or
  ridden. A native 0.18.62 candidate session on the owner's PC loaded the 18 WAVs from its own
  staging folder (26 Oct), not from a pack. On that candidate getting on the board still refuses
  (the mount blocker at the top of [TODO.md](../../../TODO.md)), so a pack made from the current
  candidate cannot be ridden either until that is solved; a pack from the pinned release rides
  the older 0.18.40 plugin. The size of a real pack, Steam Input on the friend's shortcut and two
  clients on the owner's server are all still to be seen.
- Known limits: Rust in a Steam library other than `C:` is refused, the Steam shortcut is made by
  hand, and the owner's server must accept the second rider (its ShortcutSkate plugin, or its
  speed checks turned off; see [playing it](README.md#playing-it-the-owners-launcher)).
