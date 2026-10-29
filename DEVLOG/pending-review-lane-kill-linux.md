## PB4072: the review lane's runner cannot kill a hung reviewer on Linux, and its self-test made every merge-queue run 17 minutes slower

The owner asked why everything was taking so long on 2026-10-29. Merge-group CI had gone from about 15 minutes (5 for
notes-only pull requests) to about 23 minutes at 12:28 PDT. Notes-only runs paid the full 23 whenever a code pull request
was ahead of them.

The whole difference was one self-test, `review_lane.py --self-test`. It came in with PB3608 (#760) and took 1,200 s on
the Linux runner while the host sat 4% busy. Its runner arms start a stub agent that sleeps 600 s and check that the
lane kills it on its ceiling and on a loop stop. `run_agent` starts the agent without a new session, so
`detached.kill_tree`'s `os.killpg` finds no group, raises ProcessLookupError, and swallows it. The test then waits out
both sleeps and still passes.

In production that means a hung reviewer is never killed on Linux. PB4072 records the mechanism, the measurements and the
fix: start the agent in its own session, make the self-test assert the stub was killed by its exit code rather than by
the recorded outcome, and stop `kill_tree` from hiding a caller that spawned without a session. The sibling sweep finds
no other caller with the bug.
