## Review R1-1: rule 8 judges the whole command line (train-1985-c finisher)

**Finding.** The branch review of train-1985-c (round 1, R1-1, medium) showed that rule 8 refused a by-name kill only
when `Stop-Process` was fed DIRECTLY by `Get-Process` or took `-Name`. Five forms that kill every cobol.exe passed
(reviewer's probe `E:\COBOL-coord\scratch\w20262131\review-train-1985-c\hookprobe.py`, exit 0 for each at ea897433f0):
a `Where-Object` stage in the pipe, `Stop-Process -Id (Get-Process cobol).Id`, `Get-Process -Name cobol |
ForEach-Object { $_.Kill() }`, `kill $(pgrep cobol)` and `pgrep cobol | xargs kill`.

**Root cause.** The rule modelled a by-name kill as one shape, a pipe's two neighbours. A by-name kill has two halves
that can stand anywhere in the line: a step that ENUMERATES processes by name (or all of them) and a step that KILLS.

**Fix.** `scripts/hooks/forbidden_commands.py` rule 8 now classifies every step of the shared command model
(`shell_location.steps`, which already walks substitutions, script blocks, loops and payloads): `_enumerates`
(Get-Process / gps / PowerShell ps without -Id, Bash ps without -p, pgrep, pidof, tasklist without a PID filter, wmic or
a Win32_Process query not filtered to one ProcessId, .NET GetProcesses / GetProcessesByName), `_kills` (Stop-Process /
spps / kill, taskkill, pkill, killall, a kill run by xargs / find / timeout, `.Kill()` / `.Terminate()`, Invoke-CimMethod
Terminate, Remove-CimInstance / Remove-WmiObject, wmic delete or call terminate) and `_kills_by_name` (one step that is
both: Stop-Process -Name, taskkill /IM or an image /FI, pkill, killall). An enumeration and a kill anywhere in one
command line are refused, in any order, so the next spelling of either half is caught with no new shape. A kill of a pid
the command started, with no enumeration in the line, stays allowed.

**Evidence.** `scripts/hooks/test_forbidden_commands.py` gains 31 cases (the reviewer's five, 15 more enumerate-and-kill
forms, 11 allowed forms): 348/368 RED on the hook at ea897433f0 (the 20 new block cases), 368/368 GREEN after; the
reviewer's probe exits 2 for all six of its forms. MANDATORY-PRACTICES P18 states the two halves.
