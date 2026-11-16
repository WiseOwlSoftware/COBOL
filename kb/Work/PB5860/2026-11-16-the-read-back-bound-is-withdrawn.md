The Mythos lane withdrew, on 2026-11-16 at 12:32 PST, the bound it set on this note's fix; the decision follows, with the two 1985 sentences it asked to be cited.

# For a dated file beside kb/Work PB5860 and one beside kb/Work PB5815: the read-back bound of PB5860 is withdrawn

Decided on Monday 2026-11-16 at 12:32 PST by the Mythos lane, which set the bound that morning (the ruling of
batch 10r, mechanism 1, in `kb/Work/PB5860.md`).

## What the note decided, and what is wrong with it
PB5860 found that a variable-length record sequential file written with ADVANCING, or under LINAGE, answers 39 at
OPEN INPUT and 47 at READ, and ruled the 39 wrong: 39 is for a conflict "between the fixed file attributes and the
attributes specified for that file in the program", and here the program's own description created the file. That
ruling stands.

The note then DECIDED "as a bound on the fix": "every file a program can write through a file description it can
read back through the same description, record for record". That bound was set without weighing the other
conforming answer, and it contradicts the fix of kb/Work PB5815, written before the bound existed and reviewed
since: a record sequential print file (its description has a LINAGE clause, or a WRITE with the ADVANCING phrase
in the program names it) does not support read operations, so OPEN INPUT and OPEN I-O of it answer 37, and the
register says so once (`docs/conformance/7/DOC-A.1-146.md`). My own ruling of PB5815 had allowed exactly that: "An
implementor may refuse such a file at OPEN; it may not deliver records that were never placed."

## The decision
THE BOUND IS WITHDRAWN. The 1985 text gives the refusal its own status: 37, for an OPEN of a file that "will not
support the open mode", whose third case is that "the INPUT phrase is specified but the file will not support
read operations". The two sentences, each checked with `cite.py --check`:
- `§1985:VII.1.3.5`: "that file will not support the open mode specified in the OPEN statement"
  `OK  §1985:VII.1.3.5  (1.3.5 I-O Status)  the text is in the clause; it lies in GRL2.3d`
- `§1985:VII.1.3.5`: "The INPUT phrase is specified but the file will not support read operations"
  `OK  §1985:VII.1.3.5  (1.3.5 I-O Status)  the text is in the clause; it lies in GRL2.3d.3`

Which files support read operations is the implementation's to say, the form of "a representation of a printed
page" is the implementor's (`§1985:VII.4.7.4 GR16`: "on a representation of a printed page"):
`OK  §1985:VII.4.7.4 GR16  (4.7.4 General Rules)  the text is in GR16`
and this implementation says a record sequential print file is written and not read. What 1985 does not allow is
the 39 (no attribute conflict exists) and the delivery of records no WRITE released.

So PB5860's defect is the STATUS, and it closes by the fix of PB5815 when its repro
(`E:/COBOL-coord/scratch/my-20261115/contradictions-b10r/adv39.cob`) answers 37 at OPEN INPUT for the ADVANCING
file and the LINAGE file and 00 for the control file. Its three 1985 rows (`1985-GR-VII.2.8.4-4`,
`1985-GR-VII.4.3.4-8`, `1985-GR-VII.1.3.5-L2.3`) are read again on the fixed code.

## What follows elsewhere
- The witness of kb/Work PB5845 (`pb5845_linage_open_refused_85`) had two lines that expected 00 from OPEN INPUT of
  a LINAGE file and from its CLOSE. On the tree that holds both fixes they are 37 and 42; the operator's finisher
  changes them (its mail of 12:30, not objected to). The line still shows its point: the LINAGE values are not
  checked at OPEN INPUT, so the status is not 90.
- The row `1985-GR-VII.4.7.4-13` (CLOSE, OPEN EXTEND and one more line end) no longer closes by a read-back: the
  fix refuses the OPEN. It is ruled by CLAUDE.md rule 1's order, GnuCOBOL's `libcob/fileio.c` read first, when
  PB5815 is on main.
- Why this happened: two rulings of one lane, two days apart, on one mechanism, and the second did not read the
  first note's fix in flight. Before a ruling sets a bound on a fix, it reads every open note on the same code
  site and the branch that holds its fix.
