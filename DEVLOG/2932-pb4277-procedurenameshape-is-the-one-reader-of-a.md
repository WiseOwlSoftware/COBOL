## Entry 2932 — 2026-11-01 01:34 PST — PB4277 — ProcedureNameShape is the one reader of a procedureName node, and a drift test says so

PB4238 gave the grammar's `procedureName` node (`word ((OF | IN) word)?`, ISO §8.4.2.2.2 qualification) one reader,
`ProcedureNameShape`, and wrote that nothing else reads it. Three consumers still called `GetText()`, which glues the
words of a qualified name (`P1 OF S1` reads `P1OFS1`).

**Re-probe (Debug build, 0/0).** `GO TO DP1 OF D1` from the nondeclarative portion printed `GO TO 'DP1OFD1': 'DP1OFD1' is
a procedure of declarative section 'D1'` (COBOL2376, §14.9.49.3 SR4); `GO TO MAIN-P OF S1` inside a declarative printed
`'MAIN-POFS1'` (COBOL2375, SR3); `RESUME AT DP2 OF D1` printed `'DP2OFD1'` (COBOL0714, §14.9.33.3 SR3). All three
reproduced, so the note stood.

**Fix.** `ProcedureNameShape.AsWritten(pn)` is the name as the program wrote it (`Head` + `QualifierAsWritten`).
`ResolveProcedureOperand` passes it to `CheckDeclarativesBoundary` and to the COBOL1639 report (which built the same string
inline), `HeaderName` uses it for its COBOL5716 message instead of `pn.GetChild(0).GetText()`, and `EcBinder`'s RESUME AT
message uses it. After the fix the same programs print `DP1 OF D1`, `MAIN-P OF S1` and `DP2 OF D1`. No code is added or changed
and no behavior other than the text of those three messages differs.

**Drift test.** `ProcedureNameReaderDriftTests` parses every compiler source and refuses `GetText()` on a value that is a
`ProcedureNameContext` (a typed parameter, local, foreach or pattern variable, a variable bound from `.procedureName(...)`,
an element of one, or the accessor call itself) anywhere but `ProcedureNameShape.cs`. A planted source proves the scan sees
ten forms of the read and ignores comments, strings and other contexts. The scan found nothing else in the compiler
(the Frontend's `procedureName()` readers only count nodes). `ProcedureNameMessageTests` pins the three messages. No
conformance golden: the rules are unchanged and the conformance suite compares codes and output, not message text.

**Not closed.** PB4252 (the glued `GetText()` class across the other contexts) and PB4267 (EVALUATE, OO, MOVE, CALL, SET
messages) stay open. PB4277 claims no inventory row.

