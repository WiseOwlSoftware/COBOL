## PB3600 - the CONSTANT RECORD clause is a 2014 introduction, not 2002

**Re-probe.** The note's repro (the row's own `KCR` source) compiled clean at `--std 2002` on the base tree
(`c1c9b6d3a`) and drew COBOL0900 only at 85, so the defect stood. The archived 2002 texts (FCD 1989:2001 and the
2001-12-04 post-ballot text) have no CONSTANT RECORD clause, the 2009 CD has it at clause 13.18.15, and Annex E.2 item 10
of the 2023 text (`cite.py --check E.2 "where previously external items could not be strongly typed"` is OK) shows the
2014 standard carried it.

**Fix.** The registry row `constant-record-2002` became `constant-record-2014` with `introducedIn` 2014; the id carried
the edge, so every caller changed in the same commit: `scripts/gen-constructs.ps1` regenerated `ConstructRegistry.g.cs`
and `Constructs.g.cs`, `VersionConformancePass.VisitConstantRecordClause` asks `Constructs.ConstantRecord2014`, and the
grammar and gate comments say 2014. The 0900 message now reads "requires COBOL-2014 (targeting COBOL-2002)". The version
matrix runs the row's own source below 2014 on its own, so the 85 and 2002 cells are covered without a new test class.
VCR row 7.48 documents the edge with its sources and carries the `gate:constant-record-2014` anchor, and the generated
status index lists it (the clause had no VCR row before).

**Edition-gate sweep.** Nine accepted goldens use the clause in code (`constant_record`, `l1c04_constant_record_ws_ls`,
`pb1233_constant_record_initialize_content`, `pb881_constant_record_sending_roles`, `pb1273_property_subject`,
`pb3129_type_in_constant_record_ok`, `pb551_constant_record_value_content`, `pb1418_function_argument_manner`,
`_unassigned/pb1262_constant_record_fixed_table`). Each was `accept: [2002]`; each now says `[2014, 2023]` after a
re-run at 2014 and 2023 matched its `.out` at both. 27 negatives refused the clause's own rules (SR code) at 2002; below
2014 the refusal is COBOL0900, so `2002` left their `refuse-at`. The `_unassigned/constant-record-at-85` negative moved
to `13.18.15-CONSTANT-RECORD/constant-record-below-2014`, cites FMT-13.18.15.2 and is refused at 85 and 2002 with the
2014 message. Two C# tests compiled the clause at 2002 (`SameAsTests.SameAsOfConstantRecord_Rejected1556`, three
`ValueClauseScreenTests` cases); they now compile at 2014 or at `WithConstantRecord = [2014, 2023]`.

**Register.** Twelve inventory rows that are rules of the clause (`FMT-13.18.15.2`, `GR-13.18.15.4-1`,
`SR-13.18.15.3-1/-2`, `SR-13.16.3-13`, `SR-13.18.49.3-10`, `SR-13.18.60.3-4`, `SR-13.18.63.3-25`, `SR-14.9.23.3-20/-22`,
`SR-13.18.40.3-32`, `SR-13.18.42.3-5`) moved from `editions` 2002,2014,2023 to 2014,2023 through one `record_verdicts`
batch (no verdict changed, GAP 1 to 1). Four of them restated stale prose (the old row id and "COBOL-2002 introduction").
PB3600 is landed with `closes_rows: []` and its reason; its sibling edges are PB3602's.

**Self-review.** Architecture: one gate row, one parse-arm home, no second copy of the edge. Full code: the arm
implements the cited introduction rule; the placement SRs (COBOL1548/1549) stay bind-time and have no subject below
2014, as the 27 refuse-at changes record. Performance: none (a registry constant). Duplication: the two generated files
are regenerated, never edited.
