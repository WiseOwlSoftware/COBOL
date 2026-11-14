## Entry 3587 — 2026-11-13 21:41 PST — PB5500: ProcessJob.kill() returns only once the whole tree is gone

Merge group 43558438384 (#1607) went RED in "Windows script self-tests": `process_job.py --self-test` hit WinError 32
deleting `out2.txt` during `TemporaryDirectory` cleanup. The cause was the tool's contract, not the test. `kill()` asked
the job to terminate and then waited only for the root, so a killed root's orphaned grandchild could still hold the output
file it inherited. On this host under CPU load the race held the file in 16 of 50, 14 of 40 and 12 of 30 runs; idle, 0 of 50.
Waiting until the job's pid list read empty still left 18 of 50 held, because Windows drops a process from that list before
it closes the process's handles.

`kill()` now holds a SYNCHRONIZE handle to every job member before `TerminateJobObject` and waits on each process object,
which is signaled only after its handles close. On POSIX it waits until no live member is left. A member that survives 30 s
fails the kill loudly, naming its pid. After the fix the repro held the file in 0 of 100 runs under load, and the self-test
looped under load went RED 0 of 30 times. A new self-test arm stubs the kill primitive to end only the root. With the old
`kill()` it is RED on Windows and WSL; with the new one it is GREEN. Siblings are listed in
`kb/Work/PB5500/2026-11-13-sibling-sweep.md`; the review lane's `kill_tree` is PB5114's.

