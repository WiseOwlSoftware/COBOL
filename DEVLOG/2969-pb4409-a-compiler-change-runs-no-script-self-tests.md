## Entry 2969 — 2026-11-01 18:19 PST — PB4409: a compiler change runs no script self-tests

The owner's decision of 2026-11-01, after the PB4097 gate showed a compiler change paying 1,613 s of script self-tests and
644 s more for the post-build one because it touched docs/CONFORMANCE.md, a file a self-test reads as data. The lane now
carries two flags, one per consumer. `tooling`, the local gate's, is set in the full lane only by a changed script (a
tooling tree or a .py/.ps1/.sh file), never by a TOOLING_READS data file, so the gate's SELF-TESTS audit and post-build
self-tests skip a compiler change that edits only such a file; a compiler change that edits a script still runs that half.
`reads_tooling`, CI's, is the old broad flag (a script or any TOOLING_READS entry): `inert_lane.py classify` prints it, the
`changes` job outputs it and `windows-self-tests` runs on it, so the Windows-only self-tests still run in the merge group
whenever such a file changes (review R1-1 caught the first version narrowing CI's flag too). THE RULE's class (d) in
CLAUDE.md "Testing" now names that first run. The inert_lane self-test arms check both flags and the CI wiring; the gate
skill, MANDATORY-PRACTICES I1, DESIGN-test-build-ci.md, gate.py and the workflow comments say it. The gate slot cap is
back at its default 3 the same day (owner choice).

