      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-11
      *> rules: [1985-GR-XIV.3.6.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR11 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDEGI.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SYM-DEST.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO DC.
           MOVE 5 TO TL.
           MOVE "TERM1" TO SYM-DEST.
           SEND OUT-CD FROM MSG WITH EGI.
           DISPLAY "DONE".
           STOP RUN.
