## PB4568 and PB4111 — the native SDIDI lane takes its arguments as their exact values

**One root, two notes.** Under native arithmetic every intrinsic call the exact wide lane declines (an SDIDI argument beside a wide one, a binary64
beside a selection, work past `RenderWide`'s 77-digit bound, a list whose alignment may leave the carrier) reaches `IntrinsicRenderer.RenderDec`,
and so do VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE with any SDIDI or wide argument; `FloatBody`'s `ExactListBodies` arm takes the same three
over an all-fixed list. Every one of those arms took its arguments through `DecArgList`, a wide argument as its 34-digit round-to-odd image, and
the Dec bodies then rounded each step. The re-probe on the branch reproduced every leg of the wave-20261857 determination: with
P = BIG * BIG * K3 (39 digits), `ORD-MAX (P - 1, P, 2.0E0)` and `ORD-MIN (P, P - 1, 1.0E50)` answered 1 while `IF P - 1 < P` is true,
`MOD / REM (P, 2 ** 10)` 0 for 999, SUM and RANGE beside 2.0E0 showed image digits (PB4568); VARIANCE (P, P - 1) 10^10 for 0.25,
PRESENT-VALUE (0, P, 1 - P) 0 for 1, VARIANCE (BIG * BIG, BIG * BIG - 1) 0.5, VARIANCE (A * 1000, A * 1000 + 1) 0.5, PRESENT-VALUE
(3, A * 1000 + 1, -4 * A * 1000) 0.3 for 0.25, and VARIANCE of the `9(31)` items 10^30, 10^30 + 1, 10^30 + 1 0.2222223333 for 2/9 (PB4111).
Deciding citations (cite.py OK): §15.4.1 "When native arithmetic is in effect and an equivalent arithmetic expression is specified, the value
returned is an implementor-defined approximation of the value of that expression."; §8.8.1.3 "Native arithmetic is an implementor-defined method of
evaluating … all integer and numeric functions."; §15.71.4 2) "The comparisons used to determine the greatest valued argument are made according
to the rules for simple conditions." CLAUDE.md rule 1 step 2 sends the native latitude to GnuCOBOL 3.2.0, which is exact on every leg.

**The fix.** A runtime `CobolExact` holds an argument's exact value on whatever carried it (a wide value at a scale, or an SDIDI at its own
exponent; a binary64 through its §8.8.1.5.1 conversion), with the one exact mixed-carrier order (`CobolExact.Compare`, which
`FunctionValueField`'s DISPLAY ordering now delegates to). `CobolIntrinsics.ExactValue.cs` holds the native bodies — ORD-MAX, ORD-MIN, MAX, MIN,
MEDIAN, MIDRANGE, SUM, RANGE, MEAN, MOD, REM, VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE — which form the value on the 256-bit wide form and round
it once (`CobolWide.TryQuotientDec`, the quotient rule MEAN's `MeanWide` now shares, and `ToDecRounded`). VARIANCE is `(n·Σd² − (Σd)²) / n²` over
the arguments shifted by the first, so the deviations stay small exactly where the expression cancels; PRESENT-VALUE is one numerator over `p^n`
with `1 + rate = p / q` reduced. Work the wide form cannot carry is the SDIDI body over the images, DOC-A.1-123's determination past 77 digits.
`RenderDec`'s native arms and the `ExactListBodies` arm take `ExactValueArgList` over `RawWideArg`, the one keeping intake; the standard modes keep
the Dec bodies (§8.8.1.5.2 r1, §15.4.1 1)).

**A design change against the determination, and why.** The determination proposed `BigInteger`. `scripts/semgrep/verify.py` refused it
(`wiseowl-cobol-no-biginteger` 43 → 92; DESIGN §1.2 invariant 2 bans it from the runtime value model), so the bodies are bounded by the wide form
instead, which is DOC-A.1-123's existing determination for every exact native intermediate. The new bodies live in `CobolIntrinsics.ExactValue.cs`
rather than `CobolIntrinsics.Wide.cs`, and no `RenderWide` arm was needed: `AnyDecRaw` already sends a wide argument to `RenderDec`.

**After.** ORD-MAX legs 2, ORD-MIN 2, MOD / REM 999, `MOD (0 - P, 2 ** 10)` 25, `REM` -999, `SUM (P, -9.98999999999999998002E38)` 999,
`RANGE (P, P - 2000, 9.98999999999999998002E38)` 2000, MEAN / MIDRANGE / MEDIAN 499.5; VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE 0.25, 0.5,
1 on the W and I legs and 0.25, 0.5, 0.25 on the T leg; the D legs land in the binary64 carrier as .2222222222222222, .4714045207910317 and
.1111111111111111; STANDARD-DECIMAL unchanged. Goldens `pb4568_wide_argument_beside_sdidi`, `pb4111_variance_family_exact_native` and its twin
`pb4111_variance_family_standard_decimal`; `unit:CobolExactIntrinsicsTests` (every body against a `BigInteger` / `fractions` oracle, and past
the wide form against the SDIDI body); `WholeRangeBodiesDriftTests` and `IntrinsicCarrierAgreementDriftTests` widened. Docs: DOC-A.1-123 and
DOC-A.1-92b, INTRINSICS_DESIGN's PB1565 and PB3953 sections. No inventory row changes (the rows were CONFORMS and stay so).

**Left open.** The SDIDI is the returned-value carrier of such a call, so a value of more than 34 digits (`SUM (P, 1, 2.0E0)`, MAX's 39-digit
content used as an operand) is rounded once there, where GnuCOBOL keeps every digit: PB4570's carrier question.
