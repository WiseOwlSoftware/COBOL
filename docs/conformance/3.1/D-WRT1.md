- **D-WRT1 — the bytes a WRITE or REWRITE sends past the end of record-name-1** (kb/Work PB1907; Annex A.2 items 64
  and 49). §2023:14.9.51.4 GR13: *"When record-name-1 is specified, if the number of bytes to be written to the file is
  greater than the number of bytes in record-name-1, the content of the bytes that extend outside the end of
  record-name-1 are undefined"* (`cite.py --check 14.9.51.4 "the content of the bytes that extend outside the end of
  record-name-1 are undefined"` → OK 13)); §2023:14.9.35.4 GR15 says the same of REWRITE (`cite.py --check 14.9.35.4
  "the content of the bytes that extend outside the end of record-name-1 are undefined"` → OK 15)). **The NUMBER of
  bytes is not latitude, and WiseOwl COBOL writes exactly the standard's count:** §2023:13.18.43.4 GR13 a) takes it from
  the DEPENDING ON item (`--check 13.18.43.4 "If data-name-1 is specified, by the content of the data item referenced
  by data-name-1"` → OK 13) a)), GR6 makes every record of a Format 1 file integer-1 bytes (`--check 13.18.43.4
  "Integer-1 specifies the number of bytes contained in each record in the file"` → OK 6)), and the status stays
  `'00'` with GR14's `'44'` for a count outside the bounds. **The CONTENT is this compiler's determination: position
  n of the record sent, for n past the end of record-name-1, is position n of the record area, and a position that
  no record description of the file occupies is a space** (a national space in a national area). The area is the
  storage every level-1 entry under the FD implicitly redefines (§2023:13.18.33.4 GR3, `--check 13.18.33.4 "Multiple
  level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area"` → OK 3)), so a
  program that fills the long record and then writes a shorter one writes the shorter one followed by the rest of
  the long one. WRITE FROM and REWRITE FROM are the same case: GR5 a) / §2023:14.9.35.4 GR7 a) MOVE into record-name-1
  and then run the statement without FROM (`--check 14.9.51.4 "The result of the execution of a WRITE statement
  specifying record-name-1 and the FROM phrase is equivalent"` → OK 5)). One rule serves WRITE and REWRITE, fixed and
  variable-length records, record sequential, relative and indexed files; a REWRITE needs no second determination
  because §2023:14.9.35.4 GR15 is GR13's twin and the same predicate answers both. On a NATIONAL record area the space is
  the national space, the two bytes 00 20, in record sequential and relative files alike, and the short tail of a
  variable-length record read into a larger area is padded the same way (witness
  `conformance:wiseowl/cases/14.9.51-WRITE/pb1880_national_pad_record_sequential_and_relative`; the LINE SEQUENTIAL
  fill is the standard's own, §2023:14.9.30.4 GR15, `…/12.4.5.10-ORGANIZATION/pb327_national_line_sequential_fill`).
  **Where the rule does not apply, because nothing is left undefined:** (1) a variable-length file WITHOUT a
  DEPENDING ON item transfers record-name-1 itself (§2023:13.18.43.4 GR13 b)/c)); (2) a LINE SEQUENTIAL file's transfer
  is defined outright — a WRITE drops the spaces right of the rightmost non-space character (§2023:14.9.51.4 GR21,
  `--check 14.9.51.4 "any spaces to the right of the rightmost non-space character are not transferred to
  file-name-1"` → OK 21)) or, with DEPENDING ON, fills with spaces (GR22, `--check 14.9.51.4 "the record area is,
  if necessary, filled to the right of the rightmost non-space character with one or more space characters"` → OK
  22)), and a REWRITE appends spaces (§2023:14.9.35.4 GR17 c), `--check 14.9.35.4 "then a sufficient number of the space
  character is appended"` → OK 17) c)); (3) a record sequential REWRITE whose record-name-1 is shorter than the
  record it replaces is unsuccessful, `'44'` (§2023:14.9.35.4 GR16, PB1168), so no byte is sent; (4) an out-of-line
  record-name-1 (D-FRA: a variable-length group, a dynamic-length or pointer-class record) has no window over the
  area, so it is sent at its own image and the positions past it are spaces. The predicate is
  `FileModel.TransfersPastRecord`, asked by the one image helper `SequentialIoEmitter.SentRecordImage` for the
  sequential and the keyed WRITE and REWRITE emitters; the runtime's `FileConnector.Stored` cuts the image to the
  transferred length or pads it, unchanged. **The area is read WHOLE** (`SequentialIoEmitter.RecordAreaSendImage`,
  kb/Work PB5217): when the widest record holds an OCCURS DEPENDING ON table, that table's DEPENDING item sizes that
  record (§2023:13.18.43.4 GR13 c)), never the area or another record, so a shorter record-name-1 is followed by the
  area's positions up to the transferred length, and an OCCURS DEPENDING record-name-1 below its maximum transfers
  past its own end too (its current size is the fixed portion and the occurrences at the time of execution), its
  tail again the area's (witness `conformance:cases/14.9.51-WRITE/pb5217_write_beside_odo_record`). The keyed READ,
  DELETE and START read their key out of the same whole-area image, so a key past the ODO record's current extent is
  the key the area holds.
  **Survey (CLAUDE.md rule 1).** GnuCOBOL 3.2 writes from the file's record area, but `cob_write` clamps the
  DEPENDING ON length to record-name-1's size, and a RECORD CONTAINS file with a shorter 01 becomes variable-length
  (`libcob/fileio.c` `cob_write`, `cobc/tree.c` `finalize_file`; measured with a 3.2.0 build: the variable records
  are 5 bytes, the fixed records carry the area content). That departs from §2023:13.18.43.4 GR13 a)/GR6, which control
  the length, so the length is NOT adopted from GnuCOBOL (owner decision 2026-10-01); its use of the shared area
  for the content agrees with this rule. IBM Enterprise COBOL 6.4 documents the length as ISO does and is silent on
  the content. Micro Focus Visual COBOL 7.0, WRITE statement, Format 1 general rules: *"When the file contains
  fixed-length records, the entire record area is written to the file"*, the rule extended here to
  variable-length records so that one rule decides both. Before PB1907 the bytes past record-name-1 were spaces.
  **A file with no RECORD clause** is the implied Format 1 of D-FRA (`--check 13.18.43.4 "If format 1 is implied,
  integer-1 shall be the record size of the largest record description entry in this file description entry"` → OK
  5) a)), so a short record of a multi-01 FD of fixed shape is sent at the largest record's length too; the NIST
  program RL106A's INFO-ONLY REL-TEST-10 line, which reports whether such a file wrote fixed-length records, therefore
  now reads `FIXED LENGTH RECORDS` (its golden is re-baselined: the line is informational, and §2023:13.18.43.4 GR5 leaves
  the implied format to the implementor).
  Pinned by `conformance:cases/14.9.51-WRITE/pb1907_write_past_record`, `conformance:cases/14.9.35-REWRITE/pb1907_keyed_rewrite_past_record` (relative,
  indexed, REWRITE, WRITE FROM and REWRITE FROM) and `conformance:cases/14.9.35-REWRITE/pb1907_past_record_whole_record` (the cases
  above where nothing is left undefined).
  **RELEASE is the same case** (§2023:14.9.32.4 GR6, Annex A.2 item 45; kb/Work PB322 F): `cite.py --check 14.9.32.4 "If the
  number of bytes to be released to the sort operation is greater than the number of bytes in record-name-1, the
  content of the bytes that extend beyond the end of record-name-1 are undefined."` → OK 6). The sort-merge file with no
  RECORD clause is the implied Format 1 of D-FRA, whose integer-1 is the largest record description entry's size
  (§2023:13.18.43.4 GR5 a)), so `RELEASE A` of a 4-byte description A beside a 10-byte B releases 10 bytes — A's four
  followed by the area's last six, which are B's — through the same helper (`SequentialIoEmitter.SentRecordImage`) and the
  same predicate (`FileModel.TransfersPastRecord`); an SD with a DEPENDING ON item releases that many bytes of the same
  image. Before PB322 F a RELEASE sent record-name-1's own size, so ONE SD held records of two sizes and a GIVING file
  received `AAAA` followed by spaces. Pinned by `conformance:cases/13.18.43-RECORD/pb322_release_short_record_fixed_sd`.
