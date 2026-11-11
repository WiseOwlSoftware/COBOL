**Landing (2026-11-11, wave 20262001 group B): the CALL, CANCEL and KEY sites; the SOURCE site remains.**

Re-probed on the branch's base (e3c9e8551a): `CANCEL FUNCTION UPPER-CASE(N)` (c-func.cob), `ENABLE ... WITH KEY FUNCTION
UPPER-CASE("pw")` (k1) and `DISABLE ... KEY FUNCTION REVERSE(WS-K)` (k2) each exit 65 with COBOL5506 at FUNCTION, at 85 and
at 2023.

Fix. `callTarget`, `cancelTarget` and `communicationKeyPhrase` each take the one `sendingOperand` rule, so a function-identifier
and an inline method invocation are identifier-1 there (§2023:8.4.3.2.3 SR1 bars a function-identifier only from a receiving
operand; §1985:IV.4.3.8.3.3 SR3). `CallBinder.BindCall` and `BindCancel` bind such a target through `MoveBinder.SendingOperand`
and ask §2023:14.9.4.3 SR1 / §2023:14.9.5.3 SR1 of its class (`ComputedNameTargetRefusal`: alphanumeric or national, else
COBOL1681 / COBOL1696); `CommunicationBinder.BindPath` asks §1985:XIV.3.2.3 SR4 / §1985:XIV.3.3.3 SR4 (kb/Work PB5082).
Goldens: `cases/14.9.5-CANCEL/pb5240_call_cancel_function_identifier` (accepted at every edition: CALL and CANCEL through
UPPER-CASE and REVERSE, the counts prove each CANCEL reached the program), `pb5240-cancel-numeric-function` (refused at every
edition, COBOL1696), `cases/XIV.3.3@1985-ENABLE-STATEMENT/pb5240_key_function_identifier` (accepted at 85).

Still open: the report SOURCE site (`1985-FMT-XIII.3.18.2`, this note's 2026-11-11 addendum). `ReportSectionBinder.IdentifierOperandOf`
returns a bare data reference only, so a sole function operand is arithmetic-expression-1 (COBOL2141 and COBOL0844 at every
edition, COBOL0900 at 85 from `VersionConformancePass.VisitReportSourceClause`). Admitting an alphanumeric function as
identifier-1 there changes the SOURCE model: `FieldReferenceSource` carries a data reference that the procedure phase binds as
the implicit MOVE's sender, and the 85 edition arm reads the parse tree, where a function's category is not yet known. That is
a report-writer change of its own, outside this group's grammar and binder root, so it stays on this note.
