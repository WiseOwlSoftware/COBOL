## Entry 3420 — 2026-11-10 00:36 PST — PB5110 filed: the owner decides to delete the custom merge drivers in favour of one file per entry

The owner asked why the repository needs custom git merge drivers when ordinary git merges independent changes and raises a
real conflict otherwise, and decided "I want it ASAP". The drivers (PB3610) keep both sides when two branches append to one work
note or add entries beside one neighbour in a keyed file, but GitHub's merge queue runs plain git without them, so every branch
sharing such a file with an open pull request is held (PB3642), the source of the night's waits and of a false hold when GitHub
cut a 141-file pull request's list at 100. The repository's own rule already prefers one file per entry (PB4147, which removed 71
of 79 holds). PB5110 records the decision: later additions to a note become their own files, constructs.json becomes one file
per construct, the ABI pin and public-API baselines merge as plain git, and the drivers and the hold are deleted.

