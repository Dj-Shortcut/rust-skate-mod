# Shared launcher checks and private log recovery. Dot-source for the functions; running this
# file alone only copies existing logs and prints bounded checks. It never starts or stops Rust.
function Get-SkateArtworkFiles([string]$Manifest, [string]$Directory = '') {
  if (-not (Test-Path -LiteralPath $Manifest -PathType Leaf)) { return }
  $names = @()
  foreach ($line in (Get-Content -LiteralPath $Manifest)) {
    $text = $line.Trim()
    if (-not $text -or $text.StartsWith('#')) { continue }
    if ($text -cnotmatch '^assets/authored/skate_icons/(skate_icon_[a-z0-9_-]+_128\.png)$') {
      throw 'The artwork list contains an unsupported path.'
    }
    $name = $Matches[1]
    if ($names -contains $name) { throw 'The artwork list contains a duplicate file.' }
    $names += $name
  }
  if ($names.Count -ne 12) { throw 'The artwork list must name exactly twelve icons.' }
  if ($Directory) {
    foreach ($name in $names) {
      $path = Join-Path $Directory $name
      if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Missing artwork: ' + $name) }
      $stream = [IO.File]::OpenRead($path)
      try {
        $signature = New-Object byte[] 8
        if ($stream.Read($signature, 0, 8) -ne 8 -or [BitConverter]::ToString($signature) -ne '89-50-4E-47-0D-0A-1A-0A') {
          throw ('Invalid PNG artwork: ' + $name)
        }
      } finally { $stream.Dispose() }
    }
  }
  return $names
}

