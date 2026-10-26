#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the autostart entry is a .cmd in the Windows Startup folder that opens Windows Terminal (%LOCALAPPDATA%, wt.exe)
# Self-test for autostart.ps1 and install-autostart.ps1: temp directories only, nothing real is installed or started.
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$repo = Split-Path (Split-Path $here -Parent) -Parent
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("autostart-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$checks = 0; $failed = @()
function Check([string]$name, [bool]$ok) { $script:checks++; if (-not $ok) { $script:failed += $name } }

try {
    $startup = Join-Path $tmp 'Startup'
    $replaced = Join-Path $tmp 'replaced'
    New-Item -ItemType Directory -Force -Path $startup | Out-Null
    Set-Content (Join-Path $startup 'wiseowl-cobol-autoresume.cmd') '@echo old startup entry'
    Set-Content (Join-Path $startup 'earlier-autoresume.cmd') 'pwsh -File C:/r/scripts/orchestrator/autostart.ps1'
    Set-Content (Join-Path $startup 'unrelated.cmd') '@echo another program'
    $installer = Join-Path $here 'install-autostart.ps1'

    $dry = (& pwsh -NoProfile -File $installer -StartupDir $startup -ReplacedDir $replaced -Wt 'C:/x/wt.exe' -DryRun) -join "`n"
    Check 'installer dry run shows the entry' ($dry -match 'orchestrator/autostart\.ps1' -and $dry -match '-w cobol-supervisor')
    Check 'installer dry run names the other entry that starts the loop' ($dry -match 'earlier-autoresume\.cmd' -and $dry -notmatch 'unrelated\.cmd')
    Check 'installer dry run changed nothing' ((Get-Content (Join-Path $startup 'wiseowl-cobol-autoresume.cmd') -Raw) -match 'old startup entry' -and (Test-Path (Join-Path $startup 'earlier-autoresume.cmd')) -and -not (Test-Path $replaced))

    & pwsh -NoProfile -File $installer -StartupDir $startup -ReplacedDir $replaced -Wt 'C:/x/wt.exe' | Out-Null
    $cmd = Get-Content (Join-Path $startup 'wiseowl-cobol-autoresume.cmd') -Raw
    Check 'install wrote the new entry' ($cmd -match 'orchestrator/autostart\.ps1' -and $cmd -match 'C:/x/wt\.exe')
    Check 'an earlier entry that starts the loop moved to replaced' ((Test-Path (Join-Path $replaced 'earlier-autoresume.cmd')) -and -not (Test-Path (Join-Path $startup 'earlier-autoresume.cmd')))
    Check 'an entry that does not start the loop stays' (Test-Path (Join-Path $startup 'unrelated.cmd'))
    Check 'old startup entry kept as a rollback copy' ((Get-Content (Join-Path $replaced 'wiseowl-cobol-autoresume.cmd') -Raw) -match 'old startup entry')
    & pwsh -NoProfile -File $installer -StartupDir $startup -ReplacedDir $replaced -Wt 'C:/x/wt.exe' | Out-Null
    Check 'install is idempotent' ((Get-Content (Join-Path $startup 'wiseowl-cobol-autoresume.cmd') -Raw) -eq $cmd -and @(Get-ChildItem $startup -Filter '*.cmd').Count -eq 2)
    & pwsh -NoProfile -File $installer -StartupDir $startup -ReplacedDir $replaced -Uninstall | Out-Null
    Check 'uninstall removes the entry' (-not (Test-Path (Join-Path $startup 'wiseowl-cobol-autoresume.cmd')))

    $coord = Join-Path $tmp 'coord'
    $auto = Join-Path $here 'autostart.ps1'
    $clean = (& pwsh -NoProfile -File $auto -DryRun -CoordDir $coord -RepoDir $tmp) -join "`n"
    Check 'dry run names the supervisor with -Watch' ($clean -match 'DRY RUN: pwsh .*orchestrate\.ps1 -Watch')
    Check 'dry run starts nothing' ($clean -notmatch 'starting the supervisor')
    New-Item -ItemType File -Force -Path (Join-Path $coord 'STOP') | Out-Null
    $stopped = (& pwsh -NoProfile -File $auto -DryRun -CoordDir $coord -RepoDir $tmp) -join "`n"
    Check 'STOP prevents the start' ($stopped -match 'STOP is present' -and $stopped -notmatch 'DRY RUN: pwsh')
    Remove-Item (Join-Path $coord 'STOP') -Force
    Set-Content (Join-Path $coord 'OWNER-QUESTIONS.md') 'Q1: which model for the grammar group?'
    $asked = (& pwsh -NoProfile -File $auto -DryRun -CoordDir $coord -RepoDir $tmp) -join "`n"
    Check 'a waiting owner question prevents the start and is shown' ($asked -match 'owner question is waiting' -and $asked -match 'Q1: which model')
}
finally {
    Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
foreach ($f in $failed) { Write-Host "FAIL: $f" }
Write-Host "autostart self-test: $($checks - $failed.Count)/$checks checks $(if ($failed.Count) { 'RED' } else { 'OK' })"
exit ($(if ($failed.Count) { 1 } else { 0 }))
