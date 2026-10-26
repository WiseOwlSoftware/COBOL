#Requires -Version 7.0
# SELF-TEST-PLATFORM: windows — fuzz_night.ps1 reads the Windows "GPU Engine" performance counters and Win32_Process; this test drives only its pure logic (fuzz_night_lib.ps1), but the tool it covers is Windows-hosted
# Self-test for scripts/fuzz_night.ps1 (kb/Work PB3561 item 5): the game detection, the pause/resume state machine and the
# pause actions, driven by a stubbed counter feed through scripts/fuzz_night_lib.ps1. Nothing real is touched: no GPU counter,
# no process, no Ollama; a temp directory holds the PAUSE file. Run: pwsh -NoProfile -File scripts/test_fuzz_night.ps1, or every
# self-test at once: python scripts/self_tests.py
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'fuzz_night_lib.ps1')
$checked = 0; $fails = @()
function Check([string]$name, $got, $want) {
    $script:checked++
    if ("$got" -ne "$want") { $script:fails += "${name}: got '$got', want '$want'" }
}

$tmp = Join-Path ([IO.Path]::GetTempPath()) ("fuzz-night-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
try {
    $names = @{ 100 = 'Diablo IV'; 200 = 'chrome'; 300 = 'python'; 400 = 'Overwatch'; 500 = 'ollama' }
    $nameOf = { param($processId) $names[[int]$processId] }
    $exclude = @('chrome', 'Python', 'ollama')

    # ---- Find-Game
    function Game($load, $harness = @(300), $ollama = @(500)) {
        Find-Game -Load $load -HarnessPids $harness -OllamaPids $ollama -Exclude $exclude -Threshold 30 -NameOf $nameOf
    }
    Check 'a process above the threshold is the game' (Game @{ 100 = 91.2 }) 'Diablo IV (pid 100, 3D 91 %)'
    Check 'the heaviest of two games is named' (Game @{ 100 = 40; 400 = 77 }) 'Overwatch (pid 400, 3D 77 %)'
    Check 'a load below the threshold is no game' (Game @{ 100 = 29.9 }) ''
    Check 'the harness tree is never the game' (Game @{ 100 = 10; 600 = 99 } @(600)) ''
    Check 'Ollama is never the game' (Game @{ 700 = 99 } @(300) @(700)) ''
    Check 'an excluded name is never the game, whatever its case' (Game @{ 200 = 99; 300 = 99 } @()) ''
    Check 'a process that has gone (no name) is skipped, the next heaviest is the game' (Game @{ 999 = 99; 100 = 50 }) 'Diablo IV (pid 100, 3D 50 %)'
    Check 'an empty poll has no game' (Game @{}) ''

    # ---- the state machine, through Invoke-WatchPoll with recorded actions
    $pause = Get-PausePath $tmp
    $log = [System.Collections.Generic.List[string]]::new()
    $act = @{
        Pause  = { param($g) $log.Add("pause:$g") }
        Beat   = { $log.Add('beat') }
        Resume = { $log.Add('resume') }
    }
    function Feed([string[]]$polls, [int]$samples = 3, [int]$quiet = 120, [int]$poll = 10) {
        # one poll per element: 'g' a game at 80 %, '-' no load; returns the actions, one letter-name per poll
        $state = New-WatchState; $now = [datetime]'2026-10-26T21:00:00'; $out = @()
        foreach ($p in $polls) {
            $load = if ($p -eq 'g') { @{ 100 = 80 } } else { @{} }
            $a = Invoke-WatchPoll -State $state -Load $load -HarnessPids @(300) -OllamaPids @(500) -Exclude $exclude -Threshold 30 `
                -NameOf $nameOf -Now $now -GameSamples $samples -QuietSeconds $quiet -Act $act
            $out += if ($a) { $a } else { '.' }
            $now = $now.AddSeconds($poll)
        }
        return ($out -join ' ')
    }
    $log.Clear()
    Check 'a game seen for three polls pauses at the third, beats while it goes on, and resumes after 120 quiet seconds (12 polls)' `
        (Feed @('-', 'g', 'g', 'g', 'g', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-')) `
        '. . . pause beat beat beat beat beat beat beat beat beat beat beat beat resume . .'
    Check 'the pause names the game' ($log -join '|') 'pause:Diablo IV (pid 100, 3D 80 %)|beat|beat|beat|beat|beat|beat|beat|beat|beat|beat|beat|beat|resume'
    Check 'a blip of two polls never pauses, and the count starts again' (Feed @('g', 'g', '-', 'g', 'g', '-', '-')) '. . . . . . .'
    Check 'a game that returns during the quiet period restarts it (the pause goes on)' `
        (Feed @('g', 'g', 'g', '-', '-', 'g', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-') 3 50) `
        '. . pause beat beat beat beat beat beat beat resume . . . . . . .'
    Check 'no game ever: nothing happens' (Feed @('-', '-', '-', '-')) '. . . .'
    Check 'a second game after the resume pauses again' `
        (Feed @('g', 'g', 'g', '-', '-', 'g', 'g', 'g') 3 20) '. . pause beat resume . . pause'

    # ---- the pause actions
    $order = [System.Collections.Generic.List[string]]::new()
    $said = [System.Collections.Generic.List[string]]::new()
    $endRunner = { $order.Add("end-runner (PAUSE exists: $(Test-Path -LiteralPath $pause))"); return 2 }
    $sleep = { param($s) $order.Add("grace $s") }
    $say = { param($m) $said.Add($m) }
    Start-FuzzPause -PausePath $pause -Game 'Diablo IV (pid 100, 3D 91 %)' -GraceSeconds 5 -EndRunner $endRunner -Sleep $sleep -Log $say
    Check 'the pause writes PAUSE first, waits the grace, and only then ends the runner' ($order -join '; ') 'grace 5; end-runner (PAUSE exists: True)'
    Check 'PAUSE says what the game was' ((Get-Content -LiteralPath $pause -Raw) -match 'Diablo IV') $true
    Check 'the pause is logged' (($said -join ' ') -match 'PAUSE written' -and ($said -join ' ') -match '2 process') $true
    Check 'the pause does not use `ollama stop` (it waits for the generations in flight) or suspend the harness' `
        (((Get-Content (Join-Path $PSScriptRoot 'fuzz_night.ps1') -Raw) + (Get-Content (Join-Path $PSScriptRoot 'fuzz_night_lib.ps1') -Raw)) -match '(?m)^\s*[^#\r\n]*(&\s*ollama\s+stop|NtSuspendProcess)') $false

    (Get-Item -LiteralPath $pause).LastWriteTime = [datetime]'2000-01-01'
    Send-PauseBeat -PausePath $pause
    Check 'a beat rewrites PAUSE (its modification time is the lease the harness reads)' ((Get-Item -LiteralPath $pause).LastWriteTime -gt [datetime]'2020-01-01') $true
    Send-PauseBeat -PausePath (Join-Path $tmp 'nowhere/PAUSE')
    Check 'a beat with no PAUSE is not an error' $true $true
    $said.Clear()
    Stop-FuzzPause -PausePath $pause -QuietSeconds 120 -Log $say
    Check 'the resume removes PAUSE and says so' "$(Test-Path -LiteralPath $pause) $($said -join ' ' -match 'PAUSE removed')" 'False True'
    Stop-FuzzPause -PausePath $pause -QuietSeconds 120 -Log $say
    Check 'a resume with no PAUSE is not an error' $true $true

    # ---- the wrapper and the harness agree on the PAUSE file and on the run directory
    $harnessArgs = Get-HarnessArguments -Model 'qwen3.6:35b' -Hours 8 -Jobs 4 -Seed 7 -Cobol 'E:/judge/cobol.exe' -RunDir $tmp `
        -ModelOption @('qwen3.6:35b:repeat_penalty=1.1', 'qwen3.6:35b:repeat_last_n=64')
    $outAt = [array]::IndexOf($harnessArgs, '--out')
    Check 'the harness is told the wrapper''s run directory' $harnessArgs[$outAt + 1] $tmp
    Check 'each model option is passed as its own --model-option' (($harnessArgs | Where-Object { $_ -eq '--model-option' }).Count) 2
    Check 'the harness script is the first argument' $harnessArgs[0] 'scripts/differential_fuzz.py'
    $py = Get-Content (Join-Path $PSScriptRoot 'fuzz_ollama.py') -Raw
    Check 'fuzz_ollama.py spells the pause file the way the wrapper does' ([regex]::Match($py, '(?m)^PAUSE_FILE = "([^"]+)"').Groups[1].Value) $script:PauseFileName
    Check 'the wrapper looks for PAUSE in the run directory' (Get-PausePath 'E:/run') (Join-Path 'E:/run' $script:PauseFileName)
}
finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

if ($fails.Count -gt 0) {
    $fails | ForEach-Object { Write-Host "  SELF-TEST FAILED: $_" }
    Write-Host "test_fuzz_night: FAILED ($($fails.Count) of $checked)"
    exit 1
}
Write-Host "test_fuzz_night: PASSED ($checked checks)"