function Save-SkateLogSnapshot([string]$RustPath, [string]$ProbePath, [string]$Reason = 'before-launch') {
  $sources = @('BepInEx/plugins/skate.log', 'BepInEx/LogOutput.log', 'BepInEx/ErrorLog.log')
  $present = @($sources | Where-Object { Test-Path -LiteralPath (Join-Path $RustPath $_) -PathType Leaf })
  if ($present.Count -eq 0) { return [pscustomobject]@{ Count = 0; Directory = '' } }
  $name = 'run-recovered-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
  $destination = Join-Path $ProbePath $name
  New-Item -ItemType Directory -Path $destination -ErrorAction Stop | Out-Null
  $entries = @(); $failed = $false
  foreach ($relative in $present) {
    $source = Join-Path $RustPath $relative
    $target = Join-Path $destination (Split-Path $source -Leaf)
    $inputLog = $null; $outputLog = $null
    try {
      $before = Get-Item -LiteralPath $source -ErrorAction Stop
      $length = $before.Length; $modified = $before.LastWriteTimeUtc
      # An active log may be shared for append. The receipt states if it changed during copying.
      $inputLog = [IO.File]::Open($source, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
      $outputLog = [IO.File]::Open($target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
      $inputLog.CopyTo($outputLog)
      $outputLog.Dispose(); $outputLog = $null
      $inputLog.Dispose(); $inputLog = $null
      $after = Get-Item -LiteralPath $source -ErrorAction Stop
      $entries += [ordered]@{ source = $relative; file = (Split-Path $target -Leaf); status = 'copied';
        modifiedUtc = $modified.ToString('o'); bytes = (Get-Item -LiteralPath $target).Length;
        sha256 = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant();
        sourceChangedDuringCopy = ($length -ne $after.Length -or $modified -ne $after.LastWriteTimeUtc) }
    } catch {
      $failed = $true
      $entries += [ordered]@{ source = $relative; status = 'unreadable'; errorType = $_.Exception.GetType().Name }
    } finally {
      if ($outputLog) { $outputLog.Dispose() }
      if ($inputLog) { $inputLog.Dispose() }
    }
  }
  [ordered]@{ createdUtc = [DateTime]::UtcNow.ToString('o'); reason = $Reason; files = $entries } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'recovery.json') -Encoding UTF8 -ErrorAction Stop
  if ($failed) { throw ('Existing client logs could not all be preserved. Nothing may be cleaned or started; inspect ' + $destination) }
  return [pscustomobject]@{ Count = $entries.Count; Directory = $destination }
}

function Show-SkateChecks([string]$RustPath, [string]$ProbePath) {
  $launcher = Join-Path $ProbePath 'launcher'
  $version = Join-Path $launcher 'version.txt'
  if (Test-Path -LiteralPath $version) {
    $text = ("" + (Get-Content -LiteralPath $version -First 1)).Trim()
    if ($text -match '^Skate mod [0-9a-f]{7,40}, prepared [0-9 :-]+ for Rust build [0-9]+$') { Write-Host ('  ' + $text) }
    else { Write-Host '  Version: the installed version receipt is not recognized.' }
  } else { Write-Host '  Version: no installed launcher receipt.' }
  $candidateReceipt = Join-Path $launcher 'candidate.txt'
  if (Test-Path -LiteralPath $candidateReceipt -PathType Leaf) {
    $candidateCommit = ("" + (Get-Content -LiteralPath $candidateReceipt -First 1)).Trim()
    if ($candidateCommit -match '\A[0-9a-f]{40}\z') { Write-Host ('  Candidate: ' + $candidateCommit + '; in-game acceptance is pending.') }
  }
  $plugin = Join-Path $ProbePath 'play/ShortcutSkateClient.dll'
  Write-Host ('  Plugin: ' + $(if (Test-Path -LiteralPath $plugin -PathType Leaf) { 'installed; SHA256 ' + (Get-FileHash -LiteralPath $plugin -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant() } else { 'missing' }))
  $acf = Join-Path (Split-Path (Split-Path $RustPath -Parent) -Parent) 'appmanifest_252490.acf'
  $build = ''
  if (Test-Path -LiteralPath $acf) {
    foreach ($line in (Get-Content -LiteralPath $acf)) { if ($line -match '^\s*"buildid"\s+"([0-9]+)"') { $build = $Matches[1]; break } }
  }
  $complete = $build -and (Test-Path -LiteralPath "$ProbePath/gen-$build/out/Assembly-CSharp.dll") -and
    (Test-Path -LiteralPath "$ProbePath/gen-$build/gen.log") -and
    (Select-String -LiteralPath "$ProbePath/gen-$build/gen.log" -Pattern 'GEN DONE.* errors=0( |$)' -Quiet)
  Write-Host ('  Rust build: ' + $(if ($build) { $build } else { 'not found' }) + '; matching interop: ' + $(if ($complete) { 'ready' } else { 'not ready' }))
  $manifest = Join-Path $ProbePath 'play/artwork.txt'
  try { $icons = @(Get-SkateArtworkFiles $manifest (Join-Path $ProbePath 'play')); Write-Host ('  Menu artwork: ' + $icons.Count + '/12 installed; missing artwork uses the built-in icons.') }
  catch { Write-Host '  Menu artwork: incomplete or invalid; reinstall the prepared client package.' }
  Write-Host '  Recent diagnostics (existing files only; no game or controller changes):'
  $folders = @((Join-Path $RustPath 'BepInEx'))
  foreach ($filter in 'run-*', 'removed-*') {
    $folders += @(Get-ChildItem -LiteralPath $ProbePath -Directory -Filter $filter -ErrorAction SilentlyContinue |
      Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 3 | ForEach-Object { $_.FullName })
  }
  $shown = 0
  foreach ($folder in $folders) {
    foreach ($relative in 'skate.log', 'plugins/skate.log', 'BepInEx/plugins/skate.log', 'removed/BepInEx/plugins/skate.log', 'LogOutput.log', 'BepInEx/LogOutput.log', 'removed/BepInEx/LogOutput.log', 'ErrorLog.log', 'BepInEx/ErrorLog.log', 'removed/BepInEx/ErrorLog.log') {
      $path = Join-Path $folder $relative
      if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
      try {
        $tail = @(Get-Content -LiteralPath $path -Tail 2500 -ErrorAction Stop)
        $prepared = [bool]($tail -match 'SKATE PREPARE ready ')
        $mounted = [bool]($tail -match 'SKATE MOUNT yaw=')
        $refused = [bool]($tail -match 'ROOTSYNC mount refused:')
        $native = [bool]($tail -match 'ROOTSYNC mount refused:.*Method unstripping failed')
        $updated = (Get-Item -LiteralPath $path).LastWriteTimeUtc.ToString('u')
        Write-Host ('  ' + (Split-Path $folder -Leaf) + '/' + $relative + ' @ ' + $updated + ': prepared=' + $prepared + ', mount=' + $mounted + ', proxy-refused=' + $refused + ', unavailable-native-method=' + $native)
      } catch { Write-Host ('  ' + (Split-Path $folder -Leaf) + ': a saved log is unreadable.') }
      $shown++; break
    }
  }
  if ($shown -eq 0) { Write-Host '  No client logs have been saved yet.' }
  Write-Host ('  Logs stay private in ' + $ProbePath + '; no archives are purged.')
}

if ($MyInvocation.InvocationName -ne '.') {
  $diagnosticProbe = "$HOME\Downloads\claude-loader-probe"
  $diagnosticRust = 'C:\Program Files (x86)\Steam\steamapps\common\Rust'
  $saved = Save-SkateLogSnapshot $diagnosticRust $diagnosticProbe 'manual-diagnostics'
  if ($saved.Count) { Write-Host ('  Preserved ' + $saved.Count + ' existing logs in ' + $saved.Directory) }
  Show-SkateChecks $diagnosticRust $diagnosticProbe
}
