      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-15
      *> rules: [1985-GR-XIV.3.6.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR15 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDADV.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(5) VALUE "HELLO".
       01 LINE-CT PIC 9 VALUE 2.
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO OUT-COUNT.
           MOVE 5 TO OUT-LEN.
           MOVE "TERM01" TO OUT-DEST.
           SEND OUT-CD FROM MSG WITH EMI
               AFTER ADVANCING LINE-CT LINES.
           DISPLAY "DONE".
           STOP RUN.
