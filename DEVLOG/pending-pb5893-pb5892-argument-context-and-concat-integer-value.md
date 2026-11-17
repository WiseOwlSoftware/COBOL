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
and `RawArgPairs`) and the argument landing `Landed` pass it. `IntrinsicArgumentIntakeContractDriftTests.EveryArgument_RendersInTheArgumentReceiver`
holds every `num.AsNum(` and `num.Landed(` of the renderer to it. After the fix every line of the probe prints the derived value
(`runs-br/rcv23-001`). One golden line moves by the rule: `COMPUTE F2 = FUNCTION ABS (A / 3)` (A = 7) prints 2.33333333333333,
the nested quotient at 14 guard digits as under a scale-0 fixed resultant, where main printed the binary64 2.3333333333333335
(`pb5891_argument_receiver_scale` line 15).

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
