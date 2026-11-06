## PB4830 - a folded or group-sum LENGTH no longer passes where SR12 bars an integer function

Re-probe: `FUNCTION CONCAT(FUNCTION LENGTH(FX) "X")` compiled and printed `3X`; the same held for the run-time LENGTH of an OCCURS DEPENDING group, BYTE-LENGTH, and `BASECONVERT(... 10 8)`. ISO 8.4.3.2.3 SR12 bars an integer function other than integer ABS from a position requiring an unsigned integer.

Two roots. `BindLengthFamily` returns ahead of the generic path and never sent its fold through `Folded`, so the folded literal had no `FunctionType` or `FoldedFunction` (the generic path's LENGTH and BYTE-LENGTH arms were unreachable; deleted). And `UnsignedIntegerViolation`, `CheckConcatArgs` and `CheckBaseConvertArgs` read only one of the three trees a function-identifier arrives in. `IntrinsicArgumentRules.NumericFunctionResult` is now the one reader of (name, type) for the call, the folded literal and the variable-length group sum, and all three consumers ask it.

Goldens: four refusals `pb4830-*` under `conformance-suite/cases/8.4.3.2-Function-identifier/`. No inventory row closed (SR-8.4.3.2.3-12 closed under PB1420).

The first gate went RED on two pins of the old behaviour: golden `pb59_numeric_literal_argument` (its `CONCAT(A2 FUNCTION LENGTH(B2))` line) and `Concat_AdmitsNumericLiterals_AndFoldedNumericFunctions_2023` asserted a folded LENGTH legal at CONCAT argument-2, which SR12 forbids. Both were corrected (the LENGTH halves dropped, the test renamed `Concat_AdmitsNumericLiterals_2023`), and inventory row RV-15.18.4-1 retired the old witness through `record_verdicts`.
