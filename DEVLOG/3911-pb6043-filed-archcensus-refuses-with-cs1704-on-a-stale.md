## Entry 3911 — 2026-11-22 02:20 PST — PB6043 filed: ArchCensus refuses with CS1704 on a stale Release analyzer after a version bump

Filed kb/Work/PB6043 (process-only, MINOR, area tooling). plan_wave.py at origin/main 2061bdb7bb stopped in ArchCensus with
CS1704 because the Version bump of PB2743 left a version-31 analyzer in bin/Release beside the version-32 project
reference (the Tests.Unit reference uses SetConfiguration=Release, which a Debug solution build does not rebuild).
Workaround that proved the cause: build the analyzer in Release. The note records the root fix (the census builds or
verifies its inputs; the solution build rebuilds the Release reference on a Version change) and the sibling sweep.
No code changed.

