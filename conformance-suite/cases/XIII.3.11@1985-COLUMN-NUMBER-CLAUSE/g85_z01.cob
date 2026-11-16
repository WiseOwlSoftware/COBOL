      *>--- conformance-case
      *> id: g85_z01
      *> rules: [1985-GR-XIII.3.11.4-3, 1985-GR-XIII.3.11.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.4 GR3 - the RWCS supplies space characters
      *> for all positions of a print line that are not occupied by
      *> printable items: columns 1-2 (before the first item) and
      *> columns 5-7 (between the items) are spaces.
      *> §1985:XIII.3.11.4 GR4 - the leftmost position of the print
      *> line is column number 1, so COLUMN 3 is the third byte of the
      *> line and COLUMN 8 the eighth (with GR2: integer-1 is the
      *> column of the item's leftmost character).
      *> Observing the line: §1985:XIII.3.2.4 GR2 leaves the report
      *> file's logical record structure to the implementor; this
      *> compiler's choice is docs/conformance DOC-A.1-159 (every
      *> edition): one print line is one record, one byte per column,
      *> no prefix. The line is the first of the file (LINE 1 on the
      *> first page), read back one byte per record (DOC-A.1-146 (a)).
      *> Expected: [  AB   CDE], LEAD SPACES, GAP SPACES,
      *> AB AT COLUMN 3, CDE AT COLUMN 8.
      *> GR3 is pinned by the two SPACES lines, GR4 by the two COLUMN
      *> lines; a gap filled with anything but spaces, or columns
      *> counted from 0, prints NOT in its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85Z01R".
           SELECT IN-FILE ASSIGN TO "G85Z01R".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE.
       01  IN-BYTE             PIC X.
       WORKING-STORAGE SECTION.
       01  WS-LINE             PIC X(10) VALUE SPACES.
       01  WS-I                PIC 99 VALUE 0.
       01  WS-EOF              PIC X VALUE "N".
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 10 LINES.
       01  DETL TYPE IS DETAIL LINE NUMBER IS 1.
           05  COLUMN 3  PIC X(2) VALUE "AB".
           05  COLUMN 8  PIC X(3) VALUE "CDE".
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM P-TAKE VARYING WS-I FROM 1 BY 1
               UNTIL WS-I > 10 OR WS-EOF = "Y".
           CLOSE IN-FILE.
           IF WS-EOF = "Y"
               DISPLAY "SHORT FILE".
           DISPLAY "[" WS-LINE "]".
           IF WS-LINE (1:2) = SPACES
               DISPLAY "LEAD SPACES"
           ELSE
               DISPLAY "LEAD NOT SPACES".
           IF WS-LINE (5:3) = SPACES
               DISPLAY "GAP SPACES"
           ELSE
               DISPLAY "GAP NOT SPACES".
           IF WS-LINE (3:2) = "AB"
               DISPLAY "AB AT COLUMN 3"
           ELSE
               DISPLAY "AB NOT AT COLUMN 3".
           IF WS-LINE (8:3) = "CDE"
               DISPLAY "CDE AT COLUMN 8"
           ELSE
               DISPLAY "CDE NOT AT COLUMN 8".
           STOP RUN.
       P-TAKE.
           READ IN-FILE
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               MOVE IN-BYTE TO WS-LINE (WS-I:1).
