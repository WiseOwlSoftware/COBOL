*>--- conformance-case
*> id: pb1880_report_file_optional_open_extend
*> rules: [GR-14.9.27.4-7, GR-9.1.13.2-4, GR-9.1.13.6-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.2 item 4 a): "For an OPEN statement, it is successfully executed but the file is described as
      *> optional and the physical file is not present at the time the OPEN statement is executed. If the open mode is I-O
      *> or extend, the physical file has been created." - the I-O status is 05. §9.1.13.6 item 5: "I-O status = 35. A
      *> permanent error exists because an OPEN statement with the INPUT, I-O, or EXTEND phrase is attempted on a file that
      *> is not described as optional and the physical file is not present." A report file is a file like any other for
      *> these two rules (its OPEN takes only OUTPUT or EXTEND, §14.9.27.3 SR1), and the OPTIONAL flag is exactly what the
      *> report-file registration hands the runtime (kb/Work PB1880: ReportFileRegistrationEmitterTests pins the emitted
      *> text; this program witnesses the behaviour):
      *>   RPT  not OPTIONAL, the physical file absent, OPEN EXTEND -> '35'; the connector stays closed, so the CLOSE that
      *>        follows is the logic error '42' (§9.1.13.7 item 2) and no file is created.
      *>   RPO  OPTIONAL, absent, OPEN EXTEND -> '05' and the file has been created; the report is written, closed ('00'),
      *>        and a second OPEN EXTEND over the now-present file is a plain '00' (item 1) that appends.
      *> Both are answered with FILE STATUS, so the fatal '35' does not end the program (docs/CONFORMANCE.md DOC-A.1-103).
      *> The OPTIONAL file is read back through a line sequential description of the same file, blank lines skipped (the
      *> vertical placement of a group is a separate question, kb/Work PB484): the first extend wrote "E=1", the second "E=2".
      *> WHY EACH LEG CAN FAIL: an optional argument dropped or inverted in the report-file arm would answer '35' for RPO and
      *> '05' for RPT, and a '05' that did not create the file would lose "E=1" at the second OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880RO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1880ro-absent.rpt"
               FILE STATUS IS FS.
           SELECT OPTIONAL RPO ASSIGN TO "pb1880ro-opt.rpt"
               FILE STATUS IS FS2.
           SELECT RBACK ASSIGN TO "pb1880ro-opt.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-X.
       FD RPO REPORT IS R-Y.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 FS     PIC XX.
       01 FS2    PIC XX.
       01 WS-N   PIC 9 VALUE 1.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-X PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 15.
       01 DET-X TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "X=".
       RD R-Y PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 15.
       01 DET-Y TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "E=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       MAIN.
           OPEN EXTEND RPT.
           DISPLAY "RPT-ABSENT " FS.
           CLOSE RPT.
           DISPLAY "RPT-CLOSE " FS.
           OPEN EXTEND RPO.
           DISPLAY "RPO-ABSENT " FS2.
           INITIATE R-Y.
           GENERATE DET-Y.
           TERMINATE R-Y.
           CLOSE RPO.
           DISPLAY "RPO-CLOSE-1 " FS2.
           MOVE 2 TO WS-N.
           OPEN EXTEND RPO.
           DISPLAY "RPO-PRESENT " FS2.
           INITIATE R-Y.
           GENERATE DET-Y.
           TERMINATE R-Y.
           CLOSE RPO.
           DISPLAY "RPO-CLOSE-2 " FS2.
           OPEN INPUT RBACK.
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       IF RB-REC NOT = SPACES
                           DISPLAY RB-REC
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RBACK.
           STOP RUN.
