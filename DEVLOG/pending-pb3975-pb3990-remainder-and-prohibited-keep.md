## PB3975 + PB3990: DIVIDE's remainder after a quotient size error follows GnuCOBOL, and ROUNDED MODE IS PROHIBITED never stores a value it cut

Wave 20261740 group A, the arithmetic emitter's two notes, each re-probed on main 1da5e0d6b against GnuCOBOL 3.2.

**PB3975 (DIVIDE ... GIVING q REMAINDER r).** The filed case (`DIVIDE 1000 BY 4 GIVING A REMAINDER R`, A `PIC 99`) is not
a defect: main and GnuCOBOL both print `50 0`. The cases that differed: with no SIZE ERROR phrase and checking off,
`DIVIDE 1003 BY 4 GIVING B REMAINDER R3` (B `PIC 99`) stored R3 = 803 where GnuCOBOL stores 003, and `DIVIDE 333 BY 1
GIVING B6 REMAINDER R8` (B6 `PIC 9`) stored 30 where GnuCOBOL stores 00. §14.9.12.4 6) c) (`cite.py --check 14.9.12.4 "If
the size error condition is not raised, a subsidiary quotient is developed"` OK) gives the subsidiary quotient
identifier-3's digit count only when no size error was raised, and then the quotient fits identifier-3; after a quotient
size error with checking off the receivers are §14.6.13.1.3 8)'s (`cite.py --check 14.6.13.1.3 "how any receiving operands
are affected"` OK), so CLAUDE.md rule 1 follows GnuCOBOL, which keeps every integer digit. The PB129 digit cap is gone
(`SubsidiaryShape` has no digits; `CobolNum.CapDigits`, `RuntimeApi.NumCapDigits` deleted; `CobolDec.SubsidiaryQuotient`
loses its digits parameter; runtime major 20 -> 21; the native back-multiply takes `NumericRenderer.QuotientDigitBound`).
The open question the note carried (a statement with ONLY a NOT ON SIZE ERROR phrase) is settled without the owner:
PB3989 gives the NOT-only statement the ON phrase's disposition for every receiver, so identifier-4 keeps its value as
under ON (where ISO decides and GnuCOBOL's 003 is against 6 c)). DOC-A.1-70 records both. Goldens
`14.9.12-DIVIDE/pb3975_remainder_after_quotient_size_error` (+ `_standard_decimal`); `pb129_remainder_no_phrase` now
R2=0001 (GnuCOBOL's); `SizeErrorDispositionTests` NOT-only rows.

**PB3990 (ROUNDED MODE IS PROHIBITED, no phrase, checking off).** Every receiver usage and category stored the value cut
(`COMPUTE D5 ROUNDED MODE IS PROHIBITED = V`, V 1.5: D5 = 01, COMP-5 001, a quotient 03, a product 02, an in-place sum
08, a COMP-1 0.099999994), and under STANDARD-DECIMAL the same statement terminated the run unit, because the SDIDI's
rounding table always raised and nothing caught it. §14.7.4.3 rule 7 (`cite.py --check 14.7.4.3 "the content of the
resultant identifier is unchanged"` OK) keeps the resultant whether or not checking is enabled; GnuCOBOL keeps it too.
The fix is one mechanism: PROHIBITED is never a rounding direction, so `CobolNum.BumpsMagnitude` and
`CobolDec.RoundFromRemainder` (and `FloatResultant`'s unchecked entries, `CobolEdit.FormatFloatStore`) raise the one
`CobolSizeError.ProhibitedInexact()` from every final transfer, checked or not, and a Try form answers false. A checked
statement's catch takes it as before; an unchecked one gets `ArithmeticEmitter.KeepsOnProhibitedInexact`, a try around a
PROHIBITED receiver's compute-and-store that catches that condition alone, so the receiver is unchanged and the statement
continues. That made four mechanisms redundant, and they are deleted: `CobolNum.DivideOrThrow`, PB654's StoreArith gate
(`CobolFloat.InexactAtScale`), `FromDouble`'s checked-only raise and `TransferWide`'s unchecked mask. DOC-A.1-70's PB1196
determination (truncate) is reversed, and `pb1196_float_resultant_rounded_mode`'s unchecked leg now keeps 5. A value
that is exact but too big still takes the low-order digits (150 -> 50, GnuCOBOL's). Golden
`14.7.4-ROUNDED/pb3990_prohibited_unchecked_keeps_the_resultant` (2014, 2023); GnuCOBOL differs only on the exact 2.0 (it
keeps 07) and 0.1 into a COMP-1 (it stores it), both ISO's. `SizeErrorDispositionTests` 24 rows, both lanes.

Inventory: two `record_verdicts` batches (witnesses, notes, three `DivideOrThrow` code-locations retired), no verdict
moved, GAP 31 -> 31. The fleet stop (STOP-loop) arrived before the gate.
