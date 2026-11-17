**2026-11-17: the sibling, the SUM clause's RESET ON operand (branch review R1-1), and the sweep of clause XIII.**
The review found the same defect one clause over. At `--std 85`, `SUM WS-AMT RESET ON WS-KEY OF WS-GRP` compiled
(probe `E:/COBOL-coord/scratch/my-20261115/w20262097/review-w20262097-t/resetq.cob`, exit 0). The RESET operand is
data-name-2 of `§1985:XIII.3.19.2`. `§1985:XIII.3.19.3 SR4` says only "Data-name-2 must be one of the data-names
specified in the CONTROL clause for this report" and gives no permission to qualify, so `§1985:III.2` bars the
qualifier. `§2002:13.16.52.2 SR7`, `§2014:13.18.54.2 SR8` and `§2023:13.18.54.3 SR8` each add "may be qualified and
reference modified".

Fixed in the same gate: `VersionConformancePass.ParseArm.VisitReportSumReset` asks the RESET operand through
`GateCobol85ControlName` with `mayBeQualified: false`. The construct row is renamed
`report-control-name-qualification-2002` and now covers both operands. The RESET operand's reference modifier now also
draws `report-control-reference-modification-2002`; before, it was refused only through its reference-modified CONTROL
operand (kb/Work/PB5573/2026-11-15-reset-on-correction.md).

**The sweep: every data-name or identifier position of clause XIII (cite.py --check OK on each):**
- CONTROL clause data-name-1: may be qualified (`§1985:XIII.3.7.3 SR1`, "Data-name-1 may be qualified"). Not gated.
- TYPE CH / CF data-name-1, data-name-2: no permission (`§1985:XIII.3.20.3 SR4`). Gated (this note).
- SUM RESET ON data-name-2: no permission (`§1985:XIII.3.19.3 SR4`). Gated (this file).
- SUM UPON data-name-1: "Data-name-1 may be qualified by a report-name" (`§1985:XIII.3.19.3 SR2`). Not gated. A detail
  group is an 01 entry of its report, so a report-name is the only qualifier that can name it.
- GENERATE data-name-1: "Data-name-1 must name a type detail report group and may be qualified by a report-name"
  (`§1985:XIII.4.3.3 SR1`). Not gated, for the same reason.
- INITIATE and TERMINATE: report-name-1 (`§1985:XIII.4.4.3 SR1`, `§1985:XIII.4.7.3 SR1`), a report, not a data-name.
  Nothing to ask.
- SUM identifier-1 (`§1985:XIII.3.19.3 SR1`), SOURCE identifier-1 (`§1985:XIII.3.18.3 SR1`) and USE BEFORE REPORTING
  identifier-1 (`§1985:XIII.4.9.3 SR2`): each is an identifier, which by `§1985:III.2` "consists of a data-name,
  together with its qualifiers, subscripts, and reference-modifier, as required for uniqueness of reference". Not gated.

Cases at 85 (outcome derived first): `conformance:cases/XIII.3.19@1985-SUM-CLAUSE/pb5910-sum-reset-qualified-85`
(refused, COBOL0900 at the RESET operand) and `pb5910_reset_plain_qualified_control_85`. The second is legal: a
qualified CONTROL operand, a plain RESET operand, UPON D1 OF R1, GENERATE D1 OF R1, and a qualified SUM addend and
SOURCE operand. It prints `CF 1 1 +03`, `CF 1 2 +06` and `CF 2 2 +03` (`§1985:XIII.3.19.4 GR1`, `GR9c` and `GR12`;
`§1985:XIII.3.20.4 GR6` and `GR13a`).

Rows: `1985-FMT-XIII.3.19.2` re-recorded CONFORMS with the refusal and its witnesses. `1985-SR-XIII.3.19.3-4`,
`1985-SR-XIII.3.19.3-2` and `1985-SR-XIII.4.3.3-1` gain the new witnesses. No row besides the two FMT rows rested on
the qualified forms: `pb5423_reset_on_qualified_control` writes its RESET operand plain.
