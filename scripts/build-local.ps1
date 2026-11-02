# build-local.ps1 — THE GATE, as one command (kb/Work PB1708, PB3022, PB4328; docs/rearchitecture/DESIGN-test-build-ci.md
# §3.14.1–3.14.6, §3.17.3; the pwsh twin of build-local.sh). It sets the process PRIORITY and hands the gate to
# scripts/gate.py, which holds this worktree's gate lock, decides the change's lane, runs the audits beside everything
# after them, takes a gate slot, builds the solution and runs the WHOLE Unit, Conformance and Characterization
# populations, three hosts at once (the classes red at the previous gate first, then longest first by the committed
# weights), checks each population by name and prints `=== BUILD-LOCAL GATE: … ===`.
# ⛔ THE GATE OUTLIVES THIS COMMAND (kb/Work PB3536; DESIGN-test-build-ci.md §3.14.12): the driver starts the gate
# DETACHED in its run directory (`TestResults/build-local/<run>/`: gate.log, gate.pid, verdict.json) and follows it, so
# a caller killed mid-gate (a workflow agent's return) leaves it running to its verdict, which
# `python scripts/gate.py --await <run>` reads; and a re-gate of an unchanged tree answers from the recorded
# GREEN (`… GREEN — REUSED: …`), running nothing.
# ⛔ ONE GATE, ONE SHAPE (owner 2026-10-11, kb/Work PB3015/PB3022): there is no mode and no scope. Every gate — an
# implementer's, a re-gate after a fix — runs the whole population inside a gate slot (the shared cap, 3 by default,
# `gate_slot.py set-cap`, kb/Work PB2514); CI's merge group is the first run of the merged tree, Linux and Release.
# ⛔ ORDER, DON'T SKIP (owner, 2026-09-28): no gate filters.
# Usage:  pwsh scripts/build-local.ps1 -Priority BelowNormal     (every implementer gate)
#         pwsh scripts/build-local.ps1                           (a landing's gate, at Normal priority)
# ⛔ -Priority (owner decision 2026-09-22): Windows passes BelowNormal/Idle DOWN to child processes (and only those two
# classes), so an implementer that sets it here runs its build, dotnet test, every testhost and every compiled COBOL
# program below a landing's Normal-priority gate, which shares the one 32-core host with them.
param([ValidateSet('Normal', 'BelowNormal', 'Idle')][string]$Priority = 'Normal')
$ErrorActionPreference = 'Continue'
if ($Priority -ne 'Normal') {
    [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$Priority
    Write-Host "build-local: running at $Priority priority (inherited by build, test hosts and compiled programs)"
}
Set-Location (Split-Path -Parent $PSScriptRoot)
# THE ONE INTERPRETER (scripts/python-resolve.ps1, the twin of python-resolve.sh; kb/Work PB1637, PB3197): a host with no
# runnable Python ends on the gate's own verdict line, NOT RUN [interpreter] (an ENVIRONMENT cause in
# gate.NOT_RUN_CAUSES) with exit 2 as the driver's NOT RUN does.
try {
    . (Join-Path $PSScriptRoot 'python-resolve.ps1')
} catch {
    Write-Host "=== BUILD-LOCAL GATE: NOT RUN [interpreter] — $($_.Exception.Message) ==="
    exit 2
}
& $PY scripts/gate.py
exit $LASTEXITCODE
