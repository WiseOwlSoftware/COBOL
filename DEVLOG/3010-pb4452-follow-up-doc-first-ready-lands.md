## Entry 3010 — 2026-11-02 13:48 PST — PB4452 follow-up (doc): first ready lands

DESIGN-architecture-review §8.7 and DESIGN-orchestrator-loop's landing-lane table now say what landing_check.py does.
An edit that meets another in-flight branch's edit only warns, and the first branch that is ready lands.
Exit 3 is a STOP that only a new commit clears, so the enqueue unit's finisher takes it.
This change landed in its own branch, so the script change (lc-first-ready) keeps the tooling lane (kb/Work PB4453).

