## The Completion Ledger is rebuilt per edition on edition_status.py, and "is edition N complete?" counts what it missed

kb/Work PB5994 (decision, owner 2026-11-20) and kb/Work PB5924 step 1, one landing.

**edition_status.py** (`--edition {1985,2002,2014,2023} [--json]`) measures four parts and prints
`=== EDITION <N>: COMPLETE ===` only when all four are measured and zero: the inventory GAP, the code reads owed, the open
`kb/Work` notes that touch the edition (HARM, QUESTION, TOOLING, OTHER), and the edition's normative text in no catalog row.
A part it cannot measure is `not measured` and keeps the verdict NOT COMPLETE. Its round-1 review found five things; each is
fixed here:

- R1-1: the note patterns read frontmatter only, so 24 open notes about 1985 in their body (PB4996, PB5692, PB5449) were
  missed. `touches` now reads frontmatter, body and addenda; a year not inside a word names 1985, 2002 and 2014, and 2023
  is named only by `§2023:`, a `--std` spelling or an unprefixed rule id (its bare year is the standard's number). 1985:
  93 open notes before, 166 now. The structural fix, a declared `editions:` field, is filed as kb/Work PB5995.
- R1-2: part 4 counted deontic sentences (198), a floor. It now counts kb/Work PB479's unit: the printed paragraphs of the
  normative region (Sections IV, VI-XVI, A) in no catalog row at or under their locator, none discounted until PB5452
  records dispositions: **840**, in 495 numbered paragraphs (420 with no row at all), with 128 deontic sentences as a
  labelled floor and 19 paragraphs that are another clause's row as a lead.
- R1-3: `catalog_prose_holes` copied `rule_catalog_1985.parse`'s walk. `parse` now records every paragraph it walks
  (`Catalog.paragraphs`) and the copy is deleted; a committed row the walk does not reach refuses the measurement.
- R1-4: coverage is scoped to the rows at or under the paragraph's locator.
- R1-5: the self-test runs the real inventory and code-read parts (2014's `not measured` pinned) and an unreadable register.

**The ledger** (`scripts/spec/gen_ledger.py`) was built for one goal, 2023 v1.0, and named 1985 nowhere in its plan half.
It now shows, in the owner's order 1985, 2002, 2023, 2014: one line per edition with its verdict and blocking counts, each
`edition_status`'s; then per edition its parts, its open notes (HARM first, with titles), its text in no catalog row and the
work in flight on it (`prune_worktrees.live_survey`, which reads GitHub's pull requests and merge queue; a branch belongs
to an edition by its notes and its name through `edition_status.touches`); the work in flight on no edition; and pacing
(`budget.current`, and the landings of the last 7 days with the notes each edition filed and closed). An unreadable input
is UNMEASURED with its reason; `--check` renders from the repository alone. Removed, per the decision and rule 4:
`ledger_plan.py` (the v1.0 buckets, lanes, gates and estimate), the GAP burn-down and its trend series, the battery, the
A.1/A.3/A.4 posture, the suite counts, `landing_measure.landing_rate_input` and `headline` (their only reader was the
ledger), and their CSS. The self-tests move to `scripts/selftests/spec/gen_ledger/`: each edition's verdict on the page
equals `edition_status.verdict` for the same parts, and the page reads COMPLETE for an edition only when all four parts are
measured and zero (81 combinations).

At the branch head every edition is NOT COMPLETE: 1985 by 166 open notes and 840 paragraphs in no row; 2002 by 1,173 GAP,
704 code reads and 328 notes; 2023 by 4 GAP and 518 notes; 2014 by its unheld inventory and 256 notes. This entry also
carries the edition_status branch's own (its pending entry is folded in here).
