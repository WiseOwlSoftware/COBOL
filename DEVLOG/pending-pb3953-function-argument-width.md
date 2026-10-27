## PB3953 adjudicated: a function argument is exact to the Int128 window, and DOC-A.1-123 now says so

PB3953 asked whether the determination in DOC-A.1-123 (native arithmetic techniques) covers an arithmetic expression used
as a function argument, or whether the argument should be exact to the 77-digit lane a COMPUTE has. Re-probe on a fresh
build: `FUNCTION ABS (BIG * BIG * BIG)` over three `PIC 9(18)` items of 999999999999999999 still shows
`999999999999999997000000000000000100000000000000000000`, where the exact product ends `...0002999999999999999999`. The
re-probe moved the boundary the note named. It is the 38-digit `Int128` window, not 34 digits: `ABS (BIG * BIG * K2)` with
`K2 PIC 99` (38 digits) is exact, `ABS (BIG * BIG * K3)` with `K3 PIC 999` (39 digits) ends `...100000` for `...000999`, and
`FUNCTION MOD (BIG * BIG * K3, 1000)` answers 0 where the exact residue is 999. Under `ARITHMETIC IS STANDARD-DECIMAL`
the same ABS ends `...000000`, the 34-digit SDIDI rounded to nearest, which is what the standard requires there
(§8.8.1.5.2 "a maximum precision of 34 decimal digits").

Mechanism: `NumericRenderer.Settle` lowers the exact 256-bit intermediate (`NumX.Wide`) to the SDIDI by round-to-odd for every
consumer that is not a final transfer, and `ExactWideSettlementDriftTests` closes the list of consumers that take it exact
at the arithmetic statements and the relation. A function argument is not on it, and no function body has a wide carrier
(ABS, MAX, MIN and MEDIAN would also need a wide returned value, which `FunctionValueField` does not have). Making the
argument exact is therefore a change to the whole function family, not to one expression, and the note's acceptance names
recording the case as an equal outcome. §8.8.1.3 and §15.4.1 leave native arithmetic and the returned value's representation
to the implementor, so no clause is violated; the departure from the precedence order of CLAUDE.md rule 1 (GnuCOBOL's
argument temporary is arbitrary-precision) is named in the row.

Landed: DOC-A.1-123 carries the determination, the numbers above and the reopening condition; `IntrinsicRenderer.RawArg` and
`FunctionValueField.OfValue` comment it where the lowering arrives; the golden `pb3953_function_argument_past_int128` pins the
38-digit and 39-digit legs of ABS and MOD and the exact COMPUTE (`BIG * BIG * K3 - (BIG * BIG * K3 - 999)` is 999), and
`pb3953_function_argument_below_2014` is its negative (ABS is a 2014 function, COBOL1502 at 85 and 2002). No inventory row
is claimed or closed.

Found on the way and not fixed (another mechanism): an `EVALUATE` subject that is an arithmetic expression is held in a
`S9(21)V9(17)` temporary, so `EVALUATE BIG * BIG WHEN 1 ... WHEN BIG * BIG` (a 36-digit subject) takes OTHER where the
second WHEN equals the subject; without the first WHEN it is exact. It is a lead in the implementer's report.
