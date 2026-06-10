<#
.SYNOPSIS
  Invoke ANTLR4 on the COBOL grammars to generate C# lexer and parser files — portably (Windows AND Linux).
.DESCRIPTION
  - Expects:
      - antlr-4.13.2-complete.jar in ANTLR4/
      - CobolLexer.g4 in Grammar/Core/
      - CobolParserCore.g4 in Grammar/ (imports + tokenVocab resolve from Grammar/Core/)
      - java on PATH (the only external prerequisite besides pwsh itself)
  - Generates into Generated_temp/, then copies the results to Generated/ (a BUILD OUTPUT — .gitignored,
    never checked in; every build regenerates when a grammar is newer).
  - CobolParserCoreBase.cs is hand-maintained in Parsing/ and is NEVER overwritten.

  PORTABILITY (the DEVLOG-554 CI break): ANTLR mirrors a grammar's RELATIVE DIRECTORY under -o, but it detects
  "has a directory" with the PLATFORM separator — `Core/CobolLexer.g4` is a bare name on Windows (flat output)
  yet a nested path on Linux (output lands in <out>/Core/), which silently broke the tokens hand-off. So each
  grammar is generated FROM ITS OWN DIRECTORY with a BARE filename (flat output everywhere), and the parser's
  -lib inputs (imported .g4s + CobolLexer.tokens) are STAGED into a temp lib dir — no source-tree writes, no
  platform divergence.
#>

function Invoke-Antlr4CSharp {
    [CmdletBinding()]
    param(
        [string]$JarPath = (Join-Path $PSScriptRoot 'ANTLR4' 'antlr-4.13.2-complete.jar'),
        [string]$OutputDir = (Join-Path $PSScriptRoot 'Generated'),
        [string]$PackageName = 'CobolSharp.Compiler.Generated'
    )

    $grammarDir = Join-Path $PSScriptRoot 'Grammar'
    $coreDir = Join-Path $grammarDir 'Core'
    $tempDir = Join-Path $PSScriptRoot 'Generated_temp'
    $libDir = Join-Path $tempDir 'lib'

    # Files we NEVER overwrite (hand-maintained in Parsing/)
    $protectedFiles = @(
        'CobolParserCoreBase.cs'
    )

    # Validate prerequisites — fail LOUD with an actionable message (this script's exit code fails the build).
    if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
        Write-Error "java not found on PATH - ANTLR generation requires a JRE (the jar is vendored in ANTLR4/)."
        return 1
    }
    if (-not (Test-Path $JarPath)) {
        Write-Error "ANTLR JAR not found at: $JarPath"
        return 1
    }
    $parserGPath = Join-Path $grammarDir 'CobolParserCore.g4'
    if (-not (Test-Path $parserGPath)) {
        Write-Error "Grammar file not found at: $parserGPath"
        return 1
    }
    $lexerGPath = Join-Path $coreDir 'CobolLexer.g4'
    if (-not (Test-Path $lexerGPath)) {
        Write-Error "Lexer grammar not found at: $lexerGPath"
        return 1
    }

    # Clean and create the staging directories.
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $libDir -Force | Out-Null
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    # Run one ANTLR generation from $workDir with a BARE grammar filename (flat output on every platform).
    function Invoke-AntlrStep([string]$workDir, [string]$grammarName, [string[]]$extraArgs) {
        Push-Location $workDir
        try {
            Write-Host "Generating C# from: $grammarName" -ForegroundColor Cyan
            $antlrOutput = & java -jar $JarPath `
                -Dlanguage=CSharp `
                -no-listener -visitor `
                -package $PackageName `
                -o $tempDir `
                @extraArgs `
                $grammarName 2>&1
            $exitCode = $LASTEXITCODE
            $hadDiag = $false
            foreach ($line in $antlrOutput) {
                $text = $line.ToString().Trim()
                if ($text -match '^(warning|error)\(') {
                    $hadDiag = $true
                    Write-Error $text
                } elseif ($text) {
                    Write-Host $text
                }
            }
            if ($exitCode -ne 0 -or $hadDiag) {
                Write-Error "Generation failed for $grammarName (exit code $exitCode)."
                return $false
            }
            Write-Host "$grammarName generation succeeded." -ForegroundColor Green
            return $true
        }
        finally {
            Pop-Location
        }
    }

    # --- Step 1: lexer, from Grammar/Core (bare name => flat output: Generated_temp/CobolLexer.*) ---
    if (-not (Invoke-AntlrStep $coreDir 'CobolLexer.g4' @())) { return 1 }

    # --- Step 2: stage the parser's -lib inputs: imported sub-grammars + the lexer tokens ---
    Copy-Item -Path (Join-Path $coreDir '*.g4') -Destination $libDir -Force
    $tokensFile = Join-Path $tempDir 'CobolLexer.tokens'
    if (-not (Test-Path $tokensFile)) {
        Write-Error "CobolLexer.tokens not produced at: $tokensFile (flat-output assumption violated)."
        return 1
    }
    Copy-Item -Path $tokensFile -Destination $libDir -Force

    # --- Step 3: parser, from Grammar (bare name), imports + tokenVocab from the staged lib dir ---
    if (-not (Invoke-AntlrStep $grammarDir 'CobolParserCore.g4' @('-lib', $libDir))) { return 1 }

    # --- Step 4: copy results to Generated/, skipping protected files and the staging lib dir ---
    $copiedCount = 0
    $skippedCount = 0
    foreach ($file in (Get-ChildItem -Path $tempDir -File)) {
        if ($protectedFiles -contains $file.Name) {
            Write-Host "  SKIPPED (hand-maintained): $($file.Name)" -ForegroundColor Yellow
            $skippedCount++
        } else {
            Copy-Item -Path $file.FullName -Destination $OutputDir -Force
            $copiedCount++
        }
    }

    Write-Host "Copied $copiedCount files, skipped $skippedCount protected files." -ForegroundColor Green
    Write-Host "All ANTLR generation completed successfully." -ForegroundColor Green

    Remove-Item -Path $tempDir -Recurse -Force
    return 0
}

# If script is called directly, invoke the function and PROPAGATE the exit code (a silent 0 here was half the
# DEVLOG-554 CI break: generation failed, the build continued on a stale checked-in parser).
if ($MyInvocation.MyCommand.Path -eq $PSCommandPath) {
    $result = Invoke-Antlr4CSharp
    exit $result
}
