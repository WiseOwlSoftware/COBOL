#Requires -Version 7.0
<#
.SYNOPSIS
  Run the overnight differential fuzz, giving the GPU to a game while one is played and taking it back afterwards, with
  no program lost (kb/Work PB3561).

.DESCRIPTION
  Starts scripts/differential_fuzz.py with --out <run directory> and then polls the Windows "GPU Engine" performance
  counters every -PollSeconds. A GAME is any process, other than the harness, Ollama, the desktop and the processes named
  in -Exclude, whose 3D-engine utilization stays above -GameThreshold percent for -GameSamples consecutive polls.

  The pause is a FILE, <run directory>/PAUSE, and the harness does the rest (scripts/fuzz_ollama.py has the protocol):
    game seen   PAUSE is written; after -GraceSeconds Ollama's runner process is ended, which frees the GPU at once. The
                generations in flight fail, the harness sees PAUSE and asks each of them again after it; no new generation
                starts while PAUSE exists. Nothing is suspended: the compiles and runs of programs already generated finish.
    game gone   PAUSE is rewritten every poll while the pause lasts (the harness treats a PAUSE not rewritten for five minutes
                as left by a wrapper that died); after -QuietSeconds with no game it is removed, and the harness's generations
                go on (Ollama reloads the model on the first request).
  The harness records every pause in <run directory>/pauses.jsonl and its report.md counts the paused time and the generations
  asked again, so the morning reads a pause as a pause and not as the model's failures.

  Ctrl+C stops the watcher AND the harness (the run's partial results stay on disk; its report.md is written by the harness
  on its own exit) and removes PAUSE. Every pause and resume is logged.

  The pure logic (finding the game, the state machine, the pause actions) is scripts/fuzz_night_lib.ps1, which
  scripts/test_fuzz_night.ps1 drives with a stubbed counter feed.

.EXAMPLE
  pwsh scripts/fuzz_night.ps1 -Cobol E:/tmp/fuzz-judge-20261026/cobol.exe
  pwsh scripts/fuzz_night.ps1 -Model qwen3.6:35b -Hours 10 -Exclude chrome,msedge,vlc -ModelOption 'qwen3.6:35b:repeat_penalty=1.1'
#>
param(
  [string]$Model = 'qwen3.6:35b',
  [double]$Hours = 8,
  [int]$Jobs = 4,
  [int]$Seed = 20261024,
  [Parameter(Mandatory)][string]$Cobol,                # the judge: a frozen build of the compiler under test
  [string]$Repo = (Split-Path $PSScriptRoot -Parent),
  [string]$RunDir = '',                                # default: <repo>/conformance-suite/results/fuzz-<stamp>
  [string[]]$ModelOption = @(),                        # MODEL:KEY=VALUE, passed to the harness's --model-option
  [string]$Log = '',                                   # default: <run directory>/wrapper.log, beside the harness's own files
  [switch]$NoFastForward,                              # leave the checkout as it is (default: fast-forward it to origin/main first)
  [int]$PollSeconds = 10,
  [double]$GameThreshold = 30,   # percent of a 3D engine, per process
  [int]$GameSamples = 3,         # consecutive polls above the threshold before pausing (30 s by default)
  [int]$QuietSeconds = 120,      # no game load for this long before resuming
  [int]$GraceSeconds = 5,        # between writing PAUSE and ending the runner: a worker between its check and its request
  [string[]]$Exclude = @('System','dwm','explorer','WindowsTerminal','OpenConsole','pwsh','powershell','python',
                         'ollama','ollama app','ollama_llama_server','llama-server','chrome','msedge','firefox','Code','devenv',
                         'SearchHost','StartMenuExperienceHost','ShellExperienceHost','TextInputHost','csrss','LogonUI',
                         'nvcontainer','NVIDIA Overlay','steamwebhelper')
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'fuzz_night_lib.ps1')
. (Join-Path $PSScriptRoot 'python-resolve.ps1')    # $PY: the one interpreter the scripts run (kb/Work PB1637, PB3197)

function Log([string]$m) { $line = "{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $m; Write-Host $line; Add-Content -Path $script:Log -Value $line }

function Get-ProcessTree([int[]]$roots) {
  # the processes below $roots (inclusive), found by parentage
  $all = Get-CimInstance Win32_Process | Select-Object ProcessId, ParentProcessId
  $out = @($roots); $queue = @($roots)
  while ($queue.Count -gt 0) {
    $next = @()
    foreach ($p in $queue) { $next += ($all | Where-Object { $_.ParentProcessId -eq $p } | ForEach-Object { $_.ProcessId }) }
    $queue = $next; $out += $next
  }
  $out | Select-Object -Unique
}

function Get-OllamaPids {
  # Ollama's server and every process it spawned (its runner is named llama-server on this build and shows its CUDA work
  # under the 3D engine type), found by PARENTAGE so a renamed runner is still Ollama's
  $roots = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(ollama|llama-server|ollama_llama_server)' } | ForEach-Object { $_.ProcessId })
  if ($roots.Count -eq 0) { return @() }
  Get-ProcessTree $roots
}

$script:emptyPolls = 0
function Get-GpuLoad {
  # @{ pid = max 3D utilization percent } for this poll; the counter's instance name carries the pid
  $r = @{}
  try {
    # SilentlyContinue: a process that vanished between enumeration and read gives one INVALID sample (Status <> 0),
    # which must not discard the whole poll; such samples are skipped
    $samples = (Get-Counter '\GPU Engine(*)\Utilization Percentage' -ErrorAction SilentlyContinue).CounterSamples
    if ($samples) { $script:emptyPolls = 0 }
    else {
      # a watcher that sees nothing would never see a game: say so at the first empty poll and every 60th after it
      $script:emptyPolls++
      if ($script:emptyPolls % 60 -eq 1) { Log "the GPU counters returned no samples (empty poll $script:emptyPolls): a game would go unseen" }
    }
    foreach ($s in $samples) {
      if ($s.Status -ne 0) { continue }
      if ($s.InstanceName -notmatch 'engtype_3d') { continue }
      if ($s.InstanceName -match '^pid_(\d+)_') {
        $p = [int]$Matches[1]
        if (-not $r.ContainsKey($p) -or $r[$p] -lt $s.CookedValue) { $r[$p] = $s.CookedValue }
      }
    }
  } catch { Log "GPU counters unavailable this poll: $($_.Exception.Message)" }
  return $r
}

function Stop-OllamaRunner {
  # ends the model runner(s) and says how many; Ollama starts a fresh one on the next request
  $n = 0
  Get-Process -Name 'llama-server', 'ollama_llama_server' -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue; $n++ }
  return $n
}

