# Copyright (c) 2026 Brent Rector. All rights reserved.
# Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#
# gen-reserved-words.ps1 — generate the per-edition reserved-word tables (VERSION_TEST_MATRIX_DESIGN
# "Phase-2 implementation plan" P2.4; derivation REVISED 2026-07-03, DEVLOG 585: the planned hand-authored
# 1985 delta list was replaced by published per-standard source lists after the API content filter blocked
# every attempt to move a word list through the conversation stream — 4th occurrence).
#
# Inputs:
#   specs/ISO_COBOL_2023.md                 (in-repo)  — ISO/IEC 1989:2023 §2023:8.9 = THE authoritative 2023 list.
#   docs/VERSION_CHANGE_REFERENCE.md   (in-repo)  — row 32 = the 16 words newly reserved in 2023 (Annex E.2 item 25).
#   tests/version-matrix/reserved-words-2002.json (in-repo) — the ISO/IEC 1989:2002 §2002:8.9 list, transcribed from the
#     2002 post-ballot text (kb/Work PB4297) = THE authoritative 2002 list, with its Annex F.1 item 6 list of the
#     words 2002 ADDED, which holds the 85 flag to the text (kb/Work PB3957).
#   tests/version-matrix/words-2009-cd.json (in-repo) — the 2009 committee draft's 8.9 and 8.10 lists, read for
#     the one 2014 question step 5 states (kb/Work PB4297).
#   GnuCOBOL config/{cobol85,cobol2002,cobol2014}.words (fetched to .cache/, NOT committed) — the per-standard
#     reserved-word lists curated by the GnuCOBOL project (FSF; the FILES are GPL — only the derived FACTS,
#     word/edition reservation flags with provenance, enter this repo).
#
# Outputs (BOTH committed; ReservedWordsDriftTests asserts they agree):
#   src/WiseOwl.Cobol.Editions/ReservedWords.Table.cs
#   tests/version-matrix/reserved-words.json
#
# Derivation: flags = set membership per source (85 = GnuCOBOL; 2002 = the 2002 standard's own §2002:8.9 list; 2014 =
# GnuCOBOL, less the 1985 and draft words neither the 2009 draft nor 2023 reserves; 2023 = the ISO spec itself).
# CLAUDE.md rule 1: an ISO text outranks GnuCOBOL, whose lists also reserve implementor words (kb/Work PB4297).
# Confidence:
#   high   — sources agree with the expected edge structure (incl. the re-reserved pair, which falls out of
#            membership mechanically: in 85, out 2002/2014, in 2023 = Annex E.2 item 25).
#   medium — disagreement buckets (a 2023 §2023:8.9 word unknown to the 2014 source yet NOT an Annex-E addition,
#            or any other non-monotone surprise). Medium rows are present but INERT: only high rejects
#            (the conservative policy — a wrong entry must never reject a valid program).
# ISO-2023 §2023:8.9 WINS every 2023 dispute (the spec is the authority; GnuCOBOL is a curation net).
#
# ⚠ CONTENT-FILTER RULE (DEVLOG 578/584/585 — tripped 4×): NOTHING here prints a word list — counts only.
#   Never cat/grep these outputs into a conversation stream.
#
# The generated table is the per-unit DEFAULT: the 2023 COBOL-WORDS directive (Annex E.3.3 item 12) mutates
# the effective set per compilation unit via the ReservedWordSet layer (roadmap ISO-validation D9).

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LfText.psm1') -Force   # every tracked output is written LF (kb/Work PB3029)
$repo = Split-Path -Parent $PSScriptRoot
$specPath = (& python (Join-Path $PSScriptRoot 'spec/spec_pdf.py') --markdown 2023)   # the one edition table (kb/Work PB4815)
if ($LASTEXITCODE -ne 0) { throw 'spec_pdf.py --markdown failed' }
$vcrPath  = Join-Path $repo 'docs/VERSION_CHANGE_REFERENCE.md'
$csOut    = Join-Path $repo 'src/WiseOwl.Cobol.Editions/ReservedWords.Table.cs'
$jsonOut  = Join-Path $repo 'tests/version-matrix/reserved-words.json'
# The §2023:8.10 CONTEXT-SENSITIVE word table (kb/Work PB250). >>COBOL-WORDS SR3 admits a context-sensitive
# word as literal-1/3/4 and SR4 bars one as literal-2/5/6, so the compiler needs the §2023:8.10 population as
# well as §2023:8.9's - it had only the words that happen to be lexer tokens, and rejected legal directives
# naming HEX, CURRENT, LC_ALL, ANUM, BYTE, ACTIVATING, STACK, TOP-LEVEL, UCS-4, UTF-8, UTF-16, ...  The
# directive is a 2023 introduction (cobol-words-directive-2023), so §2023:8.10's 2023 table is exactly the
# population its rules speak about; the per-word introduction edition (step 7c, kb/Work PB4174) serves the
# syntax diagnostics that name the words expected, not the directive.
$ctxCsOut   = Join-Path $repo 'src/WiseOwl.Cobol.Editions/ContextSensitiveWords.Table.cs'
$ctxJsonOut = Join-Path $repo 'tests/version-matrix/context-sensitive-words.json'
$cache    = Join-Path $repo '.cache/gnucobol-words'

