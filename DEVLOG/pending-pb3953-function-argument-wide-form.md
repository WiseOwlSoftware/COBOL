## PB3953: an argument expression past the Int128 carrier reaches the exact function family exact, on the wide intermediate

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
