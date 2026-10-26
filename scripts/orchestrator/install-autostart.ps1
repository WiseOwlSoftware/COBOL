#Requires -Version 7.0
<#
.SYNOPSIS
Install (or remove) the logon entry that starts the orchestrator loop in a Windows Terminal tab. Run by the OWNER, once.

.DESCRIPTION
Writes ONE file into the Startup folder, `wiseowl-cobol-autoresume.cmd`, which opens Windows Terminal in the repo and runs
scripts/orchestrator/autostart.ps1 from the repository. The script itself lives in git, so a change to it is a reviewed
commit and not a hand edit under %LOCALAPPDATA%.

EXACTLY ONE ENTRY STARTS THE LOOP: every other Startup entry whose text runs orchestrator/autostart.ps1 (one written
under an earlier name before the product's names were retired, kb/Work PB3837) is moved into
%LOCALAPPDATA%\WiseOwl.Cobol\replaced\, and an entry under this name that does not start the loop is copied there before
it is replaced, so the owner can restore either (a rollback copy, not a compatibility layer: nothing calls it).

-Uninstall removes the entry and leaves the Startup folder without one. -DryRun prints what it would move and write.
Idempotent: running it twice writes the same file and moves nothing the second time.
#>
[CmdletBinding()]
param(
    [string]$StartupDir = [Environment]::GetFolderPath('Startup'),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$ReplacedDir = (Join-Path (Join-Path $env:LOCALAPPDATA 'WiseOwl.Cobol') 'replaced'),
    [string]$Wt = (Join-Path $env:LOCALAPPDATA 'Microsoft/WindowsApps/wt.exe'),
    [switch]$Uninstall,
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'

function Say([string]$m) { Write-Host $m }

$Cmd = Join-Path $StartupDir 'wiseowl-cobol-autoresume.cmd'
$Script = Join-Path (Join-Path $RepoDir 'scripts') 'orchestrator/autostart.ps1'
if (-not (Test-Path $Script)) { throw "autostart.ps1 not found at $Script" }
$StartsTheLoop = 'orchestrator/autostart\.ps1'

$content = @"
@echo off
rem COBOL orchestrator: after desktop login open Windows Terminal in the repo and start the supervisor loop
rem (scripts/orchestrator/autostart.ps1, in git). Replaces the old interactive-session auto-resume (owner 2026-10-04).
rem To stop it from starting: create E:\COBOL-coord\STOP, or run scripts/orchestrator/install-autostart.ps1 -Uninstall.
"$Wt" -w cobol-supervisor -d "$RepoDir" pwsh -NoExit -NoProfile -ExecutionPolicy Bypass -File "$Script"
"@

if ($Uninstall) {
    if ($DryRun) { Say "DRY RUN: would delete $Cmd"; return }
    if (Test-Path $Cmd) { Remove-Item $Cmd -Force; Say "removed $Cmd" } else { Say "nothing to remove: $Cmd does not exist" }
    return
}

# Every OTHER Startup entry that starts the loop: an entry under an earlier name, which would start a second supervisor.
$others = @(if (Test-Path $StartupDir) {
    Get-ChildItem -LiteralPath $StartupDir -Filter '*.cmd' -File |
        Where-Object { $_.FullName -ne $Cmd -and ((Get-Content -LiteralPath $_.FullName -Raw) -match $StartsTheLoop) }
})

if ($DryRun) {
    foreach ($o in $others) { Say "DRY RUN: would move $($o.FullName), which also starts the loop, to $ReplacedDir" }
    Say "DRY RUN: would write $Cmd :"
    Say $content
    return
}

foreach ($o in $others) {
    New-Item -ItemType Directory -Force -Path $ReplacedDir | Out-Null
    Move-Item -LiteralPath $o.FullName (Join-Path $ReplacedDir $o.Name) -Force
    Say "moved $($o.FullName), which also started the loop, to $ReplacedDir"
}
if ((Test-Path $Cmd) -and -not ((Get-Content $Cmd -Raw) -match $StartsTheLoop)) {
    New-Item -ItemType Directory -Force -Path $ReplacedDir | Out-Null
    Copy-Item $Cmd (Join-Path $ReplacedDir 'wiseowl-cobol-autoresume.cmd') -Force
    Say "kept a copy of the old Startup entry in $ReplacedDir"
}
New-Item -ItemType Directory -Force -Path $StartupDir | Out-Null
Set-Content -Path $Cmd -Value $content -Encoding ascii
Say "wrote $Cmd"
Say "It runs at the next logon. Test it now without rebooting:  pwsh -File $Script -DryRun"
