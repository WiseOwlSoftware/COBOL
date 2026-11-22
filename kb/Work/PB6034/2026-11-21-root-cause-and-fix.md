## Root cause and fix (train-1985-c, group m2; finding by the controller, account 2)

**Not a flake of the tree.** The controller's finding (`E:\COBOL-coord\scratch\w20262131\pb6034\finding.md`, repro
`...\pb6034\repro.py`): at 2026-11-21T20:51:15Z the wave 20262131 group-u implementer cleaned up after its own
`cobol.exe check-batch` and `dotnet-stack` sampling with

    Get-Process dotnet-stack,cobol -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

`Get-Process cobol` matches every cobol.exe on the host, so it also killed the run.py compiles of the train-1985-b gate's
BothRunnersVerdictDriftTests P3-P7, which were in their 2023 round then. `Stop-Process` is `TerminateProcess(h, -1)`, and
Python reads that exit as 4294967295 with empty streams. The repro prints `returncode 4294967295 stderr bytes 0`. The gate's
own job kills use code 1 (`scripts/process_job.py`). The observations log has no RETRY lines, and the failures spread
across five run.py trees while the testhost survived, which is the pattern of a kill by image name.

**Fix 1, the kill (the root):** `scripts/hooks/forbidden_commands.py` rule 8 refuses a kill by process name in any
Bash or PowerShell call: `Stop-Process`/`spps`/`kill` with `-Name`/`-ProcessName` (any prefix), a `Stop-Process` fed by a
`Get-Process` stage with no `-Id`, `taskkill /IM` (and Git Bash's `//IM`), `pkill` and `killall`. Kills of the agent's
own pid stay allowed (`Stop-Process -Id $p.Id`, `$p | Stop-Process`, `$p.Kill()`, `taskkill /PID`, `kill <pid>`).
`test_forbidden_commands.py` has 21 new cases, the exact command above among them. The base hook gives
`forbidden_commands self-test: 323/337 RED` (the 14 refused forms pass through), and the head gives `337/337 GREEN`.
MANDATORY-PRACTICES gains row P18, and `check_practices.py` pins its presence (`=== PRACTICES CHECK: GREEN ===`).

**Fix 2, the verdict (the sibling):** `run.py`'s `crashed()` tests `returncode < 0`, which never fires on Windows
because Python reads the exit code unsigned. So an externally killed compiler read as FAIL "did not translate"
instead of NO VERDICT. The WiseOwl adapter (`conformance-suite/adapters/wiseowl.json`) now lists 4294967295
(TerminateProcess(-1)) among its crash exit codes, beside the three NTSTATUS crash codes it already listed. The
harness's self-test already holds that an adapter-declared crash code is NO VERDICT (`crashes`, `flag-crash`).

No inventory row states the hook's or the adapter's behaviour, so the note closes with no row.
