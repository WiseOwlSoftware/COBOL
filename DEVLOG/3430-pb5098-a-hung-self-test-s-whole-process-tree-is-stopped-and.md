## Entry 3430 — 2026-11-10 05:07 PST — PB5098: a hung self-test's whole process tree is stopped, and no orphan can block the runner

The PB5109 gate sat 87 minutes. A test supervisor that `test_orchestrate_account.ps1` started never exited. At 3,600 s
the HANG stop killed the test's own `pwsh` and nothing else. The runner then waited forever in `subprocess.run`'s
post-kill `communicate()` on the stdout pipe the orphan still held, so the hang detector itself hung. On 2026-11-09 the
same gap left 21 test supervisors running for an hour after their gates.

New `scripts/process_job.py`: the root of a tree starts suspended, joins a kill-on-close Job object and then resumes
(on POSIX it leads a new session), so one kill ends the whole tree, including an orphan whose parent has exited, which
`taskkill /T` cannot reach. The self-test runner runs every self-test that way, with its output in files instead of
pipes. It kills a hung self-test's whole tree, and it kills and names anything a passing self-test leaves running. The
gate's audits and test hosts use the same module, and `gate_slot`'s copy of the Job-object code is deleted.

