      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-2
      *> rules: [1985-GR-XIV.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.2.4 GR2 (THE DISABLE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DISTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD OUTCD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO OUT-COUNT.
           MOVE "TERM1" TO OUT-DEST.
           MOVE 5 TO OUT-LEN.
           SEND OUTCD FROM MSG WITH EMI.
           DISABLE OUTPUT OUTCD WITH KEY "PASSWORD".
           DISPLAY "DISABLED".
           STOP RUN.
