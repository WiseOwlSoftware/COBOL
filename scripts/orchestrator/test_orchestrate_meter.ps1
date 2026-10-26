#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — the orchestrator loop is Windows-hosted: these cases drive Windows Terminal (a fake wt.exe), Win32_Process, USERPROFILE and directory junctions
# Self-test for orchestrate.ps1 (the read-only meter unit, kb/Work PB2665), driven by a fake -ClaudeExe
# (testdata/fake-claude.ps1); no Pester, no real session. Proves: a meter that cannot read its page is an ENVIRONMENT
# failure, not a failure of the work: its handoff never replaces the work chain's, no synthesized `resume` stands in for
# a missing one, it neither trips nor resets the circuit breaker, three in a row close the meter and ask the owner ONCE;
# and a finished unit's handoff.json is archived, never taken for a hand-written orphan at the next start.
# One of the parallel parts of the orchestrate.ps1 self-test (testdata/orchestrate_test_lib.ps1 is their harness;
# kb/Work PB2563). Run: pwsh -NoProfile -File scripts/orchestrator/test_orchestrate_meter.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
. (Join-Path $Here 'testdata/orchestrate_test_lib.ps1')

# 1. a meter that writes a valid `failed` handoff, three times in a row (the first by -Unit's steer, the others by rule 3:
#    this coordination directory has no reading), then the next rule's unit: the meter is CLOSED, not retried a fourth time
$r = Run-Orch 'meterfail' 'meterfail' @('-Unit', 'meter', '-MaxUnits', '4', '-FastFailSeconds', '120')
Check 'three meters then the next rule''s unit' (($r.units | ForEach-Object { $_.unit }) -join ' ') 'meter meter meter wave'
Check 'a meter that could not read is an environment failure, not a failed unit' (($r.units | Select-Object -First 3 | ForEach-Object { "$($_.failed)/$($_.environment_failure)" }) -join ' ') 'False/True False/True False/True'
Check 'the fourth unit''s own crash is judged as before' $r.units[3].failed $true
Check 'three failed meters never tripped the breaker' $r.code 0
$q = Get-Content (Join-Path $r.coord 'OWNER-QUESTIONS.md') -Raw
Check 'the owner is told once, at the third' ([regex]::Matches($q, 'meter unreadable').Count) 2   # the heading and the marker line
Check 'and the question names how to fix it' ($q -match 'Claude in Chrome extension') $true
$seen = Get-Content (Join-Path $r.coord 'prev-handoff-seen.json') -Raw | ConvertFrom-Json
Check 'the fourth unit read the steer handoff the meters left, its spent name removed' "$($seen.unit) $($seen.PSObject.Properties['next_unit'])" 'operator '
Check 'no meter wrote a synthesized resume' @(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*meter.handoff.synthesized.json').Count 0

# 2. a meter that leaves NO handoff: nothing is synthesized (a `resume` would re-derive a state the meter never touched)
$r = Run-Orch 'meternohandoff' 'nohandoff' @('-Unit', 'meter', '-MaxUnits', '1', '-FastFailSeconds', '120')
Check 'no handoff from a meter is recorded as missing, not synthesized' $r.units[0].handoff_outcome 'missing'
Check 'and counts as an environment failure' "$($r.units[0].failed)/$($r.units[0].environment_failure)" 'False/True'
Check 'and the last handoff names no resume' ((Get-Content (Join-Path $r.coord 'handoff.last.json') -Raw | ConvertFrom-Json).PSObject.Properties['next_unit'] -eq $null) $true

# 2b. a meter that crashes at once (nonzero exit, nothing written, under -FastFailSeconds) is the same environment failure:
#     it cannot read, and the breaker's three-fast-exits rule is for units that touch the work
$r = Run-Orch 'meterfast' 'fastfail' @('-Unit', 'meter', '-MaxUnits', '3', '-FastFailSeconds', '120')
Check 'three instant meter crashes are three environment failures' (($r.units | ForEach-Object { "$($_.unit):$($_.failed)/$($_.environment_failure)" }) -join ' ') 'meter:False/True meter:False/True meter:False/True'
Check 'and the breaker never trips on them' $r.code 0

# 3. the breaker counts the work only: two failed units carried over, three meters that could not read (they move no
#    count: not up to a trip, not down to zero), then one more crash of a work unit is the third failure of the chain
$r = Run-Orch 'meterbreaker' 'meterfail' @('-Unit', 'meter', '-MaxUnits', '6', '-FastFailSeconds', '120', '-CarryFailures', '2')
Check 'three meters leave the carried count at two, the next crash makes three' (($r.units | ForEach-Object { $_.unit }) -join ' ') 'meter meter meter resume'
Check 'the circuit breaker trips on the work chain''s third failure' $r.code 4

# 4. a meter that READ leaves the work chain's handoff alone, and only spends the name when the handoff named it
$r = Run-Orch 'meterread' 'good' @('-Unit', 'wave', '-MaxUnits', '2', '-FastFailSeconds', '0')
Check 'the wave''s handoff names the meter and the meter runs' (($r.units | ForEach-Object { $_.unit }) -join ' ') 'wave meter'
$hl = Get-Content (Join-Path $r.coord 'handoff.last.json') -Raw | ConvertFrom-Json
Check 'the meter did not replace the wave''s handoff' $hl.summary 'fake unit done'
Check 'but the name it answered is spent' ($hl.PSObject.Properties['next_unit'] -eq $null) $true

# 5. handoff.json is the running unit's file: a finished unit's is archived beside its log, so the next start finds none
Check 'no handoff.json is left to be taken for an orphan' ((Test-Path (Join-Path $r.coord 'handoff.json')) -or ($r.out -match 'no unit of this run wrote')) $false
Check 'the unit''s handoff is archived beside its log' @(Get-ChildItem (Join-Path $r.coord 'logs') -Filter '*wave.handoff.json').Count 1

Complete-OrchTest 'meter'
