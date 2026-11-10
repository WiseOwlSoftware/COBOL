      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-2
      *> rules: [1985-GR-XIV.3.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR2 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVIO.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-MSG PIC X(80).
       COMMUNICATION SECTION.
       CD CDIO FOR I-O
           MESSAGE DATE IS CD-DATE
           MESSAGE TIME IS CD-TIME
           SYMBOLIC TERMINAL IS CD-TERM
           TEXT LENGTH IS CD-TLEN
           END KEY IS CD-EKEY
           STATUS KEY IS CD-SKEY.
       PROCEDURE DIVISION.
       P1.
           MOVE "TERM1" TO CD-TERM.
           RECEIVE CDIO MESSAGE INTO WS-MSG
               NO DATA CONTINUE.
           DISPLAY "DONE".
           STOP RUN.
