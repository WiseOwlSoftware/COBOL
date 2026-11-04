## PB4569: a native integer power is exact while its value fits 77 digits, for every base shape

`FUNCTION ABS (2 ** 130)` showed 1361129467683754000000000000000000000000, `MOD (X ** N, 1000)` of 2 ** 130 answered 0 for 824, and
`(X ** N - B65 * B65) * 1000 + 42` stored 146146501570272927154176042 for 42. `NumericRenderer.Power` sent an integer base raised to
an integer exponent to `CobolIntrinsics.PowNativeIntDec`, which looped exactly while the power fit `Int128` and past it fell back
to `Math.Pow` landed by its shortest decimal, 16 or 17 digits. That fallback was part (a) of NUMERIC_DESIGN D19, the owner's
decision of 2026-08-03 (PB18), taken from IBM's and Micro Focus's floating-point fallback five days before CLAUDE.md rule 1's
precedence existed. A scaled base (PB2617) and an exact base (PB4575) already followed GnuCOBOL's exact `mpz_pow_ui`, so the same
value read 16 digits over `PIC 9`, 34 over `PIC 9V9` and all 40 as `B65 * B65`, and MOD, REM, INTEGER, a relation and an
arithmetic statement that cancels the power inherited the wrong operand silently. The owner answered "Yes, exact" on 2026-11-04.

**Re-probe.** On the pinned build every leg of the note reproduced (probe `w20261863b/probe1.cob`: ABS 1361…754000…0, MOD
824 -> 0 and 2 -> 6, `X1 ** N = B65 * B65` FALSE, the S31 statement 146146501570272927154176042, `X3 ** NM1` 16 digits).

**Fix.** `Power` has no integer-operand arm. A non-binary64 base to an integer exponent is `CobolWide.Pow` as a `NumX.Wide` for a
literal exponent whose digit bound fits 77 digits, else `CobolExact.Pow` for an integer or exact base (exact on the wide form
while the VALUE fits, so `2 ** 130`, bound 130, is its 40 digits; `PowNativeDec` past it) and `PowNativeDec` for a scaled or SDIDI
base. `PowNativeIntDec` is deleted with its PublicAPI line, its tests (`PowNativeIntDecTests` becomes
`NativeIntegerPowerExactTests` over `CobolExact.Pow`: the 0 / 1 / -1 short cut of PB2632 is not needed, `CobolWide.TryPow` is
square-and-multiply) and the two comment blocks that described it. PB2628 keeps the architecture move and loses its member.

**Docs.** NUMERIC_DESIGN D19 (heading, part (a) with the supersession, the PB69 paragraph and its "Cost, documented" sentence,
part (c), line 14), CONFORMANCE.md DOC-A.1-123 and DOC-A.1-179, the plan's PB18 and PB69 lines. DOC-A.1-123 and -179 stay
CONFORMS (a documented latitude): `closes_rows: []`.

**Goldens.** `pb4569_native_integer_power_exact` (2002 to 2023; ABS, MOD, REM, INTEGER, the relation, the cancelling subtractions,
the quotient, the reciprocal over `9` and `9V9`, the 0 / 1 / -1 bases at exponent 999999999999999999, `0 ** 0`) with its
`_85` refusal (COBOL0802), `pb4569_standard_decimal_power_is_34_digits` (STANDARD-DECIMAL keeps 34 digits), and the pb69 golden
(T1 / T3 are the true 980012199). Its T10 is now the NESTED quotient `A ** 3 / A ** 2 * 1` = A: the FINAL-TRANSFER quotient
`COMPUTE R = A ** 3 / A ** 2` still divides the wide dividend's 34-digit SDIDI image and stores 999999999999998 (the documented
lane determination of DOC-A.1-123, which the same expression over `A * A * A / (A * A)` shows without any power), a lead for a
successor.

Gate: see the landing's record.
