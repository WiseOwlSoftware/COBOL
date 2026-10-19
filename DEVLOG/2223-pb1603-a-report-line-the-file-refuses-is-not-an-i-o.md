## Entry 2223 — 2026-10-18 15:57 PDT — PB1603: a report line the file refuses is not an I-O statement exception (a determination, with its witnesses)

Wave 1126, group D. The note asked whether a report line the medium refuses ('91' for a character with no byte image,
'34' for a full medium) should raise an exception condition, so a program could learn the line was not written.

**Re-probe.** The PB690 report program, with a FILE STATUS item and a USE AFTER EXCEPTION procedure added to the report
file, on this build: the U+20AC line is refused and leaves its blank line; FILE STATUS reads `00` after OPEN, each
GENERATE, TERMINATE and CLOSE; the USE procedure never runs; the run reaches STOP RUN.

**Determination.** The standard sets the I-O status only for CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK and
WRITE (9.1.13.1), and no rule of GENERATE, INITIATE or TERMINATE writes the line "as if a WRITE statement" (MERGE's
14.9.24.4 GR12 does, and SORT GIVING's 14.9.40.4 GR15 b)). So the refusal raises no EC-I-O condition, and the action after a fatal status is the
implementor's. CLAUDE.md rule 1 then takes GnuCOBOL 3.2.0, measured in WSL: a plain WRITE to `/dev/full` answers
FILE STATUS `34` at the 68th record, while 300 GENERATEs to a report file assigned to `/dev/full` leave FILE STATUS
`00` through TERMINATE and CLOSE and never run the USE procedure. Nothing in the runtime changes. This is the same
answer PB1956 recorded for the closed-file arm. docs/CONFORMANCE.md DOC-A.1-159 now states it for the full-medium
and the refused-character arms, INITIATE and TERMINATE included.

**Witnesses.** `ReportLineRefusalConformanceTests`: `RefusedReportLine_IsNotAnIoStatementException` (SEQUENTIAL
'91' and LINE SEQUENTIAL '71', FILE STATUS and a USE procedure on the report file, a DISPLAY after every report
statement, the blank line on the medium) and `ReportLine_RefusedByAFullMedium_IsNotAnIoStatementException` ('34'
through Linux's `/dev/full`; elsewhere it asserts the host is not Linux, as `FileBoundaryTests` does). The earlier
golden had neither a FILE STATUS item nor a USE procedure and no '34' case.
`RefusedReportLine_RaisesNoEcIo_WhenCheckingIsEnabled` (review round 1) pins the EC-I-O half under `>>TURN EC-I-O CHECKING ON`: nine combinations of organization, FILE STATUS and declarative (none, EC-I-O, file form), a refused line flushed by a later GENERATE and the final one flushed by TERMINATE, EXCEPTION-STATUS blank after every statement.

**Rows.** The note claimed no inventory row; `closes_rows` is empty with its reason. If the owner wants the refusal
observable, that is a new decision against DOC-A.1-159 and DOC-A.1-103.

