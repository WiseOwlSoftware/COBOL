## Entry 2668 — 2026-10-27 13:22 PDT — PB3760: a parenthesized numeric literal is an expression in every activation lane; the bound operand's kind states GR8's answer

**The defect.** `CALL "X" AS NESTED USING BY CONTENT (3)` into a plain-group formal compiled and stored `3`, and `(-3)` stored `L`
(the overpunched -3). `Gr8ArgumentLiteral.NumericText` recovered a literal from a computed operand so that `BY VALUE -1.234E-5`
(which the expression binder takes as `BoundNegate(BoundNumLiteral)`) read as a literal, and it could not tell that spelling from
`(3)`: the expression binder drops grouping parentheses. Its two readers, `ActivationDescriptions.OfValue` (the §14.8.2 conformance
description) and `CallEmitter.ArgCarrierText`, took the recovery for "a written literal".

**Re-probe and siblings** (fresh build, `E:/COBOL-coord/scratch/w20261720e/`): the repro reproduced. The INVOKE lane passed bind on
the same recovery and then crashed the code generator (`InvalidOperationException` in `OoEmitter.EmitInstanceInvoke`); a user-defined
function's `FUNCTION F((3))` into a group formal stored 3, because the function-argument binder maps a bound `(3)` back to the literal
through `IntrinsicBinder.OperandOf`. Found on the way: a keyword-less `(7)` against a BY VALUE formal was refused COBOL1687, the
arithmetic-expression arm being hard-coded BY CONTENT where §14.9.4.4 GR9 b) assumes BY VALUE.

**The rules** (`cite.py --check` OK each): §14.9.4.4 8) and §14.9.23.4 4), an argument "that consists merely of a single identifier or
literal"; §8.8.1.1, "an arithmetic expression enclosed in parentheses"; §8.3.3.3.2 2), a sign is the literal's leftmost character;
§14.8.2.2 2), BY CONTENT into a group formal is a MOVE question, and an expression is no MOVE sender; §8.4.3.2.4 5) b), a function
argument that "is a literal, an arithmetic expression" passes BY CONTENT; §14.9.4.4 9) b).

**The fix: one statement, by the binder.** `Gr8ArgumentLiteral` is deleted with both readers' arms. The CALL BY VALUE arm binds the
parse tree's sole numeric literal (sign included) as a `BoundNumericLiteral`, as the BY CONTENT and keyword-less arms already did, so a
`BoundComputedOperand` is now always an expression or a function value (`BoundCallArg` documents the invariant). The function-prototype
activation argument (`IntrinsicBinder.BindArgOperand`, whose flag `nullAdmitting` is renamed `activationArgument` because it now
decides two things only that position has) binds a sole written literal as the literal and a parenthesized or compound argument through
`ConditionBinder.CompoundOperand`, PB3678's CALL door. The keyword-less expression takes GR9's mode (`Gr9NonIdentifierMode`, renamed
from `Gr9BareLiteralMode` since it serves every non-identifier shape) and SR22's class screen. INVOKE needed no binder change.

**Goldens:** `cases/14.9.4-CALL/pb3760_parenthesized_literal_group_formal_refused` (COBOL1688), its INVOKE (COBOL0828) and
user-defined function (COBOL2470) siblings, `cases/14.9.4-CALL/pb3760_literal_and_expression_arguments` (BY VALUE -1.5E-3, (3), a
keyword-less (7), `- 5`, BY CONTENT (3) and `FUNCTION F((3))` into numeric formals) and its `_below_2002` refusal. Drift pin
`GroupMoveSenderChannelDriftTests.WrittenLiteral_IsTheOperandKind_NoReaderRecoversOneFromAnExpression`. docs/CONFORMANCE.md D-B4 and
docs/INTERPROGRAM_DESIGN.md updated. Rows GR-14.9.4.4-8/-9, GR-8.4.3.2.4-5 and GR-14.9.23.4-4 (already CONFORMS) gain the witnesses;
GAP 31 → 31.

**Self-review** (architecture, code, performance, duplication): one producer per fact, no bound-tree shape asked downstream; the UDF
leg binds once (no second bind of the argument); no allocation added on an emitted path. Registered, not fixed: a Format-1 CALL's
receiver-less expression or numeric function value reaching a numeric formal arrives as zero (`(1.25 + 0)` under --permissive,
`FUNCTION MAX(W 0)` strict), pre-existing on main.

**Gate:** run `000001-20261027T173638Z` was RED on two drift tests (`ParenTokenTwinDriftTests` wanted the new grouping-paren test
marked GROUPING-PAREN-ONLY; `SpecTraceabilityInventoryDriftTests` found GR-14.9.4.4-9 still citing the renamed `Gr9BareLiteralMode`,
re-sited by a second record_verdicts batch); run `000002-20261027T175125Z`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN` (Conformance 1,400 · Unit 6,384 · Characterization 36 in leg 1; Linux half (WSL clone of HEAD) GREEN), every
pb3760 golden and the drift pin among them.

**Review round 1 (R1-1, BLOCKING) — the parse-tree reduction still unwrapped grouping parentheses.** A LATER argument's B-operator
sends `boolExprAhead()` over the whole statement, so an earlier argument is parsed in a boolean-expression node, and
`ConditionBinder.ReduceArgument` unwrapped that node's `( booleanExpression )` factor to its bare operand. Re-probed on the branch
build: `CALL ... USING BY CONTENT (3) BY CONTENT B1V B-AND B2V` into a group formal stored `3     `, the INVOKE twin likewise, and the
CONFORMING `CALL ... AS NESTED USING (N) B1V B-AND B2V` passed N BY REFERENCE, so the callee's `MOVE 77` reached N, where §14.9.4.4
9) a) 2. assumes BY CONTENT for an argument that is no identifier (`N AFTER 77`; without the B-AND `N AFTER 05`). Two siblings
showed on the way: `("AB") B1V B-AND B2V` passed the literal "AB" (without the B-AND it is a syntax error), and Format 1's screen did
not count a literal the boolean node held. Fix at the one reduction: it stops at a grouping parenthesis (`SoleFactor` is the node
both readings walk; `UnwrapBareBool` keeps crossing them for conditions, where §8.8.4.2.1 makes them immaterial). The parenthesized
arithmetic operand is `ArgumentOperand.Grouped`, with no sole literal or reference, bound as the computed operand by every lane (CALL
BY CONTENT, keyword-less with GR9's mode, INVOKE, the inline form, the NULL receiver's screen); a parenthesized nonnumeric operand
stays the boolean expression it is written as (§8.8.2) and is refused as a boolean operand (COBOL1511). Goldens:
`pb3760_grouped_argument_before_boolean_argument` (run: (N) and ((N)) stay 05, the bare N becomes 77, CALL and INVOKE),
`pb3760_grouped_literal_before_boolean_argument_refused` (COBOL1688), `pb3760_invoke_grouped_literal_before_boolean_argument_refused`
(COBOL0828), `pb3760_grouped_nonnumeric_literal_argument_refused` (COBOL1511). Batch `batch-pb3760-r1.json`: 4 rows, +7 test-ref,
GAP 31 → 31.
Round-1 gate, run `000001-20261027T185043Z` at `e59657dfb`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg
2 left to CI's whole-population run` (Conformance 6,941 · Unit 36,616 · Characterization 36 in leg 1; Linux half (WSL clone of HEAD)
GREEN), the four new goldens at every accepting and refusing edition among them.

