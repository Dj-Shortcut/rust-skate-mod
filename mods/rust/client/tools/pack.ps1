# Pack the installed launcher for a friend. Takes what `zz launcher` put on this PC - the unpacked
# loader, the interop set for the installed Rust build, the changed runtime, the released plugin or explicit candidate
# with its park models, the sound pack and the launcher's own scripts - and writes one zip in Downloads with an
# installer (install.ps1 of this branch) that a friend runs on his own PC without any of the
# development tools. Nothing is built or downloaded; the pack is exactly what this PC plays.
# $pserver = 0 leaves server.txt out of the pack (by default it goes in when it is there, so that
# the friend's launcher can show the address to join). The configured local sound pack (otherwise
# sounds\, 18 WAV files that are never committed) goes in when it is complete; an incomplete
# one refuses the pack, since the plugin plays nothing then; $psounds = 0 packs without sounds.
$ErrorActionPreference = 'Stop'
$dl = "$HOME\Downloads"; $probe = "$dl\claude-loader-probe"; $L = "$probe\launcher"
$rust = 'C:\Program Files (x86)\Steam\steamapps\common\Rust'
$acf = 'C:\Program Files (x86)\Steam\steamapps\appmanifest_252490.acf'
if (-not (Test-Path $acf)) { 'ABORT: Rust is not installed in the default Steam library'; return }
$pb = ((gc $acf | sls '"buildid"').Line -split '"')[3]
if (-not $pb) { 'ABORT: could not read the installed Rust build'; return }
$version = if (Test-Path "$L\version.txt") { (gc "$L\version.txt" -First 1).Trim() } else { '' }
if (-not $version) { 'ABORT: no installed launcher (no launcher\version.txt); run zz launcher first'; return }
if ($version -notmatch "for Rust build $pb$") { "ABORT: the launcher was prepared for another Rust build ($version; installed $pb); run zz launcher first"; return }
$rel = if ($version -match '^Skate mod ([0-9a-f]{7})') { $Matches[1] } else { 'unknown' }
$gen = "$probe\gen-$pb"
if (-not ((Test-Path "$gen\out\Assembly-CSharp.dll") -and (sls -Path "$gen\gen.log" -Pattern 'GEN DONE.* errors=0( |$)' -Quiet))) { "ABORT: no complete, error-free interop set for build $pb (zz interop)"; return }
$launcherFiles = @('session.ps1', 'skate.ps1', 'watch.ps1', 'clean.ps1', 'sticks.ps1')
$need = @("$probe\bep788\winhttp.dll", "$probe\bep788\BepInEx\core\BepInEx.Core.dll", "$probe\rt\Il2CppInterop.Runtime.dll", "$probe\play\ShortcutSkateClient.dll") + @($launcherFiles | % { "$L\$_" })
$missing = @($need | ? { -not (Test-Path -LiteralPath $_) })
if ($missing) { 'ABORT: not on this PC: ' + (($missing | % { $_.Replace($dl, '~') }) -join ', '); return }
$needsDiagnostics = @($launcherFiles | Where-Object { [IO.File]::ReadAllText("$L\$_") -match 'diagnostics\.ps1' }).Count -gt 0
$sessionSource = [IO.File]::ReadAllText("$L\session.ps1")
$supportsCaptures = $sessionSource -match 'skate_captures\.skmc'
$supportsSounds = $sessionSource -match 'skate_sounds'
$icons = @(); $sounds = $false; $soundSource = $null
if ($needsDiagnostics) {
  if (-not (Test-Path -LiteralPath "$L\diagnostics.ps1" -PathType Leaf)) { 'ABORT: this installed launcher requires diagnostics.ps1; install the complete launcher again'; return }
  $launcherFiles += 'diagnostics.ps1'
  . "$L\diagnostics.ps1"
  $icons = @(Get-SkateArtworkFiles "$probe\play\artwork.txt" "$probe\play")
  if ("$psounds" -notin '0', 'false') {
    $soundSource = Resolve-SkateSoundSource $probe -Strict
    $sounds = $soundSource.Files.Count -eq 18
  }
} else {
  'Legacy launcher: the pack keeps the selected scripts; custom WAV source settings, authored icons and diagnostics are unavailable.'
  if (Test-Path -LiteralPath "$probe\sounds-source.txt") { 'The local WAV source setting is preserved and is not used or included by this legacy pack.' }
  # Earlier WAV-capable sessions predate the helper and read only this default folder.
  if ($supportsSounds -and "$psounds" -notin '0', 'false' -and (Test-Path -LiteralPath "$probe\sounds" -PathType Container)) {
    $stems = 'skate_rolling_smooth', 'skate_rolling_rough', 'skate_rolling_wood', 'skate_grit', 'skate_whirr', 'skate_push', 'skate_ollie', 'skate_landing', 'skate_drop', 'skate_seam', 'skate_grind_in', 'skate_grind', 'skate_grind_out', 'skate_slide_in', 'skate_deckslide', 'skate_scrape', 'skate_slam', 'skate_bail'
    $names = @($stems | ForEach-Object { $_ + '_01.wav' })
    foreach ($name in $names) {
      $stream = $null
      try {
        $path = Join-Path "$probe\sounds" $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 0) { throw 'Missing sound file.' }
        $stream = [IO.File]::OpenRead($path)
      } catch { 'ABORT: the default sound folder is incomplete or unreadable: ' + $name; return }
      finally { if ($stream) { $stream.Dispose() } }
    }
    $soundSource = [pscustomobject]@{ Directory = "$probe\sounds"; Files = $names; Configured = $false }; $sounds = $true
  }
}
# The installer is taken from this branch, like every other script: it is not part of the launcher.
$enc = New-Object Text.UTF8Encoding($false)
$inst = "" + (zg 'install.ps1')
if ($inst.Length -lt 200) { 'ABORT: could not fetch install.ps1 from the branch'; return }
$name = "RustSkate-$rel-build$pb"
$stage = "$probe\pack-$name"; $zip = "$dl\$name.zip"
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
$to = "$stage\claude-loader-probe"
New-Item -ItemType Directory -Force "$to\gen-$pb" | Out-Null
Copy-Item "$probe\bep788" "$to\bep788" -Recurse
Copy-Item "$gen\out" "$to\gen-$pb\out" -Recurse
Copy-Item "$gen\gen.log" "$to\gen-$pb\gen.log"
Copy-Item "$probe\rt" "$to\rt" -Recurse
# Package only the installed plugin, models and exact declared icon list; no diagnostic logs.
New-Item -ItemType Directory -Force "$to\play", "$to\launcher" | Out-Null
Copy-Item "$probe\play\ShortcutSkateClient.dll" "$to\play"
ls "$probe\play" -File -Filter *.glb | % { Copy-Item $_.FullName "$to\play" }
foreach ($icon in $icons) { Copy-Item -LiteralPath (Join-Path "$probe\play" $icon) -Destination "$to\play" }
if ($icons.Count) { Copy-Item -LiteralPath "$probe\play\artwork.txt" -Destination "$to\play" }
if ($supportsCaptures -and (Test-Path -LiteralPath "$probe\play\skate_captures.skmc" -PathType Leaf) -and
    (Test-Path -LiteralPath "$probe\play\skate_captures.approved.txt" -PathType Leaf)) {
  foreach ($f in 'skate_captures.skmc', 'skate_captures.approved.txt') {
    Copy-Item -LiteralPath "$probe\play\$f" -Destination "$to\play\$f"
  }
}
foreach ($f in ($launcherFiles + 'version.txt')) { Copy-Item "$L\$f" "$to\launcher" }
if (Test-Path -LiteralPath "$L\candidate.txt" -PathType Leaf) { Copy-Item -LiteralPath "$L\candidate.txt" -Destination "$to\launcher" }
# Package the eighteen WAVs, never the private source pointer or its machine-specific path.
if ($sounds) {
  New-Item -ItemType Directory -Force "$to\sounds" | Out-Null
  foreach ($file in $soundSource.Files) { Copy-Item -LiteralPath (Join-Path $soundSource.Directory $file) -Destination "$to\sounds" }
}
$server = ''
if (($null -eq $pserver -or $pserver) -and (Test-Path "$probe\server.txt")) { Copy-Item "$probe\server.txt" "$to\server.txt"; $server = (gc "$probe\server.txt" -First 1).Trim() }
[IO.File]::WriteAllText("$stage\install.ps1", $inst, $enc)
[IO.File]::WriteAllText("$stage\Install Rust Skate.cmd", "@echo off`r`ntitle Install Rust Skate`r`npowershell -NoProfile -ExecutionPolicy Bypass -File `"%~dp0install.ps1`"`r`necho.`r`npause`r`n", [Text.Encoding]::ASCII)
# The comma binds tighter than +, so each line is made before the list is.
$made = 'made ' + (Get-Date -Format 'yyyy-MM-dd HH:mm'); $srv = 'server ' + $(if ($server) { 'included' } else { 'not included' })
$snd = 'sounds ' + $(if ($sounds) { 'included' } else { 'not included' })
@("pack $name", $made, "release $rel", "build $pb", $version, $srv, $snd, ("artwork " + $icons.Count)) | Out-File "$stage\pack.txt" -Encoding utf8
$soundInstructions = if ($supportsSounds) {
  '   The skateboard sounds come with the pack when pack.txt says "sounds included"; without them' + "`n" +
  '   the ride is silent and the game shows "Skateboard sound pack is unavailable" once.'
} else { '   This older launcher has no private WAV staging; it retains its original audio behavior.' }
$readme = @"
RUST SKATE - the skate mod for Rust, for the owner's own server

