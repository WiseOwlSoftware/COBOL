## Entry 3125 — 2026-11-04 18:25 PST — The SELF, SUPER and NULL refusals of COBOL-2002 and later are reported at the word, not at the statement

kb/Work PB4652, a lead from PB4444's report. PB4444 moved `OoPredefinedSearchRoot`'s refusals of a bare SELF or SUPER to the
word, but its sweep stopped there. At COBOL-2002 and later `SET O TO SELF` (COBOL0867), `RAISE SELF` (COBOL0848) and the object
view (COBOL6002) still drew the statement's first column, the word sitting several columns to the right.

**Re-probe.** `probe.py 2002` in the wave scratch confirmed the three named sites and found the family behind them: `SET O TO
SUPER` (SR9), `RAISE SUPER` and `RAISE NULL` (SR2), SELF or SUPER as a relation operand (COBOL6002, COBOL2900), the object
view's SELF, SUPER or NULL identifier-1 (COBOL6002, COBOL2870), `INVOKE NULL "M" USING SELF` and an INVOKE argument SELF outside
a method, and `SET pointer TO SELF` or `SUPER` (COBOL0869, three arms for Formats 7, 8 and 9). All of them reported at the
statement.

**Root and fix.** One habit: a refusal about the SELF / SUPER / NULL word reported without moving the diagnostic cursor to
the word. Each of the arms above now opens `ctx.Edition.At(word)` around its report (`OoBinder.OoBindSelfOperand`,
`OoBindObjectView`, `OoBindSetObjectRef`, `OoScreenNullReceiverArgument`, `OoBindInvocationArg`, `EcBinder.BindRaise`, the three
`SetBinder` pointer arms). A refusal about the receiver (`SET N TO SELF`, SR8) is about another operand and stays where it was.

**Goldens.** Eight `pb4652-*` refusals pin `(line,column)` at 2002, 2014 and 2023; `pb388-set-object-ref-self-outside-method`
and `pb1425-super-operand` now pin theirs. The citations were checked with `cite.py --check` (§8.4.3.8.3 SR1 and SR3,
§14.9.29.3 SR2, §14.9.39.3 SR9, §8.4.3.5.3 SR1). No traceability row is claimed or closed.

