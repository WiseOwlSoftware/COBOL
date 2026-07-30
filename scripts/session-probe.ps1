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
Write-Host "branch : $branch @ $head $(if ($branch -ne 'phase-14' -and $branch -ne 'main') { '⚠ UNEXPECTED BRANCH' })"
if ($dirty) { Write-Host "⚠ DIRTY TREE ($(@($dirty).Count) paths) — commit or explain before proceeding" } else { Write-Host "tree   : clean" }
$unpushed = git log --oneline '@{u}..HEAD' 2>$null
if ($unpushed) { Write-Host "⚠ UNPUSHED commits: $(@($unpushed).Count)" } else { Write-Host "push   : up to date" }

# 2. Diagnostic band — the next-free claim is the CEILING of BOTH scans (the 1573/1518 collision lesson).
#    Scan the WHOLE COBOLNET1xxx band, not one decade — diagnostics crossed 1600 (PERFORM Format 3 et al.),
#    so a decade-pinned regex silently caps out and manufactures a phantom disagreement. Two channels
#    legitimately diverge: the compiler-channel raw Edition.Error codes are NOT catalog descriptors (ledger
#    V11 queues folding them in), so src-max >= catalog-max is the EXPECTED steady state, not drift. The one
#    real anomaly the cross-check still catches is catalog-max > src-max: a catalog descriptor above every
#    emitted code = an orphan (reserved-but-never-emitted at the ceiling) worth reconciling before allocating.
$grepCodes = Select-String -Path (Get-ChildItem src -Recurse -Filter *.cs | Where-Object FullName -notmatch '\\(bin|obj)\\') `
    -Pattern 'COBOLNET1[0-9][0-9][0-9]' -AllMatches | ForEach-Object { $_.Matches.Value } | Sort-Object -Unique
$maxGrep = ($grepCodes | Measure-Object -Maximum).Maximum
$catalog = Select-String -Path 'src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs' -Pattern '"(COBOLNET1\d\d\d)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$maxCat = ($catalog | Measure-Object -Maximum).Maximum
$grepNum = [int]($maxGrep -replace 'COBOLNET', '')
$catNum = [int]($maxCat -replace 'COBOLNET', '')
$nextFree = ([Math]::Max($grepNum, $catNum)) + 1
Write-Host "diag   : src-grep max $maxGrep · catalog max $maxCat $(if ($catNum -gt $grepNum) { "⚠ CATALOG ABOVE SRC (orphan descriptor $maxCat never emitted) — reconcile before allocating; next free = COBOLNET$nextFree" } else { "→ next free = COBOLNET$nextFree" })"

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
    # Once Phase B starts, the GAP count alone hides the SHAPE of what is left — a DIVERGES is a queued fix, a
    # NEEDS-OWNER-DECISION is blocked on the owner, and a CONFORMS still lacking a test is a golden away from
    # closing. Print the breakdown only when there is one, so this stays a single line before Phase B.
    $adjudicated = @($inv | Where-Object { $_.verdict })
    if ($adjudicated.Count -gt 0) {
        $byVerdict = $adjudicated | Group-Object verdict | Sort-Object Count -Descending |
            ForEach-Object { "$($_.Name) $($_.Count)" }
        Write-Host "       $($adjudicated.Count) adjudicated · $($byVerdict -join ' · ')"
        # ⛔ "test-needed" is a row that DID NOT CLOSE, not a row with an empty test-ref. The two differ: a row
        # can cite a NIST golden or a *_MatchesLegacy differential — which resolves on disk and is worth
        # recording — and still be open, because only a SPEC-DERIVED test closes a row. Keying on the empty
        # string under-reported this by 7 of 11 on the first Phase-B batch.
        $needTest = @($adjudicated | Where-Object { $_.verdict -eq 'CONFORMS' -and $_.state -eq 'GAP' }).Count
        if ($needTest -gt 0) { Write-Host "       $needTest CONFORMS still test-needed (each is one spec-derived golden from OK)" }
    }
} else {
    Write-Host "invent : not built yet (P14 Step 0)"
}

Write-Host "=== next: confirm the battery green (plan §9) before code changes; read plan §0 for the worklist ==="