# ---- 0. Fetch the GnuCOBOL per-standard lists (cached; pinned tag first, master fallback) ----
New-Item -ItemType Directory -Force $cache | Out-Null
$gcFiles = @{}
foreach ($std in 'cobol85', 'cobol2002', 'cobol2014') {
    $f = Join-Path $cache "$std.words"
    if (-not (Test-Path $f) -or (Get-Item $f).Length -lt 1000) {
        $ok = $false
        foreach ($ref in 'gnucobol-3.2', 'master') {
            try {
                Invoke-WebRequest -Uri "https://raw.githubusercontent.com/OCamlPro/gnucobol/$ref/config/$std.words" -OutFile $f -UseBasicParsing -ErrorAction Stop
                if ((Get-Item $f).Length -gt 1000) { $ok = $true; break }
            } catch { }
        }
        if (-not $ok) { throw "could not fetch GnuCOBOL $std.words (network?); cache at $cache" }
    }
    $gcFiles[$std] = $f
}

# ---- 1. Parse a GnuCOBOL .words file: 'reserved:'/'register:' entries; '*' = context-sensitive (skip);
#         'WORD=ALIAS' reserves both spellings. Returns an uppercase set. ----
function Parse-GcWords([string]$path) {
    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in Get-Content -LiteralPath $path) {
        if ($line -notmatch '^(reserved|register):\s*(.+)$') { continue }
        $tok = ($Matches[2] -split '#', 2)[0].Trim()          # strip trailing comment
        if ($tok.Length -eq 0 -or $tok.EndsWith('*')) { continue }  # context-sensitive => not reserved
        foreach ($side in $tok -split '=') {
            $w = $side.Trim().ToUpperInvariant()
            if ($w -match '^[A-Z][A-Z0-9-]*$') { [void]$set.Add($w) }
        }
    }
    return ,$set
}
$gc85   = Parse-GcWords $gcFiles['cobol85']
$gc2002 = Parse-GcWords $gcFiles['cobol2002']
$gc2014 = Parse-GcWords $gcFiles['cobol2014']

# ---- 1b. The 2002 standard's §2002:8.9 list and the 2009 draft's 8.9 / 8.10 lists (kb/Work PB4297) ----
# Transcribed word lists (words only; each file's _comment names its source and printed pages). GnuCOBOL's
# cobol2002.words and cobol2014.words also reserve words no ISO text of the edition lists (the 1985 vocabularies
# 2002 removed - POSITION, PROCEED, WORDS, the DEBUG-n registers - and the draft's ROUNDED-phrase words), and the
# compiler refused them as data-names with COBOL0901 citing §2023:8.9; an ISO text outranks GnuCOBOL (CLAUDE.md rule 1).
function Read-WordList([string]$path, [string]$key) {
    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($w in (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).$key) { [void]$set.Add($w) }
    return ,$set
}
$p2Path = Join-Path $repo 'tests/version-matrix/reserved-words-2002.json'
$cdPath = Join-Path $repo 'tests/version-matrix/words-2009-cd.json'
$iso2002   = Read-WordList $p2Path 'words'
$added2002 = Read-WordList $p2Path 'addedByAnnexF1'   # the same text's Annex F.1 item 6: the reserved words 2002 ADDED
$cdReserved = Read-WordList $cdPath 'reserved'
$cdContext  = Read-WordList $cdPath 'contextSensitive'
# ALIGNED is reserved at 2002 although the 2002 §2002:8.9 list omits it: the same text's Annex F.1 item 6 lists it among
# the reserved words 2002 added and its 13.16.1 defines the ALIGNED clause, so the omission is the list's defect.
foreach ($w in @('ALIGNED')) { [void]$iso2002.Add($w) }

