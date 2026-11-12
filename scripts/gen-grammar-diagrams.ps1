#!/usr/bin/env pwsh
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────
# gen-grammar-diagrams.ps1 — railroad (syntax) diagrams for the ANTLR grammar fragments, as Markdown notes.
#
# Pipeline: clean an ANTLR *.g4 parser fragment -> W3C-style EBNF -> bottlecaps RR (tools/rr/rr.war, stdin to stdout) -> Markdown
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
    # threshold stays high to avoid needlessly fragmenting rules in a wide viewer. A few rules (e.g. performStatement,
    # ~1397px) have a single unbreakable row that rr won't wrap below their natural width regardless. Lower this
    # (e.g. -Width 680) for a narrow reading column.
    [int]$Width = 1400,
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$repo   = Split-Path $PSScriptRoot -Parent
$rrWar  = Join-Path $repo 'tools/rr/rr.war'
$grmDir = Join-Path $repo 'src/WiseOwl.Cobol.Frontend/Grammar/Core'
# ⛔ A -Check run writes to its OWN directory. It used to be the FIXED %TEMP%\wiseowl-cobol-grammar-check, shared by
# every run on the host — and the Unit assembly's GrammarDiagramGeneratorDriftTests runs this script from EVERY
# worktree's gate, so two concurrent gates raced: one run's opening Remove-Item deleted the directory under the
# other's WriteAllText ("Could not find a part of the path '…\wiseowl-cobol-grammar-check\CobolOO.md'"), exit 1, and
# the drift test went red with a message build-local's line filter then dropped. Measured 2026-09-24 (wave 59 H):
# four concurrent -Check runs, three exited 1 that way. A per-run GUID directory makes the runs independent.
$outDir = if ($Check) { Join-Path ([System.IO.Path]::GetTempPath()) ("wiseowl-cobol-grammar-check-" + [guid]::NewGuid().ToString('N')) }
          else        { Join-Path $repo 'kb/Grammar' }

if (-not (Test-Path $rrWar)) { Write-Error "missing vendored railroad tool: $rrWar"; exit 1 }
if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Path $outDir | Out-Null

# ⛔ rr.war takes the EBNF on STDIN ('-') and writes the Markdown to STDOUT (no -out:), so no file passes between
# this script and java. It used to write <tmp>\ccf-<guid>\<frag>.md with -out: and read it straight back, and a
# freshly written file in %TEMP% is a file ANY process on the host may open (a real-time scanner, an indexer): with
# write access held by such a process, File.ReadAllText's FileShare.Read open fails with a sharing violation. The
# 10-attempt, 2.25 s backoff kb/Work PB44 put around the read only narrowed that window; on 2026-11-12 a full gate
# (witnesses-85m, two test hosts plus other gates' slots) outlasted it on CobolWords.md (kb/Work PB5447). A pipe has
# no name another process can open, so the handoff cannot be locked — the failure is excluded, not retried.
# java's own exit code and stderr are rr's verdict: a nonzero exit fails the run and prints what rr said.
function Invoke-Rr([string]$frag, [string]$ebnf) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new('java')
    foreach ($a in @('-Dstdout.encoding=UTF-8', '-jar', $rrWar, '-md', '-suppressebnf', '-noinline', "-width:$Width",
                     '-enc:UTF-8', '-')) { $psi.ArgumentList.Add($a) }
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $psi.StandardInputEncoding = $utf8; $psi.StandardOutputEncoding = $utf8; $psi.StandardErrorEncoding = $utf8
    $p = [System.Diagnostics.Process]::Start($psi)
    # Both output streams drain concurrently with the stdin write, so neither pipe's buffer can fill and stall java.
    $stdout = $p.StandardOutput.ReadToEndAsync(); $stderr = $p.StandardError.ReadToEndAsync()
    $p.StandardInput.Write($ebnf); $p.StandardInput.Close()
    $p.WaitForExit()
    if ($p.ExitCode -ne 0) { Write-Error "rr.war failed for $frag (exit $($p.ExitCode)): $($stderr.Result)"; exit 1 }
    return $stdout.Result
}

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
    # -noinline (in Invoke-Rr): keep EVERY rule as its own diagram (rr otherwise folds single-literal rules into
    # their references, so a lookup by that rule name would find nothing and the header count wouldn't match).
    $diagrams = Invoke-Rr $frag $ebnf
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
tags:
  - wiseowl-cobol
  - grammar
  - diagram
  - generated
---

# $frag — railroad syntax diagrams

> ⚙ **Generated** from ``src/WiseOwl.Cobol.Frontend/Grammar/Core/$frag.g4`` by ``scripts/gen-grammar-diagrams.ps1``
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
  - wiseowl-cobol
  - grammar
  - diagram
  - generated
  - moc
---

# Grammar syntax diagrams

> ⚙ **Generated** by ``scripts/gen-grammar-diagrams.ps1`` from the ANTLR parser fragments under
> ``src/WiseOwl.Cobol.Frontend/Grammar/Core/`` (railroad via the vendored bottlecaps ``tools/rr/rr.war``). Build
> output (gitignored) — do not edit; regenerate. **$($results.Count) fragments, $totalDia diagrams.**
> See also [[kb/Spec/Lookup/Grammar]] · [[kb/Diagrams/Grammar Hierarchy]] · [[kb/Compiler/Phases]].

| Fragment | Diagrams | Notes |
| --- | --: | --- |
$rows
"@
[IO.File]::WriteAllText((Join-Path $outDir '_Index.md'), $indexMd)

Write-Host "generated $done grammar-diagram note(s) into $outDir"
if ($Check) { Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue }
if ($done -lt 1) { Write-Error "no fragments processed"; exit 1 }
exit 0
