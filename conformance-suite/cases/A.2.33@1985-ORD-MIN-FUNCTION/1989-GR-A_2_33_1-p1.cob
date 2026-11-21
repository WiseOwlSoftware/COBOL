      *>--- conformance-case
      *> id: 1989-GR-A_2_33_1-p1
      *> rules: [1989-GR-A.2.33.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.33.1 p1 (THE ORD-MIN FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-21.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21OMIN.
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
           COMPUTE R = FUNCTION ORD-MIN(3 1 9 1).
           IF NOT (R = 2)
               DISPLAY "BAD OMN1" MOVE 1 TO E.
           COMPUTE R = FUNCTION ORD-MIN("B" "C" "A").
           IF NOT (R = 3)
               DISPLAY "BAD OMN2" MOVE 1 TO E.
           COMPUTE W = FUNCTION MOD(FUNCTION ORD-MIN(3 9 1) 5).
           IF NOT (W = 3)
               DISPLAY "BAD OMN3" MOVE 1 TO E.
           IF E = 0 DISPLAY "S21OMIN OK" ELSE DISPLAY "S21OMIN FAIL".
           STOP RUN.
