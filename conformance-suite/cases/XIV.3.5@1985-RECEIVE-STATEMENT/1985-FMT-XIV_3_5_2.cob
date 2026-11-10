      *>--- conformance-case
      *> id: 1985-FMT-XIV_3_5_2
      *> rules: [1985-FMT-XIV.3.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.2 (THE RECEIVE STATEMENT). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-MSG PIC X(80).
       COMMUNICATION SECTION.
       CD  INCD FOR INPUT.
       01  IN-REC PIC X(87).
       PROCEDURE DIVISION.
       MAIN-PARA.
           RECEIVE INCD RECORD INTO WS-MSG
               NO DATA DISPLAY "NO DATA"
           END-RECEIVE.
           STOP RUN.
