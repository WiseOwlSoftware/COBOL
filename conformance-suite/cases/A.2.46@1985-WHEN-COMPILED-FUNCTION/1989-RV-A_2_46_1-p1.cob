      *>--- conformance-case
      *> id: 1989-RV-A_2_46_1-p1
      *> rules: [1989-RV-A.2.46.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.46.1 p1 (THE WHEN-COMPILED FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 rows pass (w20262131i) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * PB5453 rows pass: the s-21 WHEN-COMPILED probe made deterministic
      * (its WC= line and its compile-day = run-day check dropped).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21WC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6)V9(6) VALUE 0.
       01 W PIC S9(6)V9(6) VALUE 0.
       01 X PIC X(30) VALUE SPACES.
       01 A1 PIC A(3) VALUE "DOG".
       01 A2 PIC A(3) VALUE "CAT".
       01 C1 PIC X VALUE LOW-VALUE.
       01 E PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-1.
           MOVE FUNCTION WHEN-COMPILED TO X.
           IF NOT (X(1:16) IS NUMERIC)
               DISPLAY "BAD WC1" MOVE 1 TO E.
           IF NOT (X(22:9) = SPACES)
               DISPLAY "BAD WC2" MOVE 1 TO E.
           IF NOT (X(17:1) = "+" OR X(17:1) = "-" OR X(17:1) = "0")
               DISPLAY "BAD WC3" MOVE 1 TO E.
           IF NOT (X(5:2) >= "01" AND X(5:2) <= "12")
               DISPLAY "BAD WC5" MOVE 1 TO E.
           MOVE FUNCTION WHEN-COMPILED(1:4) TO X.
           IF E = 0 DISPLAY "S21WC OK" ELSE DISPLAY "S21WC FAIL".
           STOP RUN.
