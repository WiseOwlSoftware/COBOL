## PB4606 — a refused or surplus EVALUATE object binds its succeeding bare operand too

PB4446 made a Table 15 refusal (COBOL1634) and an SR2 count breach (COBOL2106) stop hiding the faults in a selection
object's own operands, by binding them standalone (`ConditionBinder.BindPartialOperandsStandalone`). That walk still
returned on a `PrimaryCondition` whose `comparisonExpression` has one child, on the reading that such an operand
"belongs to the inserted subject's relation". It does belong to that relation (ISO §8.8.4.12.4 GR1), but with the pair
refused there is no subject to build the relation with, and nothing else binds the operand, so it is a reference the
program wrote that nobody resolved.

**Re-probe (branch build, before the fix).** `EVALUATE TRUE WHEN > 1 AND UNDEFX` drew COBOL1634 alone, `WHEN > 1 AND
RELF (1)` COBOL1634 alone, `NUMERIC AND UNDEFX` COBOL1634 alone, and the surplus `EVALUATE N WHEN 1 ALSO > 1 AND UNDEFX`
COBOL2106 alone. `IF`, `PERFORM UNTIL` and `SEARCH WHEN` bind through `BindCondition`, which has no such early return,
and each drew COBOL1639: the other two sites PB4446 listed as unprobed are clean.

**Fix.** The early return is replaced by `BindBareOperandStandalone`. The operand is classified by the one
`AnalyzeBareOperand`, because a level-88 condition-name, a switch-status condition-name and a boolean operand are
conditions or operands the analysis has already bound, and a class-name has no operand (it tests the selection subject
the refused pair has none to give); only a plain value is bound again, as the comparison operand the inserted subject's
relation would have read. An address-identifier binds through `ComparisonOperand`. The relation is not built, so the SR7 a)
checkpoint and the TRUE/FALSE cascade stay suppressed: `WHEN > 1 AND C88` still draws the one COBOL1634.

**Goldens.** `pb4606-evaluate-refused-pair-bare-operand-undefined-name` (COBOL1639) and
`pb4606-evaluate-surplus-object-bare-operand-subscript` (COBOL2096), each refused at 85, 2002, 2014 and 2023; eleven
refused-pair rows and two surplus rows in `EvaluateObjectOperandFaultTests` pin the exact code sets. The design's D5a
paragraph names the bare-operand arm.

Rows re-verdicted: none (the note claimed none).
