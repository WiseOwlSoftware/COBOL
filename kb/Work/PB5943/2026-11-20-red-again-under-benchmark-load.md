## 2026-11-20: red again under load, with a second self-test beside it (w20262104n, branch `listing-step-6b-r2`)

`python -I E:/COBOL-coord/scratch/my-20261115/session-scripts/run_audits.py`, run on the branch's tree (it touches no script) while two
BenchmarkDotNet processes of the section 9 measurement ran on the same host, printed `=== SELF-TESTS: RED — 2 of 211 failed:
scripts/gate_slot.py, scripts/selftests/orchestrator/land_lane/test_detached_runner.py ===`. The run before it on the same tree, with
one benchmark process running, printed SELF-TESTS GREEN (and FLEET PRACTICES red once, green on every run since). Alone, both are
green: `python scripts/self_tests.py --only scripts/gate_slot.py scripts/selftests/orchestrator/land_lane/test_detached_runner.py` ->
`=== SELF-TESTS: GREEN — 2 ran in 11s ===`. The runner keeps no failing arm's text, so the arm is again not captured: the same gap
this note names, now for a second self-test (`test_detached_runner.py`), which points at a timing assumption shared by the gate's slot
and detached-runner tests rather than at `gate_slot.py` alone.