# ---- 2. Extract the 2023 §2023:8.9 list from the ISO spec ----
$lines = Get-Content -LiteralPath $specPath
$start = -1; $end = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    # `#{2,}` not `##`: the transcription's heading depths were normalised (repairs/heading_depth.py) and
    # §2023:8.9 is now `### `, so the old two-hash anchor matched NOTHING and this script threw on every run.
    if ($start -lt 0 -and $lines[$i] -match '^#{2,}\s+8\.9\s+Reserved words') { $start = $i; continue }
    if ($start -ge 0 -and $lines[$i] -match '^#{2,}\s+8\.10\s') { $end = $i; break }
}
if ($start -lt 0 -or $end -lt 0) { throw "spec §2023:8.9 section not located (start=$start end=$end)" }
# ONE normalization for a spec word line - the list bullet, the inline-code fence that keeps Markdown from
# eating '>' and '>>', and the &nbsp; padding of an OCR continuation fragment. Both the STEM line and the
# CONTINUATION line must go through it (kb/Work PB250).
function Normalize-SpecWordLine([string]$line) {
    return ($line -replace '^\s*-\s+', '' -replace '`', '' -replace '&nbsp;', '').Trim()
}
$iso2023 = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
for ($i = $start + 1; $i -lt $end; $i++) {
    # The list is a real Markdown list now (it rendered as run-on prose as bare lines), and the
    # special-character words are inline code so Markdown cannot eat them — `>` and `>>` used to VANISH into a
    # blockquote. Strip the bullet and the code fence before matching; the word regex below is unchanged.
    $t = Normalize-SpecWordLine $lines[$i]
    if ($t.Length -eq 0) { continue }
    $t = $t.Replace([char]0x2013, '-').Replace([char]0x2014, '-').ToUpperInvariant()
    switch ($t) {                                   # OCR remaps (DEVLOG 578 scout findings)
        'EMD-START'   { $t = 'END-START' }
        'I-OICONTROL' { $t = 'I-O-CONTROL' }
    }
    # OCR line-splits: a long word wraps as 'STEM-' + an '&NBSP;&NBSP;FRAGMENT' continuation line
    # (the FLOAT-NOT-A-NUMBER-QUIET/-SIGNALING pair, DEVLOG 585) — join stem + next non-empty line.
    # ⛔ THE CONTINUATION NEEDS THE SAME NORMALIZATION AS THE STEM (kb/Work PB250). The transcription
    # repair that made the list a real Markdown list also bulleted and code-fenced the CONTINUATION lines
    # ('- `&nbsp;&nbsp;QUIET`'), and this branch stripped only the entity - so the join produced
    # 'FLOAT-NOT-A-NUMBER-- `QUIET`', the word regex dropped it, and a re-run of this generator SILENTLY
    # un-reserved FLOAT-NOT-A-NUMBER-QUIET and -SIGNALING at 2023. Two normalizations, one shape: one place.
    if ($t.EndsWith('-')) {
        for ($j = $i + 1; $j -lt $end; $j++) {
            $cont = (Normalize-SpecWordLine $lines[$j]).ToUpperInvariant()
            if ($cont.Length -eq 0) { continue }
            $t = $t + $cont; $i = $j; break
        }
    }
    # ⛔ PAGE IS A §2023:8.9 WORD (kb/Work PB655). This line used to drop PAGE with NOTE — an OCR-era guard against a
    # running page header, from before the list was a real Markdown list — and so un-reserved PAGE at 2023:
    # `- PAGE` sits between PACKED-DECIMAL and PAGE-COUNTER (`cite.py --check 8.9 "PAGE-COUNTER"`), and the
    # 2023 WRITE ADVANCING PAGE / RD PAGE LIMIT formats still spell it. Only the NOTE paragraph marker is excluded.
    if ($t -match '^[A-Z][A-Z0-9-]*[A-Z0-9]$' -and $t -ne 'NOTE') { [void]$iso2023.Add($t) }
}
# ⛔ METHOD IS NOT A §2023:8.9 WORD (kb/Work PB2623). An earlier revision added it here as an 'OCR omission (DEVLOG 578)';
# the printed list (PDF page 236, printed page 206) reads MERGE, MESSAGE-TAG, METHOD-ID, MINUS, MODE, MOVE, MULTIPLY -
# there is nothing to restore, and §2023:8.10 does not list it either. §2023:4.2.8 makes the §2023:8.9 words THE reserved words,
# so METHOD is a user-defined word at 2023 like every other 2002/2014 word the 2023 list dropped (POSITION, COBOL,
# ...: provenance 'dropped by 2023 §2023:8.9'). The END METHOD marker (§2023:10.7.2) still spells it, as a keyword in that
# position only (docs/CONFORMANCE.md, the §2023:4.2.8 determination).

# ---- 3. Parse VCR row 32: the 16 words newly reserved in 2023 (Annex E.2 item 25) ----
$vcrRow = (Get-Content -LiteralPath $vcrPath) | Where-Object { $_ -match '^\|\s*32\s*\|' } | Select-Object -First 1
if (-not $vcrRow) { throw 'VCR row 32 not found' }
if ($vcrRow -notmatch '\*\*New:\*\*\s*reserved:\s*([^.]+)\.') { throw 'VCR row 32 New:-reserved clause not parseable' }
$added2023 = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($m in [regex]::Matches($Matches[1], '[A-Z][A-Z0-9-]{2,}')) {
    if ($m.Value -ne 'B-SHIFT') { [void]$added2023.Add($m.Value) }   # exclude the family-glob remnant
}

