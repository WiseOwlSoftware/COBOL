*>--- conformance-case
*> id: pb1880_report_file_open_extend
*> rules: [GR-14.9.27.4-7, SR-14.9.27.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §14.9.27.3 SR1: "The OPEN statement for a report file shall not contain the INPUT phrase or the I-O
      *> phrase", so OUTPUT and EXTEND are the two modes a report file opens in, and §9.1.13.2 item 1 gives a successful
      *> OPEN the I-O status '00'. A report file has no record description (§13.4.5.3 SR8; §9.1.22): the bytes an earlier run
      *> left in it are print lines with terminators, whose count has no relationship to the connector's width (the widest
      *> report line, here 3 columns), and OPEN EXTEND over them must simply succeed and append. WiseOwl COBOL once treated
      *> that width as a record length and answered every such OPEN with a "not a whole multiple of the record length"
      *> notice (kb/Work PB677); the notice is stderr-only and never changes the status, so what this program can witness
      *> is the standard's half - '00', and the appended report intact after the old one - while the emitter test
      *> ReportFileRegistrationEmitterTests and RecordLayoutNoticeTests pin the registration and the silence.
      *> The exception paths of the same statements, each a status the standard assigns (§9.1.13.7, logic error condition):
      *>   OX2  OPEN EXTEND of the report file that is ALREADY OPEN -> '41' (item 1: "An OPEN statement ... is attempted for
      *>        a file connector in an open mode"); the report in progress is untouched.
      *>   CL2  CLOSE of the report file that is NOT OPEN           -> '42' (item 2: "A CLOSE ... statement is attempted
      *>        for a file connector that is not in an open mode").
      *> Both are answered with FILE STATUS; neither is a fatal exception condition for the program, because the file has a
      *> FILE STATUS clause (docs/CONFORMANCE.md DOC-A.1-103: execution continues if the connector has one).
      *> The report is read back through a line sequential description of the same file, blank lines skipped (the vertical
      *> placement of a group is a separate question, kb/Work PB484): the first run wrote "D=4", the extend run appends "D=5".
      *> WHY EACH LEG CAN FAIL: an OPEN EXTEND that answered '30'/'35' or truncated would lose "D=4"; a missing '41' would
      *> let a second open reposition the report; a missing '42' would report success for a CLOSE of a closed file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880RX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1880rx.rpt"
               FILE STATUS IS FS.
           SELECT RBACK ASSIGN TO "pb1880rx.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-X.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 FS     PIC XX.
       01 WS-N   PIC 9 VALUE 4.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-X PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3 LAST DETAIL 15.
       01 DET-X TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           DISPLAY "OPEN-OUTPUT " FS.
           INITIATE R-X.
           GENERATE DET-X.
           TERMINATE R-X.
           CLOSE RPT.
           DISPLAY "CLOSE-1 " FS.
           OPEN EXTEND RPT.
           DISPLAY "OPEN-EXTEND " FS.
           OPEN EXTEND RPT.
           DISPLAY "OX2 " FS.
           MOVE 5 TO WS-N.
           INITIATE R-X.
           GENERATE DET-X.
           TERMINATE R-X.
           CLOSE RPT.
           DISPLAY "CLOSE-2 " FS.
           CLOSE RPT.
           DISPLAY "CL2 " FS.
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
