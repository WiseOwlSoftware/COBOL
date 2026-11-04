## PB4575: a wide value beside an SDIDI operand keeps every digit (the run-time-scaled exact carrier)

**The defect.** Under native arithmetic, an expression in which an exact wide intermediate (a nested product of 39 to 77
digits, `NumX.Wide`) met an operand the SDIDI carries (a floating-point literal, a power, a NUMVAL or function value) took
the SDIDI lane, whose operand lift lowers the wide value to its 34-digit round-to-odd image. The re-probe on 9f5f0a1ae, with
`P = BIG * BIG * K3` (39 digits): `(P + 2.0E0) - P` stored 0 (owed 2), `IF P - 1 < 2.0E0 + P` was false, `(P + 2 ** 2) - P`
was 0, `(P + 2.5E-1) - P` 0.0000, `P * 2.0E0 - P * 2` -1000000. The relation had a sibling: `CompareWide` declined a wide
value beside an SDIDI or unsigned 16-byte value, so `IF P < 9.989999999999999980020000000000001E38` compared P's image, which
EQUALS that 34-digit literal, and was false.

**Why the lane could not take it.** The wide form carries its value at a compile-time scale; an SDIDI value's scale is a
run-time fact. No static scale holds both, so the pair went to the only carrier with a run-time exponent, the SDIDI.

**The fix: a run-time-scaled exact carrier.** `CobolExact` (PB4568's exact intake) became an arithmetic carrier, and the
compiler carries it as `NumX.Exact`. `CombineCore` forms a +, -, * or NESTED quotient there when a wide value meets an SDIDI
operand, and every one over such a value stays there: exact on the 256-bit form at the scale the run time aligns (a product
at its operands' own scales, so `P * 1.0E40` is the 40-digit `P * 10` at scale -39), the SDIDI only past 77 digits
(DOC-A.1-123's determination, the operands lifted exactly to 38 digits and rounded once past them). It carries its precision
(numeric design D2), so a nested quotient over it is truncated at the working scale of the operands' places and a nested
quotient's guard digits never compound. It settles where a wide value settles: rounded once at the final transfer
(`CobolExact.ToUnscaled`), compared exactly (`CobolExact.Compare`, which now also takes a wide value beside an SDIDI or
unsigned value), snapshotted for several receivers, taken by an intrinsic argument (`IntegerArgExact` at a type-6 position;
the aligned wide bodies decline it, so `RenderDec`'s exact intake takes it), shown whole by DISPLAY's temporary
(`FunctionValueField.OfExact`), and lowered by round-to-odd everywhere else. `NumX.Carrier` refuses it, as it refuses the
wide form. A scaled Int128 operand beside an SDIDI one keeps the SDIDI lane: that is DOC-A.1-82's published lane
determination (PB274), not this note's mechanism. ISO 8.8.1.3 leaves native evaluation to the implementor and CLAUDE.md rule 1
sends it to GnuCOBOL, whose `cob_decimal` sum, difference and product are exact; 8.8.4.2.4 compares algebraic values.

**After.** `(P + 2.0E0) - P` is 2, the relation is true, `POW` 4, `FRAC` 0.2500, `MUL` 0, and `(P * 2.0E0) / 2.0E0 - P`,
`P / 2.0E0 * 2.0E0 - P` and `(P * 1.0E40) / 1.0E40 - P` are 0. The golden
`cases/8.8.1.2-Native-standard-binary-and-standard-decimal/pb4575_wide_beside_sdidi_operand` pins 19 legs (sums, a negation,
products, a power, ROUNDED and truncated transfers, nested quotients with compile-time and run-time precision, MOD over the
exact argument, the size error, three relations and the DISPLAY of MAX over the exact argument), with its 85 refusal (the
floating-point literal is a 2002 introduction). `CobolExactIntrinsicsTests` drives the carrier against a BigInteger oracle, and
`ExactWideSettlementDriftTests` pins the `NumX.Exact` arm at every settling site. Docs: NUMERIC_DESIGN's wide-lane paragraph,
CONFORMANCE DOC-A.1-82 and DOC-A.1-123. No inventory row was claimed or closed (DOC-A.1-123 already reads CONFORMS).

**Not closed here.** The note's MAXOP leg, `MAX (P - 1, P, 2.0E0) - (P - 5)`, is now -994 (owed 5): MAX's value is held on
the SDIDI returned-value carrier, rounded once to 34 digits, and the now-exact difference above it shows that rounding. That is
PB4570's carrier question.
