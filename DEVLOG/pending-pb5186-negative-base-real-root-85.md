## PB5186: at --std 85 a negative base raised to a non-integer exponent has its real result

**The defect (kb/Work PB5186).** §1985:VI.6.2.3 GR5c raises the size error condition only "If no real number exists as the
result of the evaluation". `CobolIntrinsics.CheckPowRule6`, the one screen every native `**` reaches, applied the later rule
(§2023:8.8.1.2 GR6c, first in §2002:8.8.1.1 and listed in §2002:F.1 item 14) at every edition: a negative base with a
non-integer exponent was always the size error. Re-probed on this branch's base build at `--std 85`: `COMPUTE R ROUNDED = NB **
0.2 ON SIZE ERROR` with NB = -32 displayed `SIZE ERROR` and left R at 77.00, and `IF NB ** 0.2 < 0` ended the run unit with a
fatal EC-SIZE-EXPONENTIATION (exit 1), where 1985 defines the value -2, the real fifth root of -32.

**The fix.** A new edition behaviour, `DialectBehavior.NegativeBaseRequiresIntegerExponent` (changed in 2002; VCR row 7.67, both
edges quoted), is read once by `ExpressionBinder.BindPower` into `BoundPower.NegativeBaseRealRoots`. Whether a real result exists
is a property of the exponent's EXACT value, which a binary64 cannot carry (0.2 is not 1/5), so at 85 `NumericRenderer.Power`
hands the exponent to the runtime as a `CobolExact`: `CobolIntrinsics.PowNativeReal(double, CobolExact)` and its checked twin on the
native arm, and `CobolExact.PowReal`'s new `negativeBaseRealRoots` argument on the exact-operand arm (a nested selection such as
`FUNCTION MAX (NB N40) ** 0.6`). `CobolBig.HasOddDenominator` reduces `U × 10^−s`: q is odd when 2^s divides U, and the power is
`±|b|^(p/q)`, negative for an odd p; an even q is the size error, cited to §1985:VI.6.2.3 GR5c. The zero-base rule is common to the
editions and stays `CheckPowRule6`'s, and the two checked entries now share one range check (`InExponentiationRange`). The
standard-arithmetic `CobolDec.Pow` is unreachable at 85 (no ARITHMETIC clause), the integer-exponent arm (`CobolExact.Pow`) never
meets the rule, and no compile-time fold of a power with a non-integer exponent exists (`IntrinsicArgumentRules` reads integer
exponents only): that is the whole sibling sweep. The BigInteger work lives in `CobolBig`, the one file the semgrep ban exempts.

**Goldens.** `VI.6.2@1985-ARITHMETIC-EXPRESSIONS/pb5186_negative_base_real_root_85` (85): (-32) ** 0.2 = -2, ** 0.4 = 4,
** -0.2 = -0.5, (-4) ** 0.5 the SIZE ERROR with R unchanged, a nested product -6, ** 1.0 = -32, MAX (-32 -40) ** 0.6 = -8, and
the condition true. `8.8.1.2-Native-standard-binary-and-standard-decimal/pb5186_negative_base_integer_exponent_2002` (2002 2014
2023): the same powers take the phrase. `NegativeBaseRealRootTests` reaches every arm of the reduction (scale above, at and
below zero, a zero and a negative exponent, an SDIDI value, a float item's content, the checked entry and the exact arm).

**Register.** The 1985 row 1985-GR-VI.6.2.3-5 is re-verdicted PARTIAL to CONFORMS (`record_verdicts --edition 1985`, GAP 302 to
301); PB5186 is landed with `closes_rows: []` (work.py refuses a 1985 rule id there, PB5094). NUMERIC_DESIGN §D19 gains (b′).

**Found on the way, not fixed (a different mechanism).** At `--std 85` a size error with no ON SIZE ERROR phrase ends the run unit
for EC-SIZE-EXPONENTIATION (`COMPUTE R = Z ** 0`) and EC-SIZE-ZERO-DIVIDE (`COMPUTE R = 5 / Z`), where §1985:VI.6.4.2 makes the
resultant undefined and the program continues; the implementer's report carries it for the registrar.
