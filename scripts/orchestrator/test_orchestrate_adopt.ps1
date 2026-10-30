#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (adopt), driven by a fake -ClaudeExe (testdata/fake-claude.ps1) and a fake -GhExe; no Pester,
# no real session. Proves (kb/Work PB2699; DESIGN-orchestrator-loop.md section 4.10): a main commit that landed while the loop
# ran is fast-forwarded into the checkout BETWEEN units once CI's push run on it succeeded; a red main is not adopted and
# the unit still runs on the checkout as it is; a landing that changed the supervisor's own script RESTARTS the supervisor
# from the adopted checkout, carrying its arguments and counters and releasing the lock to the successor; a dry run adopts
# nothing. The repository is a scratch clone of a scratch bare origin, never this machine's real one.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_adopt.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')
# A failed check quotes the run's adoption line: adopt_main's outcome and its reason, the supervisor's line when
# adopt_main.py itself failed, and any tool's stderr the supervisor said (kb/Work PB4046).
$script:Evidence = 'adopt main:|main not adopted|wrote to stderr'

function G { & git -c user.email=t@t -c user.name=t @args 2>&1 | Out-Null; if ($LASTEXITCODE) { throw "git $args failed" } }
function Land([string]$clone, [string]$file, [string]$text) {
    New-Item -ItemType Directory -Force -Path (Split-Path (Join-Path $clone $file)) | Out-Null
    Set-Content -Path (Join-Path $clone $file) -Value $text -Encoding utf8
    G -C $clone add -A; G -C $clone commit -q -m "land $file"; G -C $clone push -q origin main
}
function Head([string]$clone) { (& git -C $clone rev-parse HEAD).Trim() }

$Origin = Join-Path $Root 'adopt-origin.git'
$Checkout = Join-Path $Root 'adopt-checkout'
$Lander = Join-Path $Root 'adopt-lander'
G init -q --bare -b main $Origin
G init -q -b main $Checkout
G -C $Checkout remote add origin $Origin
# The "supervisor" of the checkout is a stub that says it ran and with which arguments: the restart re-executes THAT file.
Land $Checkout 'scripts/orchestrator/orchestrate.ps1' 'Write-Host "STUB-SUPERVISOR v1 $args"; exit 0'
Land $Checkout 'kb/Work/PB1.md' 'the register'
G clone -q $Origin $Lander

# A fake GitHub CLI: answers with the run list the case puts in FAKE_GH_JSON (generated here, so no .cmd is tracked).
$FakeGh = Join-Path $Root 'fake-gh.cmd'
Set-Content -Path $FakeGh -Value '@type "%FAKE_GH_JSON%"' -Encoding ascii
$Green = Join-Path $Root 'gh-green.json'
$Red = Join-Path $Root 'gh-red.json'
Set-Content -Path $Green -Value '[{"status":"completed","conclusion":"success"}]'
Set-Content -Path $Red -Value '[{"status":"completed","conclusion":"failure"}]'
$unitArgs = @('-Unit', 'wave', '-MaxUnits', '1', '-FastFailSeconds', '0', '-RepoDir', $Checkout, '-GhExe', $FakeGh)

# 1. A DRY RUN ADOPTS NOTHING (and says where it would)
Land $Lander 'kb/Work/PB2.md' 'landed while the loop ran'
$before = Head $Checkout
$env:FAKE_GH_JSON = $Green
$r = Run-Orch 'adoptdry' 'good' (@('-DryRun') + $unitArgs)
Check 'dry run: says it would adopt' ($r.out -match 'DRY RUN: would adopt origin/main') $true
Check 'dry run: the checkout did not move' (Head $Checkout) $before

