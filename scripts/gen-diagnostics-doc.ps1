#!/usr/bin/env pwsh
# Render docs/DIAGNOSTICS.md (a gitignored VIEW, kb/Work PB3026; scripts/rendered_views.py) from the first-class diagnostic catalogue
# (src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs), rearch PHASE 02 P2.10.
#
# The catalogue is C#, so the renderer lives in the drift test (DiagnosticRegistryDriftTests) — this script
# runs that test with WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC=<a raw file> so it WRITES its render there instead of asserting,
# and `rendered_views.py --write` places that render as docs/DIAGNOSTICS.md with its stamp.
# The same test, run normally, proves the catalogue renders one row per descriptor; nothing is compared with a copy.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    # ⛔ THE FILTER MUST NAME A REAL TEST BEFORE ITS EXIT CODE MEANS ANYTHING (kb/Work PB751). The whole
    # regeneration is one env var plus this ONE test, so the filter is the whole selection — and vstest answers a
    # filter that matches nothing with a PASSING run of zero tests. Rename the test (or partition its class, as
    # A13 did to NistDifferentialTests) and the selection goes dead, `dotnet test` exits 0, the throw below never
    # fires, and this script prints 'Regenerated …' having regenerated nothing. `--allow-build` because the run
    # below is what builds the project: nothing is built yet at this point on a clean tree.
    $filter = 'FullyQualifiedName~DiagnosticRegistryDriftTests.DiagnosticsDoc_RendersTheWholeCatalogue'
    python scripts/filter_population.py --filter $filter `
        --filtered tests/WiseOwl.Cobol.Tests.Unit/WiseOwl.Cobol.Tests.Unit.csproj --allow-build
    if ($LASTEXITCODE -ne 0) {
        throw "the regeneration filter selects nothing (filter_population rc $LASTEXITCODE) — NOTHING WAS REGENERATED; see the finding above (kb/Work PB751)"
    }
    # The test writes the RAW render to this file; `rendered_views.py --write` places it as the view with its stamp, the one
    # writer of a view (rule (e): an unstamped docs/DIAGNOSTICS.md is a hand edit to it, wave 20262031 review R1-2).
    $raw = Join-Path ([System.IO.Path]::GetTempPath()) ("wiseowl-diagnostics-" + [guid]::NewGuid().ToString('N') + '.md')
    $env:WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC = $raw
    # SCRUBBED (kb/Work PB1718): a stray leg handshake or VSTest* property would narrow even this one-test run.
    python scripts/test_population.py scrubbed dotnet test tests/WiseOwl.Cobol.Tests.Unit/WiseOwl.Cobol.Tests.Unit.csproj -c Debug --filter $filter
    if ($LASTEXITCODE -ne 0) { throw "regeneration test failed (exit $LASTEXITCODE)" }
    if (-not (Test-Path -LiteralPath $raw)) { throw "the regeneration test passed but wrote no render to $raw — NOTHING WAS REGENERATED" }
    python scripts/rendered_views.py diagnostics --write $raw
    if ($LASTEXITCODE -ne 0) { throw "docs/DIAGNOSTICS.md was not placed (rendered_views.py --write exit $LASTEXITCODE): see the line above" }
    Write-Host 'Rendered docs/DIAGNOSTICS.md from DiagnosticCatalog.'
}
finally {
    Remove-Item Env:\WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC -ErrorAction SilentlyContinue
    if ($raw) { Remove-Item -LiteralPath $raw -ErrorAction SilentlyContinue }
    Pop-Location
}
