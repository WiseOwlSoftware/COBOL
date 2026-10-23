## A nested quotient's precision no longer grows with the expression's depth (PB3538)

The nemotron fuzz run's one compiler defect, kb/Work PB3538: `COMPUTE R = A / B / B / B / B` with A = 8 and B = 2 is 0.5.
On main it ended the run unit with EC-SIZE-OVERFLOW. With ON SIZE ERROR it took the phrase and left R unchanged.
`(A / B) * (A / B) * (A / B)` (64) aborted the same way. Re-probed on this branch's own build before the change: same
output, rc 1.

**Mechanism.** `NumericRenderer.Divide` gave a nested quotient `max(receiver, a.Scale, b.Scale) + 14` guard digits of
scale. The next division's `a.Scale` is that quotient's scale, so the guard was added again at every level: 14, 28, 42.
Once the scale reached 42, any value of 1 or more no longer fit the Int128 carrier. A clamp limited the guard added at
one step, never the total. A product of quotients compounded the same way: each quotient had no digit bound, so the
product took a checked Int128 multiply at the summed scale. The same checked multiply ended
`FUNCTION MEAN (F G) * FUNCTION MEAN (F G) * FUNCTION MEAN (F G)` (5.36 at scale 54) for every function value of scale 18.

**Fix (numeric design D2, rewritten).** `NumX` gains `Precision` / `FractionPrecision`: the fraction digits a value's
operands carry. A quotient's guard digits are not part of it, and sums, products, negations and wide powers carry it the
way they carry the scale. A nested quotient runs at `max(receiver scale, operands' precision) + 14` with truncation, so
the guard is added once at any depth. Its lane comes from its digit bound, as a product's does (PB1143): the scaled Int128
within 38 digits, and the SDIDI by round-to-odd past that. An operand of unknown bound is bounded by its Int128 carrier,
39 digits (`PicInfo.CarrierDigitBound`, also used now by the 16-byte container bound). A product therefore never takes a
checked Int128 multiply. `CobolNum.MulChecked` is deleted, which raises the runtime major to 13 and re-pins the ABI hash.
DIVIDE ... REMAINDER's subsidiary quotient carries its digit bound, and its back-multiply stays exact into the
subtraction (`Combine(..., keepWide: true)`; `ExactWideSettlementDriftTests` lists the site).

**Measured alternative, rejected.** The note proposed renormalizing every nested quotient to the SDIDI's 34 digits. I
built that first and ran the whole Conformance assembly: one red in 18,125, pb60's native `2 / 7 * 7` = 1,999999 became
2,000000. GnuCOBOL truncates the quotient and multiplies exactly (1,999999), and CLAUDE.md rule 1 puts GnuCOBOL next where
ISO leaves native arithmetic to the implementor. That design would also put every nested division on software
decimal128. The bounded scaled design keeps the pb60 value, keeps the Int128 fast path, and makes precision independent of
depth.

**Sweep.** Division chains, products, sums and negations of quotients, nested powers (`(A / B) ** 2 / B ** 2 / B / B` =
1), a receiver-less relation and a subscript expression all give the exact left-to-right value. So do a several-receiver
COMPUTE (29.63 rounded, 29.62 truncated), a zero divisor mid-chain (ON SIZE ERROR, R unchanged), a quotient by a tiny
divisor (`999999999999999999 / 0.000000000000000003 / 10 ** 18`), a function value as a dividend, and DIVIDE REMAINDER
into a COMP-5 quotient. ARITHMETIC IS STANDARD-DECIMAL never had the defect, since every operation takes the SDIDI
before `Divide`. The same holds at `--std 85` and under `--permissive`.

**Witnesses.** Goldens `pb3538_chained_division_native` (85 to 2023: the note's repro with and without ON SIZE ERROR, the
product, a relation, and depth-10 chains of 8 / 2 and 1 / 3 into scale-4 and scale-12 receivers),
`pb3538_chained_division_standard_decimal` (2014, 2023) and `pb3538_unbounded_operand_products` (2002 to 2023). Expected
values come from §8.8.1.2 3) and §14.7.4.3 2). `NestedQuotientPrecisionDriftTests` compiles every quotient-crossing shape
at depth 2 and depth 16 and requires the same division-kernel scales. On the base tree it was red, with scales up to 228.
CONFORMANCE.md DOC-A.1-123 and DOC-A.1-179 are corrected: they had both said "up to 14 guard digits" and "a QUOTIENT is
formed on the SDIDI whatever its operands". No inventory row moved; the rules' rows were already OK and gain witnesses.
