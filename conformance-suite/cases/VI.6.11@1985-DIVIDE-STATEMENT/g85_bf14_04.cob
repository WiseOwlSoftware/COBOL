      *>--- conformance-case
      *> id: g85_bf14_04
      *> rules: [1985-GR-VI.6.11.4-1, 1985-GR-VI.6.11.4-2, 1985-GR-VI.6.11.4-3, 1985-GR-VI.6.11.4-4, 1985-GR-VI.6.11.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR1 - format 1: identifier-1 is stored in a
      *> temporary that is divided into each identifier-2 left to right.
      *> L1 (01 05 15) fails if A, the divisor and the first dividend, is
      *> re-read after it becomes 01 (B and C would stay 10 and 30).
      *> §1985:VI.6.11.4 GR2 - format 2: identifier-1 is divided INTO
      *> identifier-2 and stored in each identifier-3. L2 fails if the
      *> direction is reversed (0.4) or a receiver is skipped; L3 likewise.
      *> §1985:VI.6.11.4 GR3 - format 3: identifier-1 is divided BY
      *> identifier-2 into each identifier-3. L4 fails if the direction is
      *> reversed; L5 if B, both divisor and receiver, is stored before Q.
      *> §1985:VI.6.11.4 GR4 - format 4: quotient into identifier-3, then
      *> the remainder into identifier-4, whose subscript is evaluated just
      *> before that store. L6/L7 fail on a wrong quotient or remainder; L8
      *> if T (I) is resolved with I = 1 rather than the new quotient 5.
      *> §1985:VI.6.11.4 GR5 - format 5 (BY) continues as format 4: L9,
      *> L10 and L11 as L6, L7 and L8 (new quotient 3 selects T (3)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 2.
       01 B PIC 99 VALUE 10.
       01 C PIC 99 VALUE 30.
       01 X PIC 9V9 VALUE 0.
       01 Y PIC 9 VALUE 0.
       01 Q PIC 99 VALUE 0.
       01 R PIC 99 VALUE 0.
       01 Q1 PIC 9V9 VALUE 0.
       01 R1 PIC 9V99 VALUE 0.
       01 I PIC 99 VALUE 1.
       01 TBL.
           05 T PIC 99 OCCURS 6 TIMES.
       01 EX PIC 9.9.
       01 ER PIC 9.99.
       PROCEDURE DIVISION.
       P1.
           DIVIDE A INTO A B C.
           DISPLAY "L1 " A " " B " " C.
           DIVIDE 4 INTO 10 GIVING X Y ROUNDED.
           MOVE X TO EX.
           DISPLAY "L2 " EX " " Y.
           DIVIDE B INTO C GIVING Q.
           DISPLAY "L3 " Q.
           DIVIDE 10 BY 4 GIVING X Y.
           MOVE X TO EX.
           DISPLAY "L4 " EX " " Y.
           DIVIDE C BY B GIVING Q B.
           DISPLAY "L5 " Q " " B.
           DIVIDE 3 INTO 17 GIVING Q REMAINDER R.
           DISPLAY "L6 " Q " " R.
           DIVIDE 3 INTO 10 GIVING Q1 REMAINDER R1.
           MOVE Q1 TO EX.
           MOVE R1 TO ER.
           DISPLAY "L7 " EX " " ER.
           MOVE ZEROS TO TBL.
           MOVE 1 TO I.
           DIVIDE 3 INTO 17 GIVING I REMAINDER T (I).
           DISPLAY "L8 " I " " TBL.
           DIVIDE 17 BY 3 GIVING Q REMAINDER R.
           DISPLAY "L9 " Q " " R.
           DIVIDE 10 BY 3 GIVING Q1 REMAINDER R1.
           MOVE Q1 TO EX.
           MOVE R1 TO ER.
           DISPLAY "L10 " EX " " ER.
           MOVE ZEROS TO TBL.
           MOVE 1 TO I.
           DIVIDE 20 BY 6 GIVING I REMAINDER T (I).
           DISPLAY "L11 " I " " TBL.
           STOP RUN.
