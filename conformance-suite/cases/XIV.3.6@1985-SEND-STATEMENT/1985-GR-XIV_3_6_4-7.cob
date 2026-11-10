      *>--- conformance-case
      *> id: 1985-GR-XIV_3_6_4-7
      *> rules: [1985-GR-XIV.3.6.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.4 GR7 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SENDP.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-PART PIC X(10) VALUE "PARTIAL".
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT
           DESTINATION COUNT IS DEST-COUNT
           TEXT LENGTH IS TEXT-LEN
           STATUS KEY IS STAT-KEY
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS ERR-KEY
           SYMBOLIC DESTINATION IS SYM-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO DEST-COUNT.
           MOVE "TERM1" TO SYM-DEST (1).
           MOVE 7 TO TEXT-LEN.
           SEND OUT-CD FROM MSG-PART.
           DISPLAY "PORTION SENT".
           STOP RUN.
