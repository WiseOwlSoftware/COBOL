      *>--- conformance-case
      *> id: 1989-GR-A_2_15_1-p1
      *> rules: [1989-GR-A.2.15.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.15.1 p1 (THE INTEGER FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe integer-85: INTEGER value; type integer (accepted where an integer argument is required)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PINTEGE.
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
           COMPUTE I = FUNCTION INTEGER(-1.5).
           IF I = -2
               DISPLAY "OK   INTEGER value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER value 1"
           END-IF.
           COMPUTE I = FUNCTION INTEGER(1.5).
           IF I = 1
               DISPLAY "OK   INTEGER value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER value 2"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(9.9) 7).
           IF I = 2
               DISPLAY "OK   INTEGER is an integer operand"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER is an integer operand"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
