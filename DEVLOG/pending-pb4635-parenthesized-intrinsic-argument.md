## PB4635: a parenthesized item or literal is an arithmetic expression to the intrinsic argument screens

`FUNCTION FIND-STRING(X9 "ABC" (I2))` compiled and printed 0007, although §15.37.3 3) asks argument-3 for "an integer data item or
integer literal" and `(I2)` is an arithmetic expression enclosed in parentheses (§8.8.1.1, §8.8.1.2 1)). Re-probe on this build
reproduced it. The expression binder drops grouping parentheses, so `(I2)` bound to I2's `BoundNumRef` and `(3)` to the literal, and
`IntrinsicBinder.BindArgOperandCore` mapped them through `OperandOf` to a field or literal operand that the
`DataItemOrLiteralOnly` predicate (FIND-STRING argument-3, BASECONVERT arguments 2 and 3) admitted.

Fix: the intrinsic argument binder asks the written shape (`SoleOperand.Unparenthesized`, as the function-prototype activation arm
has since PB3760) and wraps a parenthesized sole item or literal as the `BoundComputedOperand` it is. The predicate now refuses
it, COBOL1627; numeric positions keep evaluating the parenthesized form (`ABS((I2))`, `MOD((7) (I2))`). A parenthesized
alphanumeric item as a string-function argument (`TRIM((X9))`, `LENGTH((X9))`) is refused too, by its own class rule, because it
is an arithmetic expression.

Goldens: `pb4635_parenthesized_numeric_arguments` (accept 2023), negatives `pb4635-findstr-parenthesized-item` and
`pb4635-baseconvert-parenthesized-literal`. No inventory row re-verdicted.

