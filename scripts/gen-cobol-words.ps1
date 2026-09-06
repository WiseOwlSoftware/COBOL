# Copyright (c) 2026 Brent Rector. All rights reserved.
# Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#
# gen-cobol-words.ps1 — single-source the CONTEXT-SENSITIVE WORD SET (rearchitecture PHASE 04, Group A;
# docs/rearchitecture/PHASE-04-frontend-consolidation-cst-facade.md §Step A2; DESIGN-frontend-grammar.md D2).
#
# The set of tokens that are keywords in context yet legal user-defined words elsewhere was maintained by HAND
# in two physically separate grammar sources that a CobolLexer.g4 comment literally instructed a maintainer to
# keep mirrored:
#   - the parser  cobolWord rule        (CobolParserCore.g4 — admits the word in a user-defined-name slot),
#   - the lexer    _dataNameTokens set   (CobolLexer.g4     — a '(' after one of these enters SUBSCRIPT mode).
# This script makes BOTH a generated artifact of the ONE declarative source tests/version-matrix/cobol-words.json,
# exactly parallel to how scripts/gen-reserved-words.ps1 single-sources the §8.9 ReservedWords table. A silent
# desync of the two sources mis-triggers (or fails to trigger) SUBSCRIPT mode — a wrong-or-missing parse error
# with no diagnostic pointing at the cause; CobolWordsDriftTests binds the two generated artifacts to this file.
#
# Inputs (in-repo):
#   tests/version-matrix/cobol-words.json    — the single declarative source (token / nameSlot / subscriptTrigger / note).
#   tests/version-matrix/reserved-words.json — the §8.9 per-edition reserved-word flags. Cross-check (RW-1/RW-2)
#                                              AND, since kb/Work PB693, the SOURCE of the reservation gate:
#                                              step 4b derives it here instead of reading a per-row flag.
#   src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj — the <AntlrNamespace> property (single source of the generated-lexer namespace).
#
# Outputs (BOTH committed; CobolWordsDriftTests asserts they agree with the JSON, both directions):
#   src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4        — the generated cobolWord + reservedGatedWord parser
#                                                              fragments (imported by CobolParserCore).
#   src/Cobol.Net.Frontend/Parsing/CobolLexerWordSet.g.cs    — the generated `partial class CobolLexer` holding _dataNameTokens.
#
# ⚠ CONTENT-FILTER RULE (DEVLOG 578/584/585 — tripped 4× on the reserved-word lists): this script prints COUNTS
#   ONLY, never a word list, into the conversation stream. Do not cat/grep these outputs into a conversation.
#
# Fail-hard discipline: $ErrorActionPreference='Stop' + explicit throws on any structural or cross-check
# inconsistency — never silently emit a partial or inconsistent artifact.

$ErrorActionPreference = 'Stop'
$repo       = Split-Path -Parent $PSScriptRoot
$jsonIn     = Join-Path $repo 'tests/version-matrix/cobol-words.json'
$reservedIn = Join-Path $repo 'tests/version-matrix/reserved-words.json'
$csprojIn   = Join-Path $repo 'src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj'
$g4Out      = Join-Path $repo 'src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4'
$csOut      = Join-Path $repo 'src/Cobol.Net.Frontend/Parsing/CobolLexerWordSet.g.cs'

# ---- 0. Single-source the generated-lexer namespace from the csproj (mitigation R-A2 — a wrong namespace must
#         be impossible, not a silent bug). The partial class MUST land in the SAME namespace as CobolLexer. ----
$csprojText = Get-Content -Raw -LiteralPath $csprojIn
if ($csprojText -notmatch '<AntlrNamespace>\s*([^<\s]+)\s*</AntlrNamespace>') {
    throw "could not read <AntlrNamespace> from $csprojIn (the generated-lexer namespace must be single-sourced)"
}
$antlrNamespace = $Matches[1]

# ---- 1. Read + structurally validate cobol-words.json ----
if (-not (Test-Path $jsonIn)) { throw "missing input: $jsonIn" }
$doc = Get-Content -Raw -LiteralPath $jsonIn | ConvertFrom-Json
$rows = @($doc.words)
if ($rows.Count -lt 1) { throw "cobol-words.json has no words" }

