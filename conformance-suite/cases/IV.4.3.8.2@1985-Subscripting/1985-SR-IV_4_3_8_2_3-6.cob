      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_2_3-6
      *> rules: [1985-SR-IV.4.3.8.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.3 SR6 (Subscripting). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0900 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X(3) OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC" TO ELEM (-1).
           DISPLAY ELEM (1).
           STOP RUN.
