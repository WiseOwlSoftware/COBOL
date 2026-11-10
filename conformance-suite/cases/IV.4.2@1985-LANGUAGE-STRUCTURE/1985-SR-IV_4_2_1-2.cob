      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-2
      *> rules: [1985-SR-IV.4.2.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR2 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEPTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9(3) VALUE 5.
       01  B PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       P1.
           ADD A, B GIVING B; DISPLAY "SUM", " ", B.
           MOVE 1 TO A; DISPLAY "A="; A.
           STOP RUN.
