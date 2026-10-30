# Owner requested focused regression tests for PR #621 on 30 October 2026.
# Executes production PowerShell; registry and Windows file visibility are fixture-backed.
# Does not install a pack, start Steam/Rust, or touch any game folder.
param([string]$Diagnostics = "$PSScriptRoot/../../../mods/rust/client/tools/diagnostics.ps1")
$ErrorActionPreference = 'Stop'
. $Diagnostics
$fixtures = @{Files=@{}; Registry=@{}}; $script:passed = 0; $script:failed = 0
function Test-Path {
  param([Alias('Path')]$LiteralPath, $PathType, $ErrorAction)
  if ($null -eq $LiteralPath) { return $false }
  if ($LiteralPath -like 'X:*') { throw 'Drive unavailable' }
  return $fixtures.Files.ContainsKey($LiteralPath.Replace('\\', '\'))
}
function Get-ItemProperty {
  param($Path, $ErrorAction)
  if (-not $fixtures.Registry.ContainsKey($Path)) { throw 'Registry key unavailable' }
  return [pscustomobject]$fixtures.Registry[$Path]
}
function Get-Content {
  param($LiteralPath, [switch]$Raw, $Encoding, $ErrorAction)
  if (-not $fixtures.Files.ContainsKey($LiteralPath) -or $fixtures.Files[$LiteralPath] -eq '!unreadable!') { throw 'File unavailable' }
  $text = $fixtures.Files[$LiteralPath]
  if ($Raw) { return $text }
  return $text -split "`n"
}
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Scenario($Label, [scriptblock]$Body) {
  $fixtures.Files = @{}; $fixtures.Registry = @{}
  try { & $Body; $script:passed++; Write-Host "PASS $Label" }
  catch { $script:failed++; Write-Host "FAIL ${Label}: $_ $($_.ScriptStackTrace)" }
}
$default = 'C:\Program Files (x86)\Steam'; $other = 'D:\Steam'; $library = 'E:\SteamLibrary'
function Add-Rust($Library, $Manifest = '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "12345" }', $Directory = 'Rust') {
  $fixtures.Files["$Library\steamapps\appmanifest_252490.acf"] = ($Manifest -replace '\s+(?="(?:appid|installdir|buildid|nested)"|})', "`n")
  $fixtures.Files["$Library\steamapps\common\$Directory\RustClient.exe"] = ''
}
function Add-Library($Text) { $fixtures.Files["$default\steamapps\libraryfolders.vdf"] = $Text }
Scenario 'existing C installation keeps identical paths' {
  Add-Rust $default
  $r = Get-SkateRustFolder
  Assert ($r.Found -and $r.Rust -eq "$default\steamapps\common\Rust" -and $r.Acf -eq "$default\steamapps\appmanifest_252490.acf") 'C paths changed'
}
Scenario 'HKCU Steam wins over stale C executable' {
  $fixtures.Files["$default\steam.exe"] = ''; $fixtures.Files["$other\steam.exe"] = ''
  $fixtures.Registry['HKCU:\Software\Valve\Steam'] = @{SteamPath='D:/Steam/'}
  Assert ((Get-SkateSteamFolder) -eq $other) 'Ignored current user Steam'
}
foreach ($key in 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam') {
  Scenario "machine registry fallback $key" {
    $fixtures.Registry['HKCU:\Software\Valve\Steam'] = @{SteamPath='X:/Missing'}
    $fixtures.Registry[$key] = @{InstallPath=$other}; $fixtures.Files["$other\steam.exe"] = ''
    Assert ((Get-SkateSteamFolder) -eq $other) 'Machine fallback missed'
  }
}
Scenario 'all registry entries absent' { Assert ((Get-SkateSteamFolder) -eq $default) 'Default changed' }
Scenario 'modern second-drive library with doubled backslashes' {
  Add-Library '"libraryfolders" { "0" { "path" "C:\\Program Files (x86)\\Steam" } "1" { "path" "E:\\SteamLibrary" "apps" { "252490" "1" } } }'
  Add-Rust $library
  $r = Get-SkateRustFolder
  Assert ($r.Found -and $r.Rust -eq "$library\steamapps\common\Rust") 'Second library missed'
}
Scenario 'legacy numeric library path' {
  Add-Library '"LibraryFolders" { "TimeNextStatsReport" "0" "1" "E:\\SteamLibrary" }'; Add-Rust $library
  Assert ((Get-SkateRustFolder).Found) 'Legacy library missed'
}
Scenario 'unplugged library skipped before valid library' {
  Add-Library '"libraryfolders" { "1" { "path" "X:\\Missing" } "2" { "path" "E:\\SteamLibrary" } }'; Add-Rust $library
  Assert ((Get-SkateRustFolder).Found) 'Missing drive aborted search'
}
Scenario 'unreadable library index does not hide default Rust' {
  Add-Library '!unreadable!'; Add-Rust $default
  Assert ((Get-SkateRustFolder).Found) 'Unreadable index aborted discovery'
}
Scenario 'unreadable first manifest does not hide second Rust' {
  Add-Library '"libraryfolders" { "1" { "path" "E:\\SteamLibrary" } }'
  $fixtures.Files["$default\steamapps\appmanifest_252490.acf"] = '!unreadable!'; Add-Rust $library
  Assert ((Get-SkateRustFolder).Found) 'Unreadable manifest aborted search'
}
Scenario 'no installed Rust is explicit' { Assert (-not (Get-SkateRustFolder).Found) 'False detection' }
foreach ($dir in '..\..\elsewhere','D:\Elsewhere','..','Rust.','Rust ','Rust:stream','Rust*','NUL') {
  Scenario "reject unsafe installdir $dir" {
    Add-Rust $default ('"AppState" { "appid" "252490" "installdir" "' + $dir.Replace('\','\\') + '" "buildid" "12345" }') $dir
    Assert (-not (Get-SkateRustFolder).Found) 'Unsafe directory accepted'
  }
}
foreach ($manifest in @(
  '"AppState" { "appid" "440" "installdir" "Rust" "buildid" "12345" }',
  '"AppState" { "appid" "252490" "buildid" "12345" }',
  '"AppState" { "appid" "252490" "installdir" "Rust" }',
  '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "../bad" }',
  '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "1" "buildid" "2" }',
  '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "12345"',
  '"AppState" { "appid" "252490" "nested" { "installdir" "Rust" } "buildid" "12345" }'
)) {
  Scenario "reject invalid manifest $manifest" {
    Add-Rust $default $manifest
    Assert (-not (Get-SkateRustFolder).Found) 'Invalid manifest accepted'
  }
}
Scenario 'comments cannot inject installdir; custom directory and validated build returned' {
  Add-Rust $default "// `"installdir`" `"Elsewhere`"`n`"AppState`" { `"appid`" `"252490`" `"installdir`" `"Rust Staging`" `"buildid`" `"12345`" }" 'Rust Staging'
  $r = Get-SkateRustFolder
  Assert ($r.Found -and $r.Rust -eq "$default\steamapps\common\Rust Staging" -and $r.BuildId -eq '12345') 'Validated metadata lost'
}
Scenario 'commented library is ignored' {
  Add-Library "`"libraryfolders`" { // `"path`" `"E:\\SteamLibrary`"`n }"; Add-Rust $library
  Assert (-not (Get-SkateRustFolder).Found) 'Comment treated as library'
}
Scenario 'shortcut matches quoted selected client on another drive' {
  Assert (Test-SkateRustShortcut '"E:\SteamLibrary\steamapps\common\Rust\RustClient.exe"' 'E:\SteamLibrary\steamapps\common\Rust') 'Selected shortcut rejected'
}
Scenario 'shortcut in another library is rejected' {
  Assert (-not (Test-SkateRustShortcut '"C:\Steam\steamapps\common\Rust\RustClient.exe"' 'E:\SteamLibrary\steamapps\common\Rust')) 'Wrong client accepted'
}
Scenario 'shortcut supports custom manifest installdir' {
  Assert (Test-SkateRustShortcut '"e:/SteamLibrary/steamapps/common/Rust Staging/RustClient.exe"' 'E:\SteamLibrary\steamapps\common\Rust Staging') 'Custom directory rejected'
}
Scenario 'shortcut suffix and arguments cannot masquerade as selected client' {
  Assert (-not (Test-SkateRustShortcut '"E:\Rust\RustClient.exe.bak"' 'E:\Rust')) 'Suffix accepted'
  Assert (-not (Test-SkateRustShortcut '"E:\Rust\RustClient.exe" --other' 'E:\Rust')) 'Arguments accepted as executable'
}
Scenario 'bound cleanup receipt accepts only its original library' {
  Assert (Test-SkateRecoveryFolder @('rust E:\SteamLibrary\steamapps\common\Rust','file HASH winhttp.dll') 'E:\SteamLibrary\steamapps\common\Rust') 'Matching receipt rejected'
  Assert (-not (Test-SkateRecoveryFolder @('rust E:\SteamLibrary\steamapps\common\Rust') 'C:\Program Files (x86)\Steam\steamapps\common\Rust')) 'Another library accepted'
  Assert (-not (Test-SkateRecoveryFolder @('rust E:\Rust','rust E:\Rust') 'E:\Rust')) 'Ambiguous receipt accepted'
}
Scenario 'legacy cleanup receipt remains valid only for historical C folder' {
  Assert (Test-SkateRecoveryFolder @('put there yesterday','file HASH winhttp.dll') "$default\steamapps\common\Rust") 'Historical C cleanup broken'
  Assert (-not (Test-SkateRecoveryFolder @('put there yesterday') 'E:\Rust')) 'Unbound receipt accepted outside historical path'
}
Scenario 'cleanup preserves another-library receipt even if selected folder is empty' {
  $savedLookup = ${function:Get-SkateRustFolder}
  function Get-SkateRustFolder { return @{Found=$true; Rust='C:\Program Files (x86)\Steam\steamapps\common\Rust'} }
  function Get-Process { param($Name, $ErrorAction) }
  function Remove-Item { throw 'Cleanup tried to delete another-library receipt' }
  $fixtures.Files["$HOME\Downloads\claude-loader-probe\loader-in-rust-folder.txt"] = 'rust E:\SteamLibrary\steamapps\common\Rust'
  try {
    $output = & (Join-Path (Split-Path $Diagnostics) 'clean.ps1')
    Assert (("$output") -like '*ABORT*another Rust folder*') 'Cleanup did not verify receipt binding first'
  } finally { Set-Item Function:Get-SkateRustFolder $savedLookup }
}
Scenario 'cleanup preserves receipt when library unavailable' {
  $savedLookup = ${function:Get-SkateRustFolder}
  function Get-SkateRustFolder { return @{Found=$false; Rust='X:\Missing'} }
  function Remove-Item { throw 'Cleanup tried to delete the recovery receipt' }
  try {
    $output = & (Join-Path (Split-Path $Diagnostics) 'clean.ps1')
    Assert (("$output") -like '*ABORT*recovery note was kept*') 'Cleanup did not stop safely'
  } finally { Set-Item Function:Get-SkateRustFolder $savedLookup }
}
# Exercise real file decoding in addition to fixture-backed Windows discovery.
Remove-Item Function:Get-Content
Scenario 'real UTF-8 KeyValues file preserves Unicode directory' {
  $tempFile = [IO.Path]::GetTempFileName()
  try {
    [IO.File]::WriteAllText($tempFile, '"AppState" { "appid" "252490" "installdir" "R' + [char]0x00fc + 'st" "buildid" "12345" }', (New-Object Text.UTF8Encoding($false)))
    $app = (Read-SkateKeyValues $tempFile)['AppState']
    Assert ($app['installdir'] -eq ('R' + [char]0x00fc + 'st')) 'UTF-8 path corrupted'
  } finally { [IO.File]::Delete($tempFile) }
}
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
  Remove-Item Function:Test-Path
  Scenario 'Windows actual filesystem with non-default library and missing drive' {
    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('skate-discovery-' + [Guid]::NewGuid().ToString('N'))
    $steamRoot = Join-Path $tempRoot 'Steam'; $rustLibrary = Join-Path $tempRoot ('Library ' + [char]0x00fc)
    try {
      $null = New-Item -ItemType Directory -Force -Path @((Join-Path $steamRoot 'steamapps'), (Join-Path $rustLibrary 'steamapps/common/Rust'))
      [IO.File]::WriteAllText((Join-Path $steamRoot 'steam.exe'), '')
      [IO.File]::WriteAllText((Join-Path $rustLibrary 'steamapps/common/Rust/RustClient.exe'), '')
      [IO.File]::WriteAllText((Join-Path $steamRoot 'steamapps/libraryfolders.vdf'), ('"libraryfolders" { "1" { "path" "' + $rustLibrary.Replace('\','\\') + '" } }'), (New-Object Text.UTF8Encoding($false)))
      [IO.File]::WriteAllText((Join-Path $rustLibrary 'steamapps/appmanifest_252490.acf'), '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "12345" }')
      $fixtures.Registry['HKCU:\Software\Valve\Steam'] = @{SteamPath=$steamRoot}
      $r = Get-SkateRustFolder
      Assert ($r.Found -and $r.Rust -eq (Join-Path $rustLibrary 'steamapps\common\Rust')) 'Real Windows discovery failed'
      $missingDrive = @('Z','Y','X','W' | Where-Object { -not (Get-PSDrive $_ -ErrorAction SilentlyContinue) }) | Select-Object -First 1
      if (-not $missingDrive) { throw 'No unused fixture drive available' }
      Assert (-not (Test-SkateFile ($missingDrive + ':\not-present\RustClient.exe'))) 'Missing drive detected'
    } finally { Microsoft.PowerShell.Management\Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
  }
} else { Write-Host 'SKIP Windows actual-filesystem scenario (requires Windows)' }
# Parse every modified script with the real PowerShell parser.
foreach ($name in 'diagnostics','install','skate','session','clean') {
  Scenario "parse $name.ps1" {
    $tokens = $null; $errors = $null
    $path = Join-Path (Split-Path $Diagnostics) "$name.ps1"
    $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
    Assert ($errors.Count -eq 0) ($errors -join '; ')
  }
}
Write-Host "$script:passed passed; $script:failed failed"
if ($script:failed) { exit 1 }
