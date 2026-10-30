# The shared harness of the orchestrate.ps1 self-tests (scripts/orchestrator/test_orchestrate_*.ps1), dot-sourced by
# each part with `$Here` set to scripts/orchestrator. The cases were ONE file until kb/Work PB2563 split them so the
# self-test runner (scripts/self_tests.py) runs them in parallel: one file took 3-6 minutes, longer than every other
# self-test together. It is not a self-test itself (it is not named test_*), so the runner never runs it alone.
# Each part ends with `Complete-OrchTest '<part>'`, which prints its count and exits 0 only when every check passed.
$Orch = Join-Path $Here 'orchestrate.ps1'
$Fake = Join-Path $Here 'testdata/fake-claude.ps1'
$Root = Join-Path ([IO.Path]::GetTempPath()) "orch-test-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$Tele = Join-Path $Root 'telemetry'
New-Item -ItemType Directory -Force -Path $Tele | Out-Null
# The repository the supervisor under test reads (-RepoDir; kb/Work PB2590): a scratch directory that is not a git tree
# (clean, nothing unpushed, no worktrees, as the campaign part's), so no case's timing or unit choice depends on this
# machine's real repository. Its 30-odd worktrees made every checkpoint probe take minutes on a loaded host, and the
# startup watchdog's case went red. A case that needs a git history (the ledger stamp) builds its own scratch repository
# and passes it as -RepoDir (test_orchestrate_ledger.ps1).
$FxRepo = Join-Path $Root 'repo'
New-Item -ItemType Directory -Force -Path $FxRepo | Out-Null
$FakePython = Join-Path $Here 'testdata/fake-python.ps1'
# THE SELF-TESTS NEVER REACH GITHUB (kb/Work PB4046): a `gh` that fails at once comes first on every supervisor run's PATH,
# as on CI, whose step has no token. On a developer's host the real, authenticated gh made the lane tick's GitHub reads
# (queue_facts) take 1-3 s, and that delay alone let the adoption win a race that CI's fast-failing gh lost: the self-test
# passed locally and failed in CI. A case that needs GitHub's answers passes its own fake (-GhExe for adopt_main.py).
$NoGh = Join-Path $Root 'no-gh'
New-Item -ItemType Directory -Force -Path $NoGh | Out-Null
Set-Content -Path (Join-Path $NoGh 'gh.cmd') -Encoding ascii -Value '@echo gh: the orchestrate self-tests never reach GitHub (kb/Work PB4046) 1>&2& exit /b 1'
$script:fails = @()
$script:checked = 0
# A failed check quotes the lines of the newest run's output that say WHY (kb/Work PB4046: the adopt part's restart
# scenario failed in CI twice with only "got 'False', want 'True'", and adopt_main's own reason, on its `adopt main:`
# line, was dropped with the rest of the output). A part names those lines with `$script:Evidence`, a regex.
$script:Evidence = ''
$script:lastRun = $null

function Check([string]$name, $got, $want) {
    $script:checked++
    if ("$got" -ne "$want") {
        $why = if ($script:Evidence -and $script:lastRun) {
            @($script:lastRun.out -split "`n" | Where-Object { $_ -match $script:Evidence } | ForEach-Object { $_.Trim() }) -join ' | '
        } else { '' }
        $script:fails += "${name}: got '$got', want '$want'" + $(if ($why) { " [run '$($script:lastRun.case)': $why]" } else { '' })
    }
}

function Run-Orch([string]$case, [string]$mode, [string[]]$extra, [scriptblock]$before) {
    $coord = Join-Path $Root $case
    New-Item -ItemType Directory -Force -Path $coord | Out-Null
    $mark = Join-Path $coord 'fake-invocations.txt'
    if ($before) { & $before $coord }
    $env:FAKE_CLAUDE_MODE = $mode
    $env:FAKE_CLAUDE_MARK = $mark
    $repo = if ($extra -contains '-RepoDir') { @() } else { @('-RepoDir', $FxRepo) }
    $path = $env:PATH
    $env:PATH = "$NoGh$([IO.Path]::PathSeparator)$path"
    try {
        $out = & pwsh -NoProfile -File $Orch -ClaudeExe $Fake -CoordDir $coord -TelemetryDir $Tele -BackoffBaseSeconds 0 -IdleCloseSeconds 1 @repo @extra 2>&1
        $code = $LASTEXITCODE
    } finally { $env:PATH = $path }
    $runs = if (Test-Path $mark) { @(Get-Content $mark).Count } else { 0 }
    $units = if (Test-Path (Join-Path $coord 'units.jsonl')) { @(Get-Content (Join-Path $coord 'units.jsonl') | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() }
    $script:lastRun = [pscustomobject]@{ case = $case; code = $code; out = ($out -join "`n"); runs = $runs; units = $units; coord = $coord }
    return $script:lastRun
}

function Complete-OrchTest([string]$part) {
    Remove-Item $Root -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($f in $script:fails) { Write-Host "FAIL: $f" }
    Write-Host "orchestrate self-test ($part): $($script:checked - $script:fails.Count)/$($script:checked) checks OK"
    exit $(if ($script:fails.Count) { 1 } else { 0 })
}
