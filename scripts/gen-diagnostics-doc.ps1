#!/usr/bin/env pwsh
# Regenerate docs/DIAGNOSTICS.md from the first-class diagnostic catalogue
# (src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs), rearch PHASE 02 P2.10.
#
# The catalogue is C#, so the renderer lives in the drift test (DiagnosticRegistryDriftTests) — this script
# runs that test with COBOLNET_WRITE_DIAGNOSTICS_DOC=1 so it WRITES docs/DIAGNOSTICS.md instead of asserting.
# The same test, run normally in CI, fails if the committed doc drifts from the catalogue.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $env:COBOLNET_WRITE_DIAGNOSTICS_DOC = '1'
    dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug `
        --filter 'FullyQualifiedName~DiagnosticRegistryDriftTests.DiagnosticsDoc_IsInSync_WithTheCatalogue'
    if ($LASTEXITCODE -ne 0) { throw "regeneration test failed (exit $LASTEXITCODE)" }
    Write-Host 'Regenerated docs/DIAGNOSTICS.md from DiagnosticCatalog.'
}
finally {
    Remove-Item Env:\COBOLNET_WRITE_DIAGNOSTICS_DOC -ErrorAction SilentlyContinue
    Pop-Location
}
