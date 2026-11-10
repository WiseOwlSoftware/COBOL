      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-9
      *> rules: [1985-SR-IV.4.2.1-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 SR9 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEPTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(4) VALUE " AB".
       01  WS-N PIC 9.99.
       PROCEDURE DIVISION.
       P1.
           MOVE 1.25 TO WS-N.
           DISPLAY "[" WS-A "]".
           DISPLAY WS-N.
           STOP RUN.
