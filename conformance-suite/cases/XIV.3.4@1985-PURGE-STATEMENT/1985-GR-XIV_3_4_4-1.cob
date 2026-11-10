      *>--- conformance-case
      *> id: 1985-GR-XIV_3_4_4-1
      *> rules: [1985-GR-XIV.3.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.4.4 GR1 (THE PURGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PURGET.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-TEXT PIC X(10) VALUE "PARTIALMSG".
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO OUT-COUNT.
           MOVE 10 TO OUT-LEN.
           MOVE "TERM1" TO OUT-DEST (1).
           SEND OUT-CD FROM MSG-TEXT.
           PURGE OUT-CD.
           DISPLAY "PURGE DONE".
           STOP RUN.