# ---- 4. Sanity gates (fail loudly; never silently emit a wrong table) ----
if ($iso2023.Count -lt 400 -or $iso2023.Count -gt 430) { throw "2023 extraction $($iso2023.Count) outside 400..430" }
if ($gc85.Count -lt 320 -or $gc85.Count -gt 380)       { throw "gc85 $($gc85.Count) outside 320..380" }
# 2002/2014 exclude the starred context-sensitive entries (55/75 respectively — §2023:8.10-class words are NOT
# reserved) and dedupe WORD=ALIAS spellings, so the real counts sit well below the raw line counts.
if ($gc2002.Count -lt 380 -or $gc2002.Count -gt 470)   { throw "gc2002 $($gc2002.Count) outside 380..470" }
if ($gc2014.Count -lt 370 -or $gc2014.Count -gt 470)   { throw "gc2014 $($gc2014.Count) outside 370..470" }
if ($iso2002.Count -lt 395 -or $iso2002.Count -gt 410)       { throw "2002 §2002:8.9 list $($iso2002.Count) outside 395..410 - $p2Path changed shape" }
if ($cdReserved.Count -lt 380 -or $cdReserved.Count -gt 400) { throw "2009 draft 8.9 list $($cdReserved.Count) outside 380..400 - $cdPath changed shape" }
if ($cdContext.Count -lt 70 -or $cdContext.Count -gt 85)     { throw "2009 draft 8.10 list $($cdContext.Count) outside 70..85 - $cdPath changed shape" }
# GnuCOBOL's 2002 list adds implementor words to the standard's and drops only words 1985 and 2023 both reserve (the
# curation gap step 5 used to interpolate, kb/Work PB655): a 2002 §2002:8.9 word outside both means a list changed shape.
$p2NotGc = @($iso2002 | Where-Object { -not $gc2002.Contains($_) -and -not ($gc85.Contains($_) -and $iso2023.Contains($_)) })
if ($p2NotGc.Count -ne 0) { throw "cross-check failed: $($p2NotGc.Count) 2002 §2002:8.9 word(s) absent from GnuCOBOL's 2002 list and not reserved at 85 and 2023" }
# The 1985 flag is held to the 2002 text (kb/Work PB3957). GnuCOBOL's 85 list is the only 1985 source in the
# repository, so it is the base; but the 2002 standard's Annex F.1 item 6 names every reserved word it ADDED to the
# previous standard's, so an 8.9 word that item does not name was already reserved at 1985 - and a GnuCOBOL 85 list
# that lacks it is wrong. This is the check that would have caught ORDER, which an earlier revision removed from the
# 85 list on a misreading of a corpus program (ST127A spells SORT's WITH DUPLICATES IN ORDER keyword; no CCVS program
# declares ORDER), leaving COBOL-85 accepting `01 ORDER PIC 9.` that its own SORT format reserves. One direction
# only: F.1 also names a word the 85 list holds (LAST), so "added by 2002"
# does not prove "unreserved at 85".
$notReservedAt85 = @($iso2002 | Where-Object { -not $added2002.Contains($_) -and -not $gc85.Contains($_) })
if ($notReservedAt85.Count -ne 0) { throw "$($notReservedAt85.Count) 2002 §2002:8.9 word(s) that Annex F.1 item 6 does not name as added are absent from the 85 flag source - they were reserved at 1985: $($notReservedAt85 -join ', ')" }
if ($added2002.Count -lt 80 -or $added2002.Count -gt 90) { throw "2002 Annex F.1 added list $($added2002.Count) outside 80..90 - $p2Path changed shape" }
if ($added2023.Count -ne 16) { throw "VCR row 32 parse yielded $($added2023.Count) != 16 words" }
foreach ($w in $added2023) { if (-not $iso2023.Contains($w)) { throw "Annex-E addition '$w' missing from the 2023 extraction" } }
# ISO Annex E.2 item 25 is authoritative for ONE edition edge: E.1 lists "the substantive changes between the
# previous COBOL standard and this" one, so its 16 added words were user-defined at 2014 - and it says NOTHING
# about 2002 (kb/Work PB656). The 2014 source must therefore agree with it (asserted here, never overridden);
# the 2002 flag is the 2002 §2002:8.9 list's (kb/Work PB4297), which keeps three of the 16 reserved: the communication
# words SEND, RECEIVE and END-RECEIVE, a facility the 2002 standard keeps as OBSOLETE (removed 2014) - the same
# facility whose CD/DISABLE/ENABLE/PURGE/... rows read r2002 true from the same list. An
# earlier revision cleared their r2002 too, on a reading of item 25 that the E.1 scope does not support, and so
# left one facility's vocabulary half reserved at 2002. Those three are also the 1985 RE-reservations,
# discovered mechanically - the earlier recall-based classification knew only two (DEVLOG 585).
$reRes = @($added2023 | Where-Object { $gc85.Contains($_) })
if ($reRes.Count -ne 3) { throw "re-reserved set check: added2023 ∩ gc85 = $($reRes.Count), expected 3" }
$vcrVsGc = @($added2023 | Where-Object { $gc2014.Contains($_) })
if ($vcrVsGc.Count -ne 0) { throw "cross-check failed: $($vcrVsGc.Count) Annex-E 2023 additions already reserved in the 2014 source (E.2 item 25 says they were user-defined at 2014)" }

