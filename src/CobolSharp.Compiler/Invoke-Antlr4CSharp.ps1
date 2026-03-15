<#
.SYNOPSIS
  Invoke ANTLR4 on the COBOL grammars to generate C# lexer and parser files.
.DESCRIPTION
  - Expects:
      - antlr-4.13.2-complete.jar in ANTLR4/
      - CobolLexer.g4 and CobolParserCore.g4 in Grammar/ subdirectory
  - Outputs into Generated/
  - Generates both lexer and parser from split grammars
#>

function Invoke-Antlr4CSharp {
    [CmdletBinding()]
    param(
        [string]$JarPath = (Join-Path $PSScriptRoot 'ANTLR4\antlr-4.13.2-complete.jar'),
        [string]$OutputDir = (Join-Path $PSScriptRoot 'Generated'),
        [string]$PackageName = 'CobolSharp.Compiler.Generated'
    )

    $grammarDir = Join-Path $PSScriptRoot 'Grammar'

    # Grammar files to process
    $grammars = @(
        'CobolLexer.g4',
        'CobolParserCore.g4'
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
                -o ../Generated `
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

        Write-Host "All ANTLR generation completed successfully." -ForegroundColor Green
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
