## PB3390: the closed formats' error production has two forms, and the data division's run holds no unsigned integer

**The defect.** The residue run of the closed general formats (`unrecognizedClause`, refused by name by
`ClosedFormatPass`) had one opening set and one tail set for every list. One token reads differently by list: an
unsigned integer after the data division's three lists (data description entry §13.16.2, FD §13.4.5.2, SD §13.4.6.2)
may be the next entry's level-number (ISO §8.3.2.2.13), while after every other list nothing begins with one. The
shared set left the integer out of the opening and in the tail, which was wrong for both. `SOURCE-COMPUTER. XYZ 12.`
and `OBJECT-COMPUTER. XYZ 12.` read XYZ as the residue, and the leading-comma decimal (`XYZ ,5.`) did the same.
`01 X PIC X FOO` followed by `05 Y PIC X.` with the period forgotten read `FOO 05 Y` as one run and reported Y's
PICTURE as X's second (COBOLNET2423). Re-probed on the branch's own build, all four shapes reproduced
(`E:\COBOL-coord\scratch\w1129b\p`). Every probe was already refused, so the harm was the message only.

**The fix.** The production now has two forms. `unrecognizedClause : (residueOpening | residueInteger) (residueItem |
residueInteger)*` serves every list that no level-number follows. `unrecognizedDataClause : residueOpening
residueItem*` serves the three data division lists. `residueOpening` now holds the leading-comma decimal,
`residueItem` is `residueOpening | & concatOperand`, `residueInteger` holds the integer and the integer-led comma
decimal, and `genericClause` is gone. The data form leaves out `1,5` too, though it is no level-number (§8.3.5 2)),
because an alternative led by INTEGERLIT under a predicate would throw NoViableAlt at a level-number. The parse layer
refuses it by name instead: the after-period exclusion in `CobolErrorStrategy` spares a token that heads a longer
literal, using `CobolParserCoreBase.NumericLiteralWidth`, which is now shared with `numericLiteralIsLeftOperand`.
`ClosedFormats.ErrorProductionRules` is the one place the parse layer learns the two rules. `ClosedFormatPass`
refuses both forms through one `Refuse`, and `ClauseCardinality`, `DataClauseKind` and `FileDescriptionFormats`
follow the data form. A predicate on the format was rejected: it is the NoViableAlt trap PB3373 recorded, and it
would take the decisions off the DFA cache.

**The guard.** `ClosedFormatDriftTests` now reads the generated ATN. It checks that the run's pieces are called only
by the two forms. It checks that each list writes the form its follow set calls for: the data form exactly where
INTEGERLIT may begin what follows the list's period, derived and not listed. It also checks that every
`numericLiteralCore` shape appears exactly once, either in the opening or in `residueInteger`.
`ClosedFormatReservedWordTests` adds sixteen literal cases and a level-number control over the data entry, the FD
and the SD.

**Goldens.** Three negatives, refused at 85, 2002, 2014 and 2023:
- `pb3390-object-computer-integer-clause` (COBOLNET1970 '12')
- `pb3390-source-computer-comma-decimal-clause` (COBOLNET1970 ',5')
- `pb3390-data-entry-level-number-after-run` (COBOL0307 at '05')

One positive, `pb3390_integer_lists_beside_the_residue`, accepted at 85 and printing `BCD` per §12.3.7.4 GR11 b) and
GR7 k) 2.: integer lists that a clause owns keep their integers.

**Rows.** None re-verdicted. The five FMT rows were already CONFORMS. `OBJECT-COMPUTER. XYZ FOO IS 12.` (PB3391,
the predicated `segmentLimitClause`) is not this root and stays open.
