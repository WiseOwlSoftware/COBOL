## PB5240 the report SOURCE site, PB5129 and PB5316: three 1985 edition edges of identifier-1 and the report entry formats

### PB5240: a function-identifier is SOURCE identifier-1

The note's CALL, CANCEL and KEY sites landed in wave 20262001; the report SOURCE site stayed open. Re-probed on 59ee14452c,
`05 COLUMN 1 PIC X(5) SOURCE FUNCTION UPPER-CASE(WS-T).` exits 65 at --std 85 (COBOL2141, COBOL0844, COBOL0900) and at
--std 2023 (COBOL2141, COBOL0844): `ReportSectionBinder`'s one operand classifier returned only a bare data reference, so a
sole function was arithmetic-expression-1 whose entry is not numeric. A function-identifier is an identifier
(§2023:8.4.3.1.2); §2023:15.2 lets a function "be used anywhere a sending data item of that class and category may be
specified", and §1985:IV.4.3.8.3.3 SR3 admits an alphanumeric one wherever an identifier is.

`ReportSectionBinder.SourceIdentifierOf` now decides SOURCE identifier-1: the bare data reference, or the new
`SoleOperand.TemporaryIdentifier` (a function-identifier, inline method invocation, object-view or SELF / SUPER as the
whole operand). SR3's numeric entry and SR7's parentheses follow it; `FieldReferenceSource.Ref` carries the identifier as
written, and `ReportWriterBinder.SourceIdentifierOperand` binds it as the GR1 implicit MOVE's sender, so SR2's MOVE
validity screens it; a rolled SUM adds it only when its result is numeric. SUM keeps the data-reference classifier: an
addend is added. At 85 a numeric function "may be used only in an arithmetic expression" (§1985:IV.4.3.8.3.3 SR4), which
the 85 SOURCE clause does not take, and its category is a fact of the bind (MAX takes its arguments'), so the parse arm
leaves a sole function to `VersionConformancePass.GateReportSourceFunctions`, which keeps COBOL0900 for a numeric one.
ISO/IEC 1989:2002 Annex F.2 item 127 lists the function-identifier SOURCE operand as a 2002 change; the annex is
informative and no 1985 SOURCE rule prohibits a function, so the normative 1985 text decides. Goldens:
`13.18.53-SOURCE/pb5240_source_function_identifier` (85-2023, `HELLO olleh ELL CD`), `pb5240-source-numeric-function-85`.

### PB5129: a function-identifier CALL USING argument at 85

At --std 85 `CALL ... USING BY CONTENT FUNCTION UPPER-CASE (WS-A)` compiled and passed the function value; Amendment 1's
§1985:X.5.2.3 SR4 says "Identifier-2 must not be a function-identifier", and the keyword-less spelling was refused only
through the 2023 storage-section rule (COBOL1677). 2002 has no such rule (§2023:14.9.4.4 GR8), so the edge is a construct
row, `call-function-identifier-argument-2002` (COBOL0900), asked by `CallBinder.FunctionIdentifierArgumentOf` in the
up-front judgement of a COBOL-85 CALL's arguments beside `Format2OnlySpellingOf`; the refused argument is skipped, so it
draws one diagnostic. Goldens: `14.9.4-CALL/pb5129_call_by_content_function_identifier` (2002-2023),
`pb5129-call-function-identifier-85`.

### PB5316: the 1985 level arm of the report group entry formats

At --std 85 `01 DL TYPE IS DETAIL LINE PLUS 1 COLUMN 1 PIC S99 SIGN LEADING SEPARATE SOURCE WS-A.` compiled and printed.
§1985:XIII.3.9.3 SR3 puts format 2 at levels 02-48 and format 3 at 02-49, and SR4a makes a group's first entry format 1,
which prints none of the format 3 clauses. `ScreenReportEntryFormats` now refuses (COBOL2247) a level 01 entry writing
any of them, naming the clauses, and a format 2 entry at level 49 under SR3 alone. Goldens in
`13.15-Report-group-description`: `pb5316_level_49_format_3` (85-2023, prints `-03`), two refusals at 85 and the
level-01 entry's 2002-and-later legal twin.
