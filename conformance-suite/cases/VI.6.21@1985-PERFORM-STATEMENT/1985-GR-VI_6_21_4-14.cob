      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-14
      *> rules: [1985-GR-VI.6.21.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR14 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM PARA-A THRU PARA-M.
           STOP RUN.
       PARA-A.
           DISPLAY "A".
       PARA-D.
           PERFORM PARA-F THRU PARA-J.
       PARA-F.
           DISPLAY "F".
       PARA-J.
           DISPLAY "J".
       PARA-M.
           DISPLAY "M".
