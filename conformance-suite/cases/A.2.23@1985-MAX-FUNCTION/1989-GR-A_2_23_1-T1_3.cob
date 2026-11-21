      *>--- conformance-case
      *> id: 1989-GR-A_2_23_1-T1_3
      *> rules: [1989-GR-A.2.23.1-T1.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.1 Table 1 row 3 (THE MAX FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe max-integer-85: MAX of all-integer arguments is type integer (accepted as an integer argument); MAX returns the greatest
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PMAXINT.
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
           COMPUTE I = FUNCTION MAX(3 9 5).
           IF I = 9
               DISPLAY "OK   MAX(3 9 5) value"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX(3 9 5) value"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION MAX(3 9 5) 4).
           IF I = 1
               DISPLAY "OK   MAX of integers is an integer operand"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX of integers is an integer operand"
           END-IF.
           MOVE 7 TO N.
           COMPUTE I = FUNCTION MOD(FUNCTION MAX(N -1) 4).
           IF I = 3
               DISPLAY "OK   integer data item argument"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL integer data item argument"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
