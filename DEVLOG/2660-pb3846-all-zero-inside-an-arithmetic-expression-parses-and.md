## Entry 2660 — 2026-10-27 11:11 PDT — PB3846: ALL ZERO inside an arithmetic expression parses, and meets the same edition gate as ADD ALL ZERO

`COMPUTE X = 1 + ALL ZERO` drew a bare COBOL0001 at every edition (wave 20261720, group B). The re-probe on this branch's
base found the whole operand-position family failing the same way: `ALL ZERO + 1`, `1 + (ALL ZERO)`, `- ALL ZEROS`,
`2 ** ALL ZERO`, `IF X + ALL ZERO = 7`, `T(ALL ZERO + 1:2)` and `PERFORM VARYING ... BY ALL ZERO + 1`, with
`(ALL ZERO) + 1` a COBOL2269. Only the sole forms were right, because PB1415 and PB3830 had sent them through the one
numeric reading: `ADD ALL ZERO TO X` and `COMPUTE X = ALL ZERO` are accepted at 85 and refused COBOL0902 from 2002.

The cause sat in two places on the way in. `ZeroTokenRewriter` decides that a figurative ZERO is arithmetic by looking at
the tokens on each side. In `1 + ALL ZERO` the token on the left is ALL, so the ZERO stayed figurative. In `ALL ZERO + 1`
the ZERO was rewritten, but `primaryExpression` had only a bare `ZERO_ARITH` arm, so `ALL ZERO_ARITH` matched nothing.
The standard settles which reading is right. §8.3.3.6.2 Format 1 prints the figurative as `[ALL] ZERO`, with ALL not
underlined, so ALL is an optional word. §8.8.1.1 admits "the figurative constant ZERO (ZEROS, ZEROES)" as an arithmetic
operand. The ALL therefore belongs to the operand and is never its context.

The fix has three parts. The rewriter now reads the left-hand context past a written ALL. The grammar arm is now
`ALL? ZERO_ARITH`. And `ExpressionBinder.ZeroOperand` is now the one numeric reading of the figurative ZERO for both of
its parse forms: the sole figurative-constant node and the expression primary. When ALL was written it asks
`FigurativeAssociation.GateAllZeroAsNumeric`, the §8.3.3.6.3 SR1 a) gate the VALUE clause asks (VCR row 7.28). The
message spells the operand as written (`ZeroTokenRewriter.WrittenWord`), because a rewritten token's text is "0".

The sibling sweep found one new path the grammar arm opened. In a level-88 VALUE list such as `88 C VALUE ALL ZERO -5`,
the signed literal that follows the ZERO makes the rewriter mark it arithmetic. The operand now parsed, but its raw text
`ALL0` reached the C# backend as CS0103 'ALL0L'. `DataBinder.RewrittenAllZeroRawText` now reads that operand as the
written figurative, so the VALUE clause refuses it from 2002 exactly as it refuses `VALUE ALL ZERO`. The compile-time
evaluator already refused a ZERO_ARITH primary, so `CONSTANT AS 1 + ALL ZERO` (COBOL1547) and `>>IF V + ALL ZERO = 1`
(COBOL1619) are refused just as their bare-ZERO twins are. The sweep also turned up two problems outside this mechanism,
recorded as leads in the report rather than fixed here. `T(ZERO:2)` escapes the compile-time ref-mod range screen that
refuses `T(0:2)` (COBOL2670), and COBOL2119 spells a rewritten operand as `ALL0+1`.

The goldens are `pb3846_all_zero_in_expression_85` (ten legs at 85, one per operand position, the 88 list among them)
and the 2002-2023 COBOL0902 refusals `pb3846-all-zero-in-expression` and `pb3846-value-list-all-zero-signed`. Two unit
theories in `CobolLexerModeDriftTests` pin the rewrite at the token level, in both directions. SR-8.3.3.6.3-1 was
already CONFORMS; it gains five code-location and five test-ref witnesses and closes nothing. The rewriter paragraph of
DESIGN-frontend-grammar §7 said the pass was "still incomplete" for cases PB50 and PB51 had already fixed. It now
describes the current pass and the ALL rule.

Gate: see the report (leg 1 plus the Linux half).

