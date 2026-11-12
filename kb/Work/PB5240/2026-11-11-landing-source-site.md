**Landing (2026-11-11, wave 20262004 group B): the report SOURCE site; the note is landed.**

Re-probed on the branch's base (59ee14452c): `05 COLUMN 1 PIC X(5) SOURCE FUNCTION UPPER-CASE(WS-T).`
(`E:/COBOL-coord/scratch/coderead-85/out-pipe/work-slice-49/c06-fnalpha.cob`) exits 65 at --std 85 with COBOL2141,
COBOL0844 and COBOL0900, and at --std 2023 with COBOL2141 and COBOL0844.

Fix. A function-identifier, inline method invocation, object-view or SELF / SUPER written as the whole SOURCE operand is
identifier-1 (§2023:8.4.3.1.2; §2023:15.2: a function "may be used anywhere a sending data item of that class and category
may be specified"; §1985:IV.4.3.8.3.3 SR3). `ReportSectionBinder.SourceIdentifierOf` classifies it (the bare data
reference of `IdentifierOperandOf`, or `SoleOperand.TemporaryIdentifier`), so SR3's numeric entry and SR7's parentheses
are not asked of it, and `FieldReferenceSource.Ref` carries the identifier as written. `ReportWriterBinder.SourceIdentifierOperand`
binds it as the implicit MOVE's sender (§2023:13.18.53.4 GR1), so SR2's MOVE validity screens it; a rolled SUM adds it only
when its result is numeric (`NumberOf`). At 85 a numeric or integer function "may be used only in an arithmetic expression"
(§1985:IV.4.3.8.3.3 SR4), which the 85 SOURCE clause does not take: the parse arm leaves the sole function to
`VersionConformancePass.GateReportSourceFunctions`, which asks the bound category and keeps COBOL0900
(report-source-expression-2002) for a numeric one. SUM keeps `IdentifierOperandOf`: an addend is added, so a sole function
is arithmetic-expression-1 there.

Goldens: `cases/13.18.53-SOURCE/pb5240_source_function_identifier` (accepted at 85, 2002, 2014, 2023: UPPER-CASE, REVERSE,
a reference-modified UPPER-CASE and MAX of alphanumeric arguments print `HELLO olleh ELL CD`) and
`pb5240-source-numeric-function-85` (refused at 85, COBOL0900).

The 85 reading. ISO/IEC 1989:2002 Annex F.2 item 127 lists "SOURCE clause in a report description entry. The sending
operand may be a function-identifier." as a 2002 change. The annex is informative; the normative 1985 text with
Amendment 1 admits an alphanumeric function "anywhere in the general formats that an identifier is permitted and where the
rules associated with the general formats do not specifically prohibit reference to functions" (§1985:IV.4.3.8.3.3 SR3),
and §1985:XIII.3.18.3 prohibits none (SR1 says only where identifier-1 "may be defined"), so the 85 compiler admits it.
