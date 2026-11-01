## Entry 2955 — 2026-11-01 11:36 PST — PB4363 — USE FOR DEBUGGING resolves its operand before it names it: ALL on a procedure is COBOL5848, an undeclared word is COBOL1639, a qualified procedure-name is a subject

**Mechanism.** `ProcedureTableBuilder.FinalizeDebug` read the operand of `USE FOR DEBUGGING` by its parse shape and
never looked the name up. PB4237 had made `ALL identifier` parse at `--std 85`, and its ALL arm printed COBOL1571 ("the
data-item subject ... recognized but not modeled") for whatever followed ALL; the bare arm printed the file / cd / data
text for whatever it could not find as a procedure. So a program that was wrong (a name nothing declares) or that named a
different kind of subject (a paragraph) was told the compiler was incomplete, and PB4237's own test pinned that message for
`ALL P1`.

**Re-probe (f0e4650e5, Debug build, `--std 85`, WITH DEBUGGING MODE).** `ALL P1`, `ALL MAIN` (a section) and `ALL NOSUCH`
each drew COBOL1571 as filed. The sibling arm did the same for `ON NOSUCH`, and refused the legal qualified procedure-name
`ON P1 OF MAIN` with the file / cd / data text and the qualifier dropped from the message (the bare arm resolved a
procedure only when the operand had no suffix at all).

**Fix.** Both arms resolve first. `ResolveDebugProcedure` is the one place a USE FOR DEBUGGING operand is read as a
procedure-name (`word` or `word OF section`, ISO 14.4.1, 8.4.2.2.2 Format 4) and hands it to the 8.4.2.2 algorithm already
used for every procedure-name; `NamesDataItem` asks the reference resolver's silent probe; `NamesFile` the file table.
In the ALL arm: a data item stays COBOL1571; a paragraph or section is the new COBOL5848 (a procedure-name identifies no
data item, 14.4.1 with 8.4.2.1); anything else goes to the one reference resolver, which reports COBOL1639 "not defined" or
the qualifier text for a declared item under the wrong qualifier (8.4.2.2.3 SR1). The bare arm: procedure first (now
including `word OF section`), then a data item or file (COBOL1571), then the same resolver. If the resolver says nothing the
arm falls back to COBOL1571 rather than let an operand through in silence.

**Tests.** `DebugFacilityTests`: the `ALL P1` row left `AllIdentifier_EverySpelling_At85_IsTheStagedDataItemOperand` for
`AllOperandNamingAProcedure_At85_IsNoDataItem` (four spellings including a section and `P1 OF MAIN`);
`OperandNamingNothing_At85_IsTheUndefinedReference` (five); `BareOperandNamingADataItemOrFile_At85_StaysStaged`;
`QualifiedProcedureNameSubject_At85_DebugsOnlyTheQualifiedParagraph` (SEC-A and SEC-B both hold PARA-W, only SEC-A's is the
subject, `HITS=1`). Goldens `pb4363_debug_qualified_procedure` (accept 85) and two refusals at 85,
`pb4363-debug-all-procedure-name` (COBOL5848) and `pb4363-debug-all-undeclared` (COBOL1639). `docs/CONFORMANCE.md`
D-DEBUG-USE carries the determination.

**Rows.** None: the debug module has no ISO 2023 rule and so no inventory row (VCR Table 7 row 7.17).

**Observed, not this mechanism.** `ON ALL RETURN-CODE` now draws COBOL1639 "not defined", the answer any other reference to
`RETURN-CODE` gets from the one reference resolver (the binder does not declare it as an item).

**Review round 1.** The reviewer found that `NamesDataItem` asked `Refs.Probe`, which applies the reference-shape screens, so
an unsubscripted table element (the form CCVS DB201A writes, `ON B-LEVEL-1 ... ` and `ALL REFERENCES OF AB1 OF A1` over OCCURS
items) was judged no data item and drew COBOL2270 "shall be subscripted" instead of the staged COBOL1571: the inverse of the
misdiagnosis this entry fixes. `NamesDataItem` now asks `Refs.ProbeDeclaration`, the silent declaration lookup with no shape
screen (DB201A: ten COBOL1571, no COBOL2270). A lead in the same file is also fixed: a paragraph under a qualifier that is not
its section (`P1 OF NOSEC`) is reported through `RejectProcedureName` ("names no procedure") instead of the data resolver's
"no declaration gives the name P1". Tests `OperandNamingAnUnsubscriptedTableElement_At85_StaysStaged` (4) and
`ProcedureNameUnderTheWrongQualifier_At85_NamesNoProcedure` (3).