# 2. A STABLE LANDING IS ADOPTED BETWEEN UNITS; the unit then runs on the adopted tree
$r = Run-Orch 'adoptff' 'good' $unitArgs
Check 'adopt: the supervisor says it adopted main' ($r.out -match 'adopt main: adopted') $true
Check 'adopt: the checkout is origin/main now' (Head $Checkout) (Head $Lander)
Check 'adopt: the landed note is in the checkout' (Test-Path (Join-Path $Checkout 'kb/Work/PB2.md')) $true
Check 'adopt: the adopted commit is recorded for the planner guard' ((Get-Content (Join-Path $r.coord 'adopted-main.json') -Raw | ConvertFrom-Json).sha) (Head $Lander)
Check 'adopt: the unit ran' $r.runs 1
Check 'adopt: a landing that left the supervisor alone did not restart it' ($r.out -match 'restarting the supervisor') $false

# 3. A RED MAIN IS NOT ADOPTED; the unit still runs on the checkout as it is
Land $Lander 'kb/Work/PB3.md' 'landed, then CI went red'
$before = Head $Checkout
$env:FAKE_GH_JSON = $Red
$r = Run-Orch 'adoptred' 'good' $unitArgs
Check 'red: kept, with the reason' ($r.out -match 'adopt main: kept .*not stable yet') $true
Check 'red: the checkout stayed on the last adopted commit' (Head $Checkout) $before
Check 'red: the unit ran anyway' $r.runs 1

# 4. A LANDING THAT CHANGED THE SUPERVISOR RESTARTS IT, FROM THE ADOPTED CHECKOUT, CARRYING ITS ARGUMENTS
Land $Lander 'scripts/orchestrator/orchestrate.ps1' 'Write-Host "STUB-SUPERVISOR v2 $args"; exit 0'
$env:FAKE_GH_JSON = $Green
$r = Run-Orch 'adoptrestart' 'good' $unitArgs
Check 'restart: the supervisor says it restarts' ($r.out -match 'the supervisor itself changed') $true
Check 'restart: the adopted checkout supervisor ran' ($r.out -match 'STUB-SUPERVISOR v2') $true
Check 'restart: it was handed the arguments' ($r.out -match '-MaxUnits 1' -and $r.out -match '-CoordDir' -and $r.out -match [regex]::Escape("-RepoDir $Checkout") -and $r.out -match '-GhExe') $true
Check 'restart: and the counters' ($r.out -match '-CarryFailures 0' -and $r.out -match '-CarryRan 0') $true
Check 'restart: -Unit was consumed at start, not passed again' ($r.out -match 'STUB-SUPERVISOR v2.* -Unit ') $false
Check 'restart: the operator steer stays on disk for the successor' (Test-Path (Join-Path $r.coord 'handoff.operator.json')) $true
Check 'restart: no unit ran in the predecessor' $r.runs 0
Check 'restart: the lock was handed over, not left behind' (Test-Path (Join-Path $r.coord 'orchestrate.lock')) $false
Check 'restart: the exit code is the successors' $r.code 0
Check 'restart: the checkout is origin/main' (Head $Checkout) (Head $Lander)

# 5. A TOOL'S STDERR IS SAID, NEVER PARSED (kb/Work PB4046): adopt_main.py writes a line to stderr and still answers.
#    Folded into its JSON answer, that line made the answer unreadable: "main not adopted", the unit ran on the old tree.
Land $Lander 'kb/Work/PB4.md' 'landed while a tool warned'
$env:FAKE_GH_JSON = $Green
$env:FAKE_PYTHON_STDERR = 'adopt_main.py'
$env:FAKE_PYTHON_CHECKPOINT_SECONDS = '0'
$r = Run-Orch 'adoptwarn' 'good' ($unitArgs + @('-Python', $FakePython))
Remove-Item Env:FAKE_PYTHON_STDERR, Env:FAKE_PYTHON_CHECKPOINT_SECONDS -ErrorAction SilentlyContinue
Check 'stderr: the adoption still reads its answer' ($r.out -match 'adopt main: adopted') $true
Check 'stderr: the line is said, not parsed' ($r.out -match 'adopt_main\.py wrote to stderr \(said, not parsed\): .*a warning on stderr') $true
Check 'stderr: the checkout is origin/main' (Head $Checkout) (Head $Lander)

Remove-Item Env:FAKE_GH_JSON -ErrorAction SilentlyContinue
Complete-OrchTest 'adopt'
