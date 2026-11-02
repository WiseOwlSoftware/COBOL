## PB4409: a compiler change runs no script self-tests

The owner's decision of 2026-11-01, after the PB4097 gate showed a compiler change paying 1,613 s of script self-tests and
644 s more for the post-build one because it touched docs/CONFORMANCE.md, a file a self-test reads as data. In the full
lane `inert_lane.classify` now sets the tooling flag only for a changed script (a tooling tree or a .py/.ps1/.sh file),
never for a TOOLING_READS data file; a compiler change that edits a script still runs that half. CI's windows-self-tests job
decides through the same flag, so THE RULE stays whole, and the Linux audits job still runs every self-test in every merge
group. The inert_lane self-test arms state the new flag; CLAUDE.md "Testing", DESIGN-test-build-ci.md and gate.py say it.
The gate slot cap is back at its default 3 the same day (owner choice).
