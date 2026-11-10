      *>--- conformance-case
      *> id: 1985-SR-XIV_3_5_3-1
      *> rules: [1985-SR-XIV.3.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.3 SR1 (THE RECEIVE STATEMENT). A refusal: the
      *> rule forbids line 13 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6170 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVCD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-MSG PIC X(80).
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT.
       01  IN-REC PIC X(87).
       CD  OUT-CD FOR OUTPUT.
       01  OUT-REC PIC X(23).
       PROCEDURE DIVISION.
       MAIN-PARA.
           RECEIVE OUT-CD MESSAGE INTO WS-MSG.
           STOP RUN.
