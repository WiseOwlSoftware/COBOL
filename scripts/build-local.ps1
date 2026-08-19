# build-local.ps1 — the PER-COMMIT wave-local gate, as one command (kb/Work PB108; plan §0 "Gates"; the pwsh twin of
# build-local.sh): build the SOLUTION (Debug — the same binaries battery.sh PHASE 0 builds; a stale test-bin compiler
# DLL hides regressions, so no --no-build leg ever runs on an unbuilt tree), then Conformance filtered on the SUBJECT
# of the change, full Unit (~2 min), Characterization. NOT the comprehensive gate (battery.sh), NOT guard-fast.sh,
# NOT Release (the Windows CI leg). ⛔ The filter is REQUIRED: the wave-local gate is a filter chosen from what the
# change TOUCHES ("~Arithmetic|~Inspect"; add "~VersionMatrix" for an edition gate) — a default would re-create the
# PB36 mistake of filtering on where the new goldens sat. Shorthand terms ("~X|~Y") are expanded to
# FullyQualifiedName~X|FullyQualifiedName~Y — vstest silently matches NOTHING for a bare "~X" (and exits 0), so a leg
# with NO verdict line is RED here, never green by absence.
# Usage:  pwsh scripts/build-local.ps1 -Filter "~Collation|~Locale"
param([Parameter(Mandatory = $true)][string]$Filter)
$ErrorActionPreference = 'Continue'
Set-Location (Split-Path -Parent $PSScriptRoot)
$Filter = [regex]::Replace($Filter, '(^|[|&(])(!=|=|~)', '$1FullyQualifiedName$2')
dotnet build CobolSharp.sln -v quiet
if ($LASTEXITCODE -ne 0) { Write-Host '=== WAVE-LOCAL GATE: BUILD FAILED ==='; exit 1 }
$rc = 0
function Leg([string]$name, [string[]]$testArgs) {
    $out = & dotnet test @testArgs 2>&1 | ForEach-Object { "$_" }
    $legRc = $LASTEXITCODE
    $out | Where-Object { $_ -match '^(Passed!|Failed!)|error|\[FAIL\]' } | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
    $verdict = $out | Where-Object { $_ -match '^(Passed!|Failed!)' } | Select-Object -Last 1
    if (-not $verdict) { Write-Host "${name}: NO VERDICT LINE — the filter matched no test (a run must assert its population)"; $legRc = 1 }
    if ($legRc -ne 0) { $script:rc = 1 }
}
Leg 'conformance'      @('tests/Cobol.Net.Tests.Conformance', '--no-build', '--filter', $Filter)
Leg 'unit'             @('tests/Cobol.Net.Tests.Unit', '--no-build')
Leg 'characterization' @('tests/Cobol.Net.Tests.Characterization', '--no-build')
if ($rc -eq 0) { Write-Host "=== WAVE-LOCAL GATE: GREEN (filter $Filter) ===" } else { Write-Host "=== WAVE-LOCAL GATE: RED (filter $Filter) ===" }
exit $rc
