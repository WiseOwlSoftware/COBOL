**2026-11-16, landed (wave 20262068, group A): a record sequential print file is not opened INPUT or I-O; the OPEN is '37'.**

Re-probe on the branch build before the fix: `prt.cob` at `--std 85` printed `A1 00 [LF LF A]`, `A2 00`, `A3 00` (no AT END),
`L-OPEN 00`, `L-REW 00`, and `p6dead.cob` three wrong records, as the note says.

The determination (`docs/conformance/7/DOC-A.1-146.md` (c′), `docs/FILES_DESIGN.md` "A record sequential print file
supports no read"): of the two choices the note left open (return the records as placed, or refuse at OPEN), the OPEN is
refused, because nothing on the medium separates a record from a print control (a record may hold a line-feed byte), so
the first choice would be a guess. A record sequential file whose description has a LINAGE clause, or that a WRITE with the
ADVANCING phrase names (executed or not, which settles the batch-7 lead: the classification stays the description's,
and the determination now says so), does not support read operations: OPEN INPUT and OPEN I-O are '37'
(`§2023:9.1.13.6` item 6 a) 3., `§2023:14.9.27.4 GR16`; `§1985:VII.1.3.5`, `§1985:VII.4.3.4 GR17`), asked once above the
organizations (`FileConnector.SupportsReadOperations`). A LINE SEQUENTIAL file is not one. OPEN EXTEND still writes, and
the layout notice no longer fires for such a file's EXTEND. After the fix `prt.cob` prints `L-OPEN 37`, `L-REW 49` and READ
`47`; the witness is `conformance:cases/VII.4.3@1985-OPEN-STATEMENT/pb5815_print_file_not_read_back_85`.

Not decided here: `1985-GR-VII.4.7.4-13` (the extra line end after CLOSE and OPEN EXTEND, the dated file
`2026-11-15-extend-after-close-adds-a-line.md`). With the file no longer read back, the extra line is a question of the
printed page position across a CLOSE, not of the records; it stays open for its registrar.
