## 2026-11-20 — the step-1 tool's round-1 review answered (branch ledger-per-edition, with kb/Work PB5994)

The figures of the addendum `2026-11-20-measured-1985.md` are superseded. The review of the edition_status branch
(`E:/COBOL-coord/scratch/ledger-rewrite/review-edition-status.jsonl`) found five things, all fixed on the branch
`ledger-per-edition`:

- **R1-1** part 3 read frontmatter only: 24 open notes with three or more body mentions of 1985 (PB4996, PB5692, PB5449)
  were not counted. `touches` now reads the body and the addenda; 1985's open notes went from 93 to 164 on the same tree
  (166 with PB5994 and PB5995). The structural fix, a declared `editions:` field, is kb/Work PB5995.
- **R1-2** part 4 counted deontic sentences only. It now counts kb/Work PB479's unit: **840 printed paragraphs** of the
  normative region (Sections IV, VI-XVI, A) in no catalog row at or under their locator, in 495 numbered paragraphs, 420
  of which have no row at all; none is discounted until PB5452 records dispositions. The deontic sentences among them,
  **128**, are a labelled floor (198 before: the region then included Sections I and II and coverage was global).
  PB479's own method counted 871 paragraphs.
- **R1-3** the paragraph walk was a copy of `rule_catalog_1985.parse`; parse now records `Catalog.paragraphs` and the
  copy is deleted. A committed row whose locator is no walked paragraph refuses the measurement.
- **R1-4** coverage is scoped to the rows at or under the paragraph's locator; 19 uncovered paragraphs are a row of
  another clause, reported as a lead and still counted.
- **R1-5** the self-test runs the real `inventory_part`/`code_reads_part` (2014 not measured, its verdict pinned) and an
  unreadable register through `_loud`.

Verdict at the branch head: `=== EDITION 1985: NOT COMPLETE — 166 open work notes · 840 catalog prose holes ===`.