# ---- start the run -------------------------------------------------------------------------------------------
Set-Location $Repo
if (-not $RunDir) { $RunDir = Join-Path $Repo ("conformance-suite/results/fuzz-{0:yyyyMMdd-HHmmss}" -f (Get-Date)) }
New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
if (-not $Log) { $Log = Join-Path $RunDir 'wrapper.log' }
if (-not $NoFastForward) {
  $ff = git merge --ff-only -q origin/main 2>&1
  if ($LASTEXITCODE -ne 0) { Log "could not fast-forward $Repo to origin/main (the run uses the checkout as it is): $($ff -join ' ')" }
}
$pausePath = Get-PausePath $RunDir
$hargs = Get-HarnessArguments -Model $Model -Hours $Hours -Jobs $Jobs -Seed $Seed -Cobol $Cobol -RunDir $RunDir -ModelOption $ModelOption
Log "starting: $PY $($hargs -join ' ') (checkout at $(git rev-parse --short HEAD))"
# The harness is attached to this console (its per-program lines print here) and writes the morning's files itself,
# report.md and results.jsonl, in the run directory. This wrapper adds only the GPU watch; its own lines go to $Log too.
$proc = Start-Process -FilePath $PY -ArgumentList $hargs -WorkingDirectory $Repo -NoNewWindow -PassThru
Log "harness pid $($proc.Id); pause file $pausePath; watching the GPU every $PollSeconds s (game = 3D engine > $GameThreshold % for $GameSamples polls; resume after $QuietSeconds s quiet)"

$state = New-WatchState
# The actions are CLOSURES: a script block runs in its caller's scope, where a lib function's own parameter of the same name
# ($QuietSeconds in Invoke-WatchPoll) would shadow the value, and a lib parameter once shadowed $Log and failed the first
# real poll (the self-test holds the lib to reading nothing it was not passed).
$act = @{
  Pause  = { param($game) Start-FuzzPause -PausePath $pausePath -Game $game -GraceSeconds $GraceSeconds -EndRunner { Stop-OllamaRunner } -Sleep { param($s) Start-Sleep -Seconds $s } -Say { param($m) Log $m } }.GetNewClosure()
  Beat   = { Send-PauseBeat -PausePath $pausePath }.GetNewClosure()
  Resume = { Stop-FuzzPause -PausePath $pausePath -QuietSeconds $QuietSeconds -Say { param($m) Log $m } }.GetNewClosure()
}
$nameOf = { param($processId) try { (Get-Process -Id $processId -ErrorAction Stop).ProcessName } catch { $null } }
# Ctrl+C is read by THIS script (the harness, sharing the console, also receives it and writes its report on the way
# out); the wrapper then stops whatever is left of the harness tree and ends. Without this, Ctrl+C only reached the child.
# With no console (a redirected run) there is no key to read: the wrapper then simply sleeps, and ends when the harness does.
$readsKeys = try { [Console]::TreatControlCAsInput = $true; $true } catch { $false }
function SleepOrInterrupt([int]$seconds) {
  # returns $true when Ctrl+C was pressed during the wait
  $until = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $until) {
    while ($readsKeys -and [Console]::KeyAvailable) {
      $k = [Console]::ReadKey($true)
      if ($k.Key -eq 'C' -and ($k.Modifiers -band [ConsoleModifiers]::Control)) { return $true }
    }
    Start-Sleep -Milliseconds 250
  }
  return $false
}
try {
  while (-not $proc.HasExited) {
    if (SleepOrInterrupt $PollSeconds) {
      Log "Ctrl+C: stopping the run"
      $t0 = Get-Date
      while (-not $proc.HasExited -and ((Get-Date) - $t0).TotalSeconds -lt 15) { Start-Sleep -Seconds 1 }
      break
    }
    $null = Invoke-WatchPoll -State $state -Load (Get-GpuLoad) -HarnessPids @(Get-ProcessTree @($proc.Id)) -OllamaPids @(Get-OllamaPids) `
      -Exclude $Exclude -Threshold $GameThreshold -NameOf $nameOf -Now (Get-Date) -GameSamples $GameSamples -QuietSeconds $QuietSeconds -Act $act
  }
  if ($proc.HasExited) { Log "harness exited with code $($proc.ExitCode); the morning reads report.md in $RunDir" }
} finally {
  if ($readsKeys) { [Console]::TreatControlCAsInput = $false }
  Remove-Item -LiteralPath $pausePath -Force -ErrorAction SilentlyContinue
  if (-not $proc.HasExited) {
    Log "watcher ending: stopping the harness tree"
    foreach ($p in (Get-ProcessTree @($proc.Id))) { Stop-Process -Id $p -Force -ErrorAction SilentlyContinue }
  }
}