$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($r in $rows) {
    if ($null -eq $r.token -or $r.token -notmatch '^[A-Z][A-Z0-9_]*$') { throw "bad/absent token: '$($r.token)'" }
    if ($r.nameSlot -isnot [bool])         { throw "token $($r.token): nameSlot is not a boolean" }
    if ($r.subscriptTrigger -isnot [bool]) { throw "token $($r.token): subscriptTrigger is not a boolean" }
    if (-not $r.nameSlot -and -not $r.subscriptTrigger) { throw "token $($r.token): in NEITHER set (a word must be in >=1 set)" }
    if (-not $seen.Add($r.token)) { throw "duplicate token: $($r.token)" }
    # kb/Work PB693: the reservation gate is DERIVED below, never declared per row. A leftover flag is a second
    # copy of data reserved-words.json already owns, and a per-row flag is exactly what rotted for UNLOCK and 50
    # siblings — reject it rather than let the two sources disagree silently.
    if ($r.PSObject.Properties['reservationGated']) {
        throw "token $($r.token): 'reservationGated' is no longer a JSON field (kb/Work PB693) — the gate is DERIVED from reserved-words.json; delete the flag"
    }
}
# IDENTIFIER is the base user-defined word — it MUST be in both sets or every name/subscript breaks.
$ident = $rows | Where-Object { $_.token -eq 'IDENTIFIER' }
if (-not $ident)                      { throw "IDENTIFIER row missing (the base user-defined word)" }
if (-not $ident.nameSlot -or -not $ident.subscriptTrigger) { throw "IDENTIFIER must be nameSlot=true AND subscriptTrigger=true" }

# ---- 2. Ordinal-sort; derive the two membership lists (IDENTIFIER first, then ordinal — order is behaviorally
#         irrelevant: cobolWord is a single-token set rule and _dataNameTokens is a HashSet, so both are
#         order-independent; IDENTIFIER-first mirrors the retired hand-written sources for readable diffs). ----
function Ordered([object[]]$items) {
    $sorted = [System.Collections.Generic.List[string]]::new([string[]]($items | ForEach-Object { $_.token }))
    $sorted.Sort([System.StringComparer]::Ordinal)
    $out = [System.Collections.Generic.List[string]]::new()
    if ($sorted.Contains('IDENTIFIER')) { [void]$out.Add('IDENTIFIER') }
    foreach ($t in $sorted) { if ($t -ne 'IDENTIFIER') { [void]$out.Add($t) } }
    return ,$out
}
$nameSlotTokens = Ordered @($rows | Where-Object { $_.nameSlot })
$subTrigTokens  = Ordered @($rows | Where-Object { $_.subscriptTrigger })

# ---- 3. Reconcile the two sets AGAINST the documented asymmetries (DESIGN R1; the point of the exercise) ----
$nsSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$nameSlotTokens, [System.StringComparer]::Ordinal)
$stSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$subTrigTokens,  [System.StringComparer]::Ordinal)
$nameSlotOnly = @($nameSlotTokens | Where-Object { -not $stSet.Contains($_) })   # in cobolWord, not the trigger set
$subTrigOnly  = @($subTrigTokens  | Where-Object { -not $nsSet.Contains($_) })   # in the trigger set, not cobolWord

