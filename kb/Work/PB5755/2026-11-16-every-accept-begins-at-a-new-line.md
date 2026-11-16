# Ruling (the Mythos lane, Monday 2026-11-16 01:50 PST): every ACCEPT begins at a new line of the device; the register entry on the size of a transfer was incomplete, the compiler is not defective

## What was found
The writer of the 1985 ACCEPT witnesses (`E:/COBOL-coord/scratch/my-20261115/harness-witnesses/writable.json`, row
`1985-GR-VI.6.5.4-3`, its `defect` record; repro `harness-witnesses/run/defect-equal-size-001/`) fed an 85-character
line and then a line `NEXT1`, and executed `ACCEPT R80` (PIC X(80)) then `ACCEPT R5`. It derived `BBBBB` for R5 (the
second 80-character record of the first line) and observed `NEXT1`: when a receiver is filled exactly at a record
boundary, the compiler drops the line's remaining records.

## The text
`§1985:VI.6.5.4 GR2` leaves the size of a data transfer to the implementor, for each hardware device. GR3 and GR4
say what is stored from "the transferred data" and when additional data is requested. Neither says what a device
does with data a satisfied ACCEPT did not request, or where the next ACCEPT's transfer begins. That is the
device's, and so the implementor's.

## The determination, and why it is this one
`docs/conformance/7/DOC-A.1-5.md` defines the transfer as one 80-character record, a longer line as consecutive
records, and says of a SMALLER receiver that it "ignores the rest of the record AND the line's remaining records
(GR4b)". It says nothing of a receiver that one record, or a whole number of records, fills exactly. The code
treats every receiver alike (`AcceptSource.cs#Device`: it reads one line for an ACCEPT and keeps the records the
receiver takes): every ACCEPT begins at a new line of the device.

By the precedence of CLAUDE.md rule 1 GnuCOBOL decides a choice the standard leaves open. Its runtime reads ONE
LINE for each ACCEPT and moves it to the receiver, so the rest of a long line never reaches the next ACCEPT
(`libcob/termio.c`, `cob_accept`, the loop under the comment "Read a line", in the 3.3-dev source; read on
2026-11-16, not run: the compiler is not installed on this host).

## The ruling
- The compiler conforms. No defect note.
- DOC-A.1-5 is INCOMPLETE and is corrected in the branch that lands the witnesses. Its sentence that begins "A
  larger receiver requests further records until filled" is replaced, from those words to its end, by THE CORRECTED
  WORDS below. They were corrected once after the refuter's reading
  (`E:/COBOL-coord/scratch/my-20261115/harness-refute/refute.json`): a receiver that two or more records fill is
  GR4a's case, not GR3's, and the line's remaining records were never transferred, so ignoring them is this
  implementation's determination and not GR4b.

  THE CORRECTED WORDS: "A larger receiver requests further records until filled (GR4a, left-aligned); a smaller one
  keeps the leftmost characters and ignores the rest of the record (GR4b); a receiver that one record fills exactly
  takes it whole (GR3). Every ACCEPT begins at a new line of the device: the records of a line that the receiver
  did not take are ignored, whatever the receiver's size. The standard does not say where the next ACCEPT's data
  begins, so that is the implementor's, and GnuCOBOL likewise reads one line for each ACCEPT (it does not split a
  line into records). End-of-input leaves the unfilled remainder as spaces; a zero-length receiver consumes one
  record and ignores all of it (GR4b's closing sentence)."
- The witness of `1985-GR-VI.6.5.4-3` (`g85_z06`) is re-derived on the entry as corrected. What it can pin of GR3
  is that a record of the receiver's own size is stored whole. Where the next ACCEPT's data begins is the
  determination's, and a line that pins it says so.
