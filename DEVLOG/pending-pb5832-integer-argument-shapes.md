## PB5832: an integer argument that is a product, an item quotient or holds a numeric function is refused at every edition

kb/Work PB5832 (the 1985 code reads, batch 9, mechanism 1). At `--std 85` an integer argument of an intrinsic function written as
`WN * 0.5`, `A / B` or `FUNCTION SQRT (N) + 64` compiled and was silently truncated: `FACTORIAL (WN * 0.5)` answered 1, `MOD (A / B, 3)`
answered 0, `CHAR (FUNCTION SQRT (N) + 64)` answered "@", `DATE-OF-INTEGER (FUNCTION SQRT (N) + 1)` answered 16010102. §1989:A.2.2 AR4
("An arithmetic expression which will always result in an integer value must be specified", cite.py OK) requires an expression that is
always an integer, and 1985 has no EC-ARGUMENT-FUNCTION run-time net to catch the value. The re-probe on this branch's base reproduced all
six at exit 0; the control `FACTORIAL (WN / 2)` was refused with COBOL1627.

**Root cause.** `IntrinsicArgumentRules.NotAlwaysIntegral` had one special arm for a quotient of a bare item by a literal at the root of
the argument, and its additive-spine walk (`CollectAdditive`) read only `+`, `-` and negation: a product, a quotient anywhere else and a
numeric function were opaque terms and the screen failed open. Three missing arms of one walk.

**Fix.** `CollectAdditive` carries a rational factor instead of a sign. `E * c`, `c * E` and `E / c` (c an exact literal fold, nonzero)
scale the spine, so an item's net coefficient is a rational and `ItemWitness` asks one question, whether `coefficient x 10^(-scale)` is a
whole number; this replaced the root quotient arm and also refuses `WN / 2 + 1` and `W-S * 2 + 65.5`, which had failed open the same way. A
term that is provably integral but sits under a fractional factor no longer counts as integral. The witness terms of PB5208 and PB5345 are
one mechanism now, `WitnessOf`: the power (at a unit coefficient only), a quotient of two numeric items that are not the same item and
cannot share storage (`StorageOverlap`), and SQRT, LOG, LOG10, EXP, SIN, COS, TAN or ATAN of a bare item of scale 0 or more. The
arms carry no edition switch: the standard's requirement is on the program at every edition and the screen still refuses only what has a
witness valuation (a REDEFINES pair, `A / A` and a table's two subscripts fail open).

**Goldens.** Refusals `pb5832-product-by-literal`, `-quotient-in-sum`, `-item-quotient`, `-numeric-function-term`,
`-date-of-integer-function-term` and `-day-of-integer-function-term` (85 2002 2014 2023), each compiled clean with the fix reverted and was
refused with it restored; the accepted complement `pb5832_scaled_integral_shapes` (eight always-integral shapes, outputs derived from
§2023:15.64.4 RV1). The two PB1526 goldens (`pb1526_integer_argument_has_integral_value`, `..._integrality_standard_decimal`) had used
`X / 2 + 1`, `X * 0.5` and `F * 1` as their fail-open shape at 2023; those are refused now, so they use `FUNCTION MAX (X, 0) / 2 + 1`
and the like (same values and output). Two rows of `IntegerArgumentConstantWitnessTests` moved from admitted to refused, and the new
admitted rows pin the cancelling shapes.

**Rows.** `1989-AR-A.2.2-4`, `1989-AR-A.2.12.3-1`, `1989-AR-A.2.13.3-1` PARTIAL to CONFORMS in the 1985 store (GAP 56 to 53), with every
earlier code-location and test-ref kept.

**Found on the way, not fixed.** `python scripts/semgrep/verify.py` fails on main already: it lists `IntrinsicArgumentRules.cs`,
`VersionConformancePass.cs` and `DiagnosticCatalog.cs` as files semgrep cannot read, unnamed in `WHOLE_FILE_UNREADABLE`; the same three
appear with `IntrinsicArgumentRules.cs` restored to the base.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 27,748/27,748 ran · Unit 41,097/41,097 ran · RepoDrift 175/175 ran · Characterization 37/37 ran (skipped 0)` at f17b8057b0 (run `000001-20261116T213014Z`); the 56 `pb5832` cases of `Conformance.trx` all Passed.
