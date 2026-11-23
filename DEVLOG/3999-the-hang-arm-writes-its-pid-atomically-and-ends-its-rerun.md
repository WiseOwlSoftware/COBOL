## Entry 3999 — 2026-11-23 11:28 PST — The HANG arm writes its pid atomically and ends its rerun loop on a rerun that never wrote one

The planted grandchild of `scripts/self_tests.py` `fleet_arms` now writes `hang.pid` and `orphan.pid` to a temp file beside
the name and `os.replace`s it on, so a reader that sees the name reads the whole pid (a gate went RED on `int('')` when it
read between create and write). The doubling rerun loop is `pid_written`: it ends with False when a rerun ended on its own
(any state but HANG, the job's exit observed, no wall clock) without a pid, and the arm reports a named FAIL instead of
looping without end. A planted `test_nopid.py` that exits at once pins it; the run still prints its verdict. kb/Work PB5131.

