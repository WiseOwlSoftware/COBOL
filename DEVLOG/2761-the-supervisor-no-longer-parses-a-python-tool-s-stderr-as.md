## Entry 2761 — 2026-10-29 19:43 PDT — The supervisor no longer parses a Python tool's stderr as its answer, and the orchestrate self-tests quote the run's own reason (kb/Work PB4046; PB4083 and PB4084 filed)

`test_orchestrate_adopt.ps1` failed in CI's Windows self-tests a second time on 2026-10-29: PR #791, merge-group run
40987962746. Both failures said only "got 'False', want 'True'". In each, the loop did not adopt main, and the harness
threw away the run's output, where the reason is printed.

The shared harness, `testdata/orchestrate_test_lib.ps1`, now keeps the newest run's output. A failed check quotes the lines
that match the part's evidence pattern: for the adopt part, `adopt main:`, `main not adopted` and `wrote to stderr`.

The branch review found a path that pattern first missed. When adopt_main.py fails, the supervisor prints `main not
adopted (...)`, and it did the same when the tool merely wrote to stderr. `orchestrate.ps1`'s `Invoke-Py` ran each tool
with `2>&1`, and its callers parsed the merged text as JSON: adopt_main's answer, budget.py's and next_unit.py's. So one
warning from a tool that succeeded cost the supervisor its answer. adopt_main.py had already fast-forwarded the checkout,
but the loop said "main not adopted" and skipped the restart that a landing of its own script needs. `Invoke-Py` now
keeps stdout as the answer and says stderr without parsing it. The startup's account and cluster reads go through it.
Adopt scenario 5 proves it: it lands a change to the supervisor's script and plants a stderr line through the fake python.
The line is said and the supervisor still restarts; on main's supervisor the restart checks fail. This is not the cause of
CI's flake, whose scenario 4 kept the old commit.

PB4046 stays open with three causes ruled out and the evidence ready for the next run. PB4083 records the PowerShell
self-tests' repositories built outside `scratch_repo.py`'s fixture. PB4084 records four entry scripts with the same stderr
shape as `Invoke-Py`.

