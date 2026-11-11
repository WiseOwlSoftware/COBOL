## PB4996: 116 refuter-upheld verdicts of 1985 code-read slices 43-49 and 57-70 recorded (1985 GAP 606 to 490); PB5272-PB5298 filed

The operator's Opus pipeline on account 2 read slices 57-70 (139 rules) and refuted their closing verdicts. It also refuted slices 43-49, whose 55 rules waited in the slices 36-56 recording. The refute files agree with the operator's counts:
- slices 57-63: 48 upheld, 9 overturned, 4 doubtful
- slices 64-70: 38 upheld, 12 overturned, 9 doubtful
- slices 43-49: 30 upheld, 0 overturned, 6 doubtful

That is 21 overturned of 156 refuted, 13 in 100. `coderead-85/merge-pipe-2.py` takes the slice list as an argument and never records a closing verdict that has no refuter case yet. `record_verdicts.py --edition 1985` wrote the 116 upheld verdicts, all with their tests, and the 1985 GAP went from 606 (records-85c) to 490. The 19 doubtful verdicts are held in `coderead-85/held-4.json`. Seven of them, in slice 66, share one question: which CD a duplicated cd-name references.

There are 59 non-conforming rules. I reran every repro on a build of origin/main f2cf36fd15. Four rows no longer reproduce (PB5070 and PB5084 fixed them) and stay at GAP. Seven rows belong to open notes, each named in an addendum: PB5240, PB5071, PB5250 and PB5242. The other 48 rows are in 27 new notes, PB5272 to PB5298, one per root:
- PB5278: the 2002 class rules applied to RECEIVE and SEND at 85.
- PB5279: RECEIVE INTO a subscript the CD update changes.
- PB5289: P-scaled integers and the other SEND operand screens.
- PB5290: `CheckRecords` counts storage bytes, not standard-data-format characters.
- PB5286: the MCS models logical paths as one flag per endpoint.
- Six notes on debugging-section triggers.

Of the refuters' sibling leads for slices 43-49, the PADDING CHARACTER lead no longer reproduces. The REPLACE-period and BLANK WHEN ZERO leads reproduce and are already held by PB5085 and PB5191. Every citation was checked with `cite.py --check`.

This branch merges records-85c (the PB4996 correction and PB5243's harm flags) and lands after it.
