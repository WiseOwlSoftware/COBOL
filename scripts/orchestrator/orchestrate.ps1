#Requires -Version 7.0
<#
.SYNOPSIS
The unattended orchestrator loop: one fresh headless `claude -p` session per bounded unit, chained by handoff files.

.DESCRIPTION
Design: docs/rearchitecture/DESIGN-orchestrator-loop.md (kb/Work PB1981). Each iteration, in this order:
  1. single instance (orchestrate.lock holds the PID; a dead PID's lock is stale and taken over)
  2. STOP file in the coordination directory (or the owner's global scratch\STOP) ends the loop; during a unit it winds
     the unit down gracefully first (STOP-UNIT plus the LOOP's own fleet stop scratch\STOP-loop, so nothing is lost and
     no other session's agents are stopped; kb/Work PB2483), see stop.ps1
  3. circuit breaker: three consecutive units that fail (nonzero exit, invalid or missing handoff, or under
     -FastFailSeconds without a `done` handoff) stop the loop with an owner note; exponential backoff between failures.
     A READ-ONLY unit (the meter) is no failure of the work (kb/Work PB2665): its handoff never replaces the last one, it
     moves no count, and three meter units in a row without a reading close the meter for one reading age and ask the
     owner once, whatever ran in between
  4. budget.py decides go / hold-session / hold-day / stop-week
  5. the next unit: next_unit.py (the OPERATOR's steer handoff.operator.json first, written by steer.ps1 and consumed when
     its unit starts, kb/Work PB2596; -Unit writes the first unit through steer.ps1; then the last handoff's next_unit,
     then the deterministic checks);
     with -Cluster LEAD[,LEAD...] the CAMPAIGN lane (kb/Work PB2120): next_unit.py alternates `campaign` waves
     (plan_wave.py --cluster LEAD) with fix-lane waves while a lead's cluster has a ready note, the ready leads taking
     turns, and a lead's lane ends when its cluster is landed. The alternation is the supervisor's rule: a handoff that
     names `wave` names only a wave, never its lane (kb/Work PB2522), and a lead whose ready note has waited through
     more than one wave-type unit is logged `campaign LEAD ready for N h`; an operator steer naming `campaign` runs the
     first lead
  6. run `claude -p` with the unit prompt (first stream-json message on stdin, which stays open so the session outlives
     a model turn that ends with a Workflow in flight; the supervisor closes it when the stream is idle with no
     background task), a fresh session id, stream-json to logs\; watch the context size: past -MaxContextTokens the
     unit gets STOP-UNIT only (start no new step, launch no fleet; a running fleet finishes), past -HardContextTokens or
     on STOP also the loop's fleet stop, and only then a kill after the grace period (kb/Work PB2597, design 4.3);
     BESIDE the unit, the LANDING LANE: `land_lane.py tick` as the unit starts and every -LandLaneSeconds while it runs,
     in the background and never fatal, lands every branch WAITING TO LAND (kb/Work PB3103, design 4.9); the lane
     ticks on the same period for as long as the supervisor lives, between units, through a budget hold and a
     failure backoff too (Step-Lane, kb/Work PB3639), and right after each of its ticks the REVIEW LANE's
     (`review_lane.py tick`, kb/Work PB3608): one read-only reviewer per branch WAITING FOR REVIEW and one finisher per
     CONFLICT stop, each its own headless session, at most -ReviewAgents at once, charged to the budget; and the PROGRESS line every -ProgressSeconds (progress.py, in
     the background: groups running, done and reviewed, landings, the oldest waiting item; each new stall posted once
     to the operator's mailbox), and past -UnitSoftMinutes the unit gets STOP-UNIT as at the context soft cap (the
     owner's hand-off at about 60 minutes; kb/Work PB3638)
  7. validate handoff.json against handoff.schema.json
  8. append one line to units.jsonl
The Claude ACCOUNT the loop spends is a parameter (kb/Work PB2479; design section 2.1): -ConfigDir names its config dir,
default the one account.py resolves from CLAUDE_CONFIG_DIR. The supervisor exports exactly that account to every child
(CLAUDE_CONFIG_DIR set to the dir, or UNSET for the default account), finds the unit transcripts under that dir, and
names the account in its log header and in every units.jsonl line.
Exit codes: 0 stopped (STOP, -MaxUnits, stop-week, -DryRun), 3 another instance runs, 4 circuit breaker,
5 an owner question is waiting in OWNER-QUESTIONS.md, 2 -Cluster names no kb/Work cluster (or -Unit campaign without it),
or -ConfigDir names a config dir model_rules.json `accounts` does not know.

.EXAMPLE
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -DryRun
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -MaxUnits 1 -Unit meter
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -Cluster PB2108 -DryRun
pwsh -NoProfile -File scripts/orchestrator/orchestrate.ps1 -ConfigDir C:\Users\brent\.claude-acct2
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    # The owner lifts a quota hold for the named work (hold-session / hold-day only; stop-week still ends the loop).
    [switch]$OverrideHold,
    # The executable to run. A test points it at a fake that emits canned stream-json (a test seam, not a wrapper).
    [string]$ClaudeExe = 'claude',
    [string]$CoordDir = $(if ($env:COBOL_COORD_DIR) { $env:COBOL_COORD_DIR } else { 'E:\COBOL-coord' }),
    [string]$RepoDir = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    # The context policy (kb/Work PB2597; design section 4.3 records the measurements behind both numbers).
    # SOFT cap: STOP-UNIT only, so the unit starts no new step and launches no fleet, and a fleet already running finishes.
    [int]$MaxContextTokens = 200000,
    # HARD cap: STOP-UNIT plus the loop's fleet stop, as for STOP; the grace-period kill applies only after this.
    [int]$HardContextTokens = 500000,
    [int]$MaxUnits = 0,
    [ValidateSet('acceptEdits', 'auto', 'bypassPermissions', 'dontAsk', 'plan', 'manual')]
    [string]$PermissionMode = 'bypassPermissions',   # owner 2026-10-04 (D1): allowed for the COBOL work; the repo's hooks are the guard
    [double]$GraceMinutes = 30,
    [int]$BorrowDays = 0,
    [ValidateSet('', 'wave', 'campaign', 'enqueue', 'resume', 'meter')]
    [string]$Unit = '',
    # The campaign lane: the kb/Work clusters (each a lead note's id, or a campaign name; several are comma-separated,
    # because `pwsh -File` passes `-Cluster A,B` as one string) whose waves run between fix-lane waves, in turn.
    [string]$Cluster = '',
    # Test seam: the register directory next_unit.py and work.py read (default kb/Work).
    [string]$WorkDir = '',
    [switch]$Watch,
    [string]$Python = 'python',
    [string]$TelemetryDir = '',
    # The Claude account's config dir (default: account.py's resolution of CLAUDE_CONFIG_DIR, i.e. this process's account).
    [string]$ConfigDir = '',
    [int]$FastFailSeconds = 120,
    [int]$IdleCloseSeconds = 20,
    [int]$CheckpointSeconds = 300,
    # THE LANDING LANE (kb/Work PB3103, PB3639; design section 4.9): for as long as the supervisor lives, `land_lane.py
    # tick` every this many seconds, in the background, and once as each unit starts: while a unit runs, between units,
    # through a budget hold and through a failure backoff. 0 runs no lane (a branch then lands only through an enqueue
    # unit's own `land_lane.py tick`).
    [int]$LandLaneSeconds = 300,
    # THE REVIEW LANE (kb/Work PB3608; design section 4.9.1): the most reviewers and CONFLICT finishers the lane runs at
    # once, each a headless `claude -p --agent` session of its own, started by `review_lane.py tick` right after every
    # landing-lane tick. 0 runs no review lane (the enqueue unit then ticks it once, as it ticks the landing lane).
    [int]$ReviewAgents = 3,
    # THE OWNER'S FIX-CYCLE BAR (kb/Work PB3638; design section 4.3): the progress line every this many seconds while a
    # unit runs (0: none), and the unit's hand-off: past this many minutes STOP-UNIT, the soft cap's wind-down (the unit
    # starts no new step and launches no fleet; a fleet already running finishes and lands). 0 sets no time cap.
    [int]$ProgressSeconds = 600,
    [double]$UnitSoftMinutes = 60,
    [int]$StartupTicks = 300,
    [int]$BackoffBaseSeconds = 60,
    # THE LOOP ADOPTS LANDED CHANGES BY ITSELF (kb/Work PB2699; design section 4.10). The GitHub CLI adopt_main.py asks for
    # CI's state of origin/main (a test seam, like -ClaudeExe: the self-test names a fake).
    [string]$GhExe = 'gh',
    # What a restart carries over, so a supervisor that re-executes itself between units loses nothing (never set by hand):
    # the consecutive failures, the units run so far (-MaxUnits counts them) and when the last unit started.
    [int]$CarryFailures = 0,
    [int]$CarryRan = 0,
    [string]$CarryLastStarted = ''
)
$ErrorActionPreference = 'Stop'
$LaunchParameters = @{} + $PSBoundParameters   # for the in-place restart (Restart-Supervisor)
Set-StrictMode -Version Latest

# The checkout's HEAD at the moment this process loaded its script (PowerShell reads it once, here). adopt_main.py compares
# the supervisor's script at this commit with the checkout's NOW, so a restart follows a fast-forward made by anyone (the
# attended session's `git merge --ff-only` after a ledger publish is owed too), not only the loop's own (kb/Work PB2699).
$LoadedHead = [string](& git -C $RepoDir rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0) { $LoadedHead = '' }

$Here = $PSScriptRoot
$Schema = Join-Path $Here 'handoff.schema.json'
$UnitModel = @{ wave = 'opus'; campaign = 'opus'; enqueue = 'opus'; resume = 'opus'; meter = 'sonnet' }
# READ-ONLY units read the environment (a web page) and touch no branch (kb/Work PB2665; design section 4.5): they are
# invisible to the work chain. Their handoff never replaces handoff.last.json (it only spends the name when the last
# handoff named the unit), no synthesized `resume` stands in for a missing one, they do not move the breaker's count, the
# last-failed flag or the last-started time, and a failure to read is an ENVIRONMENT failure that next_unit.py counts
# per unit type (units.jsonl `environment_failure`) and tells the owner about once.
$ReadOnlyUnit = @('meter')
# A `campaign` unit is a `wave` unit whose plan_wave.py call carries --cluster: one prompt file, one placeholder.
$PromptFile = @{ campaign = 'wave' }
$Allowed = @('opus', 'sonnet')   # never Fable or Mythos: each needs the owner's approval per dispatch (owner 2026-10-02; kb/Work R69 2026-10-06)
New-Item -ItemType Directory -Force -Path $CoordDir, (Join-Path $CoordDir 'logs'), (Join-Path $CoordDir 'scratch') | Out-Null
$env:COBOL_COORD_DIR = $CoordDir
$Lock = Join-Path $CoordDir 'orchestrate.lock'
$StopFile = Join-Path $CoordDir 'STOP'
$StopUnit = Join-Path $CoordDir 'STOP-UNIT'
# The stop files are SCOPED (kb/Work PB2483; coord.py global_stop/fleet_stop; design section 4.6): the owner's GLOBAL stop
# stops every agent of every session, and the loop's FLEET stop only the agents this loop dispatched. The supervisor
# creates only its own fleet stop; a shared scratch\STOP split another session's refuter on 2026-10-07.
$GlobalStop = Join-Path $CoordDir 'scratch/STOP'
$FleetStop = Join-Path $CoordDir 'scratch/STOP-loop'
$Handoff = Join-Path $CoordDir 'handoff.json'
$LastHandoff = Join-Path $CoordDir 'handoff.last.json'
# The operator's steer (steer.ps1, kb/Work PB2596; design section 4.8): wins the next choice, consumed when its unit starts.
$OperatorHandoff = Join-Path $CoordDir 'handoff.operator.json'
$Steer = Join-Path $Here 'steer.ps1'
$UnitsLog = Join-Path $CoordDir 'units.jsonl'
$Questions = Join-Path $CoordDir 'OWNER-QUESTIONS.md'

function Say([string]$m) { Write-Host "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $m" }

function Take-Lock {
    for ($i = 0; $i -lt 2; $i++) {
        try {
            $fs = [System.IO.File]::Open($Lock, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
            $b = [System.Text.Encoding]::UTF8.GetBytes((@{ pid = $PID; started_at = (Get-Date).ToString('o'); host = [Environment]::MachineName } | ConvertTo-Json -Compress))
            $fs.Write($b, 0, $b.Length); $fs.Close()
            return $true
        } catch [System.IO.IOException] {
            $held = $null
            try { $held = Get-Content $Lock -Raw | ConvertFrom-Json } catch { }
            if ($held -and $held.pid -and (Get-Process -Id $held.pid -ErrorAction SilentlyContinue)) {
                Say "another orchestrator is running (PID $($held.pid), since $($held.started_at)); refusing to start"
                return $false
            }
            Say "stale lock (PID $($held.pid) is gone): taking it over"
            Remove-Item $Lock -Force -ErrorAction SilentlyContinue
        }
    }
    return $false
}

# The loop ends on its own STOP or on the owner's global stop (which stops every agent anyway).
function Test-Stop { return (Test-Path $StopFile) -or (Test-Path $GlobalStop) }

# Sleep until $until, waking every minute so STOP is honoured, and when the landing lane's tick is due (the lane spends no
# tokens, so a budget hold or a failure backoff is no reason to stop it, kb/Work PB3639). Returns $false when STOP appeared.
function Wait-Until([datetime]$until) {
    while ((Get-Date) -lt $until) {
        if (Test-Stop) { return $false }
        Step-Lane
        $nap = ($until - (Get-Date)).TotalSeconds
        if ($LandLaneSeconds -gt 0) { $nap = [Math]::Min($nap, $LandLaneSeconds - ((Get-Date) - $script:LastLaneTick).TotalSeconds) }
        Start-Sleep -Seconds ([Math]::Max(1, [Math]::Min(60, $nap)))
    }
    return -not (Test-Stop)
}

# The supervisor's own frequent handoff (checkpoint.py): never fatal, a missed checkpoint must not end the unit.
$script:CheckpointWrite = $null   # the background checkpoint.py write in flight: @{ proc; err }

function Write-Checkpoint([string]$unit, [string]$sessionId, [datetime]$started, $stats, [int]$bgTasks, [datetime]$lastEventAt, [switch]$Wait) {
    # ⛔ NEVER ON THE EVENT LOOP (kb/Work PB2590). checkpoint.py probes every worktree of the repository (git status and
    # rev-list in each: 8 s for 33 worktrees on an idle host, minutes on one running six gates). Called synchronously from
    # the unit's event loop, it stalled everything the loop does: no stdout read, no startup-watchdog tick, no STOP or
    # context check. Gate 20261011T041736Z-d08bb8: the startup self-test's silent unit outslept its 600 s while the first
    # checkpoint ran, so the watchdog found it exited and killed nothing. So the loop's writes run in the BACKGROUND, one at
    # a time (a write due while one runs is skipped: the next carries the newer state), and only the final write, after
    # the unit has exited, waits (-Wait: a successor reads it).
    if ($script:CheckpointWrite) {
        $p = $script:CheckpointWrite.proc
        if (-not $p.HasExited) {
            if (-not $Wait) { return }
            $p.WaitForExit()
        }
        if ($p.ExitCode -ne 0) { Say "checkpoint not written (the unit continues): $($script:CheckpointWrite.err.Result)" }
        $script:CheckpointWrite = $null
    }
    $argList = @((Join-Path $Here 'checkpoint.py'), 'write', '--unit', $unit, '--session', $sessionId,
        '--started', $started.ToString('o'), '--last-event', $lastEventAt.ToString('o'), '--calls', "$($stats.calls)",
        '--context', "$($stats.context)", '--cost', "$([double]$(if ($null -ne $stats.cost_usd) { $stats.cost_usd } else { 0 }))",
        '--bg-tasks', "$bgTasks", '--repo', $RepoDir)
    try {
        if ($Wait) { [void](Invoke-Py $argList); return }
        $script:CheckpointWrite = Start-PyBackground $argList
    } catch { Say "checkpoint not written (the unit continues): $_" }
}

# THE LANDING LANE (kb/Work PB3103; design section 4.9). A branch the survey lists WAITING TO LAND goes through
# `enqueue.py land` the moment it is ready, not when the wave that made it ends: `land_lane.py tick` reaps the finished
# landings, surveys the branches and starts each eligible landing DETACHED, so a unit (a wave supervising its fleet, an
# enqueue unit dispatching finishers) runs on while branches land. ⛔ NEVER ON THE EVENT LOOP and NEVER FATAL, exactly as
# the checkpoint (kb/Work PB2590): the tick surveys every worktree and asks GitHub for the pull requests (30 s on an idle
# host), so it runs as a background process, one at a time (a tick due while one runs is skipped), and a failed tick is
# said and the unit continues. The tick itself starts nothing once STOP-UNIT, STOP or the owner's global stop exists
# (a running landing finishes: enqueue.py is idempotent), or after a landing ended QUEUE NOT ACTIVE (HALT).
$script:LaneTick = $null   # the background land_lane.py tick in flight: @{ proc; out; err }

# ONE WAY TO START A PYTHON TOOL BESIDE THE UNIT (the checkpoint, the lane tick, the progress tick): hidden, its output
# read asynchronously in UTF-8, never awaited on the event loop (kb/Work PB2590). Returns @{ proc; out; err }.
function Start-PyBackground([string[]]$ArgList) {
    $exe, $pre = Resolve-Launch $Python
    $psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
    foreach ($a in @($pre) + $ArgList) { $psi.ArgumentList.Add([string]$a) }
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.Environment['PYTHONUTF8'] = '1'
    $proc = [System.Diagnostics.Process]::Start($psi)
    return @{ proc = $proc; out = $proc.StandardOutput.ReadToEndAsync(); err = $proc.StandardError.ReadToEndAsync() }
}

# THE PROGRESS LINE (kb/Work PB3638 defect 3; design section 4.3). The owner, 2026-10-24: "It's stupid to have work
# running with no updates or progress for multiple hours." progress.py reads the dispatch ledger, the reports and the
# landing records (no git, no GitHub), prints one PROGRESS line and a STALL line per item waiting past its limit, and
# posts each new stall once to the operator's mailbox (--post: the unit's log is the message's ref). Background, one at
# a time, never fatal, as the lane tick.
$script:ProgressTick = $null

function Complete-ProgressTick {
    if (-not $script:ProgressTick -or -not $script:ProgressTick.proc.HasExited) { return }
    $t = $script:ProgressTick
    $script:ProgressTick = $null
    foreach ($l in @("$($t.out.Result)" -split "`r?`n" | Where-Object { $_ })) { Say $l }
    if ($t.proc.ExitCode -ne 0) { Say "progress line failed (exit $($t.proc.ExitCode); the unit continues)$(if ($t.err.Result) { ": $(([string]$t.err.Result).Trim() -split "`r?`n" | Select-Object -Last 3)" })" }
}

function Start-ProgressTick([string]$unit, [datetime]$started, [string]$ref) {
    Complete-ProgressTick
    if ($script:ProgressTick) { return }   # the previous tick still runs: this one is skipped
    $argList = @((Join-Path $Here 'progress.py'), '--coord', $CoordDir, '--unit', $unit, '--since', $started.ToString('o'))
    if (Test-Path $ref) { $argList += @('--post', $ref) }
    try { $script:ProgressTick = Start-PyBackground $argList }
    catch { Say "progress line not started (the unit continues): $_" }
}

function Complete-LaneTick {
    # Non-blocking: says a finished tick's lines and its failure, if any; leaves a running one alone.
    if (-not $script:LaneTick -or -not $script:LaneTick.proc.HasExited) { return }
    $t = $script:LaneTick
    $script:LaneTick = $null
    foreach ($l in @("$($t.out.Result)" -split "`r?`n" | Where-Object { $_ })) { Say $l }
    if ($t.proc.ExitCode -ne 0) { Say "land lane tick failed (exit $($t.proc.ExitCode); the unit continues)$(if ($t.err.Result) { ": $(([string]$t.err.Result).Trim() -split "`r?`n" | Select-Object -Last 3)" })" }
    Start-ReviewTick   # on the survey this tick just wrote, a failed tick's stale one included (the review lane refuses an old one)
}

# THE REVIEW LANE (kb/Work PB3608; design section 4.9.1): `review_lane.py tick` right after each landing-lane tick, on the
# survey that tick wrote. It starts a headless reviewer or finisher session DETACHED per branch (they outlive this tick,
# like a landing), and starts none under any stop file or a budget hold. Background and never fatal, as the landing tick.
# The account is the supervisor's own (CLAUDE_CONFIG_DIR is exported at startup, below), so the agents spend what the units do.
$script:ReviewTick = $null   # the background review_lane.py tick in flight: @{ proc; out; err }

function Complete-ReviewTick {
    if (-not $script:ReviewTick -or -not $script:ReviewTick.proc.HasExited) { return }
    $t = $script:ReviewTick
    $script:ReviewTick = $null
    foreach ($l in @("$($t.out.Result)" -split "`r?`n" | Where-Object { $_ })) { Say $l }
    if ($t.proc.ExitCode -ne 0) { Say "review lane tick failed (exit $($t.proc.ExitCode); the unit continues)$(if ($t.err.Result) { ": $(([string]$t.err.Result).Trim() -split "`r?`n" | Select-Object -Last 3)" })" }
}

function Start-ReviewTick {
    if ($LandLaneSeconds -le 0 -or $ReviewAgents -le 0 -or $DryRun) { return }
    Complete-ReviewTick
    if ($script:ReviewTick) { return }   # the previous tick still runs: this one is skipped, the next carries it
    $exe, $pre = Resolve-Launch $ClaudeExe
    $argList = @((Join-Path $Here 'review_lane.py'), '--coord', $CoordDir, 'tick', '--repo', $RepoDir, '--permission-mode', $PermissionMode,
        '--max-agents', "$ReviewAgents", '--borrow-days', "$BorrowDays")
    foreach ($piece in @($exe) + @($pre)) { $argList += "--claude=$piece" }   # the one launch rule: Resolve-Launch
    if ($TelemetryDir) { $argList += @('--telemetry-dir', $TelemetryDir) }
    try { $script:ReviewTick = Start-PyBackground $argList }
    catch { Say "review lane tick not started (the unit continues): $_" }
}

function Start-LaneTick {
    if ($LandLaneSeconds -le 0) { return }
    Complete-LaneTick
    if ($script:LaneTick) { return }   # the previous tick still runs: this one is skipped, the next carries it
    try { $script:LaneTick = Start-PyBackground @((Join-Path $Here 'land_lane.py'), '--coord', $CoordDir, 'tick', '--repo', $RepoDir) }
    catch { Say "land lane tick not started (the unit continues): $_" }
}

# THE ONE LANE TIMER (kb/Work PB3639): every wait and every stretch of the loop calls this, so the lane ticks whether or
# not an LLM unit runs. It says a finished tick, and starts one when -LandLaneSeconds have passed since the last start
# (-Now: at once, as a unit starts, so a branch that became ready while no unit ran lands without waiting a period). It
# starts nothing under STOP: the callers check STOP first, and the tick itself launches nothing under any stop file.
$script:LastLaneTick = [datetime]::MinValue

# Waits, at most $seconds, for a landing-lane tick still running, and says it (kb/Work PB4046): the supervisor's own git
# work on the checkout, the adoption and the ledger check, does not run beside its tick, whose survey fetches and reads
# the same checkout. $true when no tick runs any more; a tick that outlasts the wait is said by the caller, which goes on.
function Wait-LaneTick([int]$seconds = 120) {
    if ($script:LaneTick -and -not $script:LaneTick.proc.HasExited) { [void]$script:LaneTick.proc.WaitForExit($seconds * 1000) }
    Complete-LaneTick
    return -not $script:LaneTick
}

function Step-Lane([switch]$Now) {
    if ($LandLaneSeconds -le 0) { return }
    Complete-LaneTick
    Complete-ReviewTick
    if ($Now -or ((Get-Date) - $script:LastLaneTick).TotalSeconds -ge $LandLaneSeconds) {
        Start-LaneTick
        $script:LastLaneTick = Get-Date
    }
}

# THE LOOP ADOPTS LANDED CHANGES BY ITSELF (kb/Work PB2699; adopt_main.py owns the rule, design section 4.10). Between units,
# when nothing runs, the checkout is fast-forwarded to origin/main once CI's run on it has succeeded. When that changed the
# supervisor's own script, the supervisor restarts in place (below) so a landed process fix applies at the NEXT unit.
function Invoke-AdoptMain {
    try {
        $out = Invoke-Py @((Join-Path $Here 'adopt_main.py'), 'adopt', '--repo', $RepoDir, '--coord', $CoordDir, '--gh', $GhExe, "--loaded=$LoadedHead", '--json')
    } catch { Say "main not adopted (the loop continues on the checkout as it is): $_"; return $null }
    # adopt_main.py fast-forwards before it answers, so an answer it cannot give whole is no "not adopted" (kb/Work PB4046)
    try { $a = $out | ConvertFrom-Json } catch {
        Say "adopt main: adopt_main.py's answer is unreadable (the checkout may have moved; no restart this time): $_"; return $null }
    Say $a.line
    return $a
}

# Re-executes THIS supervisor from the (just fast-forwarded) checkout and returns the child's exit code. All state a
# successor needs is on disk (handoff files, the operator's steer, units.jsonl); the few counters the loop keeps in memory
# ride along as -Carry* parameters. A landing the lane started finishes on the code it started with (it runs detached);
# the lock is released first, so the successor takes it.
function Restart-Supervisor([int]$failures, [int]$ran, $lastStarted) {
    $skip = 'Unit', 'Cluster', 'CarryFailures', 'CarryRan', 'CarryLastStarted'   # -Unit was consumed at start; the rest are set below
    $argList = [Collections.Generic.List[string]]::new()
    foreach ($k in $LaunchParameters.Keys) {
        if ($k -in $skip) { continue }
        $v = $LaunchParameters[$k]
        if ($v -is [switch]) { if ($v) { $argList.Add("-$k") } } else { $argList.Add("-$k"); $argList.Add("$v") }
    }
    if ($Clusters.Count) { $argList.Add('-Cluster'); $argList.Add(($Clusters -join ',')) }
    $argList.Add('-CarryFailures'); $argList.Add("$failures")
    $argList.Add('-CarryRan'); $argList.Add("$ran")
    if ($lastStarted) { $argList.Add('-CarryLastStarted'); $argList.Add($lastStarted.ToString('o')) }
    $self = Join-Path (Join-Path (Join-Path $RepoDir 'scripts') 'orchestrator') 'orchestrate.ps1'
    Say "restarting the supervisor from the adopted checkout: pwsh -File $self $($argList -join ' ')"
    Remove-Item $Lock -Force -ErrorAction SilentlyContinue
    $script:HandedOver = $true
    & (Get-Command pwsh).Source -NoProfile -File $self @argList | Out-Host   # the successor's output is the console's, not this function's result
    return [int]$LASTEXITCODE
}

# THE ONE WAY the supervisor runs a Python tool and reads its answer. Stdout IS the answer, and callers parse it (JSON);
# stderr is diagnostics, said and never parsed (kb/Work PB4046): with `2>&1` folded into the answer, one line on stderr
# from a SUCCESSFUL tool (a warning) made the JSON unreadable. adopt_main.py had already fast-forwarded the checkout, but
# the loop said "main not adopted" and skipped the restart a landing of its own script needs. A tool that FAILS throws
# with both streams, its own message included.
function Invoke-Py([string[]]$ArgList) {
    $all = @(& $Python @ArgList 2>&1)
    $code = $LASTEXITCODE
    $out = @($all | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
    $err = @($all | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
    if ($code -ne 0) { throw "python $($ArgList -join ' ') failed ($code): $(($out + $err) -join "`n")" }
    if ($err) { Say "python $(Split-Path -Leaf $ArgList[0]) wrote to stderr (said, not parsed): $($err -join ' | ')" }
    return ($out -join "`n")
}

function Add-OwnerQuestion([string]$title, [string]$body) {
    Add-Content -Path $Questions -Encoding utf8 -Value "`n## $(Get-Date -Format 'yyyy-MM-dd HH:mm') — $title`n`n$body`n"
}

# next_unit.py reports `meter_unreadable` while METER_ATTEMPTS meter units in a row ended without a reading (PB2665). The
# loop goes on from budget.py's estimate and tells the owner ONCE per streak: the streak's first attempt is the marker
# written into the question, so a restart of the supervisor, or any later iteration, finds it and asks nothing more.
function Report-MeterUnreadable($gate) {
    # ConvertFrom-Json already turns an ISO time into a local DateTime; a string is parsed with its offset.
    $local = { param($t) if ($t -is [datetime]) { $t } else { [datetimeoffset]::Parse([string]$t, [Globalization.CultureInfo]::InvariantCulture).LocalDateTime } }
    $marker = "meter unreadable since $((& $local $gate.since).ToString('o'))"
    if ((Test-Path $Questions) -and (Select-String -Path $Questions -SimpleMatch $marker -Quiet)) { return }
    $retry = & $local $gate.retry_at
    $say = "the last $($gate.failures) meter units of $($Account.name) ended without a reading; the next attempt waits until $($retry.ToString('yyyy-MM-dd HH:mm'))"
    if ($DryRun) { Say "DRY RUN: would tell the owner: $say"; return }
    Add-OwnerQuestion 'meter unreadable' ("$say.`n`nUsually the Claude in Chrome extension is not connected, or is signed into another account than $($Account.name) (units/meter.md step 1). " +
        "The loop goes on from budget.py's estimate, tries the meter again at that time and then once per maximum reading age, and counts none of these against the circuit breaker. " +
        "Reconnect or sign in, or record a reading yourself: python scripts/orchestrator/budget.py --record --weekly <W> --session <S> --session-reset <ISO time>.`n`n($marker)")
    Say "owner question written to ${Questions}: $say (the loop continues)"
}

# A read-only unit never replaces handoff.last.json, so a handoff (or the operator's steer) that NAMED it would name it
# again for ever. Spend the name and keep everything else the handoff said (branches_pending, workflow, outcome).
function Clear-NamedUnit([string]$unit) {
    if (-not (Test-Path $LastHandoff)) { return }
    try {
        $prev = Get-Content $LastHandoff -Raw | ConvertFrom-Json
        if ($prev.PSObject.Properties['next_unit'] -and $prev.next_unit -eq $unit) {
            $prev.PSObject.Properties.Remove('next_unit')
            $prev.PSObject.Properties.Remove('next_unit_reason')
            $prev | ConvertTo-Json -Depth 10 | Set-Content -Path $LastHandoff -Encoding utf8
        }
    } catch { Say "the name '$unit' was not spent in ${LastHandoff} (the loop continues): $_" }
}

function Resolve-Launch([string]$exe) {
    # How to start the executable: a native exe directly; a .ps1 through pwsh; a .cmd/.bat through cmd. Launch
    # mechanics only, so -ClaudeExe can name the real CLI or a test fake.
    $cmd = Get-Command $exe -ErrorAction Stop
    $path = $cmd.Source
    switch ([IO.Path]::GetExtension($path).ToLowerInvariant()) {
        '.ps1' { return @((Get-Command pwsh).Source, @('-NoProfile', '-File', $path)) }
        { $_ -in '.cmd', '.bat' } { return @($env:ComSpec, @('/d', '/c', $path)) }
        default { return @($path, @()) }
    }
}

function Build-Prompt([string]$unit, [string]$scratch, [string]$sessionId) {
    $tasksDir = (Join-Path $env:TEMP "claude/E--COBOL/$sessionId/tasks") -replace '\\', '/'   # forward slashes: the unit uses it inside bash
    $sub = {
        param($t)
        $t.Replace('{TASKS_DIR}', $tasksDir).Replace('{COORD}', $CoordDir).Replace('{HANDOFF}', $Handoff).Replace('{STOP_UNIT}', $StopUnit).
           Replace('{PREV_HANDOFF}', $LastHandoff).Replace('{SCRATCH}', $scratch).Replace('{FLEET_STOP}', $FleetStop).Replace('{GLOBAL_STOP}', $GlobalStop).
           Replace('{BORROW_DAYS}', "$BorrowDays").Replace('{CLUSTER_ARG}', $(if ($unit -eq 'campaign') { " --cluster $UnitCluster" } else { '' }))
    }
    $common = & $sub (Get-Content (Join-Path $Here 'units/common.md') -Raw)
    $own = & $sub (Get-Content (Join-Path $Here "units/$(Get-PromptFile $unit).md") -Raw)
    return "$common`n$own"
}

function Get-PromptFile([string]$unit) { if ($PromptFile.ContainsKey($unit)) { $PromptFile[$unit] } else { $unit } }

function Get-ProjectTranscriptDir([string]$sessionId) {
    # The account's config dir holds the unit transcripts (account.py project_dir: projects/<repo key>), never $HOME/.claude.
    return Join-Path $Account.project_dir $sessionId
}

function Invoke-Unit([string]$unit, [string]$model, [string]$sessionId, [string]$logBase) {
    $scratch = Join-Path $CoordDir 'scratch'
    $prompt = Build-Prompt $unit $scratch $sessionId
    # The prompt goes in as the first stream-json message and stdin STAYS OPEN. Once a one-shot `claude -p` model ends a
    # turn the process waits for background tasks only up to CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS (600 s), then
    # terminates them ("Background tasks still running after 600s; terminating": wave 1017's Workflow, 653 s). With stdin
    # open the session never enters that exit wait, and a finishing task's notification wakes the model (probed
    # 2026-10-04 with the ceiling set to 5 s: turn ended at 6 s, task done and model woken at 30 s, session alive).
    # The SUPERVISOR decides when the unit is over (idle, below), never the model's choice of words.
    $claudeArgs = @('-p', '--input-format', 'stream-json', '--model', $model, '--permission-mode', $PermissionMode,
        '--permission-prompts', 'none', '--output-format', 'stream-json', '--verbose', '--session-id', $sessionId)
    if ($unit -eq 'meter') { $claudeArgs += '--chrome' }
    $exe, $pre = Resolve-Launch $ClaudeExe
    $psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
    foreach ($a in @($pre) + $claudeArgs) { $psi.ArgumentList.Add([string]$a) }
    $psi.WorkingDirectory = $RepoDir
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardInputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $psi.Environment['COBOL_COORD_DIR'] = $CoordDir
    # Marks the session as a loop unit (coord.py LOOP_UNIT_ENV): dispatch_guard.py refuses its fleet launch once STOP-UNIT
    # exists (kb/Work PB2597). An attended session never carries it.
    $psi.Environment['COBOL_LOOP_UNIT'] = $unit
    # Exactly the loop's account: the dir for a named one; UNSET for the default (set to ~/.claude, Claude Code would look
    # for its sign-in in ~/.claude/.claude.json and find none). account.py child_config_dir is the one rule.
    if ($Account.child_config_dir) { $psi.Environment['CLAUDE_CONFIG_DIR'] = $Account.child_config_dir } else { [void]$psi.Environment.Remove('CLAUDE_CONFIG_DIR') }

    $stats = [ordered]@{ calls = 0; input = 0; output = 0; cache_read = 0; cache_creation = 0; peak_context = 0; peak_subagent_context = 0;
        context = 0; cost_usd = $null; stop_unit_sent = $false; fleet_stop_sent = $false; wind_down = $null; fleet_at_wind_down = $false
        killed = $false }
    $seen = @{}
    $graceMin = if ($unit -in @('wave', 'campaign')) { 3 * $GraceMinutes } else { $GraceMinutes }
    $stopAt = $null
    $watcher = $null
    $log = [System.IO.StreamWriter]::new("$logBase.jsonl", $false, [System.Text.UTF8Encoding]::new($false))
    $proc = [System.Diagnostics.Process]::Start($psi)
    $errTask = $proc.StandardError.ReadToEndAsync()
    # ⛔ NEVER WRITE THE PROMPT SYNCHRONOUSLY. The CLI writes a large `init` event to stdout BEFORE it reads stdin; once that
    # fills the stdout pipe it blocks until we read, and a synchronous write of a prompt larger than the stdin pipe buffer
    # (about 4 KB: the wave prompt grew past it on 2026-10-04) blocks until it reads: a cross-pipe deadlock, found as a wave
    # unit that sat two hours with a 0-byte log. The write is a task, and the read loop below starts at once.
    $promptWrite = $proc.StandardInput.WriteLineAsync((@{ type = 'user'; message = @{ role = 'user'; content = $prompt } } | ConvertTo-Json -Compress -Depth 5))
    $startTicks = 0          # loop ticks (one per second waited) with no event at all: immune to the clock, unlike a timestamp
    $anyEvent = $false
    $resultSeen = $false     # the model has ended a turn and nothing has started since
    $bgTasks = 0             # background tasks (a Workflow, a gate) the session reports as running
    $fleetTasks = 0          # of those, the fleets: a Workflow (local_workflow) or a background agent (local_agent)
    $lastEventAt = Get-Date
    $stdinClosed = $false
    $unitStarted = Get-Date
    $lastCheckpoint = Get-Date
    $lastBgChecked = 0
    Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt   # one at once: a crash in the first minutes still leaves one
    Step-Lane -Now   # one at once too: a branch that became ready while no unit ran lands without waiting a period
    $lastProgress = Get-Date   # the first progress line comes one period in: at the start nothing has moved yet
    if ($Watch -and $unit -eq 'wave') {
        try {
            $watcher = Start-Process -PassThru -WindowStyle Hidden -FilePath (Get-Command pwsh).Source -ArgumentList @(
                '-NoProfile', '-File', (Join-Path $Here 'open-watchers.ps1'), '-SessionDir', (Get-ProjectTranscriptDir $sessionId))
        } catch { Say "watchers did not start (the unit continues): $_" }
    }
    try {
        $read = $proc.StandardOutput.ReadLineAsync()
        while ($true) {
            if (-not $read.Wait(1000)) {
                # Startup watchdog: a unit that has emitted NO event after -StartupTicks ticks is hung at start (a deadlock, a
                # network or hook stall); a healthy session emits its init event within seconds. Count ticks, not wall time,
                # so a suspended machine cannot trip it. The unit counts as failed and the supervisor synthesizes its handoff.
                if (-not $anyEvent) {
                    $startTicks++
                    if ($startTicks -ge $StartupTicks -and -not $proc.HasExited) {
                        Say "no event from the unit in $StartupTicks s of waiting: it is hung at start; killing it (the loop continues)"
                        $proc.Kill($true)
                        $stats.killed = $true
                    }
                }
            }
            if ($promptWrite -and $promptWrite.IsCompleted) {
                if ($promptWrite.IsFaulted) {
                    Say "the prompt could not be delivered ($($promptWrite.Exception.GetBaseException().Message)); killing the unit"
                    if (-not $proc.HasExited) { $proc.Kill($true) }
                    $stats.killed = $true
                }
                $promptWrite = $null
            }
            if ($read.IsCompleted) {
                $line = $read.Result
                if ($null -eq $line) { break }
                $anyEvent = $true
                $log.WriteLine($line); $log.Flush()
                $lastEventAt = Get-Date
                $ev = $null
                try { $ev = $line | ConvertFrom-Json -Depth 64 } catch { }
                if ($ev -and $ev.PSObject.Properties['type']) {
                    if ($ev.type -eq 'assistant') { $resultSeen = $false }
                    elseif ($ev.type -eq 'result') { $resultSeen = $true }
                    elseif ($ev.type -eq 'system' -and $ev.subtype -eq 'background_tasks_changed') {
                        $bgTasks = @($ev.tasks).Count
                        $fleetTasks = @($ev.tasks | Where-Object { $_ -and $_.PSObject.Properties['task_type'] -and $_.task_type -in @('local_workflow', 'local_agent') }).Count
                    }
                    if ($ev.type -eq 'assistant' -and $ev.message -and $ev.message.PSObject.Properties['usage']) {
                        # stream-json repeats one model call's usage on each content block: count each message id once.
                        $id = [string]$ev.message.id
                        if (-not $seen.ContainsKey($id)) {
                            $seen[$id] = $true
                            $u = $ev.message.usage
                            $g = { param($n) if ($u.PSObject.Properties[$n]) { [long]$u.$n } else { 0 } }
                            $stats.calls++
                            $stats.input += & $g 'input_tokens'
                            $stats.output += & $g 'output_tokens'
                            $stats.cache_read += & $g 'cache_read_input_tokens'
                            $stats.cache_creation += & $g 'cache_creation_input_tokens'
                            $ctx = (& $g 'input_tokens') + (& $g 'cache_read_input_tokens') + (& $g 'cache_creation_input_tokens')
                            # A subagent's messages stream into this log too, tagged with parent_tool_use_id: they are another
                            # transcript with its own turn cap (maxTurns), so only the unit's OWN messages measure its context.
                            if ($ev.PSObject.Properties['parent_tool_use_id'] -and $ev.parent_tool_use_id) {
                                $stats.peak_subagent_context = [Math]::Max($stats.peak_subagent_context, $ctx)
                            } else {
                                $stats.context = $ctx
                                $stats.peak_context = [Math]::Max($stats.peak_context, $ctx)
                            }
                        }
                    } elseif ($ev.type -eq 'result' -and $ev.PSObject.Properties['total_cost_usd']) {
                        $stats.cost_usd = $ev.total_cost_usd
                    }
                }
                $read = $proc.StandardOutput.ReadLineAsync()
            }
            # The unit is over when its model ended a turn, no background task is running and the stream has been quiet
            # for -IdleCloseSeconds (a finishing task's completion event arrives just after the task list empties, and
            # wakes the model, so a close on the first idle instant could cut a woken turn off).
            if (-not $stdinClosed -and $resultSeen -and $bgTasks -eq 0 -and ((Get-Date) - $lastEventAt).TotalSeconds -ge $IdleCloseSeconds) {
                try { $proc.StandardInput.Close() } catch { }
                $stdinClosed = $true
            }
            # Frequent handoffs: a checkpoint every -CheckpointSeconds, and at once (at most every 10 s) when the set of
            # background tasks changes, because that is when a Workflow starts, a train lands or a gate ends.
            $sinceCp = ((Get-Date) - $lastCheckpoint).TotalSeconds
            if ($CheckpointSeconds -gt 0 -and ($sinceCp -ge $CheckpointSeconds -or ($bgTasks -ne $lastBgChecked -and $sinceCp -ge 10))) {
                Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt
                $lastCheckpoint = Get-Date
                $lastBgChecked = $bgTasks
            }
            # The landing lane beside the unit (kb/Work PB3103): a tick every -LandLaneSeconds, in the background.
            Step-Lane
            # The progress line beside the unit (kb/Work PB3638): every -ProgressSeconds, in the background.
            Complete-ProgressTick
            if ($ProgressSeconds -gt 0 -and ((Get-Date) - $lastProgress).TotalSeconds -ge $ProgressSeconds) {
                Start-ProgressTick $unit $unitStarted "$logBase.jsonl"
                $lastProgress = Get-Date
            }
            # THE CONTEXT POLICY (kb/Work PB2597; design section 4.3). Two levels, so a fleet the unit paid to launch is
            # never thrown away for the unit's own size:
            #   soft  context > -MaxContextTokens: STOP-UNIT only. The unit starts no new step and launches no fleet
            #         (dispatch_guard.py refuses it); a fleet already running finishes and lands, then the unit hands off.
            #         No kill timer: a unit supervising a Workflow legitimately stays for hours.
            #   hard  context > -HardContextTokens, or the owner created STOP: STOP-UNIT plus the LOOP's own fleet stop
            #         (every implementer and lander it dispatched checkpoints and returns SPLIT), then the grace-period kill.
            # Never the global scratch\STOP: that is the owner's, and every session's agents obey it (kb/Work PB2483).
            $windDown = $null
            $stopFleet = $false
            if ($stats.context -gt $HardContextTokens) { $windDown = 'context-hard'; $why = "context $($stats.context) > hard cap ${HardContextTokens}"; $stopFleet = $true }
            elseif (Test-Stop) { $windDown = 'stop'; $why = 'STOP file present'; $stopFleet = $true }
            elseif ($stats.context -gt $MaxContextTokens) { $windDown = 'context-soft'; $why = "context $($stats.context) > soft cap ${MaxContextTokens}" }
            elseif ($UnitSoftMinutes -gt 0 -and ((Get-Date) - $unitStarted).TotalMinutes -gt $UnitSoftMinutes) { $windDown = 'time-soft'; $why = "the unit has run past -UnitSoftMinutes $UnitSoftMinutes (the owner's hand-off, kb/Work PB3638)" }
            if ($stopFleet -and -not $stats.fleet_stop_sent) {
                Say "${why}: winding the unit and its fleet down (STOP-UNIT and the loop's fleet stop $FleetStop; agents checkpoint and return SPLIT, the unit hands off, the loop then ends if STOP is set)"
                # The fleet stop FIRST: the unit acts on STOP-UNIT and then expects the fleet stop to exist (its dispatched
                # agents must see it); written the other way round, a loaded host showed the unit STOP-UNIT alone
                # (test_orchestrate_winddown 4f, train 1037t, 2026-10-08).
                New-Item -ItemType File -Force -Path $FleetStop | Out-Null
                New-Item -ItemType File -Force -Path $StopUnit | Out-Null
                $stats.fleet_stop_sent = $true
                $stopAt = Get-Date
            }
            if ($windDown -and -not $stats.stop_unit_sent) {
                New-Item -ItemType File -Force -Path $StopUnit | Out-Null
                $stats.stop_unit_sent = $true
                $stats.fleet_at_wind_down = $fleetTasks -gt 0
                if (-not $stopFleet) {
                    Say "${why}: STOP-UNIT (the unit starts no new step and launches no fleet; $(if ($fleetTasks) { 'its running fleet finishes and lands first' } else { 'it hands off now' }))"
                }
            }
            if ($windDown -and ($stopFleet -or -not $stats.wind_down)) { $stats.wind_down = $windDown }
            if ($stopAt -and ((Get-Date) - $stopAt).TotalMinutes -gt $graceMin -and -not $proc.HasExited) {
                Say "no handoff $graceMin min after STOP-UNIT: killing the session (last resort)"
                $proc.Kill($true)
                $stats.killed = $true
            }
        }
        $proc.WaitForExit()
        Write-Checkpoint $unit $sessionId $unitStarted $stats $bgTasks $lastEventAt -Wait   # the final state, for the log and a successor
        Complete-LaneTick   # a tick still running is said when the next unit starts one
        Complete-ProgressTick
    } finally {
        $log.Close()
        if ($watcher -and -not $watcher.HasExited) { Stop-Process -Id $watcher.Id -Force -ErrorAction SilentlyContinue }
    }
    $err = $errTask.Result
    if ($err) { Set-Content -Path "$logBase.stderr.txt" -Value $err -Encoding utf8 }
    $stats.Remove('context')
    return @{ exit = $proc.ExitCode; stats = $stats }
}

# The account this loop spends: resolved once, exported to every child (the Python tools resolve the same one from it).
$acctArgs = @((Join-Path $Here 'account.py'), '--json', '--repo', $RepoDir) + $(if ($ConfigDir) { @('--config-dir', $ConfigDir) } else { @() })
$acctOut = try { Invoke-Py $acctArgs } catch { Say "$_; refusing to start"; exit 2 }
$Account = $acctOut | ConvertFrom-Json
if ($Account.child_config_dir) { $env:CLAUDE_CONFIG_DIR = $Account.child_config_dir } else { Remove-Item Env:CLAUDE_CONFIG_DIR -ErrorAction SilentlyContinue }
Say "account: $($Account.name) (config dir $($Account.config_dir); week resets weekday $($Account.weekly_reset.weekday) $($Account.weekly_reset.hour):$('{0:d2}' -f [int]$Account.weekly_reset.minute) $($Account.weekly_reset.tz))"

# The campaign lane needs a cluster the register knows: a misspelled one would quietly run the fix lane alone.
$Clusters = [Collections.Generic.List[string]]@($Cluster -split '[,\s]+' | Where-Object { $_ })
if ($Unit -eq 'campaign' -and -not $Clusters.Count) { Say '-Unit campaign needs -Cluster <lead>'; exit 2 }
foreach ($lead in $Clusters) {
    $wa = @((Join-Path (Split-Path $Here -Parent) 'spec/work.py'), 'next', '--cluster', $lead, '--json')
    if ($WorkDir) { $wa += @('--work', $WorkDir) }
    $view = try { Invoke-Py $wa } catch { Say "no kb/Work note names the cluster '$lead' in its cluster: list ($_); refusing to start"; exit 2 }
    $v = $view | ConvertFrom-Json
    Say "campaign lane: cluster $lead, $(@($v.notes).Count) open note(s), $(@($v.notes | Where-Object { $_.ready }).Count) ready"
}
# The lead the current `campaign` unit runs (its prompt's --cluster and its units.jsonl line).
$UnitCluster = ''

if (-not (Take-Lock)) { exit 3 }
$exitCode = 0
$script:HandedOver = $false
# A DRY RUN'S STEER lives in this run's own directory under THIS coordination directory, never in the machine-wide TEMP
# (kb/Work PB2922; design section 4.8): a fixed TEMP name was seen by every other dry run on the host, and one that a killed
# dry run left behind (a process-tree kill skips the `finally` below) turned every later check of that name RED.
$dryRunDir = $null
try {
    # The lock is held, so no other supervisor of this coordination directory is alive: every dryrun-<pid> directory but
    # this process's own was left by a dry run killed before its `finally` ran. Every start reaps them.
    foreach ($stale in @(Get-ChildItem (Join-Path $CoordDir 'scratch') -Directory -Filter 'dryrun-*' | Where-Object { $_.Name -match '^dryrun-\d+$' -and $_.Name -ne "dryrun-$PID" })) {
        try { Remove-Item $stale.FullName -Recurse -Force; Say "reaped a killed dry run's directory $($stale.FullName)" }
        catch { Say "could not reap a killed dry run's directory $($stale.FullName): $($_.Exception.Message)" }
    }
    # -Unit is the operator's steer for the first unit, written through steer.ps1 like any other (one steering path,
    # kb/Work PB2596). A dry run writes it into its own directory and reads it from there: it must leave nothing behind.
    if ($Unit) {
        if ($DryRun) {
            $dryRunDir = Join-Path $CoordDir "scratch/dryrun-$PID"
            New-Item -ItemType Directory -Force -Path $dryRunDir | Out-Null
            $OperatorHandoff = Join-Path $dryRunDir 'handoff.operator.json'
        }
        if (Test-Path $OperatorHandoff) { Say "-Unit $Unit replaces the pending operator steer ($((Get-Content $OperatorHandoff -Raw | ConvertFrom-Json).next_unit_reason))" }
        & pwsh -NoProfile -File $Steer -Unit $Unit -Reason 'named by orchestrate.ps1 -Unit at start' -OutFile $OperatorHandoff | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "steer.ps1 refused -Unit $Unit" }
    }
    $leftover = Join-Path $CoordDir 'checkpoint.json'
    if ((Test-Path $leftover) -and -not $DryRun) {
        # The previous supervisor died mid-unit (a reboot, a kill): its last checkpoint is the only record. Turn it into the
        # handoff the dead unit never wrote, so the first unit of this run is a `resume` with facts.
        $cpUnit = [string]((Get-Content $leftover -Raw | ConvertFrom-Json).unit)
        if ($cpUnit -in $ReadOnlyUnit) {
            # It read a page and touched no branch: the work chain's last handoff still says everything true.
            Say "a checkpoint of read-only unit '$cpUnit' survives from a supervisor that died mid-unit: the last handoff stands"
        } else {
            Say "a checkpoint of unit '$cpUnit' survives from a supervisor that died mid-unit: synthesizing its handoff"
            [void](Invoke-Py @((Join-Path $Here 'checkpoint.py'), 'synthesize', '--unit', $cpUnit, '--out', $LastHandoff, '--repo', $RepoDir))
        }
        Move-Item $leftover (Join-Path $CoordDir "logs/$(Get-Date -Format 'yyyyMMdd-HHmmss')-$cpUnit.checkpoint.json") -Force
    }
    $failures = $CarryFailures
    $ran = $CarryRan
    $lastFailed = $failures -gt 0
    $lastStarted = if ($CarryLastStarted) { [datetime]::Parse($CarryLastStarted, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind) } else { $null }
    while ($true) {
        if (Test-Stop) { Say "STOP file present ($(if (Test-Path $StopFile) { $StopFile } else { "the owner's global $GlobalStop" })): ending the loop"; break }
        if ($failures -ge 3) {
            $note = Join-Path $CoordDir "logs/BREAKER-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
            $tail = Get-Content $UnitsLog -Tail 3 -ErrorAction SilentlyContinue
            Set-Content -Path $note -Encoding utf8 -Value "# Circuit breaker: three consecutive failed units`n`n$($tail -join "`n")"
            Add-OwnerQuestion 'circuit breaker tripped' "Three consecutive units failed (fast exit, nonzero exit or no valid handoff). See $note and the logs it names. The loop stopped; restart it once the cause is fixed."
            Say "circuit breaker: three consecutive failed units; stopped (see $note)"
            $exitCode = 4; break
        }

        # ADOPT what landed, between units: no unit runs now, so no tree a unit reads moves under it. And BEFORE the landing
        # lane ticks (kb/Work PB4046): the tick's survey fetches and reads this same checkout, and started just ahead of
        # the adoption it raced it (the adoption's fetch lost, "cannot lock ref", and the old commit was kept for a whole
        # unit; CI merge-group runs 40918334805 and 40987962746). A tick still running from the unit is waited for, bounded.
        # One that outlasts the wait does not cost the adoption a whole unit (review R1-2): the adoption goes on beside it,
        # since its fetch writes only adopt_main's own ref, and a checkout the tick holds busy is kept and said, never forced.
        if ($DryRun) { Say 'DRY RUN: would adopt origin/main here (adopt_main.py adopt) and restart if the supervisor changed' }
        else {
            if (-not (Wait-LaneTick)) { Say "adopt main: the landing lane's tick still runs after 120 s; adopting beside it (adopt_main fetches into its own ref)" }
            $adopted = Invoke-AdoptMain
            if ($adopted -and $adopted.supervisor_changed) {
                $exitCode = Restart-Supervisor $failures $ran $lastStarted
                break
            }
        }

        # The landing lane ticks between units too (kb/Work PB3639): its period, not the unit's life, decides.
        if (-not $DryRun) { Step-Lane }

        $budgetArgs = @((Join-Path $Here 'budget.py'), '--json', '--borrow-days', "$BorrowDays")
        if ($TelemetryDir) { $budgetArgs += @('--telemetry-dir', $TelemetryDir) }
        $budget = Invoke-Py $budgetArgs | ConvertFrom-Json
        Say "budget ($($budget.account)): weekly $($budget.weekly_est_pct) % of $($budget.allowance_pct) %, session $($budget.session_est_pct) % -> $($budget.decision)$(if (-not $budget.anchor) { " (no $($budget.account) meter reading this week)" })$(if ($budget.unstamped_readings) { " ($($budget.unstamped_readings) unstamped reading(s) anchor nothing)" })"
        if ($budget.decision -eq 'stop-week') { Say 'weekly cap reached: ending the loop'; break }
        $hold = $budget.decision -in @('hold-session', 'hold-day')
        if ($hold -and $OverrideHold -and -not $DryRun) { Say "owner override: $($budget.decision) lifted (-OverrideHold)"; $hold = $false }
        if ($hold -and $DryRun) {
            Say "DRY RUN: would hold until $($budget.resume_at) (-BorrowDays moves a hold-day), then:"
        } elseif ($hold) {
            Say "holding until $($budget.resume_at)"
            # ConvertFrom-Json already turns an ISO time into a local DateTime; a string is parsed with its offset.
            $until = if ($budget.resume_at -is [datetime]) { $budget.resume_at } else { [datetimeoffset]::Parse([string]$budget.resume_at).LocalDateTime }
            if (-not (Wait-Until $until)) { Say 'STOP during the hold'; break }
            continue
        }

        $na = @((Join-Path $Here 'next_unit.py'), '--repo', $RepoDir)
        if (Test-Path $OperatorHandoff) {
            # The operator's steer is validated here too (steer.ps1 validated it when written; a hand edit since is caught):
            # an invalid one is set aside LOUDLY, never ignored in silence (kb/Work PB2596).
            if ([bool]((Get-Content $OperatorHandoff -Raw) | Test-Json -SchemaFile $Schema -ErrorAction SilentlyContinue)) {
                $na += @('--operator-handoff', $OperatorHandoff)
            } else {
                $bad = Join-Path $CoordDir "logs/$(Get-Date -Format 'yyyyMMdd-HHmmss')-handoff.operator.invalid.json"
                Move-Item $OperatorHandoff $bad -Force
                Say "the operator steer does not validate against handoff.schema.json: set aside as $bad (steer with steer.ps1)"
            }
        }
        if (Test-Path $LastHandoff) { $na += @('--handoff', $LastHandoff) }
        if ($lastFailed) { $na += '--last-failed' }
        if ($lastStarted) { $na += @('--last-started', $lastStarted.ToString('o')) }
        foreach ($lead in $Clusters) { $na += @('--cluster', $lead) }
        if ($Clusters.Count -and $WorkDir) { $na += @('--work', $WorkDir) }
        $choice = Invoke-Py $na | ConvertFrom-Json
        if ($choice.PSObject.Properties['meter_unreadable']) { Report-MeterUnreadable $choice.meter_unreadable }
        # With -Cluster every choice carries each lead's lane state (`campaigns`) and the starved leads (`starved`,
        # kb/Work PB2522): a landed lead's lane ends, and a ready note that waits through wave after wave is said aloud.
        $campaigns = if ($choice.PSObject.Properties['campaigns']) { $choice.campaigns } else { $null }
        if ($campaigns) {
            foreach ($lead in @($Clusters)) {
                if ($campaigns.PSObject.Properties[$lead] -and $campaigns.$lead -eq 'landed') {
                    [void]$Clusters.Remove($lead)
                    Say "campaign ${lead}: every note is landed or retired; the campaign lane ends$(if ($Clusters.Count) { " (campaigns $($Clusters -join ', ') go on)" } else { ' and the fix lane continues' })"
                }
            }
            foreach ($s in @($choice.starved | Where-Object { $_ })) {
                Say "campaign $($s.cluster) ready for $('{0:0.0}' -f [double]$s.hours) h ($($s.waves) wave-type unit(s)), last campaign wave at $(if ($s.last_campaign_at) { Get-Date $s.last_campaign_at -Format 'yyyy-MM-dd HH:mm' } else { 'none recorded' })"
            }
        }
        $UnitCluster = if ($choice.PSObject.Properties['cluster']) { [string]$choice.cluster } else { '' }
        if ($choice.unit -eq 'campaign' -and $UnitCluster -notin $Clusters) {
            # The replacement keeps `operator`: a campaign STEER rewritten to a wave is still the steer this unit consumes,
            # else it stays in place and rule 0 chooses it again on every later iteration (train 1039b review).
            $choice = [pscustomobject]@{ unit = 'wave'; operator = [bool]($choice.PSObject.Properties['operator'] -and $choice.operator)
                reason = "a campaign was named but no campaign lane runs$(if ($UnitCluster) { " for $UnitCluster" }) (no -Cluster, or its cluster landed): $($choice.reason)" }
            $UnitCluster = ''
        }
        if ($choice.unit -eq 'owner-question') {
            $q = Get-Content $LastHandoff -Raw | ConvertFrom-Json
            Add-OwnerQuestion "question from the $($q.unit) unit" "$($q.owner_question.question)`n`n$($q.owner_question.context)"
            Say "owner question written to $Questions; stopping (the loop never guesses)"
            $exitCode = 5; break
        }
        $model = $UnitModel[$choice.unit]
        if ($model -notin $Allowed) { throw "unit $($choice.unit) maps to model '$model', which is not allowed" }
        $sessionId = [guid]::NewGuid().ToString()
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $logBase = Join-Path $CoordDir "logs/$stamp-$($choice.unit)"
        if ($DryRun) {
            Say "DRY RUN: would run unit '$($choice.unit)' ($($choice.reason)) on $model, session $sessionId, account $($Account.name) (transcripts under $(Get-ProjectTranscriptDir $sessionId))"
            Say "DRY RUN: $ClaudeExe -p <units/common.md + units/$(Get-PromptFile $choice.unit).md$(if ($choice.unit -eq 'campaign') { " with --cluster $UnitCluster" })> --model $model --permission-mode $PermissionMode --permission-prompts none --output-format stream-json --verbose --session-id $sessionId$(if ($choice.unit -eq 'meter') { ' --chrome' }) > $logBase.jsonl"
            break
        }

        # The loop's fleet stop left by a previous wind-down would stop this unit's fleet at its first step. Only the
        # loop's own: the owner's global stop is never the supervisor's to remove.
        Remove-Item $StopUnit, $FleetStop, (Join-Path $CoordDir 'milestones.jsonl'), (Join-Path $CoordDir 'checkpoint.json') -Force -ErrorAction SilentlyContinue
        # handoff.json is the RUNNING unit's file. One found here was written by no unit of this run (a hand edit: the
        # operator's instruction of 2026-10-07 23:50 was deleted here in silence); it is archived and said, never deleted.
        if (Test-Path $Handoff) {
            $orphan = "$logBase.handoff.orphan.json"
            Move-Item $Handoff $orphan -Force
            Say "a handoff.json no unit of this run wrote was archived as ${orphan}: it steers nothing (the operator steers with steer.ps1)"
        }
        # The operator's steer is consumed by the unit it chose: archived beside the unit's log and made the newest valid
        # handoff, so the unit reads the operator's instruction as its previous handoff ({PREV_HANDOFF}).
        if ($choice.PSObject.Properties['operator'] -and $choice.operator -and (Test-Path $OperatorHandoff)) {
            Copy-Item $OperatorHandoff "$logBase.handoff.operator.json" -Force
            Move-Item $OperatorHandoff $LastHandoff -Force
        }
        Say "unit '$($choice.unit)' ($($choice.reason)) on $model, session $sessionId"
        $started = Get-Date
        $r = Invoke-Unit $choice.unit $model $sessionId $logBase
        $duration = ((Get-Date) - $started).TotalSeconds
        $valid = $false
        $outcome = 'missing'
        $h = $null
        $readOnly = $choice.unit -in $ReadOnlyUnit
        if (Test-Path $Handoff) {
            $raw = Get-Content $Handoff -Raw
            $valid = [bool]($raw | Test-Json -SchemaFile $Schema -ErrorAction SilentlyContinue)
            if ($valid) {
                $h = $raw | ConvertFrom-Json
                $outcome = $h.outcome
                Copy-Item $Handoff "$logBase.handoff.json" -Force
                if (-not $readOnly) { Copy-Item $Handoff $LastHandoff -Force }
            } else { $outcome = 'invalid'; Copy-Item $Handoff "$logBase.handoff.invalid.json" -Force }
        }
        if (-not $valid -and -not $readOnly) {
            # No usable handoff from the model (a crash, a kill, a terminated background task): the supervisor writes
            # one from its last checkpoint and the worktrees as they are now, so the successor starts with facts, not a
            # guess. The unit still counts as failed for the breaker. (A read-only unit gets none: it touched no branch,
            # so a synthesized `resume` would only re-derive the state the work chain's last handoff already holds.)
            try {
                [void](Invoke-Py @((Join-Path $Here 'checkpoint.py'), 'synthesize', '--unit', $choice.unit, '--out', $Handoff, '--repo', $RepoDir))
                Copy-Item $Handoff $LastHandoff -Force
                Copy-Item $Handoff "$logBase.handoff.synthesized.json" -Force
                $outcome = "synthesized ($outcome)"
            } catch { Say "handoff not synthesized: $_" }
        }
        # Archived above beside the unit's log: handoff.json is the RUNNING unit's file, and one left here would be taken
        # for a hand-written orphan at the next unit's start (31 false "no unit of this run wrote" lines in one log).
        Remove-Item $Handoff -Force -ErrorAction SilentlyContinue
        if ($readOnly) { Clear-NamedUnit $choice.unit }
        # Moved, not copied: a checkpoint.json that is still here at the next supervisor start means the supervisor itself died mid-unit.
        if (Test-Path (Join-Path $CoordDir 'checkpoint.json')) { Move-Item (Join-Path $CoordDir 'checkpoint.json') "$logBase.checkpoint.json" -Force }
        if (Test-Path (Join-Path $CoordDir 'milestones.jsonl')) { Move-Item (Join-Path $CoordDir 'milestones.jsonl') "$logBase.milestones.jsonl" -Force }
        # ONE classification of how a unit ended (PB2665; the pattern of PB3673 and PB3674: an ENVIRONMENT cause is never
        # read as a failure of the WORK). A read-only unit that ended without a `done` handoff could not read its page:
        # that is `environment_failure`, counted per unit type by next_unit.py and never by the breaker. It moves none of
        # the work chain's state ($failures, $lastFailed, $lastStarted), so a meter between two failed work units neither
        # hides nor adds a failure. Every other unit is judged as before.
        $environmentFailed = $readOnly -and (($r.exit -ne 0) -or (-not $valid) -or ($outcome -ne 'done'))
        $failed = (-not $readOnly) -and (($r.exit -ne 0) -or (-not $valid) -or (($duration -lt $FastFailSeconds) -and ($outcome -ne 'done')))
        if (-not $readOnly) {
            $failures = if ($failed) { $failures + 1 } else { 0 }
            $lastFailed = $failed
            $lastStarted = $started
        }
        $line = [ordered]@{ unit = $choice.unit; reason = $choice.reason; model = $model; account = $Account.name; session_id = $sessionId
            started_at = $started.ToString('o'); ended_at = (Get-Date).ToString('o'); duration_s = [Math]::Round($duration, 1)
            exit_code = $r.exit; handoff_outcome = $outcome; next_unit = $(if ($h -and $h.PSObject.Properties['next_unit']) { $h.next_unit } else { $null })
            failed = $failed; environment_failure = $environmentFailed; log = "$logBase.jsonl" }
        # The campaign record next_unit.py reads back: whose turn a campaign was, and each lead's lane at this choice
        # (the round-robin and the starvation measure, kb/Work PB2522).
        if ($UnitCluster) { $line['cluster'] = $UnitCluster }
        if ($campaigns) { $line['campaigns'] = $campaigns }
        foreach ($k in $r.stats.Keys) { $line[$k] = $r.stats[$k] }
        Add-Content -Path $UnitsLog -Encoding utf8 -Value ($line | ConvertTo-Json -Compress -Depth 5)
        Say "unit '$($choice.unit)' ended: exit $($r.exit), handoff $outcome, $([Math]::Round($duration)) s, $($r.stats.calls) calls$(if ($failed) { " — FAILURE $failures of 3" } elseif ($environmentFailed) { " — ENVIRONMENT FAILURE: no reading (the breaker does not count it)" })"
        $ran++
        # A headless unit cannot publish the owner's ledger artifact (no Artifact tool); say loudly when a publish is owed
        # so the attended session does it (ledger_state.py compares the page's stamp with the last published one).
        try {
            # ledger_state.py fetches origin/main into the shared remote-tracking ref, as the lane tick's survey does: not
            # beside a tick (kb/Work PB4046 review R1-1), or one of the two fetches fails "cannot lock ref". A tick that
            # outlasts the wait is said; the check then runs beside it, and a lost fetch says "fetch failed" (review R2-1).
            if (-not (Wait-LaneTick)) { Say "ledger check: the landing lane's tick still runs after 120 s; checking beside it (a lost fetch race says so, and the next unit's check repeats it)" }
            $ls = Invoke-Py @((Join-Path $Here 'ledger_state.py'), 'owed', '--repo', $RepoDir)
            # A lost fetch reads main's stamp from a stale ref: said whatever the answer, so a "current" read on old facts
            # is never silent (review R3-1); an "owed" one carries it in the publish line below.
            if ($ls -notmatch '^owed' -and $ls -match 'fetch failed') { Say "ledger check: $ls" }
            if ($ls -match '^owed') {
                # $ls is read on origin/main, fetched (kb/Work PB2987); when it says the checkout is behind, the
                # publisher fast-forwards it before rendering, and mark-published refuses until then.
                Say "LEDGER PUBLISH OWED ($ls): render with gen_ledger.py (it writes $CoordDir\ledger.html and prints this account's artifact URL), publish it there, then ledger_state.py mark-published (--url the first time an account publishes)"
                # The attended operator session is woken by its mailbox, not by this log (kb/Work PB2482): one open
                # `publish` message per stamp (--unless-pending), pointing at this unit's log.
                $stampOwed = ($ls -split ' ')[1]
                try { [void](Invoke-Py @((Join-Path $Here 'mailbox.py'), '--coord', $CoordDir, 'send', '--to', 'operator', '--from', 'loop',
                    '--kind', 'publish', '--subject', "Ledger publish owed at $stampOwed ($($Account.name))",
                    '--body', "The supervisor found the ledger publish owed after unit '$($choice.unit)': $ls. Render with gen_ledger.py, publish to this account's artifact, then ledger_state.py mark-published.",
                    '--ref', "$logBase.jsonl", '--session', "orchestrate.ps1 pid $PID", '--unless-pending')) }
                catch { Say "publish message not posted to the operator's mailbox (the loop continues): $_" }
            }
        } catch { Say "ledger state not read (the loop continues): $_" }

        if ($valid -and $outcome -eq 'owner-question') {
            Add-OwnerQuestion "question from the $($choice.unit) unit" "$($h.owner_question.question)`n`n$(if ($h.owner_question.PSObject.Properties['context']) { $h.owner_question.context })"
            Say "owner question written to $Questions; stopping (the loop never guesses)"
            $exitCode = 5; break
        }
        if ($MaxUnits -gt 0 -and $ran -ge $MaxUnits) { Say "-MaxUnits $MaxUnits reached"; break }
        if (($failed -and $failures -lt 3) -or $environmentFailed) {
            # An environment failure waits the base once (the third in a row closes the meter for a maximum reading age).
            $wait = if ($failed) { [Math]::Min(1800, $BackoffBaseSeconds * [Math]::Pow(2, $failures - 1)) } else { $BackoffBaseSeconds }
            if ($wait -gt 0) {
                Say "backing off $wait s"
                if (-not (Wait-Until ((Get-Date).AddSeconds($wait)))) { Say 'STOP during the backoff'; break }
            }
        }
    }
} finally {
    # A restart handed the lock to its successor (Restart-Supervisor): it is not ours to remove.
    if (-not $script:HandedOver) { Remove-Item $Lock -Force -ErrorAction SilentlyContinue }
    # Not fatal: a directory this cannot remove is reaped, loudly, by the next start (above).
    if ($dryRunDir) { Remove-Item $dryRunDir -Recurse -Force -ErrorAction SilentlyContinue }
}
exit $exitCode
