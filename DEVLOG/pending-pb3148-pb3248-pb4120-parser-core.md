## PB3148, PB3248, PB4120 (+ PB4172), PB4168: the parser core's edition gate, reserved-word recovery and bare ZERO

Wave 20261780 group C, one root file (`CobolParserCore.g4` and the parse machinery around it). Each note was re-probed on this
tree's Debug build (0/0) before any change.

**PB3148 — VALIDATE at --std 85.** Re-probe: `VALIDATE WS-A.` at 85 drew `COBOL0307: cannot parse construct near 'WS-A'` (the
note recorded COBOL0001; the cause was the same). The statement arm was `{facilityWord("VALIDATE")}?`, a reservation predicate,
so at 85 (VALIDATE a §8.9 user word) the arm stood down and the parse died at the operand, where every other post-85 statement
parses at every edition and names its introduction. Fix: the arm is the union grammar (`{wordAhead("VALIDATE")}?`), the edition
pass names the introduction on recognition (`VisitValidateFacilityStatement`, new constructs.json row `validate-statement-2002`,
COBOL0900), and the binder's two copies of "below the introduction the facility is not this edition's to decline" (VALIDATE and
the MCS RECEIVE/SEND) became one method, `StatementBinder.BindDeclinedStatement`, so 85 gets COBOL0900 without a COBOL1580 beside
it. Sweep: COMMIT / ROLLBACK keep `facilityWord`, correctly: their statement is the bare verb, so below 2023 `COMMIT.` is a
paragraph header (probed). Golden: `cases/14.9.50-VALIDATE/pb3148-validate-statement-below-2002` (refused at 85, COBOL0900).

**PB3248 — one reserved-word reference, two diagnostics.** Re-probe: `DISPLAY RECEIVE.` drew COBOL0901 and `COBOL0001: unexpected
'.'` at 85, 2002 and 2023; the sweep showed every operand slot holding RECEIVE or SEND cascaded (`ADD RECEIVE TO N.` drew three),
while CONSTANT never did. Mechanism (traced, not the listener): RECEIVE and SEND begin a statement, so they are in every
statement-level recovery set, and `ConsumeUntil` resumed AT the word the report had just named; the statement loop parsed it as a
RECEIVE statement and refused the next token. Fix: `CobolErrorStrategy.ConsumeUntil` consumes the token the last report named when
the listener re-codes it to COBOL0901 (`IsReportedReservedUserWord`), as the operand it stood for. Witness:
`ReservedWordPositionConformanceTests.AReservedVerbWrittenAsAnOperand_DrawsOneDiagnostic` (nine rows, exactly one COBOL0901 each).
`PERFORM RECEIVE.` draws one diagnostic too but not a 0901 (the inline PERFORM's reading); reported as a lead.

**PB4120 (and PB4172, the same defect filed from the slot side) — a bare ZERO in an arithmetic-expression-only slot.** Re-probe:
`SET IX UP BY ZERO`, `SET IX TO ZERO`, `CONTINUE AFTER ZERO SECONDS`, `ALLOCATE ZERO CHARACTERS` each drew `COBOL0001: unexpected
'ZERO'`, legal by ISO §8.8.1.1 ("the figurative constant ZERO (ZEROS, ZEROES)"). Fix, derived from the grammar rather than a slot
list: `ZeroSignReading` (PB3981's parse-informed round in the one parse loop) now also puts a FIGURATIVE ZERO the parse refused on
trial as `ZERO_ARITH` and parses again, taking it back if refused at the ZERO again. R1-1 follows: `SET IX UP BY ZERO -1` is now
§8.7.1's COBOL2993, as `Y -1` is. `AdmittedKinds.Refusal` lost its PB3861 neighbour-test narrowing (a ZERO written wherever the
parse expects ZERO_ARITH now parses), so `SET IX TO "A"` names "the figurative constant ZERO"; `WrittenKindRefusalTests` and the
`pb3861-set-index-to-alphanumeric-literal` golden were re-pinned, and its control row "SET IX TO ALL ZERO is a COBOL0001" was
deleted (that source now parses and meets PB3846's ALL ZERO gate). `ZeroTokenRewriter.AZeroWrittenAtIsArithmetic` is private
again. Goldens: `cases/14.9.39-SET/pb4120-set-index-bare-zero` (85-2023, output C/B) and `pb4120-set-up-by-zero-touching-sign`
(COBOL2993); `ZeroSignReadingTests` pins the trial protocol and eight slots (SET x3, CONTINUE AFTER, ALLOCATE, RETRY, START LENGTH,
B-SHIFT-L).

**PB4168 — retired, not a defect.** The three probes' SEND lines are 74-83 characters long in fixed form, so column 72 cut
`END-SEND` to `END-SEN` and `EXCEPTION` to `EXCEPTIO`; `LINE-COUNTER` is two characters shorter, hence "parsed". Rewrapped inside
column 72 they draw the PB4118 diagnosis (COBOL2024). The real gap (a word cut by the margin is reported as an ordinary unexpected
token) is a lead.

No inventory row changed verdict (the claimed rows were none; the touched format rows already CONFORM). Codes COBOL5596-5598 were
not needed and are returned. Docs: DESIGN-frontend-grammar.md (the reservation gate's recovery, the ZERO round, `facilityWord`'s
scope), docs/CONFORMANCE.md A.4.14.
