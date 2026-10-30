# A `python` for the orchestrate.ps1 self-tests (orchestrate.ps1 -Python): every call goes to the real python, except
#  - the FIRST `checkpoint.py write` in a coordination directory, which first sleeps FAKE_PYTHON_CHECKPOINT_SECONDS
#    (default 40): the worktree probe on a loaded host (kb/Work PB2590, 2026-10-10: minutes with six gates running). A
#    supervisor whose event loop waits on that write cannot read the unit or tick its startup watchdog until it returns.
#  - every `land_lane.py ... tick`, which only appends a line to `fake-python-lane-ticks.txt` in the coordination directory
#    (the supervisor's tick is what a case measures, not the lane's survey of a scratch repository: kb/Work PB3639); with
#    FAKE_PYTHON_STOP_AFTER_TICKS=N the Nth tick creates the loop's STOP file, so a case can prove that no tick starts once
#    STOP exists.
#  - every `review_lane.py ... tick`, which appends its argument list to `fake-python-review-ticks.txt` (kb/Work PB3608).
#  - with FAKE_PYTHON_STDERR=<script file name>, that script's calls, which write one line to stderr first (kb/Work PB4046).
# Every landing-lane tick and every `adopt_main.py` and `ledger_state.py` call appends its interval
# (`tick|adopt|ledger <start ticks> <end ticks>`, UTC) to `fake-python-timeline.txt` (a tick also its start, `tickstart
# <ticks>`, at once). With FAKE_PYTHON_TICK_AFTER_UNIT=N a tick lasts until the supervisor has recorded a unit
# (`units.jsonl` exists) and N seconds more, so it is still running when the unit ends however loaded the host (review
# R2-2), and a case can prove the supervisor never fetches the checkout while its lane ticks (kb/Work PB4046: their
# fetches raced on one ref).
$ErrorActionPreference = 'Stop'
$Timeline = Join-Path $env:COBOL_COORD_DIR 'fake-python-timeline.txt'
if ($args.Count -ge 2 -and ([string]$args[0]).EndsWith('checkpoint.py') -and $args[1] -eq 'write') {
    $mark = Join-Path $env:COBOL_COORD_DIR 'fake-python-slow-checkpoint'
    if (-not (Test-Path $mark)) {
        Set-Content -Path $mark -Value (Get-Date -Format o)
        Start-Sleep -Seconds ([int]($env:FAKE_PYTHON_CHECKPOINT_SECONDS ?? '40'))
    }
}
if ($args.Count -ge 1 -and ([string]$args[0]).EndsWith('land_lane.py') -and $args -contains 'tick') {
    $began = [DateTime]::UtcNow.Ticks
    Add-Content -Path $Timeline -Value "tickstart $began"   # a tick the supervisor outlives still counts, open-ended
    $log = Join-Path $env:COBOL_COORD_DIR 'fake-python-lane-ticks.txt'
    Add-Content -Path $log -Value (Get-Date -Format o)
    if ($env:FAKE_PYTHON_STOP_AFTER_TICKS -and @(Get-Content $log).Count -ge [int]$env:FAKE_PYTHON_STOP_AFTER_TICKS) {
        Set-Content -Path (Join-Path $env:COBOL_COORD_DIR 'STOP') -Value 'fake-python.ps1'
    }
    if ($env:FAKE_PYTHON_TICK_AFTER_UNIT) {
        $units = Join-Path $env:COBOL_COORD_DIR 'units.jsonl'
        $guard = (Get-Date).AddMinutes(5)   # a hang guard, never a verdict: a run that records no unit ends the wait
        while (-not (Test-Path $units) -and (Get-Date) -lt $guard) { Start-Sleep -Milliseconds 200 }
        Start-Sleep -Seconds ([int]$env:FAKE_PYTHON_TICK_AFTER_UNIT)
    }
    Add-Content -Path $Timeline -Value "tick $began $([DateTime]::UtcNow.Ticks)"
    exit 0
}
if ($args.Count -ge 1 -and ([string]$args[0]).EndsWith('review_lane.py') -and $args -contains 'tick') {
    # the review lane's tick (kb/Work PB3608): one line per tick, the whole argument list, so a case can read what the supervisor passed
    Add-Content -Path (Join-Path $env:COBOL_COORD_DIR 'fake-python-review-ticks.txt') -Value ($args -join ' ')
    exit 0
}
# FAKE_PYTHON_STDERR=<script file name>: that script's call first writes one line to stderr, then runs as it is, so a
# case can prove the supervisor says a tool's stderr and still parses its answer (kb/Work PB4046)
if ($env:FAKE_PYTHON_STDERR -and $args.Count -ge 1 -and ([string]$args[0]).EndsWith($env:FAKE_PYTHON_STDERR)) {
    Write-Error "fake-python: a warning on stderr from $($env:FAKE_PYTHON_STDERR)" -ErrorAction Continue
}
$began = [DateTime]::UtcNow.Ticks
& python @args
$code = $LASTEXITCODE
foreach ($kind in @(@('adopt_main.py', 'adopt'), @('ledger_state.py', 'ledger'))) {
    if ($args.Count -ge 1 -and ([string]$args[0]).EndsWith($kind[0])) { Add-Content -Path $Timeline -Value "$($kind[1]) $began $([DateTime]::UtcNow.Ticks)" }
}
exit $code
