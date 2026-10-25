## Entry 2555 — 2026-10-25 12:32 PDT — A parenthesized function or item is an arithmetic expression, whatever tree it binds to

PB3678 (a branch-review lead from PB3613). `BoundComputedOperand.IsNumericFunctionValue`, the one question every channel that sends a
value into a plain group asks, answered true for ANY numeric `BoundIntrinsicCall` and read the binder's `FunctionValue` flag only for other
trees. The expression binder drops grouping parentheses, so `(FUNCTION MIN(N 3))` binds to the same `BoundIntrinsicCall` the bare call does
and was accepted as a MOVE sender (`CALL ... AS NESTED USING BY CONTENT (FUNCTION MIN(N 3))` into a group formal stored 15), while
`(FUNCTION LENGTH(VG))`, whose tree is a `BoundBinary` sum, was refused COBOLNET1688: the verdict depended on how the intrinsic lowered.

**Re-probe.** Reproduced on a fresh build: MIN accepted, LENGTH(VG) refused, and `(N)` / `(+ N)` accepted too. The note's `MOVE (...)` spelling
does not parse (a MOVE sender is an identifier or a literal); the lanes are CALL AS NESTED (Format 2), Format-1 CALL (refuses any parenthesized
argument at bind, COBOLNET0899) and INVOKE (already refused all three).

**Spec.** The question the clerk left open: an argument that "consists merely of a single identifier or literal" is regarded as one
(§14.9.4.4 GR8); a parenthesized function is not, so it is an arithmetic expression, and an arithmetic expression is no MOVE sender, so
§14.8.2.2 rule 2 ("the same as for a MOVE statement with the argument as the sending operand") refuses it opposite a group formal, as
docs/CONFORMANCE.md D-B4 already said. One outcome for every tree: refused. (The note's `rejects_legal_source` premise was wrong: the LENGTH
refusal was right, the MIN acceptance the under-rejection.)

**Fix.** `IsNumericFunctionValue` is now `FunctionValue` plus the call's result category (a function-identifier of another class is no numeric
value), never the tree's shape. The sibling arm was the same defect one level up: `CallBinder.ExpressionOperand`'s last arm (what is left after
the sole-function, sole-OO-identifier and keyword-less-function arms) mapped a bound `(C)` back to the item `C` through `IntrinsicBinder.OperandOf`,
so `(N)` was sent to a group formal as N's storage image. It now wraps the expression as the computed operand it is, through
`ConditionBinder.CompoundOperand` (PB1935's rule for comparison and EVALUATE operands, made internal and shared rather than copied).

**Goldens.** `14.2-Procedure-division-structure/pb3678_parenthesized_function_group_formal_refused`, `..._length_group_formal_refused` and
`..._item_group_formal_refused` (COBOLNET1688 from 2002); `pb3678_parenthesized_function_numeric_formal` (the same parenthesized functions
accepted by a numeric formal, COMPUTE's rule 2 a), output `[03] [05] [03]`) with its negative `_below_2002` (COBOLNET0900). Drift pin:
`GroupMoveSenderChannelDriftTests.ParenthesizedFunction_IsAnExpression_TheTreeShapeIsNeverAsked`.

**Review round 1 (R1-1).** The first fix wrapped EVERY residue of `CallBinder.ExpressionOperand` as a computed operand, but a sole NUMERIC
constant-name has no sole-shape arm above the last one, so `CALL ... AS NESTED USING BY CONTENT K` (`01 K CONSTANT AS 42`) into a PIC X(4) formal became
a computed expression with no character image and aborted the run unit (NotImplementedCobolFeatureException, "computed expression in a string
context"); before the branch it stored `42  ` like the written literal. A constant-name is literal-1 (ISO 13.10.4 GR1), so the method now has two more
sole-shape arms ahead of the wrapper: a sole constant-name (the ONE alias resolution, `ExpressionBinder.LiteralAliasOf` / `AliasOperand`) and a sole
numeric literal (`NumericLiteralOperand`). Golden `pb3678_numeric_constant_by_content_moved_formal` (accept 2002/2014/2023; K and 42 both display
`[42  ]`) was red before; no earlier golden covered a numeric constant-name BY CONTENT.

**Left open.** A parenthesized numeric LITERAL, `(3)` or `(-3)`, is still taken for the literal: `Gr8ArgumentLiteral.NumericText` recovers a
literal from a computed operand for the BY VALUE `-1.234E-5` spelling and cannot tell a parenthesized one. A different mechanism, reported as a
lead. Rows re-verdicted: none (the note claimed 0). The ordered gate's verdict is in the branch report.

