## Entry 3425 — 2026-11-10 02:45 PST — PB5109: a shared file never holds a branch

The owner has said many times that two branches editing one file is what merge resolves. The landing still held branches
with exit 16, DRIVER FILE QUEUED, whenever another open pull request changed a merge-driver file. Tonight it held
notes-pb5099 and PB5060's branch behind #1444. Both holds were false: #1444 has 141 files, GitHub lists 100, and the guard
read the cut-off list as "changes every driver file" (Mythos's diagnosis).

Deleted the guard outright: `driver_file_holders`, exit 16, the queue-entry lock that served only it, the landing lane's
exit-16 wait, and their self-tests. A branch is now stamped and queued whatever other pull requests change. A merge group
that conflicts bounces, and a finisher fixes it then. A new self-test and two shape checks keep the guard from returning.
The two held branches were landed by hand with the fixed script before this change lands (#1445 and notes-pb5099).

