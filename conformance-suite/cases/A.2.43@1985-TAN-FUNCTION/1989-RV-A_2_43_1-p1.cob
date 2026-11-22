      *>--- conformance-case
      *> id: 1989-RV-A_2_43_1-p1
      *> rules: [1989-RV-A.2.43.1-p1, 1989-FMT-A.2.43.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Also the accepted witness of §1989:A.2.43.2, the general format
      *> FUNCTION TAN ( argument-1 ): each reference below writes that
      *> form (kb/Work PB5740).
      *> §1989:A.2.43.1 p1 (THE TAN FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-21.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21TAN.
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
           COMPUTE R = FUNCTION TAN(0).
           IF NOT (R = 0)
               DISPLAY "BAD TAN1" MOVE 1 TO E.
           COMPUTE R ROUNDED = FUNCTION TAN(0.7853981634).
           IF NOT (R = 1)
               DISPLAY "BAD TAN2" MOVE 1 TO E.
           COMPUTE R = FUNCTION TAN(0.5).
           IF NOT (R >= 0.546302 AND R < 0.546303)
               DISPLAY "BAD TAN3" MOVE 1 TO E.
           COMPUTE W = FUNCTION TAN(0) + 0.
           IF E = 0 DISPLAY "S21TAN OK" ELSE DISPLAY "S21TAN FAIL".
           STOP RUN.
