## Entry 2678 — 2026-10-27 16:35 PDT — PB3954: an exact quotient or square root on the SDIDI is held at the preferred exponent, not padded to 34 digits

**Mechanism.** `CobolDec.Div` pre-scales every numerator so the integer quotient has 34 to 36 digits, then rounds. An EXACT
quotient (`6 / 3`, `(1 + 2 + 3) / 3`) therefore came out as `2000...0E-33`, and `CobolDec.Sqrt` padded an exact root the same
way (`SQRT (4)` as `2000...0E-35`). ISO §8.8.1.5.3 performs division as ISO/IEC 60559:2020 5.4.1 does, and §15.84.4 r2 gives SQRT the
same definition; an exact result takes the representation nearest the preferred exponent (the dividend's less the divisor's; half
the operand's rounded down), and only an inexact one fills the 34 digits. The sum, the product and every zero result already did
this (PB3538 review finding R5-1); the quotient and the root were the arms left over.

**Re-probe.** The note's symptom (`DISPLAY FUNCTION MEAN(1 2 3)` printing 34 digits) no longer reproduced: PB3514's device form drops
the trailing fraction zeros before DISPLAY prints. The padded places were still read elsewhere, by the nested native quotient's
working scale (`CobolDecWithPrecision.Own`): `COMPUTE R = FUNCTION MEAN(1 2 3) / 3 * 3` stored +002.00000000 where the literal
`2 / 3 * 3` stores +001.99999999. That is the red the golden pins.

**Fix.** `CobolDec.ToPreferredExponent` (new, private) drops trailing zeros of a KNOWN-exact magnitude up to the preferred exponent;
`Div` calls it when the division remainder is zero, `Sqrt` when the root is exact. An inexact result keeps all 34 digits.
`DivTruncatedToOdd` is deliberately unchanged (its quotient is the nested native quotient held at the working scale, which is the
design); `Ln 1` and `Exp 0` keep exponent 0 (60559 5.4.1 does not govern them).

**Tests.** `CobolDecDivisionTests` (exact and inexact quotients and roots, MEAN, round-to-odd agreement); conformance
`15.60.4-Returned-value-rule/pb3954_exact_quotient_places` (accepted from 2014) and
`pb3954_exact_quotient_places_below_2014` (refused at 2002, COBOL0803). No golden printed the padded form, so none changed.
`docs/NUMERIC_DESIGN.md` states the rule beside the zero-result rule. No inventory row is closed (`closes_rows: []`).