What you need: Rust installed through Steam in Steam's own library on C:, Steam signed in, and
(for the real controls) an Xbox controller. The mod runs on the owner's server only, which has no
anti-cheat. Rust started by this launcher cannot join other servers, and Rust started the normal
way is unchanged: the mod is in the Rust folder only while the launcher's window is open.

1. Unpack this zip anywhere (right-click, Extract All) and open the unpacked folder.
2. In Steam: Games > Add a Non-Steam Game to My Library > Browse, pick
   C:\Program Files (x86)\Steam\steamapps\common\Rust\RustClient.exe and add it. Keep its name,
   RustClient. In that game's page: controller icon > controller layout > choose the template
   "Keyboard (WASD) and Mouse". Without the shortcut the launcher does not start; with another
   layout the controller does nothing in Rust.
3. Double-click "Install Rust Skate.cmd". It checks your PC, copies the mod to
   Downloads\claude-loader-probe and puts two files on your desktop.
4. Start "Rust Skate" from the desktop and keep its window open while you play. In Rust's main
   menu press F1 and type   connect <address>   (the window shows the address when it was packed
   in; otherwise ask the owner), then Enter.
5. On the server: Y on the controller (or K on the keyboard) gets you on the board. A pushes,
   the left stick steers, B brakes, the right stick held back and flicked forward is an ollie,
   LT/RT held in the air is a grab, Y gets you off. The first seconds on the board show the controls.
