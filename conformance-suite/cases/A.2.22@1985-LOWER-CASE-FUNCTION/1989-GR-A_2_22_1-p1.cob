      *>--- conformance-case
      *> id: 1989-GR-A_2_22_1-p1
      *> rules: [1989-GR-A.2.22.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.22.1 p1 (THE LOWER-CASE FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe lower-case-85: LOWER-CASE: value and type (the control of lower-case-type-refused-85)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PLOWERCA.
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
           MOVE FUNCTION LOWER-CASE("AbC1-Z") TO X.
           IF X = "abc1-z"
               DISPLAY "OK   uppercase letters lowered, others kept"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL uppercase letters lowered, others kept"
           END-IF.
           COMPUTE N = FUNCTION LENGTH(FUNCTION LOWER-CASE("AbC1-Z")).
           IF N = 6
               DISPLAY "OK   same length as argument-1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL same length as argument-1"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