# Both asymmetry sides are PINNED to their documented membership (FU-1), so a one-sided flip of a currently-SHARED
# word is drift and fails here — NOT just RW-1 below (which only catches a non-reserved stray; a reserved shared
# word like COLUMN/LENGTH/SCREEN flipped to nameSlot=false stays 2023-reserved and would slip past RW-1, but lands
# in subscriptTrigger-only and fails the exact pin). A NEW asymmetry updates the constant + the FU ledger.
# nameSlot-only: exactly {BIT, AS} (safe latent under-triggers — in cobolWord, not the lexer trigger set).
# AS (P10 Step 15): the §13.10 constant entry's `AS (arith-expr)` must lex its parenthesized expression in
# NORMAL mode, so AS cannot be a subscript trigger; a table NAMED As subscripted at 85 is the ledgered
# under-trigger (the BIT precedent).
$allowedNameSlotOnly = @('BIT', 'AS')
$unexpectedNsOnly = @($nameSlotOnly | Where-Object { $allowedNameSlotOnly -notcontains $_ })
$missingNsOnly    = @($allowedNameSlotOnly | Where-Object { $nameSlotOnly -notcontains $_ })
if ($unexpectedNsOnly.Count -gt 0 -or $missingNsOnly.Count -gt 0) {
    throw "nameSlot-only must be EXACTLY {BIT, AS}: unexpected [$($unexpectedNsOnly -join ', ')] missing [$($missingNsOnly -join ', ')] — if intended, update `$allowedNameSlotOnly + the FU ledger"
}
# subscriptTrigger-only: exactly the six functionName-collision words (reserved keywords that collide with a
# function-name '(' — deliberately NOT in cobolWord). Pinned SYMMETRICALLY with the nameSlot-only pin.
$allowedSubTrigOnly = @('DISPLAY', 'MERGE', 'RANDOM', 'SIGN', 'SORT', 'SUM')
$unexpectedStOnly = @($subTrigOnly | Where-Object { $allowedSubTrigOnly -notcontains $_ })
$missingStOnly    = @($allowedSubTrigOnly | Where-Object { $subTrigOnly -notcontains $_ })
if ($unexpectedStOnly.Count -gt 0 -or $missingStOnly.Count -gt 0) {
    throw "subscriptTrigger-only must be EXACTLY the six functionName collisions: unexpected [$($unexpectedStOnly -join ', ')] missing [$($missingStOnly -join ', ')] — a shared word flipped to nameSlot=false (dropping its name-slot admission) lands here; if intended, update `$allowedSubTrigOnly + the FU ledger"
}

# ---- 4. Reserved-words cross-check (SOUND form — see the DESIGN DEVIATION note below) ----
# token -> COBOL word spelling: ANTLR '_' becomes '-', a trailing '_' is a generator-clash guard (FULL_ = 'FULL').
function To-Word([string]$t) { return ($t -replace '_', '-').TrimEnd('-') }
if (-not (Test-Path $reservedIn)) { throw "missing input: $reservedIn (run scripts/gen-reserved-words.ps1)" }
$reserved = Get-Content -Raw -LiteralPath $reservedIn | ConvertFrom-Json
$rwMap = @{}
foreach ($e in $reserved.words) { $rwMap[$e.word] = $e }
#
# ⚠ DESIGN DEVIATION (PHASE-04 doc Step A2 item 4 — recorded per process rule 4 "explain why the original
#   wasn't followed"). The plan proposed: "a subscriptTrigger=true word must be a legitimate user-word at >=1
#   edition per the reserved-words flags, else fail." That predicate is UNSOUND against the actual data:
#     (a) the subscriptTrigger-only words (DISPLAY/MERGE/RANDOM/SIGN/SORT/SUM) are RESERVED keywords at every
#         edition — they are in the trigger set only because they COLLIDE with functionName ('(' opens the
#         argument list), NOT because they are user words; and
#     (b) two nameSlot words (COLUMN, LENGTH) are §8.9-reserved at all four editions yet appear in cobolWord —
#         syntactically admitted in a name slot, with the §8.9 funnel making the semantic rejection.
#   So the sound, valuable cross-checks that DO hold are:
#     RW-1 (fail-hard): every subscriptTrigger-ONLY word (nameSlot=false) is a genuine reserved keyword — it maps
#           to a reserved-words entry reserved at 2023. This catches a NON-reserved stray word wrongly kept out of
#           cobolWord. (A RESERVED word wrongly dropped from cobolWord — the COLUMN/LENGTH/SCREEN class — is caught
#           NOT here but by the exact subscriptTrigger-only pin above, since it stays 2023-reserved.)
#     RW-2 (report):    the count of nameSlot words that are reserved at all four editions (the §8.9-funnel-gated
#           name-slot admissions) — surfaced as a COUNT, not an error.
$rwViolations = @()
foreach ($t in $subTrigOnly) {
    $w = To-Word $t
    $e = $rwMap[$w]
    if ($null -eq $e -or -not $e.r2023) { $rwViolations += "$t($w)" }
}
if ($rwViolations.Count -gt 0) {
    throw "RW-1: subscriptTrigger-only word(s) not a 2023-reserved keyword [$($rwViolations -join ', ')] — a functionName-collision word must be reserved; a user-word belongs in cobolWord too"
}
$nameSlotReservedAll4 = @($nameSlotTokens | Where-Object {
    $e = $rwMap[(To-Word $_)]; $e -and $e.r85 -and $e.r2002 -and $e.r2014 -and $e.r2023
})