# ---- 5. Classify every word in the union ----
$all = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($s in @($gc85, $gc2002, $gc2014, $iso2023)) { foreach ($w in $s) { [void]$all.Add($w) } }
$mediumBuckets = @{ isoOnly = 0; nonMonotone = 0 }
$reservedNowhere = 0
$rows = foreach ($w in $all) {
    $f85 = $gc85.Contains($w); $f02 = $iso2002.Contains($w); $f14 = $gc2014.Contains($w); $f23 = $iso2023.Contains($w)
    # The 2014 edge (kb/Work PB4297). No 2014 text of record is held, so GnuCOBOL's 2014 list decides, with ONE
    # correction from the 2009 committee draft: a word the draft KNOWS - a 1985 reserved word (its 8.9 is a full
    # list, so it kept or dropped each) or a word of its 8.9 or 8.10 lists - that neither the draft's 8.9 nor the
    # 2023 §2023:8.9 reserves was not reserved by the 2014 standard between them (the 1985 words COBOL, POSITION,
    # PROCEED, ...; the communication word COMMUNICATION; the draft's context-sensitive NEGATIVE-INFINITY and
    # POSITIVE-INFINITY). A word the 2014 final added after the draft (PHYSICAL, the FLOAT-BINARY-n renames) is one
    # the draft cannot speak to, so it keeps GnuCOBOL's flag; and the draft's own 8.9 is NOT imported wholesale,
    # since its five names the final renamed (FLOAT-BINARY-7/16/34, INFINITY, REPRESENTS-NOT-A-NUMBER) never were.
    $cdSpeaks = $gc85.Contains($w) -or $cdReserved.Contains($w) -or $cdContext.Contains($w)
    $f14Draft = $f14 -and $cdSpeaks -and -not $cdReserved.Contains($w) -and -not $f23
    if ($f14Draft) { $f14 = $false }
    if (-not ($f85 -or $f02 -or $f14 -or $f23)) { $reservedNowhere++; continue }   # a GnuCOBOL-only word: no row
    $conf = 'high'; $prov = $null
    if ($added2023.Contains($w)) {
        # ISO Annex E.2 item 25 decides the 2014 edge only (E.1: the changes against the PREVIOUS standard), and
        # step 4 asserted the 2014 source agrees; every other flag is its own source's (kb/Work PB656).
        $prov = if ($f85 -and $f02) { '1985/2002-reserved (GnuCOBOL 85 list; ISO 2002 §2002:8.9: the communication facility, obsolete at 2002), unreserved 2014, re-reserved 2023 (ISO Annex E.2 item 25 = VCR row 32, which speaks of 2014 only)' }
                elseif ($f85)       { '1985-reserved, unreserved 2002/2014, re-reserved 2023 (ISO Annex E.2 item 25 = VCR row 32)' }
                else                { 'added 2023 (ISO Annex E.2 item 25 = VCR row 32)' }
    }
    elseif ($f23 -and -not $f14 -and $f85) {
        # Reserved in 85 AND 2023, with Annex E recording NO 2023 (re-)addition. ISO E.3.1: "Such incompatible
        # additions are documented in E.2", and E.2 25) lists every word 2023 reserved that was user-defined
        # before (the $added2023 branch above, which already caught the three 1985 RE-reservations). A word
        # absent from that list was therefore NOT user-defined at 2014 — reservation did not flicker, so the
        # 2014-source absence is a curation gap: interpolate it. kb/Work PB655: this is the REPORTS row, which sat
        # 'medium' + "(inert)" while the lexer token made it anything but inert — reserved-at-85/2023 but refused
        # as a raw parse error at 2002/2014 by a token no cobolWord admitted. The 2002 §2002:8.9 list holds every such
        # word (kb/Work PB4297), so only the 2014 flag is interpolated; one it lacked would break the premise.
        if (-not $f02) { throw "'$w' is reserved at 85 and 2023 with no Annex-E addition, but the 2002 §2002:8.9 list omits it - decide its edges" }
        $prov = 'continuous since 1985; the 2014 flag INTERPOLATED (85+2002+2023 reserved, Annex E silent ⇒ the 2014 source list has a gap)'
        $f14 = $true
    }
    elseif ($f23 -and -not $f14) {
        # In the ISO 2023 list, unknown to the 2014 source, NOT an Annex-E addition: source disagreement.
        $conf = 'medium'; $mediumBuckets.isoOnly++
        $prov = 'in ISO 2023 §2023:8.9 but not the 2014 source list and not an Annex-E addition — needs review (inert)'
    }
    elseif ($f85 -and -not $f02 -and ($f14 -or $f23)) {
        # Gone in 2002 but back later without an Annex-E record: non-monotone surprise.
        $conf = 'medium'; $mediumBuckets.nonMonotone++
        $prov = 'non-monotone reservation across sources — needs review (inert)'
    }
    elseif ($f85 -and $f23) { $prov = 'continuous since 1985 (GnuCOBOL 85 and 2014 lists; ISO 2002 and 2023 §2023:8.9)' }
    # A word 1985 reserved and 2023 does not: the label names the LAST edition the sources reserve it at, read from
    # the same flags it describes (kb/Work PB656: one label said 'removed post-85' for every such word, so 35 rows
    # still reserved at 2002 - SEGMENT-LIMIT, COBOL, the communication and debug vocabularies - contradicted their
    # own flags). The word's reservation is its own fact: a construct removed at an edition may keep its leading
    # word reserved there (GnuCOBOL 3.2 at -std=cobol2002 refuses the SEGMENT-LIMIT clause AND the word).
    elseif ($f85 -and -not $f23) {
        $prov = if ($f02 -and $f14) { '1985-reserved through 2014 (GnuCOBOL 85 and 2014 lists; ISO 2002 §2002:8.9); dropped by 2023 §2023:8.9' }
                elseif ($f02 -and $f14Draft) { '1985-reserved, kept by ISO 2002 §2002:8.9, removed by 2014 (absent from the 2009 draft 8.9 and ISO 2023 §2023:8.9)' }
                elseif ($f02)       { '1985-reserved, kept by ISO 2002 §2002:8.9, removed by 2014 (absent from the GnuCOBOL 2014 list and ISO 2023 §2023:8.9)' }
                else                { '1985-reserved, removed by 2002 (absent from ISO 2002 §2002:8.9 and ISO 2023 §2023:8.9)' }
    }
    elseif ($f02) {
        $src = if ($gc2002.Contains($w) -and $w -ne 'ALIGNED') { 'ISO 2002 §2002:8.9' } else { 'ISO 2002 Annex F.1 item 6; §2002:8.9 omits it' }
        $prov = "added 2002 ($src" + $(if ($f23) { '; ISO 2023 §2023:8.9)' } elseif ($f14) { '; dropped by 2023 §2023:8.9)' } elseif ($f14Draft) { '; absent from the 2009 draft 8.9 and 2023 §2023:8.9)' } else { '; dropped by 2014)' })
    }
    elseif ($f14) { $prov = 'added 2014 (GnuCOBOL 2014 list' + $(if ($f23) { '; ISO 2023 §2023:8.9)' } else { '; dropped by 2023 §2023:8.9)' }) }
    else { $prov = 'ISO 2023 §2023:8.9' }
    [pscustomobject]@{ word = $w; r85 = $f85; r2002 = $f02; r2014 = $f14; r2023 = $f23; confidence = $conf; provenance = $prov }
}

