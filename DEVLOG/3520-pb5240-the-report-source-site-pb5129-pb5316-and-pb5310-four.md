## Entry 3520 — 2026-11-11 18:32 PST — PB5240 the report SOURCE site, PB5129, PB5316 and PB5310: four 1985 edition edges of identifier-1, the report entry formats and the sum counter reference

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

### PB5310: at 85 a data-division clause may name a sum counter

At --std 85 a working-storage `OCCURS 1 TO 5 DEPENDING ON KTOT`, KTOT a CF FINAL sum counter, was refused COBOL5092 under
§2023:13.8.6.2.3, the section rule ISO/IEC 1989:2002 introduced (§2002:13.7.5.1.3). 1985's rule is §1985:XIII.3.9.3 SR10d
alone ("Data-name-1 may be referenced only if the entry defines a sum counter"). `ReportSectionBinder.SumCounterOperand85`
resolves the name among the sum counters below 2002, and `OdoResolvePass` and `DataBinder.ResolveClauseOperand` ask it before
the refusal, so the counter's register is data-name-1 and each clause's own rules judge it. The register's place is the
engine's: `IPlaceFacts.SumCounterPlaceOf` makes the one place builder answer a `ReportSumCounterPlace`, and the place table
holds the registers. The SR18 GLOBAL test reads the counter's report (`DataBinder.SumCounterReportOf`), and at 85 the refusal
of a non-counter entry cites SR10d. Goldens: `XIII.3.9@1985-REPORT-GROUP-DESCRIPTION-ENTRY/pb5310_sum_counter_occurs_depending_object`
(85: `[ABC] 3`, then `[ABCDE] 5` after a GENERATE adds 2 to the counter), `13.8.6.2.3-Report-Items/pb5310-sum-counter-as-occurs-depending-object`
(refused from 2002).

Branch review round 1 (R1-1) found `SumCounterOperand85` re-deriving the sum-counter name lookup without its scope step
(§1985:X.1.3.8.2 GR3a): a nested program's own counter KTOT and its container's GLOBAL report's KTOT were reported ambiguous
(COBOL2145, then COBOL5092). The lookup now lives once, `ReportScope.SumCountersNamed` (qualifier walk, current-report filter,
nearest declaring program), read by `CounterResolver.SumCounterFamilyFor` and `SumCounterOperand85`. The sweep found a sibling:
`DataBinder.SumCounterReportOf` and `SumCounterRegisters` read only the unit's DECLARED reports, so a nested program naming its
container's GLOBAL counter at 85 generated C# naming a field it does not have (CS0103); both now read `ReportScope.Visible`.
Goldens: `pb5310_nested_sum_counter_hides_global` and `pb5310_nested_global_sum_counter_odo_object` (85: `[ABCD]` then `[AB]`).

### Gate reds of the first gate (000001), fixed at the root

`EditionGateArmDriftTests`: report-source-expression-2002 now fires from the parse arm (expressions) and the bound arm (a
sole function by its bound category); both tables record why. `pb3969_format1_numeric_function_value` passed function
values BY CONTENT at 85, which §1985:X.5.2.3 SR4 forbids: it is accepted from 2002. Gate 000002 then found the three new
binder members missing from `binder-member-readers.json` (BinderMemberReadersDriftTests); they are listed with their
measured readers.

Self-review: `SoleOperand.TemporaryIdentifier` overlaps `ConditionBinder.SoleOoIdentifier` (OO forms only; left, both read
`SolePrimary`-shaped descents); `DataBinder.SumCounterPlaceOf` is asked on every place build and answers a non-register by
one pattern test. Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance up to date (run 000002-20261112T014116Z of
wf_16f7f780-4b5-2, 24,170 cases) · Unit 40,773/40,773 ran · Characterization up to date (run 000002-20261112T014116Z of
wf_16f7f780-4b5-2, 37 cases) (skipped 0) … (1 class(es) first; run 000003-20261112T015254Z) ===`.

