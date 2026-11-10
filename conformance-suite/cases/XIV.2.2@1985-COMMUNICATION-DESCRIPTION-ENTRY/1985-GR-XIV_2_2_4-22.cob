      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-22
      *> rules: [1985-GR-XIV.2.2.4-22]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR22 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR22T.
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
           MOVE "XX" TO SK.
           MOVE "X" TO EK.
           MOVE "NOSUCHDEST" TO SYM-DEST.
           SEND OUT-CD FROM MSG WITH EMI.
           DISPLAY "SEND DONE".
           DISPLAY SK.
           DISPLAY EK.
           STOP RUN.
