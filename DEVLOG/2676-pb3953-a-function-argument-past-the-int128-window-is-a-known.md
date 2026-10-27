## Entry 2676 — 2026-10-27 16:10 PDT — PB3953: a function argument past the Int128 window is a known departure from GnuCOBOL, and the note stays open

PB3953 asked whether an arithmetic expression used as a function argument is covered by the native-arithmetic latitude of
DOC-A.1-123, or should be exact to the 77-digit lane a COMPUTE has. Re-probe on a fresh build: `FUNCTION ABS (BIG * BIG * BIG)`
over three `PIC 9(18)` items of 999999999999999999 still shows `999999999999999997000000000000000100000000000000000000`, where
the exact product ends `...0002999999999999999999`. The boundary the re-probe measured is the 38-digit `Int128` window, not 34
digits: `ABS (BIG * BIG * K2)` with `K2 PIC 99` (38 digits) is exact, `ABS (BIG * BIG * K3)` with `K3 PIC 999` (39 digits) ends
`...100000` for `...000999`, and `FUNCTION MOD (BIG * BIG * K3, 1000)` answers 0 where the exact residue is 999. Under
`ARITHMETIC IS STANDARD-DECIMAL` the same ABS ends `...000000`, the 34-digit SDIDI, which is what the standard requires there
(§8.8.1.5.2 "a maximum precision of 34 decimal digits").

Mechanism: `NumericRenderer.Settle` lowers the exact 256-bit intermediate (`NumX.Wide`) to the SDIDI by round-to-odd for every
consumer that is not a final transfer, and `ExactWideSettlementDriftTests` closes the list of consumers that take it exact at the
arithmetic statements and the relation. A function argument is not on it, no function body has a wide carrier, and ABS, MAX, MIN
and MEDIAN would also need a wide returned value, which `FunctionValueField` (a `CobolDec`) does not have.

First pass, refused by review round 1 (R1-1): the first adjudication recorded the limit in DOC-A.1-123 as a determination and
closed the note, naming its departure from the precedence order of CLAUDE.md rule 1 (GnuCOBOL's argument temporary is
arbitrary-precision). The review found that §8.8.1.3 and §15.4.1 make the width an implementor latitude, that for a latitude rule 1
makes GnuCOBOL's behaviour the answer, and that no owner decision departs from it; closing the note also took a silent wrong
answer out of `work.py next`, and two golden legs pinned the non-GnuCOBOL value as conforming. The finding is right.

Now: the exact carrier for the function family (a `CobolWide` form for each of about fifteen bodies, ABS, MAX, MIN and MEDIAN
returning a wide value, the family added to the `ExactWideSettlementDriftTests` list, and the `Intrinsic*DriftTests` contracts
that close the intake vocabulary) was weighed as a redesign beyond a review round, so under I9 it is a note and not a fix:
PB3953 is `open` again, with its acceptance rewritten to the exact lane. DOC-A.1-123 states the limit as a known departure that
PB3953 tracks, and says the standard-decimal image is the value there. The comments at `IntrinsicRenderer.RawArg` and
`FunctionValueField.OfValue` say the same. The golden is renamed `pb3953_function_argument_int128_window` and keeps only the
exact legs (the 38-digit ABS and MOD, and the COMPUTE that keeps every digit of the 39-digit product); the two legs that pinned
the 39-digit image are gone, and the fix adds its own. `pb3953_function_argument_below_2014` stays as its negative (ABS is a
2014 function, COBOL1502 at 85 and 2002). No inventory row is claimed or closed.

Found on the way and not fixed (another mechanism): an `EVALUATE` subject that is an arithmetic expression is held in a
`S9(21)V9(17)` temporary, so `EVALUATE BIG * BIG WHEN 1 ... WHEN BIG * BIG` (a 36-digit subject) takes OTHER where the second WHEN
equals the subject; without the first WHEN it is exact. It is a lead in the implementer's report.

