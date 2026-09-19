<#
.SYNOPSIS
    Fetch the GnuCOBOL testsuite into the git-ignored external-corpus tree.

.DESCRIPTION
    Plan §11 row A4 / PHASE-14 Step 13 — the EXTERNAL DIFFERENTIAL CORPUS.
    Design SSOT: docs/rearchitecture/DESIGN-test-build-ci.md §3.10 (the instrument-gate inventory row for
    `ExternalCorpusPopulationDriftTests`) and plan §9 (corpus mechanics).

    ⚖ LICENSING POSTURE (load-bearing — do NOT deviate):
    GnuCOBOL and its testsuite are GPL-3.0; this repository is BSL 1.1. Their test TEXT is therefore
    NEVER committed here. It is fetched ON DEMAND by this script into `tests/external/gnucobol/`, which
    is git-ignored. The artifacts this repo DOES own and commit are:
        * this retrieval script
        * scripts/gnucobol_extract.py            (the .at autotest extractor)
        * the adapter test project
        * tests/external/gnucobol-expectations.json  (independently-authored FACTS about cases:
          id -> classification + our §-cited rationale — never their source or their expected output)

    A missing corpus is handled two ways, deliberately different (PB209/PB277): the adapter conformance
    suite SKIPS with a loud notice rather than silently passing, while the population drift gate
    (ExternalCorpusPopulationDriftTests) FAILS — a gate that cannot see its population must not read as
    green. CI runs this script on demand (cached, pinned) so that gate measures everywhere.

    ⛔ THREE RULES THIS SCRIPT EXISTS TO KEEP (kb/Work PB897 — it broke all three, and the resulting permanent
    red turned that gate into one every agent was told to ignore, which is a gate that has stopped gating):

    1. NOTHING IS DELETED BEFORE THE REPLACEMENT EXISTS. The extraction lands in a staging directory beside
       the destination and is swapped in ONLY after tar succeeded AND the payload was verified. A failed
       fetch therefore leaves the previous corpus exactly as it was; it can never empty the tree it is
       about to fail to fill.
    2. THE ARCHIVE IS NAMED RELATIVE TO ITS OWN DIRECTORY. `tar -xf E:\…\gnucobol-3.2.tar.xz` makes GNU tar
       read `E:` as a REMOTE HOST (`Cannot connect to E: resolve failed`, exit 128) while Windows' System32
       bsdtar accepts it — so which tar is first on PATH decided whether the fetch worked. ⛔ `--force-local`
       is NOT the repair: GNU tar takes it, bsdtar rejects it outright (`Option --force-local is not
       supported`, exit 1), so it merely moves the failure to the other machine. Push-Location to the
       archive's directory and pass a bare file name, which NEITHER implementation misreads.
    3. THE tar THAT CAN DO THE JOB IS FOUND BY DOING IT. Git-for-Windows' GNU tar cannot read a .tar.xz at all
       — it execs an `xz` binary it does not ship (`tar (child): xz: Cannot exec`) — so `Get-Command tar`
       answering with it meant no corpus, on that machine only. The extraction walks an ORDERED candidate list
       and MEASURES each one by attempting the extraction; rule 1's staging directory is what makes a failed
       attempt free. See Get-TarCandidates.

    Every failure exit prints one `FETCH FAILED: <cause>` line and exits non-zero, so the caller can say
    WHY the corpus is absent instead of leaving the population reds unattributed.

.PARAMETER Force
    Re-download even when a verified tarball is already present.

.PARAMETER KeepArchive
    Keep the downloaded tarball after extraction (default: keep, so re-runs are offline).

.PARAMETER SelfTest
    Prove the two rules above by DRIVING THEM, over a synthetic archive in a temporary sandbox: no network,
    no repository state touched. A check that has never been observed failing is not evidence
    (`feedback_green_gates_arent_evidence`), so the self-test asserts the FAILURE branch preserves the prior
    corpus, not merely that the success branch works. `ExternalCorpusFetchTests` (tests/Cobol.Net.Tests.Unit)
    runs it, and so must anyone changing the extraction.
