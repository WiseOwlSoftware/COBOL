      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-2
      *> rules: [1985-GR-VI.6.21.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR2 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFIX.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 3 TIMES INDEXED BY IX.
       01  START-VAL PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC" TO TBL.
           PERFORM SHOW-PARA VARYING IX FROM START-VAL BY 1
               UNTIL IX > 3.
           STOP RUN.
       SHOW-PARA.
           DISPLAY ELT (IX).
