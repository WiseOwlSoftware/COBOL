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
    [string[]]$Fragments = @('CobolControlFlow'),
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
foreach ($frag in $Fragments) {
    $g4Path = Join-Path $grmDir "$frag.g4"
    if (-not (Test-Path $g4Path)) { Write-Error "grammar fragment not found: $g4Path"; exit 1 }
    $ebnf = Convert-G4ToEbnf ([IO.File]::ReadAllText($g4Path))
    $ruleCount = ($ebnf -split "`n" | Where-Object { $_ -match '::=' }).Count
    $ebnfFile = Join-Path $tmp "$frag.ebnf"; [IO.File]::WriteAllText($ebnfFile, $ebnf)
    $mdFile   = Join-Path $tmp "$frag.md"
    & java -jar "$rrWar" -md -suppressebnf "-out:$mdFile" "$ebnfFile"
    if ($LASTEXITCODE -ne 0) { Write-Error "rr.war failed for $frag (exit $LASTEXITCODE)"; exit 1 }
    $diagrams = [IO.File]::ReadAllText($mdFile)

    $header = @"
---
title: $frag — syntax diagrams
generated: true
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
> **other** fragments render as leaf non-terminals. **$ruleCount rules.** See also [[kb/Spec/Lookup/Grammar]] ·
> [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Compiler/Phases]].

"@
    [IO.File]::WriteAllText((Join-Path $outDir "$frag.md"), $header + $diagrams)
    Write-Host ("  {0,-22} {1,3} rules" -f $frag, $ruleCount)
    $done++
}

Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
Write-Host "generated $done grammar-diagram note(s) into $outDir"
if ($Check) { Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue }
if ($done -lt 1) { Write-Error "no fragments processed"; exit 1 }
exit 0