Review round 1 found two blockers in the first wrap. (R1-1) It bound the parenthesized item with the function-argument context,
which suspends the section 8.8.1.1 class screen, so `ABS((X9))` over a PIC X item digit-decoded (and so did a numeric-edited, group,
index, pointer or boolean item). The binder now binds every argument that is not a sole unparenthesized operand through the screened
`BindExpr`; the same change refuses the older `ABS(X9 + 1)`, which digit-decoded under STRICT before this branch. (R1-2) The wrapped
operand answered false to `IsIntegerOperand`, so `ABS((I2))` resolved NUMERIC (`PERFORM FUNCTION MAX((I2) 1) TIMES` became COBOL1646,
`DISPLAY FUNCTION ABS((I2))` printed 000000002). `IntrinsicResultType.IsIntegerArgument` reads a parenthesized sole integer item or
literal as the always-integer arithmetic expression it is (section 15.3 item 6); the shared `IsIntegerOperand` is untouched so the
relation screen and PERFORM TIMES still refuse a parenthesized item (PB1935). (Round 1 answered the DISPLAY image of `ABS((I2))` as
implementor-defined and left it at `ABS(I2 + 0)`'s 000000002; round 2 overturned that, below.) Also fixed: the comment quote in the
BASECONVERT negative and the stale `BindArgOperand` summary. New goldens: `pb4635-abs-parenthesized-alphanumeric`,
`pb4635-abs-compound-alphanumeric`, and INTTYPE/TIMES lines in `pb4635_parenthesized_numeric_arguments`.

Review round 2 found three more blockers, all of the same shape: the first two rounds treated a parenthesized item as an arithmetic
expression everywhere, and three readers needed the parentheses to be transparent. (R2-1) `INTEGER-OF-BOOLEAN((BB))` over a class-boolean
item was COBOL0844 (it compiled on main): the same written shape is also "a boolean expression enclosed in parentheses" (section 8.8.2),
the other alternative section 8.4.3.2.3 SR8 lists. A new operand context, `OperandContext.GroupedFunctionArgument`, binds a parenthesized
sole argument (the written shape asked through `SoleOperand.Unparenthesized`), and `OperandContextRules.Rules()` gained a fourth axis,
`BooleanOperandAdmitted`, so its class screen (`ExpressionBinder.ClassScreenAdmits`, both screen doors) admits class boolean beside
numeric (round 3 below makes it the boolean-expression operand 8.8.2 calls it). `ABS((BB) + 1)` is a compound arithmetic expression and is refused
(COBOL0844; main accepted it). (R2-2) The device image of `ABS((I3))` was 000000003 where main and GnuCOBOL 3.2.0 show `+03`: the
description of a selecting function's returned value is implementor-defined (section 15.4.1), CLAUDE.md rule 1 gives it to GnuCOBOL
(DOC-A.1-92), and GnuCOBOL's parser makes `(I3)` the identifier. `FunctionDeviceText.Field` now describes a parenthesized item or literal
as the item or literal (`BoundComputedOperand.EnclosedOperand`, the one reader of "the parentheses are transparent", also behind
`IntrinsicResultType.IsIntegerArgument`); a genuine expression keeps the value-described image. (R2-3) Under `--permissive`,
`TRIM((X9))` warned and then died with `NotImplementedCobolFeatureException`: the string-argument visitor had no arm for the wrapper.
Its coercion now renders the enclosed item, as the bare spelling did on main. Not fixed, outside this mechanism: a genuinely compound
numeric argument at a string position (`TRIM(I2 + 1)`) still reaches that loud arm under `--permissive` on main and here.
Goldens: `pb4635_parenthesized_boolean_argument` (accept 2023), `pb4635_parenthesized_argument_description` (accept 2014, 2023, each
parenthesized line beside its bare spelling), negative `pb4635-abs-boolean-arithmetic-expression`; unit test
`ParenthesizedIntrinsicArgumentPermissiveTests`; `OperandContextRulesTests` pins the fourth axis. Comment repairs: the stale "an argument
is NOT an arithmetic expression" statements (IntrinsicBinder, ExpressionBinder.BindFunctionArgumentExpr, IntrinsicArgumentRules) and the
ABS negative's 8.8.1.2 citation (the quote is in 8.8.1.1). Docs: DESIGN-binder-bound-tree.md 3.6 table gained axis D and its row.

Review round 3 found one blocker and two leads. (R3-1) The wrapper that made `(I3)` an arithmetic expression hid the item from the two
functions whose class kind admits class numeric: `CONCAT((I3) "A")` printed `0LA`, `CONCAT((F2) "A")` `25A`, `BASECONVERT((F2) 10 16)` `19` and
`BASECONVERT((I3) 10 16)` an empty string, with no diagnostic, where main and the bare spellings are COBOL1627 (15.18.3 r3) and COBOL1642
(15.12.3 r1). Before round 1 the same arguments died in the string visitor's loud arm; the round-2 arm that renders the enclosed item turned
that into a silent wrong answer. The root is that both rules open with a SHAPE ("data items or literals", "a usage display or national data
item or literal") that neither schema row declared: `CONCAT` was `Uniform('c')` and `BASECONVERT` argument-1 `UnscreenedKind`, owned by
`CheckBaseConvertArgs`, which reads only a field or literal operand. They now carry the `DataItemOrLiteralOnly` predicate (the CONCAT
tail, BASECONVERT position 1), the same one FIND-STRING argument-3 and the BASECONVERT bases carry, and the predicate's text names the
shape alone ("not a data item or literal"). `IntrinsicArgumentRules.IsCoercible` is the one answer to whether `--permissive` offers a
coercion for a predicate: a data-item rule never does, a data-item-or-literal rule does at an INTEGER position (the expression's value) and not
at a character-reading one (an expression has no character image), so the two new refusals are errors in every mode and the renderer's
enclosed-item arm now serves only the class-coercion sites (TRIM, UPPER-CASE). Every other position that says "data item or literal" is a
string position, where the numeric class of the wrapper is already refused (`REVERSE((I2))`, `FIND-STRING((I2) ...)`). (R3-3) A parenthesized
class-boolean item was returned as the field operand "to every reader", so `LENGTH((BB))`, `BYTE-LENGTH((BB))` and `CONCAT((BB) "A")` compiled
where `LENGTH((I2))` is refused. It is "a boolean expression enclosed in parentheses" (8.8.2), so the argument binder now returns the
`BoundBoolOperand` (over `BoundBoolRef`) that `BB B-AND BC` binds to; LENGTH and BYTE-LENGTH (15.50.3 r1 and 15.14.3 r1 list no boolean
expression) refuse it as they refuse that, and `INTEGER-OF-BOOLEAN((BB))`, `((BB))`, `(BT)`, `(BE(1))` and the ref-mod keep their values (swept
over REVERSE, UPPER-CASE, TRIM, ORD, NUMVAL, ABS, MAX, MIN, ORD-MAX, NATIONAL-OF, DISPLAY-OF: `(BB)` now answers exactly as `BB B-AND BC` at
each). CONCAT admits a boolean expression (15.3 type 3, the class list of 15.18.3 r1; the PB124 golden `pb124_boolean_argument_boundary` pins
`CONCAT(WS-A B1 B-AND B0)`), so `CONCAT((BB) "A")` stays legal and prints `1010A`; a first draft of this round refused a boolean expression at the
CONCAT shape predicate and that golden went red at the gate, so the predicate reads arithmetic (computed) operands only. (R3-2, answered no change) Under `--permissive`,
`MAX((X9) "A")` differs from `MAX(X9 "A")`: the parenthesized item is the arithmetic expression 8.8.1.1 calls it, whose alphanumeric operand
the COBOL0844 warning says is digit-decoded, and the second warning (COBOL1627) says the character literal then disagrees in class with a
numeric first argument. The string-image coercion of round 2 belongs to the positions that read only characters; a selecting function reads
either, and the strict edition refuses the program, so the warned permissive answer is the documented extension, not a divergence from a
rule. Goldens: `pb4635_unparenthesized_data_item_arguments` (accept 2023) guards the legal side, and the refusals
`pb4635-concat-parenthesized-item`, `pb4635-baseconvert-parenthesized-argument-1` and
`pb4635-length-parenthesized-boolean-item`, plus a `CONCAT((BB) "A")` line in `pb4635_parenthesized_boolean_argument`; the unit test `AParenthesizedItemAtACharacterReadingPosition_IsAnErrorEvenUnderPermissive` pins the
permissive half. Docs: INTRINSICS_DESIGN.md names the three rules the predicate serves and `IsCoercible`.

Finish review round 1 (R1-1, R1-2). Making the parenthesized boolean item a `BoundBoolOperand` (R3-3) hid it from `IntrinsicArgumentRules.StaticUsageOf`,
which answered null, and the three usage screens read null as "leave it to the runtime": `CONCAT((BT) "A")` and `CONCAT((BB) N"A")` (15.18.3 r2),
`BASECONVERT((BT) 10 16)` (15.12.3 r1) and `CONVERT((BT) ANUM NAT)`, `CONVERT((BB) NAT ANUM)` (15.19.3 r5, r6) compiled and printed where the bare spellings
are COBOL1627, COBOL1642 and COBOL1514. It was R3-1's shape again, a wrapper hiding the item from a rule that asks about the item. The fix is at the one
reader: `StaticUsageOf` answers the usage of the item a `BoundBoolRef` names, so every screen that rides it (and the codegen readers `RepresentationOfText`
and `RepresentationDiffersFromText`, which ask the same question of the same operand) sees it; a compound boolean expression still answers null. Under
`--permissive` the new refusals demote to the same COBOL1627 warning as the bare spelling. Five refusal goldens (one per probe above, 2023). Leads left for
the clerk, both pre-existing and unchanged on main: a boolean expression is admitted at CONCAT and BASECONVERT argument-1 though neither rule lists one,
and a parenthesized numeric or boolean function-identifier passes LENGTH and BYTE-LENGTH.
