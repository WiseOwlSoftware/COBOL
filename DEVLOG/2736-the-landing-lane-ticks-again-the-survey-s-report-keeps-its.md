## Entry 2736 — 2026-10-29 13:42 PDT — The landing lane ticks again: the survey's report keeps its path (PB4055)

#760 (PB3608) made `land_lane.survey_rows` read `r.report.path` for the review lane. The survey's
`reports.IndexedReport` held only the report's file name. From 13:12 PDT every landing-lane tick on main raised
`AttributeError`, and no branch WAITING TO LAND was launched. That included PB3840, whose `docs/CONFORMANCE.md` edit
held the Mythos lane's A.1 chain. No gate saw it, because the land lane's self-test fakes the whole survey.

`IndexedReport` now holds `path`, and `name` is derived from it. The row mapping is the pure `land_lane.row_dicts`,
and a new self-test arm runs it on a planted `Row` with a real report. `prune_worktrees.py --json` serializes the
path. The operator landed PB3840 by hand meanwhile.

