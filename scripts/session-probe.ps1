# session-probe.ps1 — the §0 SESSION BOOTSTRAP step-③ mechanizer (plan §0; added 2026-07-19, DEVLOG 922).
# Prints the live repo state a session must confirm before touching code. Mechanical, because manual state
# ledgers drift (the COBOLNET1573 collision class). Read-only; safe to run any time.
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "=== SESSION PROBE ($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ==="

# 1. Branch + HEAD + cleanliness
$branch = git rev-parse --abbrev-ref HEAD
$head = git rev-parse --short HEAD
$dirty = git status --porcelain
Write-Host "branch : $branch @ $head $(if ($branch -ne 'phase-13-m4-2023' -and $branch -ne 'main') { '⚠ UNEXPECTED BRANCH' })"
if ($dirty) { Write-Host "⚠ DIRTY TREE ($(@($dirty).Count) paths) — commit or explain before proceeding" } else { Write-Host "tree   : clean" }
$unpushed = git log --oneline '@{u}..HEAD' 2>$null
if ($unpushed) { Write-Host "⚠ UNPUSHED commits: $(@($unpushed).Count)" } else { Write-Host "push   : up to date" }

# 2. Diagnostic band — BOTH scans must agree (the 1573/1518 lesson)
$grepCodes = Select-String -Path (Get-ChildItem src -Recurse -Filter *.cs | Where-Object FullName -notmatch '\\(bin|obj)\\') `
    -Pattern 'COBOLNET15[0-9][0-9]' -AllMatches | ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
$maxGrep = ($grepCodes | Measure-Object -Maximum).Maximum
$catalog = Select-String -Path 'src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs' -Pattern '"(COBOLNET15\d\d)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$maxCat = ($catalog | Measure-Object -Maximum).Maximum
Write-Host "diag   : src-grep max $maxGrep · catalog max $maxCat $(if ($maxGrep -ne $maxCat) { '⚠ SCANS DISAGREE — reconcile before allocating' } else { "→ next free = COBOLNET$(([int]($maxGrep -replace 'COBOLNET','')) + 1)" })"

# 3. VCR todo count (the P14 burn-down instrument)
$todo = (Select-String -Path 'docs/VERSION_CHANGE_REFERENCE.md' -Pattern '<!-- todo -->' -AllMatches | ForEach-Object { $_.Matches }).Count
Write-Host "VCR    : $todo todo anchors remaining (P14 Step 1 drives to zero)"

# 4. Corpus + negative counts (drift signal, not a gate)
$pos = (Get-ChildItem tests/conformance -Recurse -Filter *.out | Measure-Object).Count
$neg = (Get-ChildItem tests/conformance/negative -Filter *.err | Measure-Object).Count
Write-Host "corpus : $pos positive goldens · $neg negative fixtures"

# 5. Traceability inventory (exists after P14 Step 0)
if (Test-Path 'tests/version-matrix/traceability-inventory.json') {
    $inv = Get-Content 'tests/version-matrix/traceability-inventory.json' -Raw | ConvertFrom-Json
    $gaps = @($inv | Where-Object { $_.state -eq 'GAP' }).Count
    Write-Host "invent : $(@($inv).Count) rows · $gaps GAP (v1.0 = zero GAP)"
} else {
    Write-Host "invent : not built yet (P14 Step 0)"
}

Write-Host "=== next: confirm the battery green (plan §9) before code changes; read plan §0 for the worklist ==="
