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

**Left on the image, and OPEN WRONG ANSWERS rather than determinations (review lead R1-2), filed as leads in the report:** the binary64 family (SIN of a 39-digit argument
depends on every digit) and MEAN (its SDIDI quotient over a round-to-odd image can differ from the correctly rounded quotient in
the 34th digit: `MEAN (BIG * BIG * K3, 1)` shows `…0001000…` where the quotient rounded once at 34 digits ends in zeros).

**Review round 1 (R1-1, fixed by the review finisher).** `FunctionValueField.Compare` ordered a wide temporary against a non-wide
one by the wide value's 34-digit image whenever the images differed, on the claim that the round-to-odd lowering is monotone. It
is, but only against another image: a non-wide temporary holds up to 39 exact digits (an Int128 value), off that grid. With
`A = X * Y + C = 170141183460469231731687303715884105000`, just under `Int128.MaxValue`, and `A + 1000` just past it, the wide
value's image (`…8841` then zeros) lies below A, so the branch showed `MAX (A, A + 1000)` = A, `MIN` = A + 1000 and
`MEDIAN (A + 1000, A, A + 2000)` = A + 2000, while ORD-MAX (on the exact `RenderWide` arm) answered 2. `Compare` now orders any
pair holding a wide value by the exact values (`CobolWide.Compare`); the one temporary with no exact wide form, an SDIDI value
past the 256-bit range, is larger than every wide value, so its sign decides. Golden legs 8-11 and
`CobolWideIntrinsicsTests.AWideTemporary_OrdersExactlyAgainstANarrowOneOffTheImageGrid` (integer, fractional and past-256-bit
pairs) witness it. The `RawArg` comment and the note no longer call MEAN and the binary64 family determinations (lead R1-2).
