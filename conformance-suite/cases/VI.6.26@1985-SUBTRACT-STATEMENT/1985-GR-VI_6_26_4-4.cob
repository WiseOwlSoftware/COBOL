      *>--- conformance-case
      *> id: 1985-GR-VI_6_26_4-4
      *> rules: [1985-GR-VI.6.26.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.4 GR4 (THE SUBTRACT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBT4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A  PIC 9  VALUE 9.
       77  B  PIC 9  VALUE 9.
       77  C  PIC 99 VALUE 20.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT A B FROM C.
           DISPLAY C.
           STOP RUN.
