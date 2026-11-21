      *>--- conformance-case
      *> id: 1989-GR-A_2_20_1-p1
      *> rules: [1989-GR-A.2.20.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.20.1 p1 (THE LOG FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe log-85: LOG value; type numeric (arithmetic use; INTEGER(...) as integer argument is the control)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PLOG.
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
           COMPUTE R ROUNDED = FUNCTION LOG(1).
           IF R = 0
               DISPLAY "OK   LOG value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG value 1"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION LOG(10).
           IF R = 2.302585
               DISPLAY "OK   LOG value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG value 2"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION LOG(1) * 2 - FUNCTION LOG(1).
           IF R = 0
               DISPLAY "OK   LOG in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG in arithmetic expression"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION LOG(1))
               7).
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
