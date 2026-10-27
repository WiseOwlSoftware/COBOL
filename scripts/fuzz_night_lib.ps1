#Requires -Version 7.0
# The logic of scripts/fuzz_night.ps1 that needs no GPU, no process table and no console: finding the game in a poll's GPU
# load, the pause/resume state machine, and the three actions a pause takes (create PAUSE and end Ollama's runner; rewrite
# PAUSE; remove PAUSE). Dot-sourced by fuzz_night.ps1 and by its self-test (test_fuzz_night.ps1), which drives every function
# here with a stubbed counter feed (kb/Work PB3561 item 5). Every side effect is a parameter, so the self-test touches
# nothing real.

# The name of the pause file in the run directory, as fuzz_ollama.py's PAUSE_FILE spells it (the self-test holds the two
# to one spelling).
$script:PauseFileName = 'PAUSE'

function Get-PausePath([string]$RunDir) { Join-Path $RunDir $script:PauseFileName }

function Get-HarnessArguments {
    # The harness's command line. --out is the wrapper's run directory, so the PAUSE file the wrapper makes is the one the
    # harness watches (the harness prints the same path at its start).
    param([string]$Model, [double]$Hours, [int]$Jobs, [int]$Seed, [string]$Cobol, [string]$RunDir, [string[]]$ModelOption = @())
    $a = @('scripts/differential_fuzz.py', '--model', $Model, '--hours', $Hours, '--jobs', $Jobs, '--seed', $Seed,
           '--cobol', $Cobol, '--out', $RunDir)
    foreach ($o in $ModelOption) { $a += @('--model-option', $o) }
    return $a
}

function Find-Game {
    # The heaviest process that is a GAME: a process whose 3D-engine load in this poll is at least $Threshold percent and is
    # neither in the harness's tree, nor Ollama's, nor named in $Exclude. $Load is @{ pid = percent }. $NameOf maps a pid to
    # its process name, or $null for one that has gone. Returns "name (pid N, 3D P %)", or $null.
    param([hashtable]$Load, [int[]]$HarnessPids, [int[]]$OllamaPids, [string[]]$Exclude, [double]$Threshold, [scriptblock]$NameOf)
    $excluded = $Exclude | ForEach-Object { $_.ToLowerInvariant() }
    foreach ($kv in ($Load.GetEnumerator() | Sort-Object Value -Descending)) {
        if ($kv.Value -lt $Threshold) { continue }
        if ($HarnessPids -contains $kv.Key) { continue }
        if ($OllamaPids -contains $kv.Key) { continue }
        $name = & $NameOf $kv.Key
        if (-not $name) { continue }
        if ($excluded -contains $name.ToLowerInvariant()) { continue }
        return "$name (pid $($kv.Key), 3D $([math]::Round($kv.Value)) %)"
    }
    return $null
}

function New-WatchState { return @{ Paused = $false; Hot = 0; LastGame = [datetime]::MinValue } }

function Step-Watch {
    # One poll of the state machine. $Game is Find-Game's answer for the poll. Returns the action the poll calls for:
    #   'pause'   the game has been seen for $GameSamples polls in a row: create PAUSE and free the GPU
    #   'beat'    the pause goes on: rewrite PAUSE (the harness treats a PAUSE not rewritten for minutes as abandoned)
    #   'resume'  no game for $QuietSeconds: remove PAUSE
    #   $null     nothing to do
    param([hashtable]$State, [string]$Game, [datetime]$Now, [int]$GameSamples, [int]$QuietSeconds)
    if ($Game) {
        $State.Hot++
        $State.LastGame = $Now
        if ($State.Paused) { return 'beat' }
        if ($State.Hot -ge $GameSamples) { $State.Paused = $true; return 'pause' }
        return $null
    }
    $State.Hot = 0
    if (-not $State.Paused) { return $null }
    if (($Now - $State.LastGame).TotalSeconds -ge $QuietSeconds) { $State.Paused = $false; return 'resume' }
    return 'beat'
}

function Invoke-WatchPoll {
    # Find-Game, Step-Watch, and the action the step calls for, run through $Act (@{ Pause; Beat; Resume } script blocks;
    # Pause gets the game's description). Returns the action.
    param([hashtable]$State, [hashtable]$Load, [int[]]$HarnessPids, [int[]]$OllamaPids, [string[]]$Exclude, [double]$Threshold,
          [scriptblock]$NameOf, [datetime]$Now, [int]$GameSamples, [int]$QuietSeconds, [hashtable]$Act)
    $game = Find-Game -Load $Load -HarnessPids $HarnessPids -OllamaPids $OllamaPids -Exclude $Exclude -Threshold $Threshold -NameOf $NameOf
    $action = Step-Watch -State $State -Game $game -Now $Now -GameSamples $GameSamples -QuietSeconds $QuietSeconds
    switch ($action) {
        'pause'  { & $Act.Pause $game }
        'beat'   { & $Act.Beat }
        'resume' { & $Act.Resume }
    }
    return $action
}

function Start-FuzzPause {
    # The pause, in the order that loses nothing: PAUSE first (the harness's workers stop starting generations), a short
    # grace for a worker that was between its check and its request, and then the runner is ended at once (the model's
    # VRAM is free for the game). The generations in flight fail, see PAUSE, and are asked again after it (fuzz_ollama.py).
    # `ollama stop` is not used: it waits for the generations in flight, which kept the GPU pegged for minutes on 2026-10-22.
    param([string]$PausePath, [string]$Game, [int]$GraceSeconds, [scriptblock]$EndRunner, [scriptblock]$Sleep, [scriptblock]$Say)
    Set-Content -Path $PausePath -Value "paused for $Game" -Encoding utf8NoBOM
    & $Say "GAME detected: $Game -> PAUSE written ($PausePath); the runner ends in $GraceSeconds s"
    & $Sleep $GraceSeconds
    $ended = & $EndRunner
    & $Say "Ollama's runner ended ($ended process(es)); the GPU is the game's, and the harness asks its generations again after the pause"
}

function Send-PauseBeat {
    # The lease: PAUSE is rewritten every poll while the pause lasts.
    param([string]$PausePath)
    if (Test-Path -LiteralPath $PausePath) { (Get-Item -LiteralPath $PausePath).LastWriteTime = [datetime]::Now }
}

function Stop-FuzzPause {
    param([string]$PausePath, [int]$QuietSeconds, [scriptblock]$Say)
    Remove-Item -LiteralPath $PausePath -Force -ErrorAction SilentlyContinue
    & $Say "no game load for $QuietSeconds s -> PAUSE removed; the harness's generations resume and Ollama reloads the model on the first request"
}
