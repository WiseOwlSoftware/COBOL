      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-4
      *> rules: [1985-GR-XIV.3.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR4 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           TEXT LENGTH IS TXT-LEN
           STATUS KEY IS STAT-KEY
           ERROR KEY IS ERR-KEY
           SYMBOLIC DESTINATION IS SYM-DEST.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO DEST-CNT.
           MOVE 5 TO TXT-LEN.
           MOVE "TERM1" TO SYM-DEST.
           MOVE "XX" TO STAT-KEY.
           SEND OUT-CD FROM MSG WITH EMI.
           IF STAT-KEY = "XX"
               DISPLAY "NOT UPDATED"
           ELSE
               DISPLAY "UPDATED".
           STOP RUN.
