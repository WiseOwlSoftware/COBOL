      *>--- conformance-case
      *> id: 1985-SR-XIV_3_6_3-2
      *> rules: [1985-SR-XIV.3.6.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.3 SR2 (THE SEND STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6171 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       01  IND-OK PIC 9 VALUE 1.
       01  IND-BAD PIC S9 VALUE 1.
       COMMUNICATION SECTION.
       CD  COM-OUT FOR OUTPUT
           DESTINATION COUNT IS DEST-COUNT
           TEXT LENGTH IS TEXT-LEN
           STATUS KEY IS STAT-KEY
           SYMBOLIC DESTINATION IS SYM-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO DEST-COUNT.
           MOVE 5 TO TEXT-LEN.
           MOVE "TERM1" TO SYM-DEST.
           SEND COM-OUT FROM MSG WITH IND-BAD.
           STOP RUN.
