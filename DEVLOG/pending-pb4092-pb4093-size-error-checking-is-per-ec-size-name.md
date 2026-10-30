## PB4092, PB4093: size error checking is per EC-SIZE name, never per family

Wave 20261767, group B (one root: the family-wide question "is some EC-SIZE name enabled?" chose the checked kernels, the Try
store and the catches of every size computation).

**Re-probe.** One program per name x statement (`E:/COBOL-coord/scratch/mythos/w-n3/w20261767b/p/`). With
`>>TURN EC-SIZE-TRUNCATION CHECKING ON` alone, `IF WS-D * WS-D > 0` (COMP-2 1.0E200) ended the run unit with EC-SIZE-OVERFLOW,
which is not enabled (PB4092); with EC-SIZE-OVERFLOW alone `COMPUTE WS-C = WS-A * WS-A` (100 squared into `PIC 999` holding 7) left
`007` where checking off stores `000` (PB4093). The same two shapes appeared for every other EC-SIZE name enabled alone, and two
siblings shared the root: a zero divisor or an exponentiation-rule violation under ANOTHER name was latched, dropped and the statement
went on with the receiver unchanged (`C=007`) where checking off terminates the run unit (DOC-A.1-70), and a binary64 overflow into a fixed
receiver left `007` where checking off stores `000`. The report compose inherited all of it through the family flag
`ExceptionState.SizeChecking`.

**Spec.** §14.6.13.1.1: a condition is raised "if checking for an exception condition is enabled", and its NOTE 3: "An exception
condition is set to exist only if checking for that condition is enabled" (`cite.py --check` OK). A condition not enabled takes
§14.6.13.1.3 item 8's disposition, which DOC-A.1-70 determines (the low-order digits; the IEEE value of a binary64 intermediate; and, for a
zero divisor or an exponentiation violation, which every kernel raises, the run unit's termination).

**Fix.** `SizeChecks` (Overflow, Underflow, Truncation; `All` for a SIZE ERROR phrase) is the one value that says which conditions a
statement takes. `ReceiverContext.InSizeError` became `ReceiverContext.Checks`; `EcState.SizeChecking` became `EnabledChecks`
(the names the statement enables), with `EcState.SizeCheckRows` the one table (Table 13 name, bit, run-time flag).
`CobolFloat.AddChecked/SubChecked/MulChecked/DivChecked`, `CobolIntrinsics.PowNativeRealChecked` and `CobolNum.AddChecked/SubChecked`
take the set and raise only the conditions in it. `NumericRenderer` chooses the checked operators from `RangeChecks` and the checked
landing from `CheckedLanding` (Truncation taken), `ArithmeticEmitter.StoreArith` writes the Try store only where the statement takes
EC-SIZE-TRUNCATION (`EcState.SizeErrLandingVar`) and the plain low-order-digits store otherwise, and a no-phrase statement's catches
(`WriteSizeCatches`) take only the names it enables (plus rule 7's keep), so a zero divisor not enabled propagates and terminates as with
checking off. The report compose's one family flag became one flag per name (`SizeOverflowChecking`, `SizeUnderflowChecking`,
`SizeTruncationChecking`), read back as `ExceptionState.SizeChecksEnabled` and handed to every checked operator at run time. Found
and fixed on the way (I9): `EcEmitter.FatalAmbientGates` had no EC-SIZE-UNDERFLOW row, so the condition the checked binary64 operators raise
reached no declarative. No wrapper, no old overload kept: every caller changed, `CheckingFlags.Size` and `ExceptionState.SizeChecking` deleted.

**Goldens.** `14.7.5-SIZE-ERROR-phrase-and-size-error/pb4092_size_checking_is_per_name` (2002+; twelve steps: each name against an
overflow, an underflow, a truncation and a zero divide), `pb4093_truncation_not_enabled_stores_low_order_digits` (2002+; MULTIPLY GIVING,
ADD TO, COMPUTE into COMP-3, an edited receiver, two receivers; EC-SIZE-OVERFLOW alone, EC-SIZE-ZERO-DIVIDE alone, and the
EC-SIZE-TRUNCATION control), `13.18.53-SOURCE/pb4092_report_compose_per_name` (2002+), and the refused-below
`pb4092-turn-size-per-name-85` (COBOL0900 at 85). Expected values are computed from the rule, then compared with the run.
`SizeErrorDispositionTests.SizeCondition_OtherNameEnabled_TerminatesLikeCheckingOff` (seven rows),
`FloatResultantTests.CheckedOperations_RaiseOnlyTheConditionsTaken` and `SizeChecksDriftTests` (the table against the enum and the
run-time flags). No diagnostic code used (COBOL5497-5499 returned).

**Rows.** GR-14.7.5-3, GR-14.7.5-4, GR-7.3.25.4-6, DOC-A.1-70 and DOC-A.1-179 were already CONFORMS (the rows never saw the
per-name question); one record_verdicts batch added the goldens and code sites as witnesses, GAP unchanged (20). DOC-A.1-70 and
DOC-A.1-179 carry the per-name determination; REPORT_WRITER_DESIGN, NUMERIC_DESIGN and DESIGN-codegen-backend name the new shape.
