## PB1880: the I-O bundle's five test-breadth gaps closed with spec-derived goldens and unit tests, the exception paths as full as the success paths

kb/Work PB1880 (wave 1126, group G, campaign PB3353). The 2026-09-30 I-O bundle (PB1562, PB1541, PB1513, PB677, PB679) shipped
with five gaps its own self-review named. Each was re-probed on this branch's build and each behaved as the standard and the
recorded determinations say, so nothing in `src/` changed: the work is the tests, and they are the owner's requirement for the
SORT and file I/O campaign (the abnormal and exception paths as fully as the success paths, MANDATORY-PRACTICES I11).

**1. Report files register through `RegisterReport`.** `ReportFileRegistrationEmitterTests` reads the emitted C# of a program with
two report files (one OPTIONAL, different line widths) and a file with records: `RegisterReport("PROG::RPT", ..., 3, false)`,
`RegisterReport("PROG::RPT2", ..., 12, true)`, `Register(... "REC" ...)` for the control, and no record registration for a report
file. The standard's observable half is the golden `14.9.27-OPEN/pb1880_report_file_open_extend`: OPEN EXTEND over an earlier
report is '00' (§9.1.13.2 item 1; §14.9.27.3 SR1 names OUTPUT and EXTEND as the report file's modes), the second OPEN of the
open file is '41' and a CLOSE of the closed file '42' (§9.1.13.7 items 1 and 2), and the appended report is intact.

**2. ODO READ validation (`SequentialIoEmitter.OdoRecordExceedsMaximum`, D-ODO2).** Four goldens under
`9.1.13.6-*/pb1880_odo_read_*`, every value computed from §9.1.13.6 item 4 b) and §13.18.43.4 GR4/GR5/GR7: an explicit RECORD
clause (RECORD CONTAINS 14, where the specified value equals the description's maximum; RECORD CONTAINS 16, where it exceeds it,
so a count the implied value would refuse succeeds; RECORD IS VARYING 2 TO 16), a USAGE BIT table (the unit is the byte and the
last partial byte counts whole: 24 bits over a 1 TO 20 table is five bytes and succeeds, 25 is six and is '34'), a DEPENDING ON
item in WORKING-STORAGE (the record's own header "99" is irrelevant), and the exception paths: a USE procedure and no FILE
STATUS, both, the relative (keyed READ) arm, and an uncovered '34', where the run unit terminates abnormally (Annex A.1 item 103).
Every unsuccessful READ runs no NOT AT END and the permanent error stays in effect until the CLOSE.

**3. PB1562 round trip, relative and indexed.** `13.18.43-RECORD/pb1880_variable_group_under_fixed_record_clause_relative` and
`_indexed`: the LIMIT-bounded variable-length group under RECORD CONTAINS 20 round trips in each organization, with the invalid
key ('22' duplicate slot / duplicate prime key, '23' empty slot / deleted record, START '23') and AT END arms. The indexed
RECORD KEY sits behind a variable member, so the case pins §9.1.7.4's fixed key position: one window in every record, records
ordered by the key (§9.1.8.2).

**4. WRITE ... WITH LOCK under a permanent error.** The golden `14.9.51-WRITE/pb1880_write_with_lock_under_permanent_error`
(relative: a key-0 WRITE makes the '34', then WITH LOCK writes of an existing and a new slot answer '34', INVALID KEY and NOT
INVALID KEY run neither, another file connector can lock the existing record and finds the new one absent, CLOSE ends the
condition and the same WRITE WITH LOCK then locks; and a USE-only file whose declarative runs for both) and
`WriteWithLockPermanentErrorTests` (sequential, relative and indexed, under every RETRY form, a connector holding its maximum
fifteen locks: the healthy WRITE answers '54', the one under the condition '34', nothing is stored; and the relative lock identity
control).

**5. National pad.** The LINE SEQUENTIAL fill is the standard's own (§14.9.30.4 GR15) and `pb327_national_line_sequential_fill`
already witnessed it. For record sequential and relative files the content is WiseOwl COBOL's determination (D-WRT1, FILES_DESIGN
D30), so the WiseOwl golden `14.9.51-WRITE/pb1880_national_pad_record_sequential_and_relative` pins the uncovered tail of a WRITE
(RECORD CONTAINS 12 over an 8-byte national record) and the short tail of a varying READ, bytes 00 20 and never 20 20, and
`NationalBlankRecordAreaTests` pins the blank area an unsuccessful READ returns (invalid key, AT END, not open), with the
alphanumeric control, which no program can observe because the generated READ leaves the area alone.

**Re-verdicts and docs.** No inventory row changes (the note claimed none; `closes_rows: []` with its reason). docs/CONFORMANCE.md
D-ODO2, DOC-A.1-105 (a WRITE WITH LOCK is a WRITE, and the replay precedes the lock ceiling), D-FRA (vi) and D-WRT1 name their new
witnesses; FILES_DESIGN D30 names the pad and report-file witnesses.

**Self-review (pr-test-analyzer, silent-failure-hunter, comment-analyzer).** The unit tests assert the control first (the '54' at
the ceiling, the '51' after a successful lock) so a vacuous pass is impossible; the emitter test pins both arms, not one; the
goldens overwrite their members with junk before every READ; no test uses a wall clock; the comments were reread against the cited
text and `cite.py --check` ran on every clause number written. Nothing was found to fix in `src/`.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261018T215443Z-d36544): the nine new
conformance cases, 13 + 8 + 2 new unit cases passed in leg 1; the Linux gate is the landing's.
