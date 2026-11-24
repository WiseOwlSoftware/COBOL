# PB5702 addendum (2026-11-24): the note's title and harm flags describe the GR4b defect that landed, not the GR4c work that stays open

**Source:** `E:/COBOL-coord/scratch/reports/w20262163c-PB5702-report.md`, Review findings R2-1 (LEAD, low, not blocking at round 2).

`kb/Work/PB5702.md` keeps the title and harm flags of the GR4b defect its branch fixed ("... is accepted", `under_rejects: true`,
`rejects_legal_source: false`). What stays open is GR4c (addendum `2026-11-24-gr4c-fold-is-the-2014-table.md`: 2002 folds with the
2014 table, 86 pairs of which are absent from 2002 Annex D, so XU+2160Y and XU+2170Y are one name where 2002 has two), which refuses
legal source. The frontmatter no longer describes the open defect. The 2002 fold needs a design (an edition-keyed comparer, or a
spelling no table folds) and a transcription of 2002 Annex D checked against the printed pages, so it is not a clerk change; whoever
takes the GR4c work retitles the note and sets `rejects_legal_source: true` in the same change.

Related leads filed as their own notes from the same reports: the stand-in spelling in printed diagnostics (PB6176) and the
`NotPermitted` arm's 2023 citation at 2002 (PB6175).
