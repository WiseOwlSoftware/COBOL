## PB3951: FUNCTION LENGTH and BYTE-LENGTH of a numeric function measure the temporary DISPLAY shows, as GnuCOBOL does

`FUNCTION LENGTH(FUNCTION ABS(S))` answered 30 for every numeric function (the fixed S9(30) "returned-value item" PB1398 gave them)
where GnuCOBOL 3.2.0 answers the size of the temporary that holds the value: 7 for `ABS` of an `S9(5)V99` item, 5 for a COMP-3 argument,
2 for a COMP one, 4 for `INTEGER(S)`, 8 for `DATE-OF-INTEGER` (kb/Work PB3951, filed from the PB3514 review). Wave 20261728, group D.

**Re-probe on the wave's build, and the adjudication.** The note reproduced on every line. A matrix of about eighty lines (every
function shape over unsigned, signed, SIGN SEPARATE, COMP, COMP-3, COMP-1, COMP-2 items, signed and unsigned literals, expressions, nested
calls and a table(ALL), and NUMVAL values on each side of 2**31, 2**32 and 2**63) was run through `cobc` 3.2.0 (WSL) and the new build.
GnuCOBOL's LENGTH is the size of the SAME temporary DISPLAY shows, so the question PB3514 left open on purpose (DOC-A.1-92: "the LENGTH of a
returned-value item is a separate decision") has the answer the precedence of CLAUDE.md rule 1 already gave DISPLAY: ISO §8.4.3.2.1 makes
the function-identifier the data item the evaluation produces, §15.4 puts the value in "a temporary elementary data item", §15.50.4 r3 and
§15.14.4 r1 measure a data item, and §15.4.1 leaves the representation to the implementor (A.1 item 92). A second, fixed description, which
no other consumer used, was a second answer to a question the three shapes already answer.

**The fix is one item asked two questions.** `FunctionValueField` (PB3514) gains `ByteLength`: a copied argument's image width
(the compiler's own `DataItem.ByteWidth` and `ItemLength.Positions` ride on `OfItem`: a packed argument's bytes, a binary one's, a SIGN SEPARATE one's extra position), a literal's digits written plus
a sign position when one was written, DATE-OF-INTEGER's 8 and DAY-OF-INTEGER's 7, the 4-byte, 8-byte or DISPLAY item a value describes
(`CobolNum.ReturnedValueProfile` now also returns the bytes, because its `Digits` is the position count DISPLAY pads to, 9 or 20), and 8 for
a binary64. `IntrinsicBinder.FunctionResultLength` no longer folds a width: every numeric or integer function result (and a folded literal)
goes to the runtime as a `BoundIntrinsicCall`, and the renderer's first LENGTH / BYTE-LENGTH arm, ahead of the float-lane routing that
sent `LENGTH(FUNCTION SQRT(4))` to a `LengthReal` the runtime does not have, reads the field `FunctionDeviceText.Field` builds. The one
predicate `FunctionDeviceText.IsNumericFunctionResult` is asked by both sides. The `DeviceForm` catalog column therefore decides a function's
SIZE as it decides its text, so a function added tomorrow needs no edit, and `FunctionDeviceFormDriftTests` pins the wiring.
`SendingValueTemp.NumericReturnedValuePic` had one reader and is deleted; `FunctionValuePic` and `IntegerFunctionValuePic` stay as the
nominal items IN-ARITHMETIC-RANGE reads (DOC-A.1-92 says so; the drift test's reader list shrank to match). The length of a numeric function is
now a run-time value and DISPLAYs as a computed one (`000000007`), as GnuCOBOL's does.

**Not copied, and documented (DOC-A.1-92).** A binary64 result is 8 bytes where GnuCOBOL's DISPLAY temporary has 38 to 96 digits (PB3505
declined that image for DISPLAY too); a DISPLAY temporary is its true digit count where GnuCOBOL's is a GMP estimate (`-9223372036854775808`
is 19, cobc 20); a call the binder folded to a literal (`LENGTH(LENGTH(X))`, which GnuCOBOL refuses) is measured as the literal's own field.
Under a standard arithmetic mode the binary64 functions are SDIDI values and take the value's description, as they do for DISPLAY.

