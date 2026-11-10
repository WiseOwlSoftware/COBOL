      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-11
      *> rules: [1985-GR-VI.6.21.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR11 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFRNG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM PARA-A THRU PARA-C.
           DISPLAY "BACK".
           STOP RUN.
       PARA-A.
           DISPLAY "A".
           GO TO PARA-X.
       PARA-B.
           DISPLAY "B".
       PARA-C.
           DISPLAY "C".
       PARA-X.
           DISPLAY "X".
           GO TO PARA-C.
