## Entry 3999 — 2026-11-23 11:28 PST — The HANG arm writes its pid atomically and ends its rerun loop on a rerun that never wrote one

The planted grandchild of `scripts/self_tests.py` `fleet_arms` now writes `hang.pid` and `orphan.pid` to a temp file beside
the name and `os.replace`s it on, so a reader that sees the name reads the whole pid (a gate went RED on `int('')` when it
read between create and write). The doubling rerun loop is `pid_written`: it ends with False when a rerun ended on its own
(any state but HANG, the job's exit observed, no wall clock) without a pid, and the arm reports a named FAIL instead of
looping without end. A planted `test_nopid.py` that exits at once pins it; the run still prints its verdict. kb/Work PB5131.

The same race had two siblings, and the merge group's `audits` job went RED on one of them (`scripts/detached.py --self-test`,
`int('')` on `target.pid`): `detached.py`'s detached target and the first-RED-audit arm of `scripts/selftests/gate/test_driver.py`
(audit SLOW's `grandchild.pid`) wrote their pid in place; both now write a temp file and `os.replace` it onto the name. A sweep
of `scripts/` read every other pid or mark file a child writes and a parent reads, and ruled each out (reader waits for
non-empty content, or reads only after the writer exited, or tolerates a partial file). kb/Work/PB5131/2026-11-23-detached-sibling.md.
