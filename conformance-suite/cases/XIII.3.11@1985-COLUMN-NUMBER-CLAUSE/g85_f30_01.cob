      *>--- conformance-case
      *> id: g85_f30_01
      *> rules: [1985-GR-XIII.3.11.4-2, 1985-GR-XIII.3.11.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.4 GR2 - integer-1 is the column of the leftmost
      *> character of the printable item; §1985:XIII.3.11.4 GR4 - column 1
      *> is the leftmost position of the print line.
      * Three printable items at COLUMN 3 (PIC XXX "ABC"), COLUMN 8
      * (PIC XX "DE") and COLUMN 11 (PIC X SOURCE WS-C = "Z") in one
      * detail line of a report with no PAGE clause.  The report file
      * is read back byte by byte and the first 12 positions of each
      * non-empty line are DISPLAYed between brackets.
      * DERIVATION: GR4 numbers positions from 1; GR2 puts A at 3, B 4,
      * C 5, D at 8, E 9, Z at 11; GR3 supplies spaces at 1-2, 6-7, 10
      * and 12  =>  [  ABC  DE Z ]   and one line only.
      * A leg fails if an item is placed one off (0-based column, right
      * alignment) or a gap is not filled with spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3001.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3001.RPT".
           SELECT CHK ASSIGN TO "G85F3001.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-PC   PIC 9     VALUE 0.
       01  WS-LC   PIC 999   VALUE 0.
       01  WS-C    PIC X     VALUE "Z".
       REPORT SECTION.
       RD  R1.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 3  PIC XXX VALUE "ABC".
           05  COLUMN NUMBER IS 8  PIC XX  VALUE "DE".
           05  COLUMN NUMBER IS 11 PIC X   SOURCE WS-C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DL.
           TERMINATE R1.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:12) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
