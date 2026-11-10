      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-4
      *> rules: [1985-GR-XIV.3.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR4 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(20).
       COMMUNICATION SECTION.
       CD  CMI FOR INPUT.
       01  CMI-REC.
           02  CMI-QUEUE  PIC X(12).
           02  FILLER     PIC X(75).
       PROCEDURE DIVISION.
       P1.
           MOVE SPACES TO CMI-REC.
           MOVE "INQ1" TO CMI-QUEUE.
           RECEIVE CMI MESSAGE INTO MSG
               WITH DATA DISPLAY "WITH DATA"
           END-RECEIVE.
           DISPLAY "END".
           STOP RUN.
