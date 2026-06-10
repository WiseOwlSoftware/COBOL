<#
.SYNOPSIS
  Regenerates ANTLR C# parser files if the grammar has changed (Windows AND Linux).
.DESCRIPTION
  Compares every .g4 under Grammar/ against the generated lexer/parser and regenerates when anything is newer
  or the generated files are absent. Generated/ is a BUILD OUTPUT (.gitignored, never checked in) — a fresh
  checkout ALWAYS regenerates, so java (+ pwsh, which is running this) are build prerequisites on every
  platform. A failed generation FAILS this script (and therefore the build) — the silent-fallback that broke CI
  (DEVLOG 554) is not possible anymore.
#>

$GrammarDir = Join-Path $PSScriptRoot 'Grammar'
$GeneratedDir = Join-Path $PSScriptRoot 'Generated'
$LexerFile = Join-Path $GeneratedDir 'CobolLexer.cs'
$ParserFile = Join-Path $GeneratedDir 'CobolParserCore.cs'

# Collect all grammar files: top-level .g4 files and all imported .g4 files in subdirectories
$allGrammarFiles = Get-ChildItem -Path $GrammarDir -Filter '*.g4' -Recurse

# Check if regeneration is needed
$needsRegeneration = $false

if (-not (Test-Path $LexerFile) -or -not (Test-Path $ParserFile)) {
    Write-Host "Parser files not found. Generating..." -ForegroundColor Yellow
    $needsRegeneration = $true
}
else {
    $lexerTime = (Get-Item $LexerFile).LastWriteTime
    $parserTime = (Get-Item $ParserFile).LastWriteTime
    $oldestGenerated = if ($lexerTime -lt $parserTime) { $lexerTime } else { $parserTime }

    foreach ($g4 in $allGrammarFiles) {
        if ($g4.LastWriteTime -gt $oldestGenerated) {
            Write-Host "Grammar file $($g4.Name) is newer than generated files. Regenerating..." -ForegroundColor Yellow
            $needsRegeneration = $true
            break
        }
    }
}

if ($needsRegeneration) {
    # Import and run the ANTLR generation — and PROPAGATE failure to the caller (MSBuild Exec): a nonzero exit
    # here fails the build instead of compiling against stale or absent generated files.
    . (Join-Path $PSScriptRoot 'Invoke-Antlr4CSharp.ps1')
    $result = Invoke-Antlr4CSharp
    if ($result -ne 0) {
        Write-Error "ANTLR generation FAILED - failing the build (no stale-parser fallback)."
        exit 1
    }
    exit 0
}
else {
    Write-Host "Parser files are up to date." -ForegroundColor Green
    exit 0
}
