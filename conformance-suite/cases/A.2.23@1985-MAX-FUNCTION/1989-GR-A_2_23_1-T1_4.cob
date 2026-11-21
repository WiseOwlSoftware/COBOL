      *>--- conformance-case
      *> id: 1989-GR-A_2_23_1-T1_4
      *> rules: [1989-GR-A.2.23.1-T1.4, 1989-GR-A.2.23.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.1 Table 1 row 4; §1989:A.2.23.1 p1 (THE MAX
      *> FUNCTION). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-20.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      *> s-20 probe max-numeric-85: MAX with some integer and some numeric arguments: value and type (the control of max-numeric-type-refused-85)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PMAXNUME.
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
           COMPUTE R = FUNCTION MAX(3 9.5 -2).
           IF R = 9.5
               DISPLAY "OK   MAX value 9.5"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX value 9.5"
           END-IF.
           COMPUTE R = FUNCTION MAX(3 9.5) * 2.
           IF R = 19
               DISPLAY "OK   MAX in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX in arithmetic expression"
           END-IF.
           COMPUTE R = FUNCTION MAX(4 4.0 1).
           IF R = 4
               DISPLAY "OK   equal greatest values"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL equal greatest values"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION MAX(3
               9.5)) 4).
           IF I = 1
               DISPLAY "OK   INTEGER(MAX(...)) is an integer operand"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER(MAX(...)) is an integer operand"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
