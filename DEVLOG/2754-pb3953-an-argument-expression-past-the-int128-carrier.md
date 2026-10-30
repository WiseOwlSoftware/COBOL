## Entry 2754 — 2026-10-29 17:59 PDT — PB3953: an argument expression past the Int128 carrier reaches the exact function family exact, on the wide intermediate; PB1369: the compile-time arithmetic determination is re-verdicted against the code, and the three places that still told the old story are corrected

### PB3953: an argument expression past the Int128 carrier reaches the exact function family exact, on the wide intermediate

**The defect.** A nested native product of 39 to 77 digits is carried exactly as a `CobolWide` (kb/Work PB1900), but used as a
function argument it was lowered at the intake to the 34-digit SDIDI by round-to-odd, and the function answered from that image.
Re-probed on this branch's build before the fix, with `BIG PIC 9(18)` = 999999999999999999 and `K3 PIC 999` = 999:
`DISPLAY FUNCTION ABS (BIG * BIG * K3)` showed `998999999999999998002000000000000100000` (the product ends `…000999`),
`FUNCTION MOD (BIG * BIG * K3, 1000)` was 000000000 (999), `FUNCTION ABS (BIG * BIG * BIG)` ended `…100000000000000000000`
(GnuCOBOL 3.2.0: `…2999999999999999999`), and `MOVE FUNCTION INTEGER (BIG * BIG * K3) TO R` stored `…0000000100000`. Under native
arithmetic §8.8.1.3 makes the method of evaluating "all integer and numeric functions" the implementor's and §15.4.1 the returned
value's representation, so CLAUDE.md rule 1 makes GnuCOBOL's arbitrary-precision argument temporary the answer. The review of the
first adjudication had refused to record the limit as a determination; this landing removes it.

**The fix, shaped so the next exact function is automatic.**
- `IntrinsicRenderer.RawWideArg` is the position intake rendered with `keepWide`; a §15.3 type-6 position takes its integer on the
  wide form (`CobolIntrinsics.IntegerArgWide`, the same integrality test as every carrier). `RawArg` is now `RawWideArg` lowered,
  for the consumers with no wide form.
- `IntrinsicRenderer.RenderWide`, consulted under native arithmetic before the SDIDI arm, evaluates ABS, SIGN, INTEGER,
  INTEGER-PART, FRACTION-PART, MOD, REM, MAX, MIN, ORD-MAX, ORD-MIN, SUM, RANGE, MEDIAN and MIDRANGE on `CobolWide`
  (`CobolIntrinsics.Wide.cs`, the `…Scaled` bodies' contracts on the wider carrier) whenever the arguments' digit bounds prove the
  work fits 77 digits, and returns a `NumX.Wide` that `NumericRenderer` settles like a nested product: rounded once into a
  resultant, compared exactly by a relation, kept by an enclosing function. A call with an SDIDI or binary64 argument, or past the
  bound, keeps its lane.
- The three consumers that lowered a function's value now take it exact: the string-image channel (`OperandText`, so a MOVE
  to an alphanumeric item sends `CobolNum.FormatWideFunctionText`'s every digit), DISPLAY (`FunctionDeviceText` renders with `keepWide`;
  `CobolNum.DeviceFunctionText(CobolWide, …)` and `FunctionValueField.OfWide`, whose MAX, MIN and MEDIAN refine the order of two
  values whose 34-digit images agree; the returned-value description is one rule over a digit string, `ReturnedValueProfile`),
  and MOVE (`NumericRenderer.TransferWide`, the final transfer `Settle` itself now spells, unchecked as a MOVE is, so its
  high-order truncation keeps the value's own low-order digits, §14.6.8.2 4).
- `CobolWide` gained the primitives the bodies stand on (`DivRem`, `MagnitudeDigits`, `TryFrom`, `TryToInt128`, `FromUnsigned`).

**Guards.** `ExactWideSettlementDriftTests` registers the four new settling sites (`IntrinsicRenderer`, `FunctionDeviceText`,
`MoveEmitter`, `OperandText`) with assertions that each settles what it keeps, and `EveryExactArmTakingANumericArgument_HasAWideBody` holds the
family closed: an exact-switch arm with a numeric intake needs a wide body or a stated reason (COMBINED-DATETIME: argument-2 is
below 86 401 and its value is formed on the SDIDI). `IntrinsicCarrierAgreementDriftTests` now reads the position intake in
`RawWideArg`.

**Goldens.** `pb3953_function_argument_int128_window` gains the 39-digit legs (ABS, MOD, MAX of two values sharing one image, and
the MOVE of INTEGER), each value derived from the rule; its negative `pb3953_function_argument_below_2014` stands.
`CobolWideIntrinsicsTests` holds each body against a `BigInteger` oracle. STANDARD-DECIMAL is unchanged (the SDIDI is the value).

**Rows.** DOC-A.1-123 now states the determination instead of the departure; it and RV-15.4.1-1 already CONFORM, so the landing
closes no inventory row (`closes_rows: []`, with the reason in the note).

**Left on the image, by determination, and filed as leads in the report:** the binary64 family (SIN of a 39-digit argument
depends on every digit) and MEAN (its SDIDI quotient over a round-to-odd image can differ from the correctly rounded quotient in
the 34th digit: `MEAN (BIG * BIG * K3, 1)` shows `…0001000…` where the quotient rounded once at 34 digits ends in zeros).

### PB1369: the compile-time arithmetic determination is re-verdicted against the code, and the three places that still told the old story are corrected

**What the note claimed.** The registrar's batch 15a found that the Annex A.1 item 29 documentation (the mode of
compile-time arithmetic, §7.3.6.3 GR2, and the intermediate rounding, §7.3.6.2 SR2) contradicted the code: it called
`System.Decimal` a standard mode, said division truncates, named the deleted `EvalConstExpr`, and the evaluator
reported an over-range operand as an SR1 b) violation. The note still claimed four rows: GR-7.3.6.3-2, SR-7.3.6.2-2,
DOC-A.1-79 and DOC-A.1-80.

