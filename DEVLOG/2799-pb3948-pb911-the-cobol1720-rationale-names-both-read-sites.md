## Entry 2799 — 2026-10-30 12:30 PDT — PB3948, PB911: the COBOL1720 rationale names both READ sites, and the token that ends an empty required block is never deleted by the parser's recovery

Two diagnostic-quality notes of the DiagnosticCatalog group, fixed in order of harm.

**PB911 (empty imperative-statement in PERFORM).** The re-probe on this build did not reproduce the headline. `PERFORM
WHEN EC-ALL ...` (Format 3, empty imperative-statement-1) already drew `COBOL2072: cannot parse construct near 'WHEN'`
naming the PERFORM statement and §5.2.6.2 / §5.2.6.3, the same code and rule as `PERFORM UNTIL ... END-PERFORM`: kb/Work
PB3832 (`70be735c0`) had made the ATN detector read the parse at the offending token, and the detector was never a
per-format table. The re-probe found a different defect in the same mechanism, in both formats alike: with a statement
after the PERFORM (`PERFORM UNTIL X = 0 END-PERFORM STOP RUN.`, `PERFORM 3 TIMES END-PERFORM`, and every Format-3 WHEN,
WHEN OTHER, WHEN COMMON and FINALLY phrase with an empty body) the compile drew a SECOND, false `COBOL0001: unexpected
'.'` at the next sentence's period. Cause: ANTLR's single-token deletion removed the owner's END-PERFORM because the token
after it (STOP) begins a statement; STOP RUN became the PERFORM's body and the PERFORM, whose END-PERFORM is required,
ran to the period without one. IF, EVALUATE and SEARCH have optional terminators and hid it. Fix:
`CobolErrorStrategy.SingleTokenDeletion` declines when `OffendingPosition.EmptyBlockOwner()` names an owner at the
token, that is, when one statement written there would let the token follow (the owner's own terminator or next phrase).
The drift test `RequiredImperativeStatementDriftTests.EveryRuleThatCallsStatementBlock_HasAnEmptyBlockWitness` reads
the 11 rules that call `statementBlock` from the ATN and fails until each has an empty-block witness; the theory
`AnEmptyRequiredBlock_AtEveryRulePosition_DrawsExactlyOneCOBOL2072` compiles each witness at depth 0 and inside the open
`IF ... ELSE IF` nest and requires exactly the one COBOL2072 (14 of its 30 rows fail without the fix). Golden:
`conformance-suite/cases/14.9.28-PERFORM/pb911-perform-f3-empty-imperative-1.cob` (refused at 2023, the edition that
introduces Format 3). The recovery bullet of `DESIGN-frontend-grammar.md` §3.9 records the rule.

**PB3948 (COBOL1720 rationale).** The catalog text explained the READ INVALID KEY arm for sequential organization
only. It now names both sites (`SequentialIoBinder.BindRead`; `KeyedIoBinder.BindRead` for an INDEXED or RELATIVE file
that binds sequential, kb/Work PB3569) and quotes §14.9.30.4 GR19, §14.9.30.3 SR8 and SR9 and §12.4.5.5.2 SR2, each
checked with `cite.py --check`; the citation list gained SR8, SR9 and GR19. The stale quotation in
`StatementValidation.CheckReadKeyFormat`'s summary and in `docs/CONFORMANCE.md` was reworded to match.

Rows: none claimed, none closed (both notes carry `closes_rows_reason`). Both notes are `landed`.

