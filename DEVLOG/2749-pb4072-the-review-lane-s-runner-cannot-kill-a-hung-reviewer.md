## Entry 2749 — 2026-10-29 16:34 PDT — PB4072: the review lane's runner cannot kill a hung reviewer on Linux, and its self-test made every merge-queue run 17 minutes slower; The review lane kills a hung reviewer on Linux, and its self-test stops waiting 20 minutes to pass (kb/Work PB4072)

### PB4072: the review lane's runner cannot kill a hung reviewer on Linux, and its self-test made every merge-queue run 17 minutes slower

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

### The review lane kills a hung reviewer on Linux, and its self-test stops waiting 20 minutes to pass (kb/Work PB4072)

From #760 (2026-10-29 12:28 PDT) every pull-request and merge-queue CI run spent 20 minutes in one script self-test,
`review_lane.py --self-test`, and reported it GREEN. Its runner started the reviewer with a plain Popen, and `kill_tree`'s
POSIX arm kills a process GROUP, which the reviewer did not lead: `killpg` raised ProcessLookupError, `kill_tree` took it for
"already gone", and the hung stub's two 600 s sleeps ran out while the run was recorded STALLED and STOPPED. In production a
hung reviewer was never killed on Linux. The 22-minute `audits` job also outlived the landing lane's queue-watch grace on
every pull request, which is how #773 to #777 came to read BOUNCED (kb/Work PB4052). Mythos measured it and filed it after
the owner asked why so many actions were running so long.

`run_agent` now starts the agent in its own session on POSIX. `kill_tree` treats a live process that leads no group as the
caller's defect: it kills it alone and raises ChildProcessError. The self-test's ceiling and loop-stop arms assert the run
was killed (its rc is not the stub's own 3): the old code fails them once the sleeps run out, and a reviewer started without
a session fails at once, since `kill_tree` raises. Under WSL the whole self-test now takes 0.64 s. `run_gate_legs.py`'s
`_stop_tree` and the POSIX arm of `gate_slot.py`'s self-test harness kill, two copies of the same swallow, are gone; their
callers use the one `kill_tree`.

