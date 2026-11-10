      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-4
      *> rules: [1985-SR-IV.4.2.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR4 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARENS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC 9 OCCURS 3 TIMES.
       01  N PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO E (1).
           MOVE 2 TO E (2).
           MOVE 3 TO E (3).
           COMPUTE N = (E (1) + E (2)) * E (3).
           IF (N = 9) DISPLAY "N=" N.
           STOP RUN.
