## PB4446: a selection object's own faults are indicated beside the Table 15 refusal and the SR2 count

**Mechanism.** `EvaluateBinder.BindWhenItem` returned the constant false the moment `ScreenPairing` refused a pair
(ISO §14.9.13.3 SR10, Table 15, COBOL1634), before the selection object was bound, and `Bind`'s pairing loop clamped to
`min(objects, subjects)` after `ScreenObjectCount` reported the SR2 breach (COBOL2106), so the surplus objects were never
bound. Every fault in such an object's own reference went unreported: an undefined or ambiguous name (COBOL1639), a
subscript on a scalar (COBOL2096, §8.4.2.3.3 SR2), an unsubscripted table element (COBOL2270), reference-modification
position 0 (COBOL2670), an alphanumeric operand in an arithmetic expression (COBOL0844), a bad function argument
(COBOL1627) and a range whose ends differ in class (COBOL2107). The refusal is a fault of the PAIR and the count a
fault of the SET; the operand's references are independent violations of the general formats and syntax rules, and
§4.2.2 requires the warning mechanism to indicate them.

**Re-probe.** The no-88 control and the note's GLOBAL-88 repro both printed COBOL1634 alone on the build; so did the
identifier, arithmetic, function, range, partial-expression and condition rows; `WHEN TRUE ALSO UNDEFX` printed COBOL2106
alone. Nothing had been fixed since the adjudication.

**Fix.** `BindObjectOperandsStandalone` binds an object by its own Table 15 row with no subject, no relation checkpoint
and no TRUE/FALSE-as-condition, and drains the user-function activations it registers; `BindWhenItem` calls it on the
refused-pair path and the new `BindSurplusObject` calls it for each object past the subject count. The range arm's
operands, SR4/SR9 screen and IN phrase moved into one `BindRangeObject`, so the pair and the standalone path cannot ask
the range's rules differently. `ConditionBinder.BindPartialOperandsStandalone` walks the partial-expression spine and
binds the relational operators' objects and each complete condition a connective joins; a class or sign shape without
its identifier has no operand, and a bare operand belongs to the inserted subject's relation. The pair's secondary
checks stay suppressed: `EVALUATE TRUE WHEN 3`, `EVALUATE RELF WHEN TRUE` and `EVALUATE BW WHEN TRUE` still draw
COBOL1634 alone. The SUBJECT never needed it (its slot binds it once). `docs/CONDITIONS_EXCEPTIONS_DESIGN.md` §D5a and
§D5b describe the new shape.

**Goldens.** Three refusals under `conformance-suite/cases/14.9.13-EVALUATE/pb4446-*` (a refused pair's subscript on the
scalar that hides a container's GLOBAL 88, a refused range's undefined name, a surplus object's undefined name) and
`EvaluateObjectOperandFaultTests`, which pins the exact set of error codes of 24 statements across the arms, with the
cascade guards.

**Rows.** None claimed or closed. No diagnostic code was added.

**Self-review.** Architecture: one binding of a range's operands and screens (`BindRangeObject`) and one standalone path
for both callers; the partial-expression walk lives in `ConditionBinder` beside the splice it mirrors. Full code: each arm
binds what the pair's own arm binds of that object, and the SR7 a) checkpoint, the TRUE/FALSE-as-condition bind and the
subject splice stay out (the guards in the theory). Performance: the standalone path runs only on a failing compile.
Duplication: no second copy of the range rules.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 21,519/21,519 · Unit 39,772/39,772 · Characterization 37/37 cases ran
(skipped 0)` at `18e7da35e` (run 000001-20261104T191743Z); `semgrep/verify.py` PASS with no rule count raised.
