      *>--- conformance-case
      *> id: 1985-GR-VI_4_3_4-4
      *> rules: [1985-GR-VI.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.4 GR4 (THE SOURCE-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "START".
      D    DISPLAY "DEBUG LINE".
           DISPLAY "END".
           STOP RUN.
