      *>--- conformance-case
      *> id: 1989-GR-A_2_25_1-p1
      *> rules: [1989-GR-A.2.25.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.25.1 p1 (THE MEDIAN FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe median-85: MEDIAN value; type numeric (arithmetic use; INTEGER(...) as integer argument is the control)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PMEDIAN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(8)V9(6).
       01 I PIC S9(9).
       01 N PIC S9(9).
       01 X PIC X(30).
       01 A1 PIC A(3) VALUE "ABC".
       01 A2 PIC A(3) VALUE "ABD".
       01 OK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R ROUNDED = FUNCTION MEDIAN(5 1 3).
           IF R = 3
               DISPLAY "OK   MEDIAN value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEDIAN value 1"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION MEDIAN(4 1 3 2).
           IF R = 2.5
               DISPLAY "OK   MEDIAN value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEDIAN value 2"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION MEDIAN(5 1 3) * 2 - FUNCTION
               MEDIAN(5 1 3).
           IF R = 3
               DISPLAY "OK   MEDIAN in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEDIAN in arithmetic expression"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION MEDIAN(5
               1 3)) 7).
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
