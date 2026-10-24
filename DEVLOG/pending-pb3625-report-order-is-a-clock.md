## PB3625 — the planner orders fix-lane reports by write time, not by wave number

**Defect.** `plan_wave.load_reports` sorted by the integer wave number first. The loop numbers its waves by date
(20261024) and the Mythos lane serially (1157), so every date-numbered report outranked every serial one. Wave 20261027
planned an Opus finisher for PB3340 from a 15:12 SPLIT report although `w1157a-pb3340-sequential-journal` already
contained that branch and carried a newer DONE report.

**Re-probe.** A fixture with `w20261024a-PB5` (SPLIT, written first) and `w1157a-PB5` (DONE, written an hour later)
read in the order serial, date and planned a finisher of the SPLIT branch on the unfixed tree.

**Fix.** One order, `plan_wave.report_order`: write time, then wave, then name. `load_reports` is the only sorter; the
planner, the branch survey, the landing check and the landing measure all read their "newest report" from it.
`next_wave` still takes the highest wave number: it needs a number nobody used, not a time order.
`DESIGN-orchestrator-loop.md` section 4 now says "by write time, then wave number".

**Witnesses.** `test_plan_wave.py` PB3625 block: the read order in both directions (serial DONE after date SPLIT, and
the reverse) and the planner's finisher or absence of one. Two of its three checks are red with the old key; the
self-test is 341/341 now. The landing-check, prune_worktrees, landing_measure, review_findings, rolling-wave and
ledger-state self-tests stay green.

**Self-review.** A report rewritten later (the review stamp) moves its mtime forward. A stamp only follows the report it
stamps, and a finisher's report is always written after it, so a note's newest report stays the newest.
