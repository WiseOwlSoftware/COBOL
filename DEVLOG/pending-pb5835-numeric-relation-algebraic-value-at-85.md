## PB5835: at --std 85 a numeric relation compares algebraic values, not binary64 images

Re-probe on this build (`relbin.cob`, `run_probe.py --std 85`): `S2 SQRT(4) EQ 2.00000000000000001` and `A1 ATAN NOT LT 1.57079632679489662` reproduced, as the note said.

Cause: `ConditionRenderer.RenderNumericRelational` compared in binary64 whenever either operand was a float and the arithmetic was not standard-decimal, on the authority of the native-arithmetic comparison rule (§2023:8.8.4.2.4; §2002:8.8.4.1.1.3). That rule is a 2002 text. §1985:VI.6.3.1.1.1 knows only "a comparison is made with respect to the algebraic value of the operands".

Fix: the native arm now requires `DialectLevel >= 2002`; at 85 a float operand takes the existing decimal lane (its exact value against the exact literal). After: S1 LT, S2 NOT EQ, A1 LT at 85; 2002 output is unchanged (native compare). Sibling sweep: `RenderFigurativeRelational`'s float-against-ZERO arm is the only other native compare of a relation; a double against zero orders identically to its exact value, so it needs no edition arm.

Witness: `NumericRelationAlgebraicValueAt85Tests` (xUnit, Conformance project). The 1985 catalog holds no row for the rule, and a conformance-suite case must cite one, so the registrar owes that row. Citations checked with `cite.py --check`.
