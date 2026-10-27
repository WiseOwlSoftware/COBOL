## An arithmetic statement with only NOT ON SIZE ERROR is the phraseless statement (PB3869)

**Defect (fuzz run fuzz-20261026-225310, owner-approved 2026-10-27).** `EmitArith` took the checked path whenever the
`SizeErrorPhrase` object existed, and a phrase holding only `NotOnError` exists. The checked shape's disposition (the
receiver stays unchanged, the size error is swallowed, the ON SIZE ERROR imperative runs) belongs to ON SIZE ERROR, so
`DIVIDE 1 BY 0 GIVING Q NOT ON SIZE ERROR ...` continued with Q unchanged instead of ending the run unit,
`ADD 1 TO X` (PIC 9 VALUE 9) kept 9 where the phraseless result is 0, and `MULTIPLY 50 BY M` and `COMPUTE R = 5 / Z`
did the same. Re-probed on the branch build first: all four reproduced.

**The note's one-line reading was incomplete.** §14.7.5 does two things for this phrase. Its no-phrase paragraph ends
"If a NOT SIZE ERROR phrase is specified, it is ignored" (a size error with no SIZE ERROR phrase is disposed of as the
phraseless statement is, DOC-A.1-70), and its closing paragraph transfers control to the NOT SIZE ERROR imperative when
no size error condition occurred. Asking `OnError is not null` alone, and then treating the statement as phraseless,
would have dropped the NOT imperative on every statement that fits. The statement has to learn whether the condition
existed without changing how it is disposed of.

**Fix shape.** With EC-SIZE checking off and no ON SIZE ERROR phrase, `EmitArith` renders the operands unchecked and
sets `EcState.SizeErrDetectVar`; each receiver goes through the new `ArithmeticEmitter.DetectingStore`. It runs the
receiver's CHECKED form first (which changes nothing when the condition exists), and when that raised the condition (a
failed checked landing, a `CobolSizeError`, an `OverflowException`) it runs the UNCHECKED form from the same state: the
low-order digits land, or the zero divisor ends the run unit, exactly as without the phrase. A flag records the
condition and guards the NOT imperative. `GuardedStore`'s callback now takes the form it is asked for (the ten call
sites, including the CORRESPONDING pairs, say `checkedForm =>` where they used the statement's captured flag). With
EC-SIZE checking on, the existing shape was already right and is unchanged.

**Witnesses.** Golden `14.7.5-.../pb3869_not_size_error_alone` (2002 and later: the no-phrase rules start at 2002, and
the syntax is legal at 85, so there is no refusal row), `SizeErrorDispositionTests` (a NOT-only zero divisor ends the
run unit on every divide shape and both arithmetic lanes; fixed-point, edited and float stores over a size error and a
fit; the checking-on pair). DOC-A.1-70 and the CONDITIONS_EXCEPTIONS design now say a NOT-only statement is the
phraseless one.

**Sibling sweep.** CALL with only NOT ON EXCEPTION (missing program: terminates like the phraseless CALL), STRING and
UNSTRING with only NOT ON OVERFLOW, READ with only NOT AT END or NOT INVALID KEY: each probed on the branch build, none
shares the defect, because only the arithmetic phrase selects a checked shape.
