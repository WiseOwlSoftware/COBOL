## The review lane kills a hung reviewer on Linux, and its self-test stops waiting 20 minutes to pass (kb/Work PB4072)

From #760 (2026-10-29 12:28 PDT) every pull-request and merge-queue CI run spent 20 minutes in one script self-test,
`review_lane.py --self-test`, and reported it GREEN. Its runner started the reviewer with a plain Popen, and `kill_tree`'s
POSIX arm kills a process GROUP, which the reviewer did not lead: `killpg` raised ProcessLookupError, `kill_tree` took it for
"already gone", and the hung stub's two 600 s sleeps ran out while the run was recorded STALLED and STOPPED. In production a
hung reviewer was never killed on Linux. The 22-minute `audits` job also outlived the landing lane's queue-watch grace on
every pull request, which is how #773 to #777 came to read BOUNCED (kb/Work PB4052). Mythos measured it and filed it after
the owner asked why so many actions were running so long.

`run_agent` now starts the agent in its own session on POSIX. `kill_tree` treats a live process that leads no group as the
caller's defect: it kills it alone and raises ChildProcessError. The self-test's ceiling and loop-stop arms assert the run
was killed (its rc is not the stub's own 3), so the old code fails at once; under WSL the whole self-test now takes 0.64 s.
`run_gate_legs.py`'s `_stop_tree`, a second copy of `kill_tree` with the same swallow, is gone, and its callers use the one.
