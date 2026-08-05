#!/usr/bin/env pwsh
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────
# gen-grammar-diagrams.ps1 — railroad (syntax) diagrams for the ANTLR grammar fragments, for Obsidian.
#
# Pipeline: clean an ANTLR *.g4 parser fragment -> W3C-style EBNF -> bottlecaps RR (tools/rr/rr.war) -> Markdown
# with one inline data-URI SVG railroad diagram per rule -> kb/Grammar/<Fragment>.md (a GITIGNORED build output).
#
# The cleaning drops what railroad tools choke on: block/line comments, the `grammar`/`options` header, semantic
# predicates `{ … }?` and actions `{ … }`; it then turns ALL-CAPS tokens into quoted terminals and rewrites
# `rule : body ;` to `rule ::= body`. Rule references to OTHER fragments render as leaf non-terminals (fine for a
# per-fragment view). Requires java + tools/rr/rr.war (vendored).
#
#   pwsh scripts/gen-grammar-diagrams.ps1                 # default: CobolControlFlow
#   pwsh scripts/gen-grammar-diagrams.ps1 -Fragments CobolControlFlow,CobolIO
#   pwsh scripts/gen-grammar-diagrams.ps1 -Check          # verify-only: build to temp, exit code, don't write kb/
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────
[CmdletBinding()]
param(
    # Default: all nine parser fragments imported by CobolParserCore.g4 (the CobolLexer fragment is tokens, not
    # syntax; CobolParserCore itself is the root). Override to regenerate a subset, e.g. -Fragments CobolData.
    [string[]]$Fragments = @(
        'CobolWords', 'CobolExpressions', 'CobolData', 'CobolSpecialNames',
        'CobolControlFlow', 'CobolIO', 'CobolReportWriter', 'CobolScreen', 'CobolOO'),
    # Wrap threshold (px) handed to rr's -width: rules wider than this break into multiple stacked rows. The
    # generated notes carry `cssclasses: [wide-diagram]`, which the vault snippet .obsidian/snippets/grammar-
    # diagrams.css widens to a ~1600px column — so on that surface diagrams render at native font (no downscale)
    # and we keep the threshold high to avoid needlessly fragmenting rules. A few rules (e.g. performStatement,
    # ~1397px) have a single unbreakable row that rr won't wrap below their natural width regardless; the wide
    # column is what makes those readable. Lower this (e.g. -Width 680) if viewing without the snippet.
    [int]$Width = 1400,
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$repo   = Split-Path $PSScriptRoot -Parent
$rrWar  = Join-Path $repo 'tools/rr/rr.war'
$grmDir = Join-Path $repo 'src/Cobol.Net.Frontend/Grammar/Core'
$outDir = if ($Check) { Join-Path ([System.IO.Path]::GetTempPath()) 'cobolnet-grammar-check' }
          else        { Join-Path $repo 'kb/Grammar' }

if (-not (Test-Path $rrWar)) { Write-Error "missing vendored railroad tool: $rrWar"; exit 1 }
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Path $outDir | Out-Null
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("ccf-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null

function Convert-G4ToEbnf([string]$g4) {
    $g = $g4
    $g = [regex]::Replace($g, '/\*.*?\*/', '', 'Singleline')          # block comments
    $g = [regex]::Replace($g, '//[^\r\n]*', '')                       # line comments
    $g = [regex]::Replace($g, '(?:parser|lexer)?\s*grammar[^;]*;', '')# grammar header
    $g = [regex]::Replace($g, 'options\s*\{[^}]*\}', '')              # options block
    $g = [regex]::Replace($g, '\{[^{}]*\}\??', '')                    # {…}? predicates + {…} actions
    $sb = New-Object System.Text.StringBuilder
    foreach ($m in [regex]::Matches($g, '([A-Za-z_]\w*)\s*:(.*?);', 'Singleline')) {
        $name = $m.Groups[1].Value
        $body = [regex]::Replace($m.Groups[2].Value, '\b[A-Z][A-Z0-9_]*\b', { param($t) '"' + $t.Value + '"' })
        $body = ([regex]::Replace($body, '\s+', ' ')).Trim()
        [void]$sb.AppendLine("$name ::= $body")
    }
    return $sb.ToString()
}

$done = 0
$results = @()
foreach ($frag in $Fragments) {
    $g4Path = Join-Path $grmDir "$frag.g4"
    if (-not (Test-Path $g4Path)) { Write-Error "grammar fragment not found: $g4Path"; exit 1 }
    $ebnf = Convert-G4ToEbnf ([IO.File]::ReadAllText($g4Path))
    $grammarRules = ($ebnf -split "`n" | Where-Object { $_ -match '::=' }).Count
    $ebnfFile = Join-Path $tmp "$frag.ebnf"; [IO.File]::WriteAllText($ebnfFile, $ebnf)
    $mdFile   = Join-Path $tmp "$frag.md"
    # -noinline: keep EVERY rule as its own diagram (rr otherwise folds single-literal rules into their
    # references, so a lookup by that rule name would find nothing and the header count wouldn't match).
    & java -jar "$rrWar" -md -suppressebnf -noinline "-width:$Width" "-out:$mdFile" "$ebnfFile"
    if ($LASTEXITCODE -ne 0) { Write-Error "rr.war failed for $frag (exit $LASTEXITCODE)"; exit 1 }
    # ⛔ READ WITH RETRY — the java process has exited but its handle on $mdFile is not always released by the
    # time PowerShell resumes (and a Windows AV scanner can hold it briefly besides). Under the comprehensive
    # battery's parallel load that window widened enough to FALSE-RED the whole run:
    #   Exception calling "ReadAllText": The process cannot access the file '…\CobolScreen.md'
    #   because it is being used by another process.
    # It passed on a serial re-run, which is exactly the shape §0 warns about — and a gate that can false-red
    # trains people to ignore reds, so this is hardened rather than documented.
    $diagrams = $null
    foreach ($__try in 1..10) {
        try { $diagrams = [IO.File]::ReadAllText($mdFile); break }
        catch [System.IO.IOException] { if ($__try -eq 10) { throw }; Start-Sleep -Milliseconds (50 * $__try) }
    }
    # Header states the number of diagrams ACTUALLY emitted (an image whose alt is a bare identifier — excludes
    # rr's "rr-2.6" / "Railroad-Diagram-Generator" footer marks), so it is always self-consistent with the note.
    # rr's recursion elimination can fold a directly-recursive rule into its reference (e.g. booleanFactor), so a
    # rare fragment shows one fewer diagram than grammar rules; note that when it happens.
    $diagramCount = ([regex]::Matches($diagrams, '!\[[A-Za-z_]\w*\]\(data:image/svg')).Count
    $countText = if ($diagramCount -eq $grammarRules) { "$diagramCount rules." }
                 else { "$diagramCount diagrams ($grammarRules rules; $($grammarRules - $diagramCount) folded into a recursive reference)." }

    $header = @"
---
title: $frag — syntax diagrams
generated: true
cssclasses:
  - wide-diagram
tags:
  - cobolsharp
  - grammar
  - diagram
  - generated
---

# $frag — railroad syntax diagrams

> ⚙ **Generated** from ``src/Cobol.Net.Frontend/Grammar/Core/$frag.g4`` by ``scripts/gen-grammar-diagrams.ps1``
> (railroad via the vendored bottlecaps ``tools/rr/rr.war``). Build output (gitignored) — do not edit; regenerate.
> Semantic predicates/actions are stripped; ALL-CAPS lexer tokens are terminals; camelCase rule references to
> **other** fragments render as leaf non-terminals. **$countText** See also [[kb/Spec/Lookup/Grammar]] ·
> [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Compiler/Phases]].

"@
    [IO.File]::WriteAllText((Join-Path $outDir "$frag.md"), $header + $diagrams)
    Write-Host ("  {0,-22} {1,3} diagrams" -f $frag, $diagramCount)
    $results += [pscustomobject]@{ Frag = $frag; Diagrams = $diagramCount; Rules = $grammarRules }
    $done++
}

# Folder index — one row per fragment, newest counts. Generated (part of the build output).
$rows = ($results | Sort-Object Frag | ForEach-Object {
    $note = "—"
    if ($_.Diagrams -ne $_.Rules) { $note = "$($_.Rules) grammar rules; $($_.Rules - $_.Diagrams) folded into a recursive reference" }
    "| [[kb/Grammar/$($_.Frag)]] | $($_.Diagrams) | $note |"
}) -join "`n"
$totalDia = ($results | Measure-Object Diagrams -Sum).Sum
$indexMd = @"
---
title: Grammar syntax diagrams — index
generated: true
tags:
  - cobolsharp
  - grammar
  - diagram
  - generated
  - moc
---

# Grammar syntax diagrams

> ⚙ **Generated** by ``scripts/gen-grammar-diagrams.ps1`` from the ANTLR parser fragments under
> ``src/Cobol.Net.Frontend/Grammar/Core/`` (railroad via the vendored bottlecaps ``tools/rr/rr.war``). Build
> output (gitignored) — do not edit; regenerate. **$($results.Count) fragments, $totalDia diagrams.**
> See also [[kb/Spec/Lookup/Grammar]] · [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Compiler/Phases]].

| Fragment | Diagrams | Notes |
| --- | --: | --- |
$rows
"@
[IO.File]::WriteAllText((Join-Path $outDir '_Index.md'), $indexMd)

Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
Write-Host "generated $done grammar-diagram note(s) into $outDir"
if ($Check) { Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue }
if ($done -lt 1) { Write-Error "no fragments processed"; exit 1 }
exit 0
