<#
.SYNOPSIS
  Regenerates ANTLR C# parser files if the grammar has changed.
.DESCRIPTION
  Checks if CobolLexer.g4 or CobolParserCore.g4 is newer than generated files and regenerates if needed.
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
    # Import and run the ANTLR generation script
    . (Join-Path $PSScriptRoot 'Invoke-Antlr4CSharp.ps1')
    Invoke-Antlr4CSharp
}
else {
    Write-Host "Parser files are up to date." -ForegroundColor Green
}
