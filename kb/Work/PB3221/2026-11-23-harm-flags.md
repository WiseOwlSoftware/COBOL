# PB3221, 2026-11-23: the note's harm flags contradict its own Landed section

`kb/Work/PB3221.md` carries `wrong_answer: false`, `under_rejects: false`, `rejects_legal_source: false` and `process_only: true`. Its Landed section
(and the implementer's report, E:/COBOL-coord/scratch/reports/w20262145c-PB3221-report.md) records the opposite: the change was NOT output-neutral. The
re-probe showed two live defects the unification fixed:

- **rejects_legal_source: true.** `PIC LLLPPV EDITING L FOR NEGATIVE IS "-"` was refused with COBOL1935 although it is legal source, and now renders -300
  as ` -3`.
- **wrong_answer: true** (and silent: true). `PIC LLPP99` bound at scale 0 and `MOVE -5` rendered `  00`; it is now refused with COBOL1934 (§2023:13.18.40.3 SR16).
  The mis-binding is also an **under-rejection** of the form (`under_rejects: true`), refused only after the change.

**Correct flags for the note:** `wrong_answer: true`, `silent: true`, `under_rejects: true`, `rejects_legal_source: true`, `process_only: false`;
`kind` stays `analysis` and `status` stays `landed`. The flags describe the defects the unification fixed along the way, so a reader of the register
who filters by harm finds PB3221's goldens (`pb3221_picture_p_beside_extended_float_2023`, `pb3221-picture-p-between-extended-float`).

**Why this file and not an edit.** The note is a landed record; a later finding on a filed note is a new file beside it (CLAUDE.md rule 8). The
owner of the note's next touch should apply the five flags above to `PB3221.md`'s frontmatter in the same change that moves it, and re-run
`work.py check`.

The one new defect the same probe found, a second floating string of a different extended character, is PB6116.