# ---- 6. Emit the canonical JSON ----
$json = [ordered]@{
    _comment = 'GENERATED by scripts/gen-reserved-words.ps1 — do not hand-edit. Sources: ISO/IEC 1989:2023 §2023:8.9 (specs/ISO_COBOL_2023.md, authoritative for 2023), VCR row 32 (Annex E.2 item 25), the ISO 2002 §2002:8.9 list (reserved-words-2002.json, authoritative for 2002), the 2009 committee draft 8.9/8.10 lists (words-2009-cd.json, one 2014 correction), GnuCOBOL per-standard word lists (85/2014 flags; facts only). The drift test asserts this file equals the C# table. CONTENT-FILTER RULE: never print this file into a conversation.'
    words = $rows
}
Set-LfContent -LiteralPath $jsonOut -Value ($json | ConvertTo-Json -Depth 4)

# ---- 7. Emit the C# table ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('// <auto-generated>')
[void]$sb.AppendLine('// Generated by scripts/gen-reserved-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$sb.AppendLine('// Sources: ISO/IEC 1989:2023 §2023:8.9 (authoritative, 2023 flags), VCR row 32 (Annex E.2 item 25),')
[void]$sb.AppendLine('// ISO 2002 §2002:8.9 (reserved-words-2002.json, 2002 flags), the 2009 draft (words-2009-cd.json, one 2014 correction),')
[void]$sb.AppendLine('// GnuCOBOL per-standard word lists (85/2014 flags; derived facts with provenance).')
[void]$sb.AppendLine('// ReservedWordsDriftTests asserts this table equals tests/version-matrix/reserved-words.json.')
[void]$sb.AppendLine('// </auto-generated>')
[void]$sb.AppendLine('namespace WiseOwl.Cobol.Editions;')   # must MATCH the committed table (kb/Work PB250: it said WiseOwl.Cobol.Validation, so a re-run broke the build)
[void]$sb.AppendLine('')
[void]$sb.AppendLine('public static partial class ReservedWords')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    internal static readonly ReservedWordEntry[] Entries =')
[void]$sb.AppendLine('    [')
foreach ($r in $rows) {
    $b = @($r.r85, $r.r2002, $r.r2014, $r.r2023) | ForEach-Object { if ($_) { 'true' } else { 'false' } }
    $p = $r.provenance.Replace('"', '\"')
    [void]$sb.AppendLine("        new(""$($r.word)"", $($b[0]), $($b[1]), $($b[2]), $($b[3]), ""$($r.confidence)"", ""$p""),")
}
[void]$sb.AppendLine('    ];')
[void]$sb.AppendLine('}')
Set-LfContent -LiteralPath $csOut -Value $sb.ToString()

# ---- 7b. Extract the 2023 §2023:8.10 context-sensitive word list and emit its table (kb/Work PB250) ----
$cstart = -1; $cend = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($cstart -lt 0 -and $lines[$i] -match '^#{2,}\s+8\.10\s+Context-sensitive words') { $cstart = $i; continue }
    if ($cstart -ge 0 -and $lines[$i] -match '^#{2,}\s+8\.11\s') { $cend = $i; break }
}
if ($cstart -lt 0 -or $cend -lt 0) { throw "spec §2023:8.10 section not located (start=$cstart end=$cend)" }
# The section is a sequence of two-column Markdown tables (the printed page breaks split it); every data row
# is `| WORD | language construct or context |`. The repeated header rows are skipped by name.
$ctxRows = [ordered]@{}
for ($i = $cstart + 1; $i -lt $cend; $i++) {
    if ($lines[$i] -match '^\|\s*\*{0,2}([A-Za-z][A-Za-z0-9_-]*)\*{0,2}\s*\|\s*(.+?)\s*\|\s*$') {
        $w = $Matches[1].ToUpperInvariant()
        if ($w -eq 'CONTEXT-SENSITIVE') { continue }
        if (-not $ctxRows.Contains($w)) { $ctxRows[$w] = ($Matches[2] -replace '\*', '').Trim() }
    }
}
if ($ctxRows.Count -lt 50) { throw "§2023:8.10 extraction returned only $($ctxRows.Count) words - the table shape changed" }
$ctxSorted = $ctxRows.Keys | Sort-Object

