## PB5015, PB5007: the SET absent-format label is pinned, and the VALIDATE facility's data clauses name COBOL-2002 at COBOL-85

Wave 20261936 group C, two notes on one root (edition-aware diagnostics around the SET format table and the
declined VALIDATE facility).

**PB5015: `SetFormatSelection.BraceOf`.** Its doc comment said a row outside the compiled edition is reached only after
its declaration was refused, so the "(a format first printed in COBOL-<year>, absent from this edition)" arm was
unreachable in practice. The re-probe showed it false twice. `ReportNotAdmitted` and `ReportNotSoleReceiver` name the
SELECTED row, and selection reads operand kinds, not the edition: `SET D N TO 5` over a dynamic-length item at
`--std 2014` (declarable there) selects Format 16 (2023), and `SET TC N TO 5` over a CAPACITY register at `--std 2002`
(the OCCURS DYNAMIC declaration refused COBOL0900, the item still bound) selects Format 14 (2014). The wording was
right. The comment now names the reaching arms and both witnesses. The new theory
`SetFormatSelectionDriftTests.ASelectedRowTheEditionLacks_IsLabelledWithItsIntroducingEdition` (6 cases) pins the label
below each row's introduction, and its absence at the introduction, on both report arms.

**PB5007: the validation clauses at COBOL-85.** At `--std 85`, `DEFAULT IS "AB"`, `VALIDATE-STATUS ...` and `VAL-STATUS ...`
drew COBOL5506 at the literal, and `PRESENT WHEN`, `CLASS`, `DESTINATION`, `INVALID WHEN` and `VARYING` drew COBOL1941.
The sibling VALUE format-5 tail (`88 C VALUE "Y" IS VALID`) drew COBOL1941 at IS. The cause was the two `{is2002()}?`
construct gates PB2291 recorded as D7 defects. They stayed because constructs.json had no row form for a construct
refused AT and after its introduction, and because they were believed to carry §2023:8.9 word status for DEFAULT,
PRESENT, VALID, VALIDATE-STATUS and VAL-STATUS. The token-level reservation gate (PB655, PB4958) has carried that word
status since: it retypes a DECLARED free word to a user-defined word.

The fix follows D7 and the VALIDATE statement's precedent (PB3148):
- Both predicates are deleted (the union grammar).
- `VersionConformancePass.ParseArm.VisitValidationClause` and `VisitValidateValidPhrase` call the new registry row
  `validation-clauses-2002`, so COBOL0900 names COBOL-2002 and the clause.
- `DeclinedFacilityPass` skips below that row's introduction, so a COBOL-85 program is not told about a decline of a
  facility its edition lacks.
- The version matrix gains the DECLINED row form, `declined: <code>`: the row compiles at no edition, its gate refuses
  it below the introduction and its decline from it. `VersionMatrixCatalogue.ExpectedRejection` is now the one statement
  of a reject cell's code, used by both matrix theories and `EditionGateArmDriftTests`.
- `EditionReadDriftTests` drops the two construct gates. One remains: `{is2023()}? applyCommitClause`, outside this
  group, reported as a lead.

Legal COBOL-85 names DEFAULT, PRESENT, VALID (a symbolic-character VALUE operand), VALIDATE-STATUS and VAL-STATUS, in
the entry-name slot, a KEY list and an INDEXED BY list, still compile and run (`ValidationClauseWords_AreLegalNamesAt85`).

Goldens: `cases/13.18.17-DEFAULT/pb5007-default-clause-below-2002` and
`cases/13.18.63-VALUE/pb5007-content-validation-below-2002` (refused at 85, COBOL0900). The theory
`DeclinedFacilityTests.ValidationClausesAt85_AreRefusedByTheirEdition_NotByTheDecline` has ten arms, one per clause word
and VALID/INVALID, each a single COBOL0900. Inventory: SR-13.18.11.3-1 keeps DOCUMENTED-NON-SUPPORT. A record_verdicts
batch retired its renamed witness and corrected its 85 note. No row is re-verdicted. Docs updated: CobolDeclined.g4's
header, DESIGN.md D7, VERSION_TEST_MATRIX_DESIGN.md, CONFORMANCE.md row A.4.14, and the COBOL1708 catalog text.
