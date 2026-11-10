      *>--- conformance-case
      *> id: 1985-SR-XIV_3_6_3-3
      *> rules: [1985-SR-XIV.3.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.3 SR3 (THE SEND STATEMENT). A refusal: the rule
      *> forbids line 21 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6171 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDADV.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       01  ADVN PIC 9V9 VALUE 1.0.
       01  ADVI PIC 9 VALUE 1.
       COMMUNICATION SECTION.
       CD  OUTCD FOR OUTPUT
           DESTINATION COUNT IS OUTCNT
           TEXT LENGTH IS OUTLEN
           STATUS KEY IS OUTSTAT
           ERROR KEY IS OUTERR
           SYMBOLIC DESTINATION IS OUTDEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO OUTCNT.
           MOVE "TERM1" TO OUTDEST.
           MOVE 5 TO OUTLEN.
           SEND OUTCD FROM MSG WITH EMI AFTER ADVANCING ADVN LINES.
           STOP RUN.
