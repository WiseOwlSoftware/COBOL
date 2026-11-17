## PB5893 and PB5892: every function argument is evaluated in the argument context, and CONCAT tests an integer by its value

**PB5893 (wrong answer, every edition with a floating-point usage).** A function argument's evaluation technique followed the
statement's RECEIVER at every position but an integer one.
- Under a COMP-1 or COMP-2 resultant an all-fixed-point argument was evaluated in binary64.
- `COMPUTE F = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)` over 31-digit X and Y stored 672 where a fixed-point resultant stores 41.
- ABS, MAX, SUM, MEAN and ANNUITY over E = 41 printed 0, 1, 1, 2 and 0.0000, for 41, 41, 42, 22 and 0.9000. This is main d77571151b,
  run `E:/COBOL-coord/scratch/my-20261115/w20262099/d/runs-main/rcv23-002`.
- kb/Work PB5891 review R3-1 had fixed only the integer positions (`IntrinsicRenderer.ReceiverFor`).

**Fix: one argument context.** An argument is evaluated individually (§2023:8.4.3.2.4 GR2), so its technique is its own operands'.
DOC-A.1-92b is unchanged: a floating-point argument still takes binary64. The receiver takes the function's value by the ordinary
rules.
- `NumericRenderer.ArgumentReceiver(integerPosition)` is the one context of every argument render in `IntrinsicRenderer`. It is the
  receiver without its floating-point facts, at its scale.
- It is applied by `ReceiverFor` at an argument's position, and by `NumericArgument` to an operand read without its index.
- The nine intakes that rendered under `num.Receiver` now pass it: `PresentValueBase`, `DomainArg`, `DecOf`, `ExactValueOf`, `DblOf`,
  the table(ALL) enumerations of `RenderWide`, `AlignedArgs` and `RawArgPairs`.
- `IntrinsicArgumentIntakeContractDriftTests.EveryArgument_RendersInTheArgumentReceiver` holds every `num.AsNum(` of the renderer to it.

**Exactness where exact, significance where not** (the coordinator's ruling on review round 1).
- A floating-point resultant (`ReceiverContext.IsFloatingPoint`: COMP-1, COMP-2, every FLOAT usage, a floating-point edited item) has
  no fraction scale to give. So an argument at a numeric position keeps significance (`NumericLanes.ArgumentKeepsSignificance`).
- Its +, - and * stay exact.
- A quotient that may not terminate is the SDIDI's final quotient, 34 significant digits at every magnitude
  (`NumericLanes.QuotientIsFinal`).
- A quotient whose divisor is a literal that divides exactly within the guard digits stays exact
  (`TerminatesWithinGuardDigits`): `REM (... * 4 / 4, 1000)` is 10.25.
- `LaneOf` reads the rule through `LaneContext.KeepsSignificance` and the renderer reads it through `NestedQuotient`, so `AssertLane`
  holds.
- Every arm of `LaneOf` reads an argument in `LaneContext.ArgumentContext`, the bound twin of the renderer's argument context.
  Review R2-1 found the call arm reading no context. `ABS (ABS (A / 3) + 1)` into COMP-2 stopped on the lane assertion; so did
  `ABS (- ABS (A / 3))`, which also failed on main (R2-2). Both compile now.
- `ArgumentLaneSweepTests` generates argument shapes and compiles them: 864 statements per program at 2023 (12 atoms, each alone and
  under 8 operations, at 8 positions) and 504 at 85 (8 atoms, 7 positions), under six receiver kinds at 2023 and the two fixed ones at 85.
  The atoms are an item, quotients that terminate and that do not, negations, nested calls, a call as a divisor, a subscripted item, and
  (from 2002 on) SUM, RANGE, MEAN and MIDRANGE over a floating-point item beside a quotient. The positions are numeric, integer and
  result-type. It requires each to compile. Without the call's one decider (`FunctionValueLane.IsBinary64` in `RenderNum`), 168 of 864
  statements fail under each floating-point receiver (round 3); without the round-2 call arm, 56 of the then 270.
- The first form of the fix, a fixed 17-digit working scale, cut `ABS (1 / C / C)` to 0 and `ABS (1 / T)` under a `+9.999E+99` mask to
  +0.000E+00. It is deleted.

**The landing, by the value's lane** (the coordinator's rulings, the second narrowing the first at review R4-1;
`FunctionValueLane.DeliversByNearest`).
- An exact value lands in a COMP-1 or COMP-2 receiver by the receiver's mode, truncation without ROUNDED, as on main
  (`COMPUTE F2 = R`). That includes an exact SDIDI value: a floating-point literal or a NUMVAL parse (kb/Work PB1196, PB3839).
- A value on the SDIDI into which a quotient that kept significance went is already an approximation, so it converts to binary64 by
  round-to-nearest-even.
