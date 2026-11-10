      *>--- conformance-case
      *> id: 1985-SR-VI_5_4_3-1
      *> rules: [1985-SR-VI.5.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.4.3 SR1 (THE BLANK WHEN ZERO CLAUSE). A refusal: the
      *> rule forbids line 5 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2405 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BWZTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ITEM PIC X(3) BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
