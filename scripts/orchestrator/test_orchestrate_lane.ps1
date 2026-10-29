#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (lane), driven by a fake -ClaudeExe (testdata/fake-claude.ps1) and a fake -Python
# (testdata/fake-python.ps1, which records every `land_lane.py tick`); no Pester, no real session. Proves: the landing lane
# ticks for as long as the supervisor lives, unit or no unit (kb/Work PB3639): through a failure backoff (the same
# Wait-Until a budget hold sleeps in), on its own -LandLaneSeconds period, never once STOP exists, and never at -LandLaneSeconds 0.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_lane.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

function Run-Lane([string]$case, [string[]]$extra) {
    # Run-Orch fixes -BackoffBaseSeconds, which these cases set themselves, so the supervisor is started here.
    $coord = Join-Path $Root $case
    New-Item -ItemType Directory -Force -Path $coord | Out-Null
    $env:FAKE_CLAUDE_MODE = 'fastfail'
    $env:FAKE_CLAUDE_MARK = Join-Path $coord 'fake-invocations.txt'
    $out = & pwsh -NoProfile -File $Orch -ClaudeExe $Fake -CoordDir $coord -RepoDir $FxRepo -TelemetryDir $Tele -IdleCloseSeconds 1 `
        -FastFailSeconds 120 -Unit wave -Python $FakePython @extra 2>&1
    $code = $LASTEXITCODE
    $ticks = Join-Path $coord 'fake-python-lane-ticks.txt'
    return [pscustomobject]@{ code = $code; out = ($out -join "`n"); coord = $coord
        runs = $(if (Test-Path $env:FAKE_CLAUDE_MARK) { @(Get-Content $env:FAKE_CLAUDE_MARK).Count } else { 0 })
        ticks = $(if (Test-Path $ticks) { @(Get-Content $ticks).Count } else { 0 })
        review = $(if (Test-Path (Join-Path $coord 'fake-python-review-ticks.txt')) { @(Get-Content (Join-Path $coord 'fake-python-review-ticks.txt')) } else { @() }) }
}

# 1. THE LANE TICKS IN A BACKOFF, AND NOT ONCE STOP EXISTS. One unit fails at once and the supervisor backs off 600 s: no unit
# runs. The lane ticks every second of it (the unit's own are few: the loop's top and its start, and a period of 1 s if it lasts that long), until the
# fake python's 8th tick creates STOP: the wait ends at once ('STOP during the backoff') and no 9th tick starts, so the count
# is exactly 8. Without the fix the backoff never ticks, STOP never appears and the supervisor runs a second unit after 600 s
# (-MaxUnits 2 ends the run there, so the regression fails instead of looping on).
# The backoff is long enough that only STOP can end it: a fixed time limit must never decide the verdict (kb/Work PB3708). At 30 s it
# raced the 8th tick, one `pwsh` start of the fake python per second, so a host where a tick took over about 5 s ran a second unit.
$env:FAKE_PYTHON_CHECKPOINT_SECONDS = '0'   # the fake's slow first checkpoint (PB2590) would keep the unit, and its ticks, alive for 40 s
$env:FAKE_PYTHON_STOP_AFTER_TICKS = '8'
$r = Run-Lane 'lanebackoff' @('-BackoffBaseSeconds', '600', '-LandLaneSeconds', '1', '-MaxUnits', '2')
Remove-Item Env:\FAKE_PYTHON_STOP_AFTER_TICKS
Check 'backoff: the lane ticked until STOP appeared, then never again' $r.ticks 8
Check 'backoff: only the one unit ran (the 600 s backoff ended on STOP, not on its timer)' $r.runs 1
Check 'backoff: the wait ended on STOP' ($r.out -match 'STOP during the backoff') $true
Check 'backoff: a clean stop' $r.code 0
# THE REVIEW LANE (kb/Work PB3608) is chained after the landing lane's tick: one review tick per landing tick that completed (the last
# one, running when STOP appeared, may not have), each carrying the supervisor's cap, its permission mode and the resolved launch of -ClaudeExe.
Check 'review lane: it ticked beside the landing lane (at least once, never more than the landing lane did)' ($r.review.Count -ge 1 -and $r.review.Count -le $r.ticks) $true
Check 'review lane: the tick carries the cap, the permission mode and the resolved claude launch' ($r.review[0] -match ' --max-agents 3 ' -and $r.review[0] -match ' --permission-mode bypassPermissions ' -and $r.review[0] -match ' --claude=-File ' -and $r.review[0] -match 'fake-claude\.ps1') $true   # (this fake ps1 sees `--claude=C:\...` split at the colon; the real python gets each piece whole)

# 2. -LandLaneSeconds 0 runs no lane anywhere, a backoff included.
$r = Run-Lane 'lanezero' @('-BackoffBaseSeconds', '2', '-LandLaneSeconds', '0', '-MaxUnits', '2')
Check 'no lane at -LandLaneSeconds 0: two units ran' $r.runs 2
Check 'no lane at -LandLaneSeconds 0: not one tick' $r.ticks 0
Check 'no lane at -LandLaneSeconds 0: no review lane either' $r.review.Count 0

# 3. -ReviewAgents 0 runs the landing lane and no review lane.
$r = Run-Lane 'reviewzero' @('-BackoffBaseSeconds', '2', '-LandLaneSeconds', '1', '-ReviewAgents', '0', '-MaxUnits', '2')
Check 'review lane off at -ReviewAgents 0: the landing lane still ticked' ($r.ticks -ge 1) $true
Check 'review lane off at -ReviewAgents 0: not one review tick' $r.review.Count 0

Complete-OrchTest 'lane'