#>
[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$KeepArchive = $true,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

# ── PINNED RELEASE ────────────────────────────────────────────────────────────────────────────────────────
# Pinned deliberately: an unpinned "latest" would silently change the differential baseline underneath the
# expectations ledger, turning corpus drift into phantom compiler regressions.
$Version  = '3.2'
$Archive  = "gnucobol-$Version.tar.xz"
$Url      = "https://ftp.gnu.org/gnu/gnucobol/$Archive"
# SHA256 established 2026-07-19 from the official GNU mirror. If this ever mismatches, STOP and
# investigate before relaxing it — a changed hash on a released tarball is not a routine event.
$Sha256   = '3bb48af46ced4779facf41fdc2ee60e4ccb86eaa99d010b36685315df39c2ee2'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ExtDir   = Join-Path $RepoRoot 'tests/external'
$DestDir  = Join-Path $ExtDir  'gnucobol'
$ArcPath  = Join-Path $ExtDir  $Archive

# ── THE ONE FAILURE SURFACE ───────────────────────────────────────────────────────────────────────────────
# Every way this script can fail prints THIS line and nothing else prints it, so `FETCH FAILED:` in a build
# log is both greppable and attributable to a cause. build-local.{ps1,sh} surface the non-zero exit beside it.
function Write-FetchFailure {
    param([Parameter(Mandatory)][string]$Reason)
    Write-Host "FETCH FAILED: $Reason"
}

# ── WHICH tar (PB897, fault 3) ────────────────────────────────────────────────────────────────────────────
# ⛔ "The tar on PATH" is not one program. Windows ships **bsdtar** (libarchive) at System32\tar.exe with
# liblzma COMPILED IN; Git-for-Windows puts **GNU tar** earlier on PATH for anything launched from its shell,
# and that build shells out to an `xz` binary it does not ship — `tar (child): xz: Cannot exec` — so it cannot
# read a .tar.xz AT ALL. Which one answered `Get-Command tar` was therefore deciding whether the corpus
# existed, per machine, invisibly. This returns an ORDERED candidate list and the extraction tries each in
# turn: capability is MEASURED by attempting the work, never deduced from a version string
# (`feedback_reachability_is_measured_not_deduced`), and the staging directory makes a failed attempt free.
function Get-TarCandidates {
    $candidates = [System.Collections.Generic.List[string]]::new()
    # ⚠ Case-INSENSITIVE dedup: `Get-Command` reports the PATH entry's own spelling, so System32\tar.exe came
    # back twice (`System32` and `system32`) and the same binary was attempted twice on every failure path.
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    if ($IsWindows) {
        $sys = Join-Path $env:SystemRoot 'System32/tar.exe'
        if (Test-Path $sys) { $p = (Resolve-Path $sys).Path; if ($seen.Add($p)) { $candidates.Add($p) } }
    }
    foreach ($cmd in @(Get-Command tar -All -CommandType Application -ErrorAction SilentlyContinue)) {
        if ($cmd.Source -and $seen.Add($cmd.Source)) { $candidates.Add($cmd.Source) }
    }
    return , $candidates.ToArray()
}

# ── THE EXTRACTION + SWAP, AS ONE FUNCTION ────────────────────────────────────────────────────────────────
# ⛔ The ONLY place the corpus tree is replaced. It is a function so the self-test drives the REAL code rather
# than a second copy of it (`feedback_one_rule_one_place`). Returns $true on success; on failure it prints a
# FETCH FAILED line, leaves $Destination byte-for-byte as it found it, and removes its own staging directory.
function Invoke-CorpusExtraction {
    param(
        [Parameter(Mandatory)][string]$ArchiveDirectory,   # holds the archive; the staging dir is created here
        [Parameter(Mandatory)][string]$ArchiveName,        # BARE FILE NAME — never a drive-letter path (rule 2)
        [Parameter(Mandatory)][string]$Member,             # the archive member to extract, e.g. gnucobol-3.2/tests
        [Parameter(Mandatory)][string]$Destination         # replaced only after a verified extraction (rule 1)
    )

    $candidates = Get-TarCandidates
    if ($candidates.Count -eq 0) {
        Write-FetchFailure "no tar implementation found (needed to unpack $ArchiveName) — the previous corpus at $Destination is UNTOUCHED."
        return $false
    }

    $staging  = $null
    $attempts = [System.Collections.Generic.List[string]]::new()
    foreach ($tarExe in $candidates) {
        $stagingName = '.extract-' + [guid]::NewGuid().ToString('N')
        $candidateStaging = Join-Path $ArchiveDirectory $stagingName
        New-Item -ItemType Directory -Force -Path $candidateStaging | Out-Null

        $rc = 1
        Push-Location $ArchiveDirectory
        try {
            # Both operands RELATIVE to the archive's own directory: GNU tar's `host:path` rule never sees a
            # colon, and bsdtar needs no flag. This is the whole of rule 2 (`--force-local` breaks the other tar).
            & $tarExe -xf $ArchiveName -C $stagingName --strip-components=1 $Member
            $rc = $LASTEXITCODE
        } finally {
            Pop-Location
        }

        # A zero exit is not a population. An extraction that produced nothing must not replace a good tree —
        # the same "a missing observation is not a negative one" rule the drift gate itself is built on.
        $extracted = $rc -eq 0 -and $null -ne (Get-ChildItem -Path $candidateStaging -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1)
        if ($extracted) { $staging = $candidateStaging; break }

        Remove-Item $candidateStaging -Recurse -Force -ErrorAction SilentlyContinue
        $attempts.Add("$tarExe -> " + $(if ($rc -ne 0) { "exit $rc" } else { 'exit 0 but NO FILES extracted' }))
    }

    if (-not $staging) {
        Write-FetchFailure "no tar implementation could extract '$Member' from $ArchiveName [$($attempts -join '; ')] — the previous corpus at $Destination is UNTOUCHED."
        return $false
    }

    # ── THE SWAP. Both paths are siblings in $ArchiveDirectory, so each Move-Item is a same-volume rename.
    $backup = "$Destination.prev-" + [guid]::NewGuid().ToString('N')
    $moved  = $false
    try {
        if (Test-Path $Destination) { Move-Item -LiteralPath $Destination -Destination $backup; $moved = $true }
        Move-Item -LiteralPath $staging -Destination $Destination
    } catch {
        # Put the old tree back before reporting: a half-done swap is the very state rule 1 forbids.
        if ($moved -and -not (Test-Path $Destination)) { Move-Item -LiteralPath $backup -Destination $Destination }
        Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
        Write-FetchFailure "could not swap the extracted tree into $Destination ($($_.Exception.Message)) — the previous corpus was restored."
        return $false
    }
    if ($moved) { Remove-Item $backup -Recurse -Force -ErrorAction SilentlyContinue }
    return $true
}

# ── SELF-TEST ─────────────────────────────────────────────────────────────────────────────────────────────
function Invoke-SelfTest {
    # ⚠ SCRIPT-SCOPED ON PURPOSE. A `$checks` local to this function plus a `$script:checks` written by the
    # nested assert are TWO variables, and the verdict would then be computed from the one nothing increments —
    # a self-test that always says PASS, which is the exact failure class this file exists to prevent.
    $script:selfTestChecks = 0
    $script:selfTestFail   = 0
    function Assert-That {
        param([string]$What, [bool]$Condition)
        $script:selfTestChecks++
        if ($Condition) { Write-Host "  ok   $What" } else { Write-Host "  FAIL $What"; $script:selfTestFail++ }
    }

    $sandbox = Join-Path ([IO.Path]::GetTempPath()) ('cobolnet-pb897-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $sandbox | Out-Null
    try {
        $candidates = Get-TarCandidates
        Assert-That 'at least one tar implementation is available' ($candidates.Count -gt 0)
        if ($candidates.Count -eq 0) { Write-Host '=== FETCH SELF-TEST: FAIL (no tar at all) ==='; return 1 }
        Write-Host ('  tar candidates, in order: ' + ($candidates -join ', '))

        # ⛔ THE FIXTURE IS A .tar.xz — the format the pinned release actually ships. A plain .tar fixture would
        # pass on the very machine where Git-for-Windows' GNU tar cannot exec `xz`, which is the hole that hid
        # fault 3 (`feedback_probe_the_shape_the_subject_hides`). Shaped like the real archive: <root>/tests/…
        # behind --strip-components=1.
        $srcRoot = Join-Path $sandbox 'selftest-1.0/tests/testsuite.src'
        New-Item -ItemType Directory -Force -Path $srcRoot | Out-Null
        Set-Content -LiteralPath (Join-Path $srcRoot 'SELFTEST.at') -Value 'AT_SETUP([selftest])' -NoNewline
        $mk = 1
        foreach ($mkTar in $candidates) {
            Push-Location $sandbox
            try { & $mkTar -cJf 'good.tar.xz' 'selftest-1.0'; $mk = $LASTEXITCODE } finally { Pop-Location }
            if ($mk -eq 0) { break }
            Remove-Item (Join-Path $sandbox 'good.tar.xz') -Force -ErrorAction SilentlyContinue
        }
        Assert-That 'a .tar.xz fixture can be produced (some tar here speaks xz)' ($mk -eq 0)
        if ($mk -ne 0) { Write-Host '=== FETCH SELF-TEST: FAIL (no tar here writes .tar.xz — none could read the pinned release either) ==='; return 1 }
        Remove-Item (Join-Path $sandbox 'selftest-1.0') -Recurse -Force
        # A file that is NOT an archive — the failure fixture. Nothing about it is tar-flavour specific.
        Set-Content -LiteralPath (Join-Path $sandbox 'broken.tar.xz') -Value 'this is not a tar archive' -NoNewline

        $dest = Join-Path $sandbox 'corpus'

        # (1) SUCCESS replaces the tree. ⛔ THIS IS ALSO THE RULE-2 CHECK: $sandbox is a drive-letter path on
        #     Windows, so under GNU tar the OLD code's absolute `-f` argument fails here with
        #     `Cannot connect to <drive>: resolve failed` — the function passing a bare name is what makes it pass.
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Set-Content -LiteralPath (Join-Path $dest 'PRIOR.txt') -Value 'prior' -NoNewline
        $ok = Invoke-CorpusExtraction -ArchiveDirectory $sandbox -ArchiveName 'good.tar.xz' -Member 'selftest-1.0/tests' -Destination $dest
        Assert-That 'a good archive extracts and swaps in' $ok
        Assert-That 'the extracted payload is present' (Test-Path (Join-Path $dest 'tests/testsuite.src/SELFTEST.at'))
        Assert-That 'the replaced tree is gone' (-not (Test-Path (Join-Path $dest 'PRIOR.txt')))

        # (2) ⛔ THE RULE-1 CHECK: a failing extraction must leave the corpus intact (PB897's fault 2).
        $ok2 = Invoke-CorpusExtraction -ArchiveDirectory $sandbox -ArchiveName 'broken.tar.xz' -Member 'selftest-1.0/tests' -Destination $dest
        Assert-That 'a corrupt archive is reported as a failure' (-not $ok2)
        Assert-That 'the PREVIOUS corpus survived the failed extraction' (Test-Path (Join-Path $dest 'tests/testsuite.src/SELFTEST.at'))

        # (3) A member that is not in the archive fails the same way — tar exits non-zero on both flavours.
        $ok3 = Invoke-CorpusExtraction -ArchiveDirectory $sandbox -ArchiveName 'good.tar.xz' -Member 'selftest-1.0/absent' -Destination $dest
        Assert-That 'a missing archive member is reported as a failure' (-not $ok3)
        Assert-That 'the corpus survived the missing-member failure' (Test-Path (Join-Path $dest 'tests/testsuite.src/SELFTEST.at'))

        # (4) No staging directory is ever left behind, on either path.
        $leftovers = @(Get-ChildItem -Path $sandbox -Directory -Filter '.extract-*' -Force -ErrorAction SilentlyContinue)
        Assert-That 'no staging directory is left behind' ($leftovers.Count -eq 0)

    } finally {
        Remove-Item $sandbox -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($script:selfTestFail -eq 0) { Write-Host "=== FETCH SELF-TEST: PASS ($script:selfTestChecks checks) ==="; return 0 }
    Write-Host "=== FETCH SELF-TEST: FAIL ($script:selfTestFail of $script:selfTestChecks checks) ==="
    return 1
}

if ($SelfTest) { exit (Invoke-SelfTest) }

New-Item -ItemType Directory -Force -Path $ExtDir | Out-Null

function Test-Archive {
    if (-not (Test-Path $ArcPath)) { return $false }
    $actual = (Get-FileHash -Algorithm SHA256 -Path $ArcPath).Hash.ToLowerInvariant()
    if ($actual -ne $Sha256) {
        Write-Warning "checksum mismatch on $Archive"
        Write-Warning "  expected $Sha256"
        Write-Warning "  actual   $actual"
        return $false
    }
    return $true
}

if ($Force -and (Test-Path $ArcPath)) { Remove-Item $ArcPath -Force }

if (-not (Test-Archive)) {
    Write-Host "downloading $Url ..."
    try {
        Invoke-WebRequest -Uri $Url -OutFile $ArcPath -MaximumRedirection 5
    } catch {
        Write-FetchFailure "download of $Url failed: $($_.Exception.Message)"
        exit 1
    }
    if (-not (Test-Archive)) {
        Remove-Item $ArcPath -Force -ErrorAction SilentlyContinue
        Write-FetchFailure "checksum verification FAILED for $Archive — refusing to extract."
        exit 1
    }
}
Write-Host "verified $Archive (sha256 $Sha256)"

# ── EXTRACT (tests tree only — we need no GnuCOBOL sources, only its testsuite) ───────────────────────────
if (-not (Invoke-CorpusExtraction -ArchiveDirectory $ExtDir -ArchiveName $Archive -Member "gnucobol-$Version/tests" -Destination $DestDir)) {
    exit 1
}

if (-not $KeepArchive) { Remove-Item $ArcPath -Force -ErrorAction SilentlyContinue }

$src = Join-Path $DestDir 'tests/testsuite.src'
if (-not (Test-Path $src)) { $src = Join-Path $DestDir 'testsuite.src' }
$atCount = (Get-ChildItem -Path $src -Filter *.at -ErrorAction SilentlyContinue).Count
if ($atCount -eq 0) {
    Write-FetchFailure "the extracted tree at $DestDir holds no .at autotest wrappers — the population drift gate would read it as empty."
    exit 1
}

Write-Host ""
Write-Host "GnuCOBOL $Version testsuite ready:"
Write-Host "  $DestDir   ($atCount autotest .at files)"
Write-Host ""
Write-Host "This tree is GPL-3.0 and is GIT-IGNORED. Never commit its contents."
Write-Host "Next: python3 scripts/gnucobol_extract.py --summary"
