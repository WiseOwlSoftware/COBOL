## PB5893 and PB5892: every function argument is evaluated in the argument context, and CONCAT tests an integer by its value

**PB5893 (wrong answer, every edition with a floating-point usage).** A function argument's evaluation technique followed the
statement's RECEIVER at every position but an integer one: under a COMP-1 or COMP-2 resultant an all-fixed-point argument was
evaluated in binary64. `COMPUTE F = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)` over 31-digit X and Y stored 672 where a
fixed-point resultant stores 41; ABS, MAX, SUM, MEAN and ANNUITY over E = 41 printed 0, 1, 1, 2 and 0.0000 for 41, 41, 42, 22 and
0.9000 (main d77571151b, run `E:/COBOL-coord/scratch/my-20261115/w20262099/d/runs-main/rcv23-002`). kb/Work PB5891 review R3-1 had
fixed only the integer positions (`IntrinsicRenderer.ReceiverFor`).

**Fix.** An argument is evaluated individually (§2023:8.4.3.2.4 GR2), so its technique is its own operands' (DOC-A.1-92b, unchanged:
a floating-point argument still takes binary64) and the receiver takes the function's value by the ordinary rules.
`NumericRenderer.ArgumentReceiver` (the receiver without its floating-point facts, at its scale) is now the one context of every
argument render in `IntrinsicRenderer`: `ReceiverFor` is deleted, and the nine intakes that rendered under `num.Receiver`
(`PresentValueBase`, `DomainArg`, `DecOf`, `ExactValueOf`, `DblOf`, the table(ALL) enumerations of `RenderWide`, `AlignedArgs`
and `RawArgPairs`) pass it. `IntrinsicArgumentIntakeContractDriftTests.EveryArgument_RendersInTheArgumentReceiver`
holds every `num.AsNum(` of the renderer to it. After the fix every line of the probe prints the derived value
(`runs-br/rcv23-001`). The argument context's scale is one rule, `NumericLanes.ArgumentScale`: the receiver's scale, and under a
floating-point receiver at least the 17 significant digits of binary64, so a nested quotient keeps 31 fraction digits and its
value reaches the binary64 receiver whole (the coordinator's ruling; `LaneOf` reads the same scale). The function's exact value
then lands as any exact value does (truncation without ROUNDED, the coordinator's ruling; the landing rule is unchanged), so
`COMPUTE F2 = FUNCTION ABS (A / 3)` prints 2.333333333333333 where main's binary64 argument gave 2.3333333333333335, as main's own
`COMPUTE F2 = R` prints for the same exact value (`pb5891_argument_receiver_scale` line 15; the evidence is kb/Work PB5893's dated
file of 2026-11-17).

**A sibling of PB5892 (rejects legal source).** The STOP and GOBACK status operand (§2023:14.9.42.3 SR2, §2023:14.9.18.3 SR6) asked
`Scale: 0` for "an integer data item", so `STOP RUN WITH NORMAL STATUS U` over U PIC 99P COMP was refused COBOL1704; it asks
`NumericLanes.IsIntegerByValue` (case `pb5892_status_p_scaled_integer`; the run passes 40 to the operating system for U = 40).

**PB5892 (rejects legal source, 2023).** CONCAT asked `Scale != 0` of a numeric argument, the spelling, so `CONCAT ("A" U)` over
U PIC 99P was refused COBOL1627. It asks `NumericLanes.IsIntegerByValue` now and returns "A04" (§2023:15.18.4 RV1, the P positions
are not characters, §2023:13.18.40.4 GR14d); BASECONVERT's unsigned-integer test asks the same question (no change of behaviour).
`IntrinsicRenderer.RawWideArg` (`x.Scale != 0`) and `ReportWriterEmitter.VaryInteger` (`x.Scale == 0`) choose how an integer's
UNSCALED value is formed, not whether it is an integer, so neither changes.

**Cases.** `pb5893_argument_float_receiver_every_position` (2014, 2023: REM, MOD, ABS, MAX, SUM, MEAN, ANNUITY under COMP-2, COMP-1
and a fixed resultant, and the DOC-A.1-92b control REM (FG * 3, 1) = 0.30000000000000004), `pb5893_argument_fixed_receiver_85`
(85, 2014, 2023: 1985 has no floating-point usage, so it shows the fixed-resultant values only), `pb5892_concat_p_scaled_integer`
(2023) and the refusal `pb5892-concat-p-scaled-fraction` (V PIC P9 holds .05, still COBOL1627). The whole conformance assembly
(NIST included) passes, 28037 of 28037.
