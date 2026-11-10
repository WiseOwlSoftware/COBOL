      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_2_3-L2_1
      *> rules: [1985-GR-IV.4.2.2.2.3-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GRL2.1 (Literals). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIGCON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X(3) VALUE ALL "XY".
       01  B PIC X(5) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "AB" TO B.
           DISPLAY A.
           DISPLAY B.
           IF B = ALL "AB" DISPLAY "EQ" ELSE DISPLAY "NE".
           STOP RUN.
