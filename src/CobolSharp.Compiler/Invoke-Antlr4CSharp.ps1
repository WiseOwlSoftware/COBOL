<#
.SYNOPSIS
  Invoke ANTLR4 on the COBOL grammars to generate C# lexer and parser files.
.DESCRIPTION
  - Expects:
      - antlr-4.13.2-complete.jar in ANTLR4/
      - CobolLexer.g4 and CobolParserCore.g4 in Grammar/ subdirectory
  - Generates into a temp folder, then copies only the files we need to Generated/
  - CobolParserCoreBase.cs is hand-maintained in Parsing/ and is NEVER overwritten
#>

function Invoke-Antlr4CSharp {
    [CmdletBinding()]
    param(
        [string]$JarPath = (Join-Path $PSScriptRoot 'ANTLR4\antlr-4.13.2-complete.jar'),
        [string]$OutputDir = (Join-Path $PSScriptRoot 'Generated'),
        [string]$PackageName = 'CobolSharp.Compiler.Generated'
    )

    $grammarDir = Join-Path $PSScriptRoot 'Grammar'
    $tempDir = Join-Path $PSScriptRoot 'Generated_temp'

    # Grammar files to process
    $grammars = @(
        'CobolLexer.g4',
        'CobolParserCore.g4'
    )

    # Files we NEVER overwrite (hand-maintained in Parsing/)
    $protectedFiles = @(
        'CobolParserCoreBase.cs'
    )

    # Validate paths
    if (-not (Test-Path $JarPath)) {
        Write-Error "ANTLR JAR not found at: $JarPath"
        return 1
    }
    foreach ($g in $grammars) {
        $gPath = Join-Path $grammarDir $g
        if (-not (Test-Path $gPath)) {
            Write-Error "Grammar file not found at: $gPath"
            return 1
        }
    }

    # Clean and create temp directory
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    # Ensure output directory exists
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    # Change to Grammar directory to avoid nested subdirectories
    Push-Location $grammarDir

    try {
        foreach ($g in $grammars) {
            Write-Host "Generating C# from: $g" -ForegroundColor Cyan

            $antlrOutput = & java -jar $JarPath `
                -Dlanguage=CSharp `
                -no-listener -visitor `
                -package $PackageName `
                -o ../Generated_temp `
                $g 2>&1
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
                Write-Error "Generation failed for $g (exit code $exitCode)."
                return 1
            }

            Write-Host "$g generation succeeded." -ForegroundColor Green
        }

        # Copy generated files to output, skipping protected files
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

        # Warn about unexpected new files from ANTLR
        foreach ($file in (Get-ChildItem -Path $tempDir -File)) {
            if ($protectedFiles -contains $file.Name) { continue }
            $existing = Join-Path $OutputDir $file.Name
            # This is just informational — all non-protected files are copied
        }

        Write-Host "Copied $copiedCount files, skipped $skippedCount protected files." -ForegroundColor Green
        Write-Host "All ANTLR generation completed successfully." -ForegroundColor Green

        # Clean up temp directory
        Remove-Item -Path $tempDir -Recurse -Force

        return 0
    }
    finally {
        Pop-Location
    }
}

# If script is called directly, invoke the function
if ($MyInvocation.MyCommand.Path -eq $PSCommandPath) {
    $result = Invoke-Antlr4CSharp
    exit $result
}
