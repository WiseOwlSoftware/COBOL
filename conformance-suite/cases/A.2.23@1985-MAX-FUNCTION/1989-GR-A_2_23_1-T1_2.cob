      *>--- conformance-case
      *> id: 1989-GR-A_2_23_1-T1_2
      *> rules: [1989-GR-A.2.23.1-T1.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.1 Table 1 row 2 (THE MAX FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe max-alphanumeric-85: MAX of alphanumeric arguments: value and type (the control of max-alphanumeric-type-refused-85)
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
           MOVE FUNCTION MAX("ABC" "ABD" "AB") TO X.
           IF X = "ABD"
               DISPLAY "OK   MAX content"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX content"
           END-IF.
           MOVE FUNCTION MAX("Z" "ABC") TO X.
           IF X = "Z"
               DISPLAY "OK   MAX selects Z"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL MAX selects Z"
           END-IF.
           MOVE FUNCTION MAX(A1 "ABE") TO X.
           IF X = "ABE"
               DISPLAY "OK   alphabetic mixed with alphanumeric"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL alphabetic mixed with alphanumeric"
           END-IF.
           COMPUTE N = FUNCTION LENGTH(FUNCTION MAX("Z" "ABC")).
           IF N = 1
               DISPLAY "OK   size of the selected argument"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL size of the selected argument"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