**One bug found on the way and fixed with it.** The first cut put the length arm after the renderer's `AnyRealArgument` routing, so
`LENGTH(FUNCTION SQRT(4))` failed to compile (`CobolIntrinsics.LengthReal`); the arm now runs first and the drift test pins the order. The
runtime ABI pin moved when the size was first written as a `NumProfile` property (a record struct prints every public property, and the pin
hashes codec images of its text), so the item's length is stated by the compiler (the one reading `LENGTH(field)` uses) and passed to `OfItem`.

**Goldens.** `pb3951_length_of_numeric_function_result` (2014, 2023; the 47 lines are identical to cobc), `..._not_copied` (2002 to 2023),
the negative `pb3951_length_of_abs_below_2014` (COBOL1502), and `pb1398_length_of_function_result` updated from 30 to the new sizes;
`FunctionDeviceTextTests` pins the byte rows. Register: PB3951 landed, `closes_rows: []` (no inventory row was claimed).

**Self-review.** Architecture: one field, one predicate, no C# text in a bound node. Full code: the renderer arm implements §15.50.4 r3 /
§15.14.4 r1 over the temporary §8.4.3.2.1 names; BYTE-LENGTH and LENGTH share it; the index and string arms are unchanged. Performance: the
nested call is evaluated at run time, as GnuCOBOL evaluates it. Duplication: `OverlappingMove.Size` states the zoned width a second time over its
own struct and was left alone (it is a digit-run size for a move, not an item's bytes).

**Gate** (run `TestResults/build-local/000002-20261027T222345Z`, head `7bb441c1a`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — Conformance 643/19,151 · Unit 27,742/38,125 · Characterization 36/36 cases ran · Linux half (WSL clone of HEAD) GREEN ===`. The first gate (run 000001) was RED on the DOC CITATIONS audit: a golden comment quoted §15.50.3 r1's words under §8.4.3.2.1, fixed in `7bb441c1a`. Leg 1 ran the seven new conformance cases (`pb3951_length_of_numeric_function_result` at 2014 and 2023, `..._not_copied` at 2002, 2014, 2023, the negative `pb3951_length_of_abs_below_2014` at 85 and 2002, all Passed) and `FunctionDeviceTextTests`' new byte rows. `scripts/semgrep/verify.py` PASS, no rule's count rose.

**Review round 1 (R1-1, fixed).** The first cut sized a copied argument from its `NumProfile` (`CobolNum.ImageWidth`), which does not say a
USAGE NATIONAL position is two bytes: `FUNCTION BYTE-LENGTH(FUNCTION ABS(NN))` over `PIC S9(5) USAGE NATIONAL` answered 5 where the item itself
is 10 (and 6 for 12 with SIGN SEPARATE). `FunctionValueField` now carries positions (`Length`, §15.50.4 r2/r3) and bytes (`ByteLength`, §15.14.4 r1)
apart; the compiler passes the argument item's own `ItemLength.Positions` and `DataItem.ByteWidth` to `OfItem`, its `ByteWidth` to `OfFloatItem`, and
the renderer reads `Length` for LENGTH and `ByteLength` for BYTE-LENGTH. The second statement of an item's width in the runtime
(`CobolNum.ImageWidth`) is deleted. Pinned by `pb3951_length_of_numeric_function_result_not_copied` (national and SIGN SEPARATE national rows: 5/10, 6/12;
GnuCOBOL's unfinished national support answers 10 to both) and `FunctionDeviceTextTests.ACopyOfANationalArgumentHasPositionsAndBytes`. R1-2 (the
design doc named `NumProfile.ImageWidth`) is fixed in `docs/INTRINSICS_DESIGN.md`.
