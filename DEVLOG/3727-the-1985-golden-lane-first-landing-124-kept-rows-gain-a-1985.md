## Entry 3727 — 2026-11-15 23:00 PST — The 1985 golden lane, first landing: 124 KEPT rows gain a 1985 case of their own, 1985 GAP 780 to 656

The gl1985 lane writes a new conformance case, accepted at 85 only and citing the row's own 1985 catalog id, for each
KEPT 1985 row that kb/Work PB5675 left without an own-edition witness and that PB5730's widening cannot reach. This
landing integrates 16 of the lane's 40 input files (waves 1 and 2). They cover the USAGE, SIGN, SYNCHRONIZED, OBJECT-
and SOURCE-COMPUTER and SPECIAL-NAMES entries, language structure and computer-independent data description, ACCEPT,
DISPLAY, CALL, CANCEL and READ, the report writer's LINE NUMBER and report subdivisions, and the REM function.

**The shape.** There were 136 rows: GR 89, SR 35, FMT 6, AR 4 and RV 2. By why the widening failed: the target's cases
cite rules 1985 lacks, 63; the target has no suite case, 33; the only case is a diagnosis at 85, 34;
refusal-not-evidence, 4; mixed, 2. The writers drafted all 136. The refuters overturned 12 verdicts:
does-not-exercise-rule 8, not-spec-derived 2, citation 1 and expected-value 1. The fixers repaired all 12. Five of the
repairs turned a golden row into a row with no golden. No writer reported a suspected compiler defect, and none
deferred a row.

**What landed.** There are 92 new cases in fixed form: 86 accepted at [85] and 6 refusals with `refuse-at: ["85"]`.
Each was run on this branch's build at `--std 85` with stdin closed:
- 85 accepted cases equal their `.out` byte for byte once CR LF is read as LF.
- g85_f07_07 equals its `.out` under the harness's normalization only: its DISPLAY lines carry trailing spaces.
- Each refusal was refused with its sidecar's code at the line its comment names.
- No PROGRAM-ID (`G85…`) or test id (`g85_…`) collided with the suite or with another draft.
- Every record's case names the row's own id in its `rules`.
- The one refusal-only witness, 1985-SR-VI.4.5.3-5, is for a diagnosable row.

**Records.** `record_verdicts.py --edition 1985` took 124 witness-only `{"rule-id","test-ref"}` records:
`rows changed: 124`, `witnesses: +130 test-ref`, `GAP: 844 → 720` on the branch's base. kb/Work PB5730's widening
landed while this branch gated, and it closed 64 other rows. After merging it, `inventory_view.py --summary` reads
`1985 2274 rules · 656 GAP (614 inherited, no own-edition witness · 14 never adjudicated · 28 open)`, against main's
780. `lineage_reads.py --check 1985` is GREEN, with 758 KEPT rows holding a witness of their own. `witness-batch
--edition 1985` wrote 0 records before and after each merge. No name collided with PB5730's cases.

**Not closed (12 rows).**
- 1985-GR-VI.6.5.4-1 to -5 (ACCEPT GR1-GR5): the harness runs every case with stdin closed, so no case can transfer
  data. kb/Work PB5755 (defect, tooling) records what a declared stdin needs. The ACCEPT rows of every edition are in
  the same position.
- 1985-GR-VI.6.10.4-10: the antecedent cannot be populated. Decision PB5756.
- 1985-GR-VI.6.17.4-7: the rule declares the result undefined. Decision PB5757.
- 1985-GR-XIII.3.21.4-1, 1985-GR-VI.5.13.4-3 and -4: every program meets the rule, given SR3 with GR4 for the first and
  DOC-A.1-197 for the other two. Decision PB5758. Each of the three decisions leaves its rows to the registrar, as own
  1985 verdicts on DRV records.
- 1985-GR-XIII.3.11.4-3 and -4 (COLUMN NUMBER: spaces fill the print line; the leftmost position is column 1). The
  fixer withdrew g85_f01_05 and called the rows closable only through a determination of the report file's record
  structure. That determination exists, `docs/conformance/7/DOC-A.1-159.md`, and
  `pb690_report_line_byte_image_85` already reads a report file back at 85. So the rows go back to the lane for a
  redraft on DOC-A.1-159, with no decision note.

