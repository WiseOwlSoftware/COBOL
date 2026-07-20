<#
.SYNOPSIS
    Fetch the GnuCOBOL testsuite into the git-ignored external-corpus tree.

.DESCRIPTION
    Plan §11 row A4 / PHASE-14 Step 13 — the EXTERNAL DIFFERENTIAL CORPUS.

    ⚖ LICENSING POSTURE (load-bearing — do NOT deviate):
    GnuCOBOL and its testsuite are GPL-3.0; this repository is BSL 1.1. Their test TEXT is therefore
    NEVER committed here. It is fetched ON DEMAND by this script into `tests/external/gnucobol/`, which
    is git-ignored. The artifacts this repo DOES own and commit are:
        * this retrieval script
        * scripts/gnucobol_extract.py            (the .at autotest extractor)
        * the adapter test project
        * tests/external/gnucobol-expectations.json  (independently-authored FACTS about cases:
          id -> classification + our §-cited rationale — never their source or their expected output)

    A missing corpus is not an error for the test suite: the adapter SKIPS with a loud notice rather
    than silently passing. This script is what turns the skip into a run.

.PARAMETER Force
    Re-download even when a verified tarball is already present.

.PARAMETER KeepArchive
    Keep the downloaded tarball after extraction (default: keep, so re-runs are offline).
#>
[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$KeepArchive = $true
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
        Write-Error "download failed: $_`nThe external corpus is optional — the adapter suite will SKIP with a notice."
        exit 1
    }
    if (-not (Test-Archive)) {
        Remove-Item $ArcPath -Force -ErrorAction SilentlyContinue
        Write-Error "checksum verification FAILED for $Archive — refusing to extract."
        exit 1
    }
}
Write-Host "verified $Archive (sha256 $Sha256)"

# ── EXTRACT (tests tree only — we need no GnuCOBOL sources, only its testsuite) ───────────────────────────
if (Test-Path $DestDir) { Remove-Item $DestDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $DestDir | Out-Null

$tar = Get-Command tar -ErrorAction SilentlyContinue
if (-not $tar) { Write-Error "tar not found on PATH (needed to unpack .tar.xz)."; exit 1 }

& tar -xf $ArcPath -C $DestDir --strip-components=1 "gnucobol-$Version/tests"
if ($LASTEXITCODE -ne 0) { Write-Error "extraction failed (exit $LASTEXITCODE)"; exit 1 }

if (-not $KeepArchive) { Remove-Item $ArcPath -Force -ErrorAction SilentlyContinue }

$src = Join-Path $DestDir 'tests/testsuite.src'
if (-not (Test-Path $src)) { $src = Join-Path $DestDir 'testsuite.src' }
$atCount = (Get-ChildItem -Path $src -Filter *.at -ErrorAction SilentlyContinue).Count

Write-Host ""
Write-Host "GnuCOBOL $Version testsuite ready:"
Write-Host "  $DestDir   ($atCount autotest .at files)"
Write-Host ""
Write-Host "This tree is GPL-3.0 and is GIT-IGNORED. Never commit its contents."
Write-Host "Next: python3 scripts/gnucobol_extract.py --summary"