# ---- 4b. DERIVE THE RESERVATION GATE (kb/Work PB693 — CLAUDE.md rule 5: never a hand-maintained list where a
#          structure belongs). ISO 8.3.2.1 rule 1: "Reserved words shall not be used as user-defined words or
#          system-names." cobolWord IS the user-defined-word slot, so a nameSlot word that ISO 8.9 reserves at
#          edition E must not be admitted there at E — otherwise an operand list absorbs the word and the
#          statement it begins vanishes (UNLOCK: `MOVE "ZZ" TO FS` + a period-less `UNLOCK F1` became a
#          three-receiver MOVE, and legal 2002+ source was rejected).
#          The gate USED to be a per-row `reservationGated` flag, added by hand for five words; fifty-one
#          siblings with the same 8.9 straddle were never flagged and nothing could see it. Deriving the set
#          from the SAME reserved-words.json the funnel and reservedHere() read makes the next word automatic
#          and leaves nothing to forget. IDENTIFIER is excluded structurally (it is the base user word, never
#          a keyword token).
#          ⛔ ONE EXCLUSION, AND IT IS DERIVED TOO: the §15 INTRINSIC FUNCTION NAMES that collide with a reserved
#          word (the `functionName` rule in Grammar/Core/CobolExpressions.g4 — LENGTH, NATIONAL, BIT are the ones
#          that are also nameSlot rows). A cobolWord occurrence of one of these is the KEYWORD-OMITTED function
#          reference §15 permits (`COMPUTE N = LENGTH(A)` parses the name through cobolWord, not through a
#          FUNCTION-led rule), i.e. a use OF the reserved word rather than a user-defined-word use — the same
#          distinction VersionConformancePass.IsBareFunctionArgumentWord draws for §15 phrase words. Gating them
#          made five conforming 2023 goldens COBOL0001. They also carry NO swallow risk: a function name leads no
#          statement and no clause, so no operand list can absorb a construct through it.
$functionNameG4 = Join-Path $repo 'src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4'
if (-not (Test-Path $functionNameG4)) { throw "missing input: $functionNameG4 (the functionName rule is the gate's exclusion source)" }
$fnLines = Get-Content -LiteralPath $functionNameG4
$fnStart = [Array]::FindIndex([string[]]$fnLines, [Predicate[string]]{ param($l) $l.Trim() -eq 'functionName' })
if ($fnStart -lt 0) { throw "no 'functionName' rule in $functionNameG4 — the gate's exclusion source moved" }
$functionNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
for ($i = $fnStart + 1; $i -lt $fnLines.Count; $i++) {
    $l = $fnLines[$i].Trim()
    if ($l -eq ';') { break }
    if ($l -match '^[:|]\s*([A-Z][A-Z0-9_]*)\s*$') { [void]$functionNames.Add($Matches[1]) }
}
if ($functionNames.Count -lt 2) { throw "the functionName rule yielded $($functionNames.Count) tokens — the parse broke" }

$gatedTokenSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$lowConfidence = @()
foreach ($t in $nameSlotTokens) {
    if ($t -eq 'IDENTIFIER') { continue }
    if ($functionNames.Contains($t)) { continue }                    # a §15 function name: a keyword use, see above
    $e = $rwMap[(To-Word $t)]
    if ($null -eq $e) { continue }                                   # a §8.10 context-sensitive word: never reserved
    if (-not ($e.r85 -or $e.r2002 -or $e.r2014 -or $e.r2023)) { continue }
    # The grammar gate keys on userWordHere() = !IsReservedAt (confidence-blind), while the §8.9 funnel only
    # REPORTS high-confidence rows (ReservedWordSet.RejectsAt). Gating a lower-confidence word would reject the
    # declaration with a bare parse error and NO COBOLNET0901 to explain it — fail hard instead of shipping that.
    if ($e.confidence -ne 'high') { $lowConfidence += "$t($($e.confidence))" ; continue }
    [void]$gatedTokenSet.Add($t)
}
if ($lowConfidence.Count -gt 0) {
    throw "RW-3: nameSlot word(s) reserved at some edition but NOT high-confidence [$($lowConfidence -join ', ')] — the derived gate would reject them with no COBOLNET0901 (the funnel only reports high-confidence rows); raise the confidence or drop the nameSlot admission"
}

