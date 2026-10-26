## Entry 2607 — 2026-10-25 20:59 PDT — The landing-lane drift rule names every Step-Lane call site

PB3709. `LandingLaneDriftTests`' class summary, the rule `drift_rules.py` prints before an edit of `orchestrate.ps1`, said the supervisor
ticks the landing lane "as each unit starts and on a period while it runs". Since PB3639 the test also requires `Step-Lane` in `Wait-Until`
(after its `Test-Stop`, so a budget hold and a failure backoff tick) and at the loop top between units. An implementer who read only the
printed rule and reordered `Wait-Until` learned the requirement from a red gate.

Re-probe: `drift_rules.py scripts/orchestrator/orchestrate.ps1` printed the unit-bound text. Fix: the summary now names the one timer and its
three call sites in a single sentence that fits the printer's 400-character cut, and the `<remarks>` carry the reasoning. No assertion changed.
PB3708 (the note's proposed companion) had already landed. Sibling sweep: O11, DESIGN-orchestrator-loop.md section 4.9 and `land_lane.py` already
describe the lane as unit-or-no-unit. Rows closed: none (tooling documentation defect).