$soundInstructions
6. Quit Rust the normal way. The window says "Done" when the Rust folder is back to normal. If it
   does not, or the PC went off while playing, run "Rust Skate - remove mod files" from the desktop
   before starting Rust normally.

After a Rust update the launcher refuses to start and says so: ask the owner for a new pack.
To remove everything: first run "Rust Skate - remove mod files" and read that the Rust folder is
clean (it holds the only key to a loader left behind), then delete the two desktop files and the
folder Downloads\claude-loader-probe.
This pack was made from the owner's own installed game; keep it among the people it was made for.
"@
[IO.File]::WriteAllText("$stage\README.txt", $readme.Replace("`r`n", "`n").Replace("`n", "`r`n"), $enc)
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
Remove-Item -LiteralPath $stage -Recurse -Force
$mb = [int]((gi $zip).Length / 1MB)
"pack: $zip ($mb MB)"
"inside: " + $version + " | interop set gen-$pb | plugin " + (gi "$probe\play\ShortcutSkateClient.dll").Length + " bytes | models " + @(ls "$probe\play" -Filter *.glb).Count + " | sounds " + $(if (-not $supportsSounds) { 'legacy audio behavior; no private WAV staging' } elseif ($sounds) { '18' } else { 'none: the friend rides without skateboard audio' }) + " | server.txt " + $(if ($server) { 'included' } else { 'left out' })
'give the zip to the friend with README.txt inside it; see FRIEND.md in the client folder'
