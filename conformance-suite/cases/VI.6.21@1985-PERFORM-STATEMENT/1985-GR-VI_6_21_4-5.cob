      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-5
      *> rules: [1985-GR-VI.6.21.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR5 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INLINE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  I PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           PERFORM 2 TIMES
               ADD 1 TO I
               DISPLAY "IN " I
           END-PERFORM
           DISPLAY "OUT " I
           STOP RUN.
