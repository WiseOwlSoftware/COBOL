# Ruling on the last open reading of the 1985 code reads: CLOSE, OPEN EXTEND and one more line end (`1985-GR-VII.4.7.4-13`; the Mythos lane, 2026-11-16)

The row stayed `undecided` in the rulings of the ten open readings (`kb/Work/PB5683/2026-11-16-rulings-open-readings.md`,
section 10) because the fix of kb/Work PB5815 was to decide it. That fix is on main (pull request 1811): a record
sequential print file is not read back (OPEN INPUT and OPEN I-O answer 37). So the first way the row could close,
"the added line is a record no WRITE released", is gone: nothing reads the file as records. The ruling of batch 7
named what decides it then: CLAUDE.md rule 1's order, GnuCOBOL first.

## The case
`p7ext` (the checker's program, `E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/`): file F is opened
OUTPUT and written AA, then BB, each `AFTER ADVANCING 1 LINE`: the file holds `0A 41 41 0A 42 42 0A`. File G is
opened OUTPUT, written AA, CLOSEd, opened EXTEND and written BB the same way: `0A 41 41 0A 0A 42 42 0A`, one line
feed more, so on a printed page BB stands two lines below AA and not one.

## The text does not decide it
`§1985:VII.4.7.4 GR13` says a WRITE to a file opened EXTEND adds records "as though the file were open in the
output mode": the records are in order, AA then BB, in both files. What ADVANCING positions is a line "on a
representation of a printed page" (`§1985:VII.4.7.4 GR16`), and the form of that representation is the
implementor's. Nothing in the WRITE or CLOSE rules says where the page stands after a CLOSE and a new OPEN: whether
the line that the last WRITE left open is ended by the CLOSE. Both answers keep every rule.

## GnuCOBOL decides it, and it does what the compiler does
GnuCOBOL (3.x branch, `libcob/fileio.c`, read from the source, not run): `sequential_write` writes the controls of
a WRITE AFTER before the record and then sets `flag_needs_nl` (lines 2524 to 2530); the record follows with no line
end (line 2551). `cob_file_close` then ends that line: for a file that is not line sequential, when `flag_needs_nl`
is set it writes one line feed (lines 2209 to 2213). So G's first CLOSE writes a line feed after AA, and the WRITE
after the OPEN EXTEND writes its own before BB: two line feeds between AA and BB, and one after BB at the last
CLOSE. Those are the compiler's bytes for G, and F's for F.

## Ruling
**CONFORMS, a recorded determination.** The reader's verdict stands. No code changes.

**Register sentence owed (1985 and later, beside `docs/conformance/7/DOC-A.1-146.md`'s account of how lines are
advanced; a new determination `D-PRINT-CLOSE-ENDS-THE-LINE`):** "When a record sequential print file is closed
after a WRITE with the AFTER ADVANCING phrase, the CLOSE ends the line that WRITE left open (one line feed). A file
then opened EXTEND begins on a new line, so its first WRITE AFTER ADVANCING 1 LINE leaves one empty line after the
last line of the earlier OPEN. The 1985 text fixes the order of the records (`§1985:VII.4.7.4 GR13`) and leaves the
form of the printed page to the implementor (`§1985:VII.4.7.4 GR16`); GnuCOBOL does the same (`libcob/fileio.c`:
`sequential_write` and `cob_file_close`)." Its witness is a case that writes the two files of `p7ext` and compares
their bytes through a record sequential read of a NON-print copy, or a unit test of the connector's bytes: the
writer of the entry chooses the one the tree's tests already use for print streams.

## For the store
`python scripts/spec/code_reads.py adjudicate --edition 1985 1985-GR-VII.4.7.4-13 --verdict CONFORMS --reason "The
1985 text does not say where the page stands after a CLOSE; GnuCOBOL's CLOSE ends the line a WRITE AFTER left open
(libcob/fileio.c), as the compiler's does; recorded as the determination D-PRINT-CLOSE-ENDS-THE-LINE." --ref <this
file's tracked copy>`. Its `work` note kb/Work PB5815 is landed and named no mechanism of this row beyond the
read-back, which is gone.