**What the re-probe showed.** Earlier landings had already done the work (DOC-A.1-29 rewritten by the doc-rows
integrator and PB1592, the operand reporting moved to SR2, DOC-A.1-79 and -80 closed by PB1376 and PB1375). On this
build, with `--source-format free`: `2 / 3 * 3` is 2, `100 / 7 * 7` is 100, `1 / 3 * 3` is 0 and `-7 / 2` is -3 at 2023
and 2014; the tie `0.0000000000000000000000000015 / 2 * 10000000000000000000000000000` is 8 at 2023 (nearest-even) and 7
at 2014 (standard arithmetic, nearest-away-from-zero); `>>DEFINE V3 AS 2 / 3 * 3` with `>>IF V3 = 2` selects; the
31-digit operand `1234567890123456789012345678901 + 0` and the product `9999999999999999999999999999 * 10` are refused at
2023 (COBOL1547 in a constant entry, COBOL1619 in a directive, both citing the documented SR2 limit) and evaluate at
2014 (`99999999999999999999999999990`). Every figure DOC-A.1-29 states is what the evaluator does.

**The fix.** No behaviour changed. GR-7.3.6.3-2 and SR-7.3.6.2-2 are re-verdicted CONFORMS (batch
`w20261759a-pb1369-ct-arith`, GAP 23 to 21) with the conformance cases `gl2m2_ct_arith_mode`,
`gl2m2-ct-arith-intermediate-overflow`, `pb1592_ct_arith_standard` and `CompileTimeArithmeticModeTests`. Three leftovers
that still described the retired evaluator are corrected: the `docs/CONFORMANCE.md` section 3 bullet said `System.Decimal`
for every expression (false at 2002 and 2014, where the mode is standard arithmetic) and now states both modes and points to
item 29; `docs/rearchitecture/DESIGN-compile-time-expressions.md` told the C2 defect story and named `EvalConstExpr`
three times and now describes the one evaluator, `EvalArith`; the section comment in `CompileTimeExpressionEvaluator.cs`
named the same deleted evaluator. DOC-A.1-79 and DOC-A.1-80 leave the note's `inventory_rows` (they were closed
by PB1376 and PB1375). `audit_annex_a1.py --check` reports no findings and no remaining obligations.

**Self-review.** The diff is documentation and one comment. Each sentence added to the section 3 bullet was checked against
`DOC-A.1-29` and against the probe values above; no second copy of the determination was created (the bullet points to
item 29 and states only the edition split).

