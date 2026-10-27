# Install or update the owner's launcher on this PC. Builds the commit named in release.txt, keeps
# that plugin in play\, saves the launcher's own scripts locally in launcher\ and writes two files
# on the desktop: "Rust Skate.cmd" and "Rust Skate - remove mod files.cmd". The launcher itself
# needs no network and never builds anything, so what the owner plays is exactly this commit.
# Explicit developer override: $pcandidate is a complete commit SHA. It never changes release.txt.
# Optional $psoundpath selects an existing complete WAV folder; its pointer stays local to this PC.
$ErrorActionPreference = 'Stop'
$probe = "$HOME\Downloads\claude-loader-probe"; $L = "$probe\launcher"
$candidate = "$pcandidate"
if ($candidate -and $candidate -notmatch '\A[0-9a-fA-F]{40}\z') { 'ABORT: pcandidate must be a complete 40-character hexadecimal commit; the launcher was left as it was'; return }
$mode = if ($candidate) { 'candidate' } else { 'release' }
$rel = if ($candidate) { $candidate.ToLowerInvariant() } else {
  ("" + (zg 'release.txt')).Trim() -split "`n" | ? { $_ -match '^[0-9a-f]{40}$' } | select -First 1
}
if (-not $rel) { 'ABORT: release.txt names no commit'; return }
'Preparing ' + $mode + ' ' + $rel + $(if ($candidate) { ' (in-game acceptance pending; release.txt is unchanged).' } else { '.' })
$previousCommit = $global:pcommit
$global:pbuilt = $null; $global:pnext = $null
try { $global:pcommit = $rel; zz build }
finally { $global:pcommit = $previousCommit }
if ($pbuilt -ne $rel) { 'ABORT: the requested ' + $mode + ' commit did not build; the launcher was left as it was'; return }
$enc = New-Object Text.UTF8Encoding($false)
$files = @{}
foreach ($f in 'session.ps1', 'skate.ps1', 'watch.ps1', 'clean.ps1', 'sticks.ps1') {
  $t = "" + (irm (zu "mods/rust/client/tools/$f" $rel))
  if ($t.Length -lt 200) { "ABORT: could not fetch $f at the requested commit; the launcher was left as it was"; return }
  $files[$f] = $t
}
# Older pinned releases predate the shared helper. Require it only when their own scripts
# use it; never fetch a replacement from another revision or weaken a modern preflight.
$needsDiagnostics = @($files.Values | Where-Object { $_ -match 'diagnostics\.ps1' }).Count -gt 0
$supportsCaptures = $files['session.ps1'] -match 'skate_captures\.skmc'
$supportsSounds = $files['session.ps1'] -match 'skate_sounds'
$icons = @(); $soundSource = $null
if ($needsDiagnostics) {
  $t = "" + (irm (zu 'mods/rust/client/tools/diagnostics.ps1' $rel))
  if ($t.Length -lt 200) { 'ABORT: could not fetch diagnostics.ps1 at the requested commit; the launcher was left as it was'; return }
  $files['diagnostics.ps1'] = $t
  . ([scriptblock]::Create($t))
  $icons = @(Get-SkateArtworkFiles "$probe\plugin\artwork.txt" "$probe\plugin")
  $soundSource = Resolve-SkateSoundSource $probe "$psoundpath"
} else {
  if ("$psoundpath") { 'ABORT: this pinned legacy launcher cannot use psoundpath; select a newer candidate or clear the override. The launcher was left as it was'; return }
  'Legacy launcher: authored menu icons, custom WAV source settings and diagnostics are unavailable in this selected commit.'
  if ($supportsSounds) { 'This selected session retains WAV staging from the default sounds folder.' }
  if (Test-Path -LiteralPath "$probe\sounds-source.txt") { 'The local WAV source setting is preserved; this legacy launcher does not use it.' }
}
New-Item -ItemType Directory -Force $L, "$probe\play", "$probe\src" | Out-Null
# Sessions use the plugin, current models and the exact declared icon list from play\.
# Preserve unrelated files, but keep old alternate plugin DLLs out of the live play folder.
Get-ChildItem "$probe\play" -File -Filter *.dll | Where-Object { $_.Name -ne 'ShortcutSkateClient.dll' } |
  ForEach-Object { Move-Item -LiteralPath $_.FullName -Destination (Join-Path "$probe\src" ('play-previous-' + [Guid]::NewGuid().ToString('N') + '-' + $_.Name)) }
