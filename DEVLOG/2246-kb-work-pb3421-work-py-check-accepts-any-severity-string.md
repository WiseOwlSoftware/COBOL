## Entry 2246 — 2026-10-19 01:11 PDT — kb/Work PB3421: work.py check accepts any severity string; PB3418 and PB3291 corrected to MINOR

The orchestrator loop's enqueue unit read the clerk's notes PB3416-PB3419 (PR #278) and found PB3418 filed
`severity: MEDIUM`, a word from the branch reviewers' scale (critical, high, medium, low, info), not the register's.
`python scripts/spec/work.py check` passed it. The register uses BLOCKER, MAJOR, MINOR, INFO and OWNER (`actionable`
ranks with `{"BLOCKER": 0, "MAJOR": 1, "MINOR": 2, "OWNER": 3}`), so an off-vocabulary value ranks after every OWNER
item with no warning. A sweep of `kb/Work/` found one more: PB3291 `minor`, lowercase.

- PB3421 (new, defect, MINOR, process_only): `check` does not validate `severity`. The fix is one constant that both
  `check` and `actionable` read, with a self-test case per arm, plus a sweep of the other closed-vocabulary fields
  (`kind`). `area` is free text by design (437 distinct values).
- PB3418 `MEDIUM` → `MINOR`. The code is right and only its witness is missing, so a user's program is not harmed.
  The body now says the review rated it medium on its own scale.
- PB3291 `minor` → `MINOR`.

`work.py check`: 2713 work items, all well-formed.

