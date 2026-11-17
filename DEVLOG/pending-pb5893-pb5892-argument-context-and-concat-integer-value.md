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
- `ArgumentLaneSweepTests` generates 270 argument shapes and compiles them:
  - under six receiver kinds at 2023, and the two fixed ones at 85;
  - at numeric, integer and result-type positions;
  - from atoms, quotients, negations and nested calls, two levels deep.
  It requires each to compile. Without the call arm's fix, 56 shapes per floating-point receiver fail.
- The first form of the fix, a fixed 17-digit working scale, cut `ABS (1 / C / C)` to 0 and `ABS (1 / T)` under a `+9.999E+99` mask to
  +0.000E+00. It is deleted.

**The landing.** The function's exact value lands as any exact value does: truncation without ROUNDED (the coordinator's ruling; the
landing rule is unchanged). So `COMPUTE F2 = FUNCTION ABS (A / 3)` prints 2.333333333333333, the double just below 7/3, as main's own
`COMPUTE F2 = R` does for the same exact value (`pb5891_argument_receiver_scale` line 15; kb/Work PB5893's dated evidence file).

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
