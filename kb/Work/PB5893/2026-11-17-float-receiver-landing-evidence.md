## 2026-11-17 - how an exact function value lands in a COMP-2 receiver (wave 20262099 group d; the coordinator's ruling TRUNCATION)

**The landing rule this branch keeps.** An exact value (fixed-point, wide or run-time exact) delivered as the final transfer to a
COMP-1 or COMP-2 resultant lands by the receiver's rounding mode, once, from all its digits (`FloatResultant.FromScaled` /
`FromExact`, kb/Work PB4730): with no ROUNDED phrase that mode is truncation toward zero. A function's exact value is such a value.
PB5893's fix makes an all-fixed-point argument exact under a floating-point receiver, so a function over it now lands by this rule
where main computed the argument in binary64 and landed a double.

**Evidence** (probe `E:/COBOL-coord/scratch/my-20261115/w20262099/d/probes/bis2.cob`, --std 2023; R PIC 9V9(30) holding
2.333333333333333333333333333333, A PIC 9 VALUE 7, F2 COMP-2; main build d77571151b run `runs-main/bis2-001`, branch run
`runs-br/bis2-001`):

| Statement | main | branch |
|---|---|---|
| `COMPUTE F2 = R` | 2.333333333333333 | 2.333333333333333 |
| `COMPUTE F2 = R + 0` | 2.3333333333333335 | 2.3333333333333335 |
| `COMPUTE F2 ROUNDED = R` | 2.3333333333333335 | 2.3333333333333335 |
| `COMPUTE F2 = FUNCTION ABS (A / 3)` | 2.3333333333333335 | 2.333333333333333 |
| `COMPUTE F2 ROUNDED = FUNCTION ABS (A / 3)` | 2.3333333333333335 | 2.3333333333333335 |
| `COMPUTE F2 = FUNCTION ABS (A / 3) + 0` | 2.3333333333333335 | 2.3333333333333335 |

`R + 0` is the binary64 lane (D16: R enters binary64 by nearest, the sum is that double); the bare forms are the final transfer of an
exact value and truncate. Row 4 changes because main's argument was binary64 (the defect), not because the landing changed.

**Not ruled here (a lead for its own note; no id allocated).** (1) Whether truncation is the right default for an exact value stored
in a binary64 receiver without ROUNDED, given that the same value as a binary64 operand (`R + 0`) lands by nearest. (2) Whether the
argument's fixed working scale under a floating-point receiver (`NumericLanes.ArgumentScale`: 17 + 14 guard fraction digits) should
give way to a significance-based one: a quotient below 10^-14 keeps fewer than 17 significant digits, so `ABS (A / B)` with
B = 3 * 10^17 prints 2.33333333333333E-17 where main printed 2.3333333333333332E-17 (probe `rcv23b.cob`).
