## ConditionBinder: the SR1 boolean-relation flag asks subject OR object, and a class condition's subject asks the binder's statement of an expression (PB4762, PB4763)

**PB4762.** The leading partial-expression (EVALUATE `WHEN = B2 AND <> B3`) seeded the section 8.8.4.12.3 SR1 carry from the
selection subject alone, where a written relation asks subject OR object. Re-probe: `EVALUATE ZERO WHEN = B2 AND <> B3` compiled
clean while `IF ZERO = B2 AND <> B3` drew COBOL2723, and the standalone walk of a refused pair (`EVALUATE TRUE WHEN = B2 AND <> B3`)
drew COBOL1634 alone. Fix: `BindAbbreviatedRelation` reports the object it bound and `BindPartialComparison` asks subject OR
object; the walk's leading arm seeds `ins.BooleanRelation` from its object. Both now draw COBOL2723.

**PB4763.** `IF (FUNCTION ABS(I2)) IS NUMERIC` and `IF (LINAGE-COUNTER) IS NUMERIC` compiled while `IF (I2) IS NUMERIC` was COBOL3318:
the screen read the tree's shape and the expression binder drops grouping parentheses. Fix: `BoundComputedOperand.ArithmeticExpression`,
set by `ConditionBinder.CompoundOperand` (the one wrapper of a parenthesized or compound operand), replaces the shape list; the sole
function-identifier and sole register stay admitted.

Goldens: four refusals (conformance-suite/wiseowl/diagnostics/pb4762-*, pb4763-*). Rows re-verdicted: none. Self-review lenses
(architecture, full code, performance, duplication): no finding; no new allocation on a hot path.
