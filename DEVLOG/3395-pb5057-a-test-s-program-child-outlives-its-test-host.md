## Entry 3395 — 2026-11-09 16:13 PST — PB5057: a test's program child outlives its test host

The Mythos session found NIST program DB205A from the deleted worktree wf_b9ab56e7-a8d-1 busy-looping one core for over
five hours with its parent gone, and killed it. The only kill of a program child, the budget kill in
`ProcessObserver.Observe` (and `LiveProcess.Dispose`), runs inside the test host, so a host that dies first leaves the
child with nothing to stop it. Filed kb/Work PB5057 with the sites and a fix shape: one starter that ties every child to
the host's lifetime (a kill-on-close job object on Windows, a parent-death signal on Linux), a drift test, and a probe of
whether DB205A itself hangs at main.

