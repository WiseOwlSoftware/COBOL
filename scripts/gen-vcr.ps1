#!/usr/bin/env pwsh
# Regenerate the "Gating status index" block of docs/VERSION_CHANGE_REFERENCE.md (rearch PHASE 03, P3.6).
#
# The block is derived from the VCR's own <!-- gate:construct-id --> anchors + tests/version-matrix/constructs.json
# (a construct's `status` — active->done / pending->pending — is itself fixture-gated by VersionMatrixTests). The
# renderer lives in VcrDriftTests (the catalogue is read there), so this script runs that test with
# COBOLNET_WRITE_VCR=1 so it WRITES the block instead of asserting. Run normally in CI, the same test fails if the
# committed block drifts, if a gate anchor names a non-existent construct, or if a spec citation does not resolve
# (unknown clause, a quoted fragment no longer inside it, or a resurrected spec LINE number).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $env:COBOLNET_WRITE_VCR = '1'
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug `
        --filter 'FullyQualifiedName~VcrDriftTests.GeneratedStatusIndex_IsInSync'
    if ($LASTEXITCODE -ne 0) { throw "regeneration test failed (exit $LASTEXITCODE)" }
    Write-Host 'Regenerated the VERSION_CHANGE_REFERENCE.md gating status index.'
}
finally {
    Remove-Item Env:\COBOLNET_WRITE_VCR -ErrorAction SilentlyContinue
    Pop-Location
}
