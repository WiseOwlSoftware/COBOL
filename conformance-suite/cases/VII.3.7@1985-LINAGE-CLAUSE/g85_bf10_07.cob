      *>--- conformance-case
      *> id: g85_bf10_07
      *> rules: [1985-SR-VII.3.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.7.3 SR2 - "Data-name-1, data-name-2, data-name-3,
      *> data-name-4 may be qualified." All four LINAGE data-names are written
      *> OF PG-A; PG-B holds items of the same names with other values.
      *> Derivation (§1985:VII.3.7.4 GR9 d, GR9 c 2; §1985:VII.4.7.4 GR18):
      *> LINAGE-COUNTER is 1 after OPEN OUTPUT; each WRITE BEFORE ADVANCING 1
      *> LINE adds 1; the end-of-page condition occurs when a WRITE makes it
      *> equal or exceed the footing value, FT OF PG-A = 3.
      *>   W1 LC=2       : no end-of-page (2 < 3).
      *>   EOP W2 / W2 LC=3 : end-of-page at the second WRITE (3 >= 3); with
      *>                   FT OF PG-B (40) it would not occur.
      *>   W3 OVERFLOW   : BEFORE ADVANCING 5 LINES from 3 would exceed LN OF
      *>                   PG-A (6): page overflow repositions to the next
      *>                   logical page, LINAGE-COUNTER is reset to one (GR9 c 4)
      *>                   so it is below 8; with LN OF PG-B (50) it would be 8.
      *> TOP and BOTTOM (data-name-3, -4, both 0 OF PG-A) are accepted; their
      *> lines are not observed (the print file's layout is implementor's).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1007.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "bf1007.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF
           LABEL RECORDS ARE OMITTED
           LINAGE IS LN OF PG-A LINES
               WITH FOOTING AT FT OF PG-A
               LINES AT TOP TP OF PG-A
               LINES AT BOTTOM BT OF PG-A.
       01  PREC                    PIC X(10).
       WORKING-STORAGE SECTION.
       01  PG-A.
           05  LN                  PIC 99 VALUE 6.
           05  FT                  PIC 99 VALUE 3.
           05  TP                  PIC 9 VALUE 0.
           05  BT                  PIC 9 VALUE 0.
       01  PG-B.
           05  LN                  PIC 99 VALUE 50.
           05  FT                  PIC 99 VALUE 40.
           05  TP                  PIC 9 VALUE 2.
           05  BT                  PIC 9 VALUE 2.
       01  WS-LC                   PIC 99.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PRTF.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "OPEN LC=" WS-LC.
           MOVE "LINE 1" TO PREC.
           WRITE PREC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP W1".
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "W1 LC=" WS-LC.
           MOVE "LINE 2" TO PREC.
           WRITE PREC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP W2".
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "W2 LC=" WS-LC.
           MOVE "LINE 3" TO PREC.
           WRITE PREC BEFORE ADVANCING 5 LINES.
           MOVE LINAGE-COUNTER TO WS-LC.
           IF WS-LC < 8
               DISPLAY "W3 OVERFLOW"
           ELSE
               DISPLAY "W3 SAME PAGE".
           CLOSE PRTF.
           STOP RUN.
