---
order: 12
---
> ⚖ **DETERMINATION — the §2023:8.8.1.5.4 r2e development of a non-integer (and past-loop-bound integer) power**
> (2026-08-29, REPLACED 2026-08-31 by owner decision D-C; kb/Work PB145 + PB167). r2e leaves the equivalent
> arithmetic expression to the implementor and constrains the DEVELOPMENT: "Operands used in the development of
> that value shall be in SDIDI form. All additions, subtractions, multiplications and divisions performed in the
> development of the result shall be performed in accordance with the corresponding rules in ISO/IEC 60559:2020."
> **WiseOwl COBOL's equivalent expression is `exp(operand-2 × ln operand-1)`, developed entirely on SDIDI operands —
> no binary64 anywhere in it** (`CobolDec.PowCore` / `Ln` / `LnReduced` / `Exponential`: the ln decomposition
> splits the decimal exponent, then takes the atanh series over one of TWO argument reductions — three exact
> square roots to m^(1/8) away from 1, and ln(1+δ) over δ = m − 1 directly inside |m − 1| ≤ ¼, where the roots
> would cancel the difference away; the exp reduction takes out `n × ln 10` so the `10^n` factor is an EXACT
> decimal exponent shift, then runs the Taylor series). **One chosen special case: at operand-2 = ½ the
> equivalent expression is `FUNCTION SQRT(operand-1)`**, whose standard-decimal value §2023:15.84.4 r2 fixes EXACTLY —
> so `b ** 0.5` and `FUNCTION SQRT(b)` agree digit-for-digit by construction rather than by luck. They disagreed
> before, from the 17th significant digit.
> ⛔ **r3 IS NOT PART OF THAT LATITUDE AND IS WRITTEN ONCE.** r2's latitude is scoped "When the value of
> operand-2 is greater than zero"; for a NEGATIVE operand-2 r3 fixes the result as the evaluation of
> `(1 / (operand-1 ** FUNCTION ABS (operand-2)))`, which under per-operation 34-digit rounding is a different
> value from `exp(-|p| × ln b)`. `CobolDec.Pow` reciprocates at the top, over the same development at
> |operand-2|, so every arm below is r3-correct by construction. One visible consequence: an out-of-range
> negative exponent takes its EC name from r3's own expression, because the operation that leaves the range is
> the INNER power — `2 ** -600000` is EC-SIZE-OVERFLOW and `0.5 ** -600000` EC-SIZE-UNDERFLOW (kb/Work PB266).
> ⚠ **What is claimed is an r2e-conforming SDIDI development, NOT 34 correct digits.** §2023:8.8.1.5.2 rounds every
> operation to 34 digits, so a development of N operations carries the accumulated rounding of all N. MEASURED
> (an independent 34-digit emulation of the development checked against 120-digit truth, not estimated):
> **≈33 correct significant digits** for a moderate exponent — 33.00–33.58 over the six
> `CobolDecPowDevelopmentTests` rows and 30.87 as the floor over a 400-case randomized in-range sweep (the
> binary64 predecessor reached ≈17) — **degrading as ≈ 33 − log₁₀|operand-2 × ln operand-1|** once that product
> passes 1 (32.15 digits at 7.1, 31.12 at 69, 30.11 at 693, 28.78 at 13 863). THAT driver is inherent to the
> equivalent expression: an exp development turns ABSOLUTE error in its argument into RELATIVE error in its
> result.
> ⛔ **This determination previously read "≈31 … degrading as |operand-2 × ln operand-1| grows and for a base
> very near 1 — … inherent to the equivalent expression", and the near-unit half of that was NOT inherent — it
> was this coding of it** (kb/Work PB269). The three-square-root reduction carried m^(1/8) at 34 SIGNIFICANT
> digits about 1, so `Sub(u, One)` cancelled away the whole of b−1: 17.05 correct digits at the worst point of
> |m−1| ≤ ¼, and EXACTLY ZERO at m = 1 + 10⁻³³, where `(1 + 10⁻³³) ** 10³⁰` answered a flat 1 against a true
> 1.0010005. The δ arm removes it — the same three cases now measure 32.4, 29.1 and 33.2 — so the near-unit
> driver is gone rather than reduced, and only the |operand-2 × ln operand-1| one remains.
> The range dispositions PB145 established are unchanged: the out-of-decimal128 directions carry their own
> §2023:8.8.1.5.2 r2 names (too large = EC-SIZE-OVERFLOW, too small = EC-SIZE-UNDERFLOW), and no SDIDI-range operand
> collapses to a silent zero or raises a spurious range error. The past-loop-bound INTEGER escape now enters the
> SAME development — it used to be a SECOND, different binary64 one, which also dropped the sign of a negative
> base raised to an odd exponent — **with the exponent OPERAND, never the loop-bound probe.** That probe clamps
> at `long.MaxValue`, and the escape used to rebuild the exponent from it, so every exponent past 2⁶³−1 was
> silently replaced by 9223372036854775807: `(1 + 10⁻³³) ** 1.0E+20` answered 1.0000000000000 where
> 1.0000000000001 is owed and `0.9999999999999999 ** 1.0E+25` answered a silent 0 where the value is below the
> decimal128 range and EC-SIZE-UNDERFLOW is owed (kb/Work PB267). ⚠ The disposition is NOT "a clamped exponent
> with |base| ≠ 1 is out of range": the closest SDIDI value to one is 1 ± 10⁻³³ and `(1 + 10⁻³³) ** 10²⁰` is in
> range, so that screen would be PB145's spurious size error again. GR-8.8.1.5.4-2/-3 are CONFORMS. Pinned by `2023/pb167_sdidi_exponentiation`,
> `2023/exp_standard_decimal_eae` and `CobolDecPowDevelopmentTests`, whose drift leg fails if any binary64
> transcendental returns to `CobolDec`.
> ⚠ **Consequence under INTERMEDIATE ROUNDING IS PROHIBITED, and it is the standard's:** §2023:11.9.11.2 rule 3 d —
> "If the PROHIBITED phrase is specified and an intermediate value cannot be represented exactly in SDIDI form,
> the EC-SIZE-TRUNCATION exception condition is set to exist and the results of the operation are undefined."
> The r2e development is a chain of inexact 34-digit operations over an irrational result, so under PROHIBITED a
> NON-INTEGER power now raises, exactly as `FUNCTION SQRT` already did for an inexact root. The binary64
> predecessor did NOT raise — it computed in IEEE double and entered the SDIDI through one conversion of the double,
> so the prohibition never saw the inexactness it exists to report. An INTEGER power still computes (r2a–r2d is
> exact multiplication). Pinned by `2023/pb167_sdidi_exponentiation_prohibited`.
