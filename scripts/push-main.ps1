# push-main.ps1 — the pwsh-callable entry point for `scripts/push-main.sh` (kb/Work/PB796, owner question 23).
#
# ⛔ THIS IS A SHIM, NOT A TWIN — deliberately unlike the `build-local.{sh,ps1}` pair. That pair has two full
# implementations because each shell's gate has to work with no other shell installed, and the cost of the
# duplication is a known, accepted one. Here the duplication would be pure loss: the whole body is `git` and `gh`
# invocations that read identically in both shells, and a second copy of the landing protocol is a second place
# for it to drift (feedback_one_mechanism_per_job) — the exact defect shape kb/Work/PB796 is about, where ONE
# rule written in four places meant none of them carried the step that mattered. `bash` is present wherever this
# repository is developed (Git for Windows ships it; CI's Windows jobs already run `shell: bash` steps), so the
# pwsh form forwards and the logic stays in one file.
#
# Usage:  pwsh scripts/push-main.ps1 [-NoDelete] [-BranchPrefix ci] [-TimeoutMinutes 45]
[CmdletBinding()]
param(
    [switch]$NoDelete,
    [string]$BranchPrefix = 'ci',
    [int]$TimeoutMinutes = 45
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if (-not (Get-Command bash -ErrorAction SilentlyContinue)) {
    Write-Host '⛔ push-main: bash is not on PATH — install Git for Windows, or run scripts/push-main.sh directly.'
    exit 2
}

$argv = @('scripts/push-main.sh', '--branch-prefix', $BranchPrefix)
if ($NoDelete) { $argv += '--no-delete' }

$env:PUSH_MAIN_TIMEOUT_MIN = "$TimeoutMinutes"
& bash @argv
exit $LASTEXITCODE
