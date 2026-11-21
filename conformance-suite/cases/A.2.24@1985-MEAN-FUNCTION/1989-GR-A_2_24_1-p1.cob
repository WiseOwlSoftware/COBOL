      *>--- conformance-case
      *> id: 1989-GR-A_2_24_1-p1
      *> rules: [1989-GR-A.2.24.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.24.1 p1 (THE MEAN FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe mean-85: MEAN value; type numeric (arithmetic use; INTEGER(...) as integer argument is the control)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PMEAN.
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
           COMPUTE R ROUNDED = FUNCTION MEAN(1 2 4).
           IF R = 2.333333
               DISPLAY "OK   MEAN value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEAN value 1"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION MEAN(6).
           IF R = 6
               DISPLAY "OK   MEAN value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEAN value 2"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION MEAN(1 2 4) * 2 - FUNCTION
               MEAN(1 2 4).
           IF R = 2.333333
               DISPLAY "OK   MEAN in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MEAN in arithmetic expression"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION MEAN(2
               4)) 7).
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