# ---- 5. Emit CobolWords.g4 (the generated cobolWord parser fragment) ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('// <auto-generated> by scripts/gen-cobol-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$sb.AppendLine('// Source: tests/version-matrix/cobol-words.json — cobolWord from the nameSlot=true rows, reservedGatedWord')
[void]$sb.AppendLine('// from the reservationGated=true rows. CobolWordsDriftTests asserts agreement with BOTH.')
[void]$sb.AppendLine('// The context-sensitive user-word list: tokens that are keywords in context but legal user-defined')
[void]$sb.AppendLine('// words in a name slot. Imported by CobolParserCore.g4; the retired hand-written rules lived there.')
[void]$sb.AppendLine('parser grammar CobolWords;')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('options { tokenVocab = CobolLexer; }')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('cobolWord')
$gated = $gatedTokenSet
for ($i = 0; $i -lt $nameSlotTokens.Count; $i++) {
    $sep = if ($i -eq 0) { ':' } else { '|' }
    $tok = $nameSlotTokens[$i]
    # kb/Work PB137/PB693: a reservation-gated word leaves the user-word space exactly where 8.9 reserves it,
    # so no operand list absorbs the bare keyword there while user-word use at the other editions survives.
    if ($gated.Contains($tok)) { [void]$sb.AppendLine("    $sep {userWordHere(`"$tok`")}? $tok") }
    else { [void]$sb.AppendLine("    $sep $tok") }
}
[void]$sb.AppendLine('    ;')
# ---- 5b. Emit reservedGatedWord — the SAME derived gate set, predicate INVERTED (kb/Work PB300/PB693) ----
# A gated word leaves cobolWord exactly where §8.9 reserves it (step 5). That is right for every REFERENCE
# slot, but a DECLARATION naming the word must still PARSE at those editions, or the §8.9 funnel's targeted
# COBOLNET0901 ("'X' is a reserved word in COBOL-nnnn") degrades to a raw COBOL0001 parse error that never
# names the cause. dataName and programName therefore re-admit the same words under the INVERSE predicate.
# That re-admission used to be a HAND-WRITTEN list of two words in CobolData.g4 (COMMIT/ROLLBACK) — so CRT and
# CURSOR, gated by kb/Work PB301, never got their 0901 and nobody noticed; then the GATE ITSELF was a
# hand-set flag and fifty-one §8.9-straddling words never got one (PB693). Deriving BOTH halves from the ONE
# reserved-words.json makes the next gated word automatic (CLAUDE.md rule 5); CobolWordsDriftTests pins it.
$gatedTokens = [System.Collections.Generic.List[string]]::new([string[]]$gated)
$gatedTokens.Sort([System.StringComparer]::Ordinal)
if ($gatedTokens.Count -lt 1) {
    throw "the derived gate set is empty: dataName references reservedGatedWord, and an empty rule is invalid ANTLR"
}
[void]$sb.AppendLine('')
[void]$sb.AppendLine('// The DECLARATION-position twin of the gated cobolWord alternatives (kb/Work PB300/PB137/PB693): the')
[void]$sb.AppendLine('// SAME derived rows under the INVERSE predicate, so `01 <word> PIC X.` and `PROGRAM-ID. <word>.`')
[void]$sb.AppendLine('// still PARSE where §8.9 reserves the word and the funnel answers with a targeted COBOLNET0901')
[void]$sb.AppendLine('// instead of a parse error. VersionConformancePass.VisitReservedGatedWord is the ONE funnel arm:')
[void]$sb.AppendLine('// every use of this rule is a definition slot, so a new slot needs no new C# (kb/Work PB693).')
[void]$sb.AppendLine('reservedGatedWord')
for ($i = 0; $i -lt $gatedTokens.Count; $i++) {
    $sep = if ($i -eq 0) { ':' } else { '|' }
    [void]$sb.AppendLine("    $sep {!userWordHere(`"$($gatedTokens[$i])`")}? $($gatedTokens[$i])")
}
[void]$sb.AppendLine('    ;')
Set-Content -LiteralPath $g4Out -Value $sb.ToString().TrimEnd() -Encoding utf8