# ---- 7c. The edition each §2023:8.10 word is first a keyword (kb/Work PB4174) ----
# §2023:8.10 carries no per-edition flag, and a syntax diagnostic that names the words EXPECTED at a refused token must
# name only the words the compile edition has (ISO §2023:8.3.2.1 1: a word not reserved at an edition is a user-defined
# word there). Three sources, in this order of authority:
#   2002  the word is in the 2002 standard's §2002:8.10 list (tests/version-matrix/context-sensitive-words-2002.json,
#         transcribed from the post-ballot text; the file's _comment names it);
#   2023  the word is in ISO/IEC 1989:2023 Annex E.3.3 item 13 ("the following words have either been added to the
#         list of context-sensitive words or the context in which they are reserved has been expanded") and NOT in the
#         2002 list: SECONDS is listed there too, and the 2002 list has it, so it is the "expanded" arm;
#   2014  every other word of the 2023 table: E.1 makes the 2014 standard the "previous COBOL standard" whose changes
#         E.3.3 lists, so a word E.3.3 does not call new was in 2014, and it was not in 2002.
# (A word an older edition RESERVED outright - MULTIPLE, a 1985 reserved word - is §2023:8.9's: AdmitsAsKeyword asks that
# table first.)
$ctx2002Path = Join-Path $repo 'tests/version-matrix/context-sensitive-words-2002.json'
$ctx2002 = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($w in (Get-Content -LiteralPath $ctx2002Path -Raw | ConvertFrom-Json).words) { [void]$ctx2002.Add($w) }
$ctx2023New = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$inItem13 = $false
foreach ($line in $lines) {
    if (-not $inItem13) { if ($line -match '^13\\\)\s+\*\*Context-sensitive words\.') { $inItem13 = $true }; continue }
    if ($line -match '^14\\\)') { break }
    if ($line -match '^[\u2014-]\s+([A-Z][A-Z0-9_-]*)\s*$') { [void]$ctx2023New.Add($Matches[1]) }
}
if ($ctx2002.Count -lt 50) { throw "the 2002 §2002:8.10 list holds only $($ctx2002.Count) words - $ctx2002Path changed shape" }
if ($ctx2023New.Count -lt 10) { throw "Annex E.3.3 item 13 yielded only $($ctx2023New.Count) words - the annex text changed shape" }
foreach ($w in $ctx2002)    { if (-not $ctxRows.Contains($w)) { throw "2002 §2002:8.10 word '$w' is not in the 2023 table - decide its edition range (it left the standard)" } }
foreach ($w in $ctx2023New) { if (-not $ctxRows.Contains($w)) { throw "Annex E.3.3 item 13 word '$w' is not in the 2023 §2023:8.10 table" } }
$ctxIntroduced = @{}
foreach ($w in $ctxSorted) {
    $ctxIntroduced[$w] = if ($ctx2002.Contains($w)) { 2002 } elseif ($ctx2023New.Contains($w)) { 2023 } else { 2014 }
}

