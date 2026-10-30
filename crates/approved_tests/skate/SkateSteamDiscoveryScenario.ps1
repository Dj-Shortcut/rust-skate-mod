# Owner requested focused regression tests for PR #621 on 30 October 2026.
# Executes production PowerShell; registry and Windows file visibility are fixture-backed.
# Does not install a pack, start Steam/Rust, or touch any game folder.
param([string]$Diagnostics = "$PSScriptRoot/../../../mods/rust/client/tools/diagnostics.ps1")
$ErrorActionPreference = 'Stop'
. $Diagnostics
$script:files = @{}; $script:registry = @{}; $script:passed = 0; $script:failed = 0
function Test-Path {
  param($LiteralPath, $PathType, $ErrorAction)
  if ($LiteralPath -like 'X:*') { throw 'Drive unavailable' }
  return $script:files.ContainsKey($LiteralPath.Replace('\\', '\'))
}
function Get-ItemProperty {
  param($Path, $ErrorAction)
  if (-not $script:registry.ContainsKey($Path)) { throw 'Registry key unavailable' }
  return [pscustomobject]$script:registry[$Path]
}
function Get-Content {
  param($LiteralPath, [switch]$Raw, $Encoding, $ErrorAction)
  if (-not $script:files.ContainsKey($LiteralPath) -or $script:files[$LiteralPath] -eq '!unreadable!') { throw 'File unavailable' }
  $text = $script:files[$LiteralPath]
  if ($Raw) { return $text }
  return $text -split "`n"
}
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Scenario($Label, [scriptblock]$Body) {
  $script:files = @{}; $script:registry = @{}
  try { & $Body; $script:passed++; Write-Host "PASS $Label" }
  catch { $script:failed++; Write-Host "FAIL ${Label}: $_" }
}
$default = 'C:\Program Files (x86)\Steam'; $other = 'D:\Steam'; $library = 'E:\SteamLibrary'
function Add-Rust($Library, $Manifest = '"AppState" { "appid" "252490" "installdir" "Rust" "buildid" "12345" }', $Directory = 'Rust') {
  $script:files["$Library\steamapps\appmanifest_252490.acf"] = ($Manifest -replace '\s+(?="(?:appid|installdir|buildid|nested)"|})', "`n")
  $script:files["$Library\steamapps\common\$Directory\RustClient.exe"] = ''
}
function Add-Library($Text) { $script:files["$default\steamapps\libraryfolders.vdf"] = $Text }
Scenario 'existing C installation keeps identical paths' {
  Add-Rust $default
  $r = Get-SkateRustFolder
  Assert ($r.Found -and $r.Rust -eq "$default\steamapps\common\Rust" -and $r.Acf -eq "$default\steamapps\appmanifest_252490.acf") 'C paths changed'
}
Scenario 'HKCU Steam wins over stale C executable' {
  $script:files["$default\steam.exe"] = ''; $script:files["$other\steam.exe"] = ''
  $script:registry['HKCU:\Software\Valve\Steam'] = @{SteamPath='D:/Steam/'}
  Assert ((Get-SkateSteamFolder) -eq $other) 'Ignored current user Steam'
}
foreach ($key in 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam','HKLM:\SOFTWARE\Valve\Steam') {
  Scenario "machine registry fallback $key" {
    $script:registry['HKCU:\Software\Valve\Steam'] = @{SteamPath='X:/Missing'}
    $script:registry[$key] = @{InstallPath=$other}; $script:files["$other\steam.exe"] = ''
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
  $script:files["$default\steamapps\appmanifest_252490.acf"] = '!unreadable!'; Add-Rust $library
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
      $script:registry['HKCU:\Software\Valve\Steam'] = @{SteamPath=$steamRoot}
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