- Between rounds 1 and 4 it truncated, and `SUM (A / 3, A / 3, A / 3)` stored 6.999999999999999 for main's 7. A MOVE of that to PIC 9
  then gave 6.
- Now SUM (A / 3 x3) and ABS (A / 3 * 3) are 7, the MOVE gives 7, and ABS (A / 3), MAX (A / 3, 1) and MEAN (A / 3, 1) print main's
  2.3333333333333335 and 1.6666666666666667 again (`pb5891_argument_receiver_scale` line 15 is main's value again).
- kb/Work PB5893's dated evidence file states the rule and why exact SDIDI values keep truncation.
- Review R5-1: the approximation mark now passes through every node kind (`FunctionValueLane.Marked`, power and negation included).
  `FunctionValueLaneDriftTests.EveryArgumentCarrierArm_DecidesTheApproximationMark` holds every arm of the walk to deciding it.
  `ABS ((A / 3 * 3) ** 2)` is 49 into COMP-2, as on main, where the power arm had dropped the mark and printed 48.99999999999999; a
  fixed-point receiver gives 048.99999.
- Review R5-2 (the coordinator's ruling): under COMP-2 a function discontinuous at the exact value (REM, INTEGER, FRACTION-PART, SIGN
  of `A / 3 * 3`) now agrees with main's own fixed receivers' decimal truncation (6.99999, 6, .99999, -1), landed by nearest. Main's
  0, 7, 0, 0 under COMP-2 came from binary64 rounding 7/3 * 3 back to 7.
- Review R5-3: `COMPUTE F2 = R` with R = .1 prints 0.09999999999999999 on main, so `ABS (O / 10)`, an exact quotient, lands so too.

**One decider of a call's family** (review R3-1, the coordinator's ruling).
- `FunctionValueLane` reads a call's arguments in the renderer's argument context, and DOC-A.1-92b is stated there
  (`FunctionValueLane.IsBinary64`): a binary64 argument makes the call binary64, also beside a value that is on the SDIDI only because it
  keeps significance.
- `IntrinsicRenderer.RenderNum` takes the float lane exactly when that predicate says so, and `LaneOf` reads the same prediction.
- Before it, `ABS (SUM (FG, A / 3) * 3)` into COMP-2 stopped on the lane assertion where main prints 8.5. Into a floating-point edited
  item it stopped on main too (R3-2). Both print 8.5 now.
- The renderer taking the family of a binary or negation node from `LaneOf` was tried and reverted. It moved the run-time binary64
  marks of kb/Work PB4891, PB4923 and PB4753 (and PB4975's golden), and made NIST IF120A an internal compiler error. It is reported
  for a ruling.

**Every printed difference from main** over every review probe is listed in kb/Work PB5893's landed file, each with its kind:
- the fix's values;
- values closer to the exact one (EXP, SIN, VARIANCE, STANDARD-DEVIATION, RANGE, REM (X / 3, 7) = 3 for main's 6, among them);
- values that are the double just below the exact value, by the truncation ruling;
- compiles that failed on main.

**A sibling of PB5892 (rejects legal source).** The STOP and GOBACK status operand (§2023:14.9.42.3 SR2, §2023:14.9.18.3 SR6) asked
`Scale: 0` for "an integer data item", so `STOP RUN WITH NORMAL STATUS U` over U PIC 99P COMP was refused COBOL1704. It asks
`NumericLanes.IsIntegerByValue` now (case `pb5892_status_p_scaled_integer`; the run passes 40 to the operating system for U = 40).

**PB5892 (rejects legal source, 2023).** CONCAT asked `Scale != 0` of a numeric argument, the spelling, so `CONCAT ("A" U)` over
U PIC 99P was refused COBOL1627.
- It asks `NumericLanes.IsIntegerByValue` now and returns "A04" (§2023:15.18.4 RV1; the P positions are not characters,
  §2023:13.18.40.4 GR14d).
- BASECONVERT's unsigned-integer test asks the same question (no change of behaviour).
- `IntrinsicRenderer.RawWideArg` (`x.Scale != 0`) and `ReportWriterEmitter.VaryInteger` (`x.Scale == 0`) choose how an integer's
  UNSCALED value is formed, not whether it is an integer, so neither changes.

**Cases.**
- `pb5893_argument_float_receiver_every_position` (2014, 2023): REM, MOD, ABS, MAX, SUM, MEAN and ANNUITY under COMP-2, COMP-1 and a
  fixed resultant; the DOC-A.1-92b control REM (FG * 3, 1) = 0.30000000000000004; the small quotients 1E-36, 9.9999995E-37 and
  +3.333E-25; and the terminating-literal line 10.25.
- `pb5893_argument_fixed_receiver_85` (85, 2014, 2023): 1985 has no floating-point usage, so it shows the fixed-resultant values only.
- `pb5892_concat_p_scaled_integer` (2023).
- The refusal `pb5892-concat-p-scaled-fraction` (V PIC P9 holds .05, still COBOL1627).
