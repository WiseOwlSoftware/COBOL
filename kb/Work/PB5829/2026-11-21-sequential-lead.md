## 2026-11-21 - the sequential lead measured: g85_bf25_01 cannot fail on GR25's second half; the row has a witness that can (wave 20262132 group e)

`1985-GR-VII.4.3.4-25` is §1985:VII.4.3.4 GR25, two requirements in one sentence: "The minimum and maximum record sizes
for a file are established at the time the file is created and must not subsequently be changed."

**g85_bf25_01, read leg by leg against a compiler that breaks each half.**
- *First half* (sizes established at creation): S4/S5 re-create the file through F-B (15/15) and read its one record
  back. A compiler that keeps a file's earlier sizes across a re-creation (one that records them in the file and does
  not re-establish them at OPEN OUTPUT) reads the 15-character record at 10 and prints another S5 line: the case CAN
  fail on this half.
- *Second half* (not changed after creation): every OPEN of the case that is no creation (S2's EXTEND and S3's INPUT
  through F-A, S5's INPUT through F-B) uses the sizes of the creation it follows, so a compiler that takes the sizes
  again from the description at such an OPEN, or re-frames the records there, prints every line the case expects. The
  case CANNOT fail on this half; this is the lead's claim, confirmed by the case's own source (no leg opens the existing
  file, without creating it, through a description of other sizes).

**The witness that can.** `conformance:cases/VII.4.3@1985-OPEN-STATEMENT/pb5829_sizes_unchanged_by_later_open_85`
creates the file at 10/10 through F-A, opens it EXTEND through F-B (15-character records; no creation; nothing
written; 00, since DOC-A.1-129 validates no record size of a fixed-length sequential file, the latitude of GR8), and
reads it back through F-A: 00 `REC-ONE`, 00 `REC-TWO`, 10. A compiler that records the sizes and takes 15/15 from F-B at
the EXTEND reads the 10-character records 04 (§1985:VII.1.3.5 GRL2.1b, "does not conform to the fixed file attributes
for that file") and fails S3; one that re-frames the records there fails the record areas. Run at `--std 85` on this
branch's build: the expected output, exactly (`E:/COBOL-coord/scratch/w20262132e/probes/pb5829_sizes_unchanged_by_later_open_85-001`).
Recorded as a second witness of the row (`record_verdicts.py --edition 1985`, a witness-only record): its `test-ref`
is now both cases. This is PB5819's measure (a compiler exists that breaks the rule and fails the test), the same
strength as `pb5819_record_sizes_kept_after_extend_85`; against an implementation that records no size, as this one
for a fixed-length sequential file, the leg holds trivially, as that one's does.

So the lead is closed and nothing of this note is 1985's: `editions` drops 1985 (the six 2023 findings stay 2023's).