$ctxJson = [ordered]@{
    _comment = 'GENERATED by scripts/gen-reserved-words.ps1 - do not hand-edit. Source: ISO/IEC 1989:2023 §2023:8.10 (specs/ISO_COBOL_2023.md), the context-sensitive word table; "introduced" is the edition the word is first a keyword (2002: tests/version-matrix/context-sensitive-words-2002.json; 2023: Annex E.3.3 item 13; else 2014 - the script says why). Consumed by the >>COBOL-WORDS SR3/SR4 category validation (a 2023-only directive) and by the syntax diagnostics that name the words expected (ReservedWords.AdmitsAsKeyword). ContextSensitiveWordsDriftTests asserts this file, the C# table and the spec section all agree. CONTENT-FILTER RULE: never print this file into a conversation.'
    words = @($ctxSorted | ForEach-Object { [pscustomobject]@{ word = $_; context = $ctxRows[$_]; introduced = $ctxIntroduced[$_] } })
}
Set-LfContent -LiteralPath $ctxJsonOut -Value ($ctxJson | ConvertTo-Json -Depth 4)

$cb = [System.Text.StringBuilder]::new()
[void]$cb.AppendLine('// <auto-generated>')
[void]$cb.AppendLine('// Generated by scripts/gen-reserved-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$cb.AppendLine('// Source: ISO/IEC 1989:2023 §2023:8.10 Context-sensitive words (the whole table, verbatim); the third column is the edition')
[void]$cb.AppendLine('// the word is first a keyword (context-sensitive-words-2002.json, Annex E.3.3 item 13; see the generator, step 7c).')
[void]$cb.AppendLine('// ContextSensitiveWordsDriftTests asserts this table equals tests/version-matrix/context-sensitive-words.json')
[void]$cb.AppendLine('// AND the spec section it came from.')
[void]$cb.AppendLine('// </auto-generated>')
[void]$cb.AppendLine('namespace WiseOwl.Cobol.Editions;')
[void]$cb.AppendLine('')
[void]$cb.AppendLine('public static partial class ContextSensitiveWords')
[void]$cb.AppendLine('{')
[void]$cb.AppendLine('    internal static readonly ContextSensitiveWordEntry[] Entries =')
[void]$cb.AppendLine('    [')
foreach ($w in $ctxSorted) {
    $c = $ctxRows[$w].Replace('\', '\\').Replace('"', '\"')
    [void]$cb.AppendLine("        new(""$w"", ""$c"", $($ctxIntroduced[$w])),")
}
[void]$cb.AppendLine('    ];')
[void]$cb.AppendLine('}')
Set-LfContent -LiteralPath $ctxCsOut -Value $cb.ToString()

# ---- 8. Report COUNTS ONLY (content-filter rule) ----
$stats = [ordered]@{
    total = @($rows).Count; iso2023 = $iso2023.Count; gc85 = $gc85.Count; gc2002 = $gc2002.Count; gc2014 = $gc2014.Count
    at85 = @($rows | Where-Object r85).Count; at2002 = @($rows | Where-Object r2002).Count
    at2014 = @($rows | Where-Object r2014).Count; at2023 = @($rows | Where-Object r2023).Count
    reserved85Not2023 = @($rows | Where-Object { $_.r85 -and -not $_.r2023 }).Count
    high = @($rows | Where-Object { $_.confidence -eq 'high' }).Count
    mediumIsoOnly = $mediumBuckets.isoOnly; mediumNonMonotone = $mediumBuckets.nonMonotone
    iso2002 = $iso2002.Count; reservedNowhere = $reservedNowhere
    contextSensitive = $ctxRows.Count
}
Write-Output ("reserved-word tables generated: " + (($stats.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '))
