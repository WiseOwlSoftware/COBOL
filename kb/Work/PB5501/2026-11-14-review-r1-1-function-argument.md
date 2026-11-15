## Review R1-1 answered: an arithmetic expression inside a function argument (wave 20262038 group A, 2026-11-14)

**Re-probe (this tree, before the fix).** `E:\COBOL-coord\scratch\w20262038a\probe\f\f1.cob` at `--std 85`:
`IF FUNCTION INTEGER (B / Z) > 1` (Z = 0) ended the run unit, `abnormal run-unit termination: EC-SIZE-ZERO-DIVIDE (fatal)`,
exit 1. Reproduced: `SizeErrorGuarded` asked only whether an operand's own top node was a `BoundBinary`, `BoundPower` or
`BoundNegate`.

**Fix.** `ConditionRenderer.ArithmeticReach`, a visitor over the generated `IBoundExprVisitor<bool>` and
`IBoundOperandVisitor<bool>`, answers whether an operand's evaluation performs an arithmetic operation anywhere: the operand
itself, or an argument of a function it calls, at any depth. `SizeErrorGuarded` asks it. After: `NOTGT`, `AFTER1`, `NGT`,
`AFTER2` (a logical NOT outside the leaf is true).

**GnuCOBOL (R1-1 LEAD).** GnuCOBOL 3.2.0 under WSL, `cobc -std=cobol85`, on the golden and on
`E:\COBOL-coord\scratch\w20262038a\probe\f\h.cob`: the program continues, but the truth values follow no rule (the not-a-number
quotient compares like an unbounded value of the dividend's sign, yet `B / Z * 0 = 0` and `M ** 0.5 < 0` are true, and
`Z ** 0 = 1` is true because it computes 0 ** 0 as 1 against §1985:VI.6.2.3 GR5a). DOC-A.1-70 records it; the determination stands.

**Witnesses.** Golden `pb5501_size_error_in_condition_continues` line 8; four new rows of
`SizeErrorDispositionTests.Cobol85_SizeErrorInACondition_ContinuesWithTheLeafFalse`.

**Sites:** `src/WiseOwl.Cobol.Compiler/CodeGen/Emit/ConditionRenderer.cs` (`SizeErrorGuarded`, `ArithmeticReach`),
`docs/conformance/7/DOC-A.1-70.md`.

**Left (separate mechanism).** A reference modifier's arithmetic expression is hoisted to a temporary before the statement
(`StatementBinder.SubscriptSegments`), so `IF X (B / Z : 1) = "A"` still ends the run unit at 85; DOC-A.1-70 already says a
1985 reference-modifier size error terminates. Registered as a lead in the wave report.