# A previous complete capture pair must not survive an absent or incomplete new source.
# Preserve its bytes outside play before staging the selected pair.
foreach ($f in 'skate_captures.skmc', 'skate_captures.approved.txt') {
  if (Test-Path -LiteralPath "$probe\play\$f" -PathType Leaf) {
    Move-Item -LiteralPath "$probe\play\$f" -Destination (Join-Path "$probe\src" ('play-previous-' + [Guid]::NewGuid().ToString('N') + '-' + $f))
  }
}
Get-ChildItem "$probe\play" -File -Filter *.glb | Remove-Item
Get-ChildItem $L -File -Filter *.ps1 | ? { -not $files.ContainsKey($_.Name) } | Remove-Item
Copy-Item "$probe\plugin\ShortcutSkateClient.dll" "$probe\play\ShortcutSkateClient.dll" -Force
Get-ChildItem "$probe\plugin" -File -Filter *.glb | % { Copy-Item $_.FullName "$probe\play\$($_.Name)" -Force }
# Private reviewed captures travel as a pair. The runtime validates the pack's
# digest and exact approved ids; neither file is fetched from the repository.
$captureFiles = @('skate_captures.skmc', 'skate_captures.approved.txt')
$captureSource = "$probe\captures"
if ($supportsCaptures -and (Test-Path -LiteralPath "$captureSource\skate_captures.skmc" -PathType Leaf) -and
    (Test-Path -LiteralPath "$captureSource\skate_captures.approved.txt" -PathType Leaf)) {
  foreach ($f in $captureFiles) { Copy-Item -LiteralPath "$captureSource\$f" -Destination "$probe\play\$f" -Force }
  'private capture pair staged; animation approval is checked by the client'
} elseif ($supportsCaptures -and ((Test-Path -LiteralPath "$captureSource\skate_captures.skmc") -or
          (Test-Path -LiteralPath "$captureSource\skate_captures.approved.txt"))) {
  'private capture pair incomplete; normal riding keeps the built-in poses'
}
foreach ($icon in $icons) { Copy-Item -LiteralPath (Join-Path "$probe\plugin" $icon) -Destination "$probe\play" -Force }
if ($icons.Count) { Copy-Item -LiteralPath "$probe\plugin\artwork.txt" -Destination "$probe\play\artwork.txt" -Force }
elseif (Test-Path -LiteralPath "$probe\play\artwork.txt") { Move-Item -LiteralPath "$probe\play\artwork.txt" -Destination ("$probe\src\artwork-launcher-previous-" + [Guid]::NewGuid().ToString('N') + '.txt') }
foreach ($f in $files.Keys) { [IO.File]::WriteAllText("$L\$f", $files[$f], $enc) }
$pb = ((gc 'C:\Program Files (x86)\Steam\steamapps\appmanifest_252490.acf' | sls '"buildid"').Line -split '"')[3]
"Skate mod " + $rel.Substring(0, 7) + ", prepared " + (Get-Date -Format 'yyyy-MM-dd HH:mm') + " for Rust build $pb" | Out-File "$L\version.txt" -Encoding utf8
# Mark development installs without changing the accepted release pin.
if ($candidate) { $rel | Set-Content -LiteralPath "$L\candidate.txt" -Encoding ASCII }
else { Remove-Item -LiteralPath "$L\candidate.txt" -ErrorAction SilentlyContinue }
if ($needsDiagnostics -and "$psoundpath") { $soundSource.Directory | Set-Content -LiteralPath "$probe\sounds-source.txt" -Encoding UTF8 }
$desk = [Environment]::GetFolderPath('Desktop')
$start = "@echo off`r`ntitle Rust Skate`r`npowershell -NoProfile -ExecutionPolicy Bypass -File `"%USERPROFILE%\Downloads\claude-loader-probe\launcher\skate.ps1`"`r`necho.`r`npause`r`n"
$clean = "@echo off`r`ntitle Rust Skate - remove mod files`r`npowershell -NoProfile -ExecutionPolicy Bypass -File `"%USERPROFILE%\Downloads\claude-loader-probe\launcher\clean.ps1`"`r`necho.`r`npause`r`n"
[IO.File]::WriteAllText("$desk\Rust Skate.cmd", $start, [Text.Encoding]::ASCII)
[IO.File]::WriteAllText("$desk\Rust Skate - remove mod files.cmd", $clean, [Text.Encoding]::ASCII)
'launcher files: ' + ((ls $L -File | % { "{0} {1}" -f $_.Name, $_.Length }) -join ', ')
$mode + ' plugin: ' + (gc "$L\version.txt" -First 1) + ' | ' + (gi "$probe\play\ShortcutSkateClient.dll").Length + ' bytes | play folder: ' + ((ls "$probe\play" | % Name) -join ', ')
# The WAVs are neither built nor fetched; an explicit private folder is not duplicated locally.
if ($needsDiagnostics) {
  'sounds: ' + $soundSource.Files.Count + '/18 files from ' + $(if ($soundSource.Configured) { 'the configured local folder' } else { 'the default sounds folder' })
  'menu artwork: ' + $icons.Count + ' declared PNGs; diagnostics: powershell -NoProfile -File "' + "$L\skate.ps1" + '" -Diagnostics (does not start Rust)'
}
'desktop: ' + ((ls $desk -Filter 'Rust Skate*' | % Name) -join ', ')
