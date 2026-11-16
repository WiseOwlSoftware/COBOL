      *>--- conformance-case
      *> id: g85_z02
      *> rules: [1985-GR-XIII.4.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR10 - OPEN EXTEND positions the report file
      *> immediately after the last logical record for that file, the
      *> last record written in it. The report is written with OPEN
      *> OUTPUT (FIRST-A, FIRST-B), closed, then reopened with OPEN
      *> EXTEND and written again (SECOND). Read back, the file holds
      *> the two earlier records, in order, followed by the new one.
      *> Observing the records: §1985:XIII.3.2.4 GR2 leaves the report
      *> file's logical record structure to the implementor; this
      *> compiler's choice is docs/conformance DOC-A.1-159 (one print
      *> line is one record, one byte per column, each followed by the
      *> host newline, or a form feed before a new page) and
      *> DOC-A.1-146 (c') (a report file is text, its records less
      *> trailing spaces). The file is read one byte per record
      *> (DOC-A.1-146 (a)); a carriage return is skipped, a line feed
      *> or form feed ends a record, and empty records (page padding)
      *> are not shown.
      *> Expected: REC 1 [FIRST-A], REC 2 [FIRST-B], REC 3 [SECOND].
      *> An EXTEND that rewrote the file shows only SECOND; one that
      *> positioned anywhere but after FIRST-B loses or reorders a
      *> record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85Z02R".
           SELECT IN-FILE ASSIGN TO "G85Z02R".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE.
       01  IN-BYTE             PIC X.
       WORKING-STORAGE SECTION.
       01  WS-TEXT             PIC X(8) VALUE SPACES.
       01  WS-LINE             PIC X(30) VALUE SPACES.
       01  WS-I                PIC 99 VALUE 0.
       01  WS-N                PIC 9 VALUE 0.
       01  WS-EOF              PIC X VALUE "N".
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 10 LINES.
       01  DETL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1  PIC X(8) SOURCE WS-TEXT.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE "FIRST-A" TO WS-TEXT.
           GENERATE DETL.
           MOVE "FIRST-B" TO WS-TEXT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN EXTEND RPT-FILE.
           INITIATE RPT.
           MOVE "SECOND" TO WS-TEXT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM P-TAKE UNTIL WS-EOF = "Y".
           CLOSE IN-FILE.
           PERFORM P-SHOW.
           STOP RUN.
       P-TAKE.
           READ IN-FILE
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               IF IN-BYTE = SYM-LF OR IN-BYTE = SYM-FF
                   PERFORM P-SHOW
               ELSE
                   IF IN-BYTE NOT = SYM-CR AND WS-I < 30
                       ADD 1 TO WS-I
                       MOVE IN-BYTE TO WS-LINE (WS-I:1).
       P-SHOW.
           IF WS-I > 0
               ADD 1 TO WS-N
               DISPLAY "REC " WS-N " [" WS-LINE (1:WS-I) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
