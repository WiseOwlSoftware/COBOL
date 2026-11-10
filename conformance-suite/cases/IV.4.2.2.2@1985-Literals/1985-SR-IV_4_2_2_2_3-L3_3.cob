      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_2_3-L3_3
      *> rules: [1985-SR-IV.4.2.2.2.3-L3.3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 SRL3.3 (Literals). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIGALL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X(3).
       01  B PIC X(3).
       PROCEDURE DIVISION.
       P1.
           MOVE ALL ZEROS TO A.
           MOVE ZEROS TO B.
           IF A = B DISPLAY "SAME" ELSE DISPLAY "DIFF".
           DISPLAY A.
           STOP RUN.
