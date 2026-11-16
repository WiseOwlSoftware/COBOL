      *>--- conformance-case
      *> id: g85_bf13_06
      *> rules: [1985-GR-XIII.3.21.4-3, 1985-GR-XIII.3.21.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.4 GR3 - a report item with USAGE IS DISPLAY
      *> (columns 5-7) is in standard data format: its digits are
      *> characters in the printed line.
      *> §1985:XIII.3.21.4 GR4 - a report item with no USAGE on it or
      *> its group (columns 9-11) is implicitly DISPLAY, the same
      *> characters as the explicit one.
      *> Observing the line: §1985:XIII.3.2.4 GR2 leaves the report
      *> file's logical record structure to the implementor. This
      *> compiler's choice is docs/conformance DOC-A.1-159 (every
      *> edition): one print line is one record, its columns written
      *> one byte each with no prefix, the line end after it. The
      *> file is read back one byte per record (DOC-A.1-146 (a): a
      *> fixed-length record sequential file is read in the reading
      *> description's lengths), so the byte count of any file is a
      *> whole number of records and no record length conflict
      *> arises; the first 11 bytes are the 11 columns of line 1.
      *> Expected: NUM=042 042 and SAME.
      *> A leg fails if either item is written in a non-character form
      *> or the two differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1306.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85BF13R".
           SELECT IN-FILE ASSIGN TO "G85BF13R".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE.
       01  IN-BYTE             PIC X.
       WORKING-STORAGE SECTION.
       01  WS-NUM              PIC 9(3) VALUE 42.
       01  WS-LINE             PIC X(11) VALUE SPACES.
       01  WS-I                PIC 99 VALUE 0.
       01  WS-EOF              PIC X VALUE "N".
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 10 LINES.
       01  DETL TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN 1  PIC X(4) VALUE "NUM=".
           05  COLUMN 5  PIC 999 SOURCE WS-NUM USAGE IS DISPLAY.
           05  COLUMN 9  PIC 999 SOURCE WS-NUM.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM P-TAKE VARYING WS-I FROM 1 BY 1
               UNTIL WS-I > 11 OR WS-EOF = "Y".
           CLOSE IN-FILE.
           IF WS-EOF = "Y"
               DISPLAY "SHORT FILE".
           DISPLAY WS-LINE.
           IF WS-LINE (5:3) = WS-LINE (9:3)
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFFERENT".
           STOP RUN.
       P-TAKE.
           READ IN-FILE
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               MOVE IN-BYTE TO WS-LINE (WS-I:1).
