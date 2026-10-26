# Build and open the in-game rigging preview through the same RustClient Steam shortcut as
# play.ps1, with its normal controller reader. Choose the server under QUICK JOIN as usual.
# Dpad left/right selects a pose, A pauses/resumes, B/Y/Back stops the preview. Keyboard:
# left/right arrows, Space and Escape. The preview restores normal play when stopped; it must
# not write skate.done or stop the session/reader. Sound ownership stays with its lifecycle.
# $plimit limits the complete game session in seconds; default and maximum are two hours.
$riggingLimit = 7200
if ($null -ne $plimit -and "$plimit".Trim().Length -gt 0) {
  $riggingRequested = 0
  if (-not [int]::TryParse("$plimit", [ref]$riggingRequested) -or $riggingRequested -lt 1) {
    Write-Host 'ABORT: plimit must be a positive whole number of seconds (maximum 7200)'
    return
  }
  $riggingLimit = [Math]::Min(7200, $riggingRequested)
}
$global:ptag='G'; $global:psteps='rigging'; $global:pdone=$riggingLimit
$global:psteam=$true; $global:pconnect=$false; $global:pkeep=$false
$global:plisten=("$plisten" -in '1','true')
$global:prt="$HOME\Downloads\claude-loader-probe\rt"; $global:pplug="$HOME\Downloads\claude-loader-probe\plugin"
$global:pwait=30; $global:ppreload=$false; $global:ptail=4
Write-Host ('Rigging preview: Steam/controller session, limit ' + $riggingLimit + ' seconds')
Write-Host 'B/Y/Back or Escape stops the preview and restores normal play; Rust keeps running'
$global:pnext='session'; zz build
