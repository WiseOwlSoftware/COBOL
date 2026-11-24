**2026-11-23, siblings of this note's pid-file race, found when the merge group's `audits` job went RED on Linux (PR #2027, run 45250073773): `scripts/detached.py --self-test` raised `ValueError: invalid literal for int() with base 10: ''` at `survivor = int(mark.read_text(...))`.**

**Mechanism (one, written five times).** A child writes a pid with `write_text` / `open(p, 'w').write(...)`, which creates the name EMPTY and then fills it; a parent that waits for the name (`exists()`) and then reads it sees the empty file in between. The fix is the one this note's branch made in `self_tests.py`: the writer writes `<name>.tmp` beside the name and `os.replace()`s it on, so the name never exists without the whole pid. The waits keep their existence predicate (no wall clock in a verdict; `_wait_for` is a hang guard that raises a named `TimeoutError`, not a verdict).

**Fixed (real siblings).**
- `scripts/detached.py` `self_test` arm 1: the detached target wrote `target.pid` in place; `_wait_for(mark.exists, ...)` then `int(mark.read_text())` read it empty. Now temp file plus `os.replace`.
- `scripts/selftests/gate/test_driver.py` `case_00` (`FirstRedStops.audit_commands`): audit SLOW wrote `grandchild.pid` in place; QUICK-RED waited on the name and exited RED, the audits stopped SLOW, and the arm's `int(mark.read_text())` could see the empty file (or a SLOW killed between create and write). Now temp file plus `os.replace`, so QUICK-RED's wait on the name guarantees the whole pid is there when the arm reads it.

**Ruled out (read, each with its reason).**
- `scripts/process_job.py` self-test `orphan_tree` / `read_pid` (lines 345-359): writes in place, but the reader's predicate is `exists and read().strip()` (waits for NON-EMPTY content) and the pid is one small write; the root-exits arm (line 367-373) reads only after `job.wait()`, the root having exited, so the file is complete.
- `scripts/gate_runs.py` `write_pid` / `write_spawn` / `_read_pid_file`: written in place by the gate, but the one reader maps an empty or partial file to `None` (the documented "no pid yet"), and its callers (`test_await.py:156` loop, `read_pid(...) is not None`) retry.
- `scripts/orchestrator/test_rolling_wave.py:418` and `scripts/selftests/orchestrator/plan_wave/test_planner.py:2689`: `gate.pid` written by the test's own parent before any reader runs; no concurrent child.
- `scripts/gate_slot.py` selftest (`held pid=%d` on stdout) and `scripts/orchestrator/coord.py:540` (a command-line marker in argv): no file is read between create and write.
- `scripts/selftests/gate/test_await.py` / `test_runs_and_locks.py` `write_pid(..., os.getpid())`: the writer is the test's own process, called before the read.
- `go` / `never` marker files (`detached.py:192`, `gate_slot.py:1036`, `test_runs_and_locks.py:53`): existence-only signals, content never read.
