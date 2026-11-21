      *>--- conformance-case
      *> id: 1989-GR-A_2_23_1-T1_1
      *> rules: [1989-GR-A.2.23.1-T1.1, 1989-GR-A.2.4-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.1 Table 1 row 1; §1989:A.2.4 p6 (THE MAX
      *> FUNCTION). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-20.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      *> s-20 probe max-alphabetic-85: MAX/MIN of alphabetic arguments: value and type (the control of max-alphabetic-type-refused-85)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PMAXALPH.
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
           MOVE FUNCTION MAX(A1 A2) TO X.
           IF X = "ABD"
               DISPLAY "OK   MAX(A1 A2) content"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX(A1 A2) content"
           END-IF.
           MOVE FUNCTION MIN(A1 A2) TO X.
           IF X = "ABC"
               DISPLAY "OK   MIN(A1 A2) content"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MIN(A1 A2) content"
           END-IF.
           MOVE FUNCTION MAX(A1 A2) TO X(4:3).
           IF X(1:6) = "ABCABD"
               DISPLAY "OK   alphanumeric sending item"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL alphanumeric sending item"
           END-IF.
           COMPUTE N = FUNCTION LENGTH(FUNCTION MAX(A1 A2)).
           IF N = 3
               DISPLAY "OK   size of the selected argument"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL size of the selected argument"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
