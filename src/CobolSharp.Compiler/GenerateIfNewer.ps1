<#
.SYNOPSIS
  Regenerates ANTLR C# parser files if the grammar has changed.
.DESCRIPTION
  Checks if CobolLexer.g4 or CobolParserCore.g4 is newer than generated files and regenerates if needed.
#>

$LexerGrammarFile = Join-Path (Join-Path $PSScriptRoot 'Grammar') 'CobolLexer.g4'
$ParserGrammarFile = Join-Path (Join-Path $PSScriptRoot 'Grammar') 'CobolParserCore.g4'
$GeneratedDir = Join-Path $PSScriptRoot 'Generated'
$LexerFile = Join-Path $GeneratedDir 'CobolLexer.cs'
$ParserFile = Join-Path $GeneratedDir 'CobolParserCore.cs'

# Check if regeneration is needed
$needsRegeneration = $false

if (-not (Test-Path $LexerFile) -or -not (Test-Path $ParserFile)) {
    Write-Host "Parser files not found. Generating..." -ForegroundColor Yellow
    $needsRegeneration = $true
}
else {
    $lexerGrammarTime = (Get-Item $LexerGrammarFile).LastWriteTime
    $parserGrammarTime = (Get-Item $ParserGrammarFile).LastWriteTime
    $lexerTime = (Get-Item $LexerFile).LastWriteTime
    $parserTime = (Get-Item $ParserFile).LastWriteTime

    if ($lexerGrammarTime -gt $lexerTime -or $parserGrammarTime -gt $parserTime) {
        Write-Host "Grammar files are newer than generated files. Regenerating..." -ForegroundColor Yellow
        $needsRegeneration = $true
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
