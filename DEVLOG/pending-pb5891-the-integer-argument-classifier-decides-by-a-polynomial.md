## PB5891: the integer-argument classifier decides by a polynomial normal form, so N * (N + 1) / 2 is an integer argument

**Re-probe.** The refuter's `fn_edge.cob` (slice 1985-247) was refused COBOL1627 at `--std 85` and `--std 2023` on main cdc9601608:
`FACTORIAL (N * (N + 1) / 2)`, `MOD (N * (N + 1) / 2 4)`, `CHAR (N * (N - 1) / 2 + 65)` and `RANDOM (N * (N + 1) / 2)` over N PIC 99.
Each argument is an integer for every N (a product of two consecutive integers is even), so §1989:A.2.2 AR4 and §2023:15.3 AR6
admit it; the rule sets no method of proof. After the fix the program compiles at both editions and prints 720, +2, C and a RANDOM
value (N = 3).

**Root cause.** Since kb/Work PB5886 the type 6 screen failed closed but decided by the SHAPE of the terms: an integer term under a
literal factor was an integer only when the factor was whole, so `N * (N + 1)` under 0.5 was refused, and an identity between
numeric function terms (`SQRT (N) - SQRT (N) + 5`) was refused as recorded debt (kb/Work PB5889).

**Fix (the ruling's design; docs/INTRINSICS_DESIGN.md, the type-6 intake, written first).** `IntrinsicArgumentRules.TypeSix.cs`
builds the argument's value as a polynomial with rational coefficients in its atoms: a fixed-point item is 10^-scale times an integer
atom keyed by its place; integer functions, index-names, counters and ODO extents are integer atoms; floating items and numeric
functions are real atoms, which must cancel; a quotient by a nonzero literal is the product by its reciprocal, by anything else is
refused. Two references are one atom only when they are the same pure term (`SameTerm`); the catalog's new `IntrinsicSig.ValueSource`
column (held over every function by `IntrinsicValueSourceDriftTests`) keeps RANDOM, the clock functions, WHEN-COMPILED and the
EXCEPTION- family from cancelling. Every intermediate must be exact (no real term, a quotient within the 14 guard digits, an integer
on the binary64 lane, the significant digits of a standard arithmetic; an integer the arithmetic rounds becomes a fresh atom), and
the polynomial must be integer-valued, decided exactly on the grid {0..d_i} of its degrees (`Polynomial.FractionDigits`; cap: degree
12 per atom, 4096 points). `CollectAdditive`, `LinearSum` and `DigitBound` are deleted.

**A sibling under-reject fixed with it.** `LinearSum` keyed an item by its description, so `T (1) - T (2)` over a scaled table
cancelled and was admitted (it is 0.1 at T (1) = 0.1, T (2) = 0); atoms are keyed by place, and it is refused.

**Tests.** Admitted: `pb5891_polynomial_integer_arguments` (85, 2014, 2023), `pb5891_standard_decimal_identity` (2014, 2023).
Refused: `pb5891-not-integer-valued`, `-impure-identity`, `-degree-over-cap`, `-table-subscripts`. `pb5889-identity-difference` is
retired (its program is now admitted); the refusal expectations of the PB5832 and PB5886 goldens follow the new reasons. Unit:
`ArgumentPolynomialTests`, `IntrinsicValueSourceDriftTests`, the shape table of `IntrinsicTypeSixPowerShapesDriftTests` (15 new rows),
`TypeSixWalkCoverageDriftTests` over `Build` and `SameTerm`. The NIST differential: 395 of 395 unchanged.

**Rows.** 1985: the four PB5891 rows take the new witnesses (their stored reads are the campaign's to renew); `1989-AR-A.2.2-4` and
2023 `AR-15.3-6` restated CONFORMS. kb/Work PB5889 keeps only the residue that is an integer by a function's meaning
(`S2 - FUNCTION MAX (S2)`).