# ---- 6. Emit CobolLexerWordSet.g.cs (the generated `partial class CobolLexer` holding _dataNameTokens) ----
$cs = [System.Text.StringBuilder]::new()
[void]$cs.AppendLine('// <auto-generated> by scripts/gen-cobol-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$cs.AppendLine('// Source: tests/version-matrix/cobol-words.json (subscriptTrigger=true rows) plus the DERIVED reservation-gate')
[void]$cs.AppendLine('// set of step 4b (kb/Work PB693). CobolWordsDriftTests asserts agreement with both.')
[void]$cs.AppendLine('// This committed partial (NOT under Generated/, so `dotnet clean` keeps it) extends the ANTLR-generated')
[void]$cs.AppendLine('// CobolLexer with the subscript-trigger set. A ''('' after one of these tokens enters SUBSCRIPT mode')
[void]$cs.AppendLine('// (PreviousTokenCouldBeDataName in CobolLexer.g4 @members). Token-type constants resolve unqualified.')
[void]$cs.AppendLine("namespace $antlrNamespace;")
[void]$cs.AppendLine('')
[void]$cs.AppendLine('public partial class CobolLexer')
[void]$cs.AppendLine('{')
[void]$cs.AppendLine('    private static readonly System.Collections.Generic.HashSet<int> _dataNameTokens = new()')
[void]$cs.AppendLine('    {')
foreach ($t in $subTrigTokens) { [void]$cs.AppendLine("        $t,") }
[void]$cs.AppendLine('    };')
[void]$cs.AppendLine('')
[void]$cs.AppendLine('    /// <summary>The RESERVATION-GATED token types (kb/Work PB693) — the same derived set step 4b')
[void]$cs.AppendLine('    /// puts behind {userWordHere("W")}? in cobolWord. A parse error ON one of these is the §8.9')
[void]$cs.AppendLine('    /// violation itself (the gate is why no name-slot alternative matched), so CobolErrorListener')
[void]$cs.AppendLine('    /// answers with the targeted COBOLNET0901 instead of a raw COBOL0001 that never names the cause.')
[void]$cs.AppendLine('    /// Generated, so a newly gated word needs no edit anywhere.</summary>')
[void]$cs.AppendLine('    internal static bool IsReservationGated(int tokenType) => _reservationGatedTokens.Contains(tokenType);')
[void]$cs.AppendLine('')
[void]$cs.AppendLine('    private static readonly System.Collections.Generic.HashSet<int> _reservationGatedTokens = new()')
[void]$cs.AppendLine('    {')
foreach ($t in $gatedTokens) { [void]$cs.AppendLine("        $t,") }
[void]$cs.AppendLine('    };')
[void]$cs.AppendLine('}')
Set-Content -LiteralPath $csOut -Value $cs.ToString().TrimEnd() -Encoding utf8

# ---- 7. Report COUNTS ONLY (content-filter rule) ----
$stats = [ordered]@{
    total            = $rows.Count
    nameSlot         = $nameSlotTokens.Count
    subscriptTrigger = $subTrigTokens.Count
    nameSlotOnly     = $nameSlotOnly.Count       # documented FU-1 latent asymmetry (BIT)
    subscriptTrigOnly = $subTrigOnly.Count        # functionName collisions
    nameSlotReservedAll4 = $nameSlotReservedAll4.Count   # RW-2: §8.9-funnel-gated name-slot admissions
    reservationGated = $gatedTokens.Count                # 4b: DERIVED — nameSlot words §8.9 reserves at >=1 edition
    functionNameExempt = @($nameSlotTokens | Where-Object { $functionNames.Contains($_) }).Count   # 4b exclusion
    namespace        = $antlrNamespace
}
Write-Output ("cobol-words generated: " + (($stats.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '))
