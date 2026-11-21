## PB5914: before 2023 the ADVANCING write of a line-framed file keeps the record at its size

kb/Work PB5914 (a lead of PB5903): at `--std 85` a file with RECORD DELIMITER IS LINE-SEQUENTIAL took the plain WRITE at its
size (PB5903) but dropped the record's trailing spaces on a WRITE with ADVANCING, because the print-control arm
(`SequentialConnector.WriteAdvancingRecord`) always ran `TrimRecordEnd` on a non-record stream. A file that has taken one
ADVANCING write routes its plain WRITEs through that arm too, so the rule PB5903 set held only until the first one.

**Re-probe.** A writer with `RECORD CONTAINS 1 TO 10` and `WRITE OREC AFTER ADVANCING 1 LINE` of `AB` followed by eight
spaces left `AB` in the file (bytes `\r\n A B \r ...`); a reader's DEPENDING item would have shown 2.

**Ruling.** DOC-A.1-146 (c') is about a RECORD SEQUENTIAL print stream. A file the delimiter clause frames is a line-framed
file, and before 2023 no line sequential file exists, so §1985:VII.2.8.4 GR1 ("Any method used will not be reflected in the
record area or the record size used within the program", checked with `cite.py`) binds it on both write arms.

**Fix.** `WriteAdvancingRecord` keeps the record at its `FitRecord` size when `_lineSequential && !LineSequentialFileRules`,
the same decider the plain arm and the READ arm ask. From 2023, and for LINAGE or report streams without the clause, the
text form is unchanged.

**Golden.** `conformance:cases/VII.4.7@1985-WRITE-STATEMENT/pb5914_line_delimited_advancing_keeps_size_85` (AFTER, plain
and BEFORE writes read back as 10-character records, spaces kept). No refusal golden: the rule is a runtime form, not a
diagnostic. No row closed; `docs/FILES_DESIGN.md` (the RECORD DELIMITER paragraph) names the print arm.
