      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-13
      *> rules: [1985-SR-XIV.2.2.3-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR13 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDCNT2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT
           DESTINATION COUNT IS DEST-CNT
           TEXT LENGTH IS TXT-LEN
           STATUS KEY IS STAT-KEY
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS ERR-KEY
           SYMBOLIC DESTINATION IS SYM-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 2 TO DEST-CNT.
           MOVE 5 TO TXT-LEN.
           MOVE "TERM1" TO SYM-DEST (1).
           MOVE "TERM2" TO SYM-DEST (2).
           SEND OUT-CD FROM MSG WITH EMI.
           IF STAT-KEY = "30"
               DISPLAY "COUNT 2 REFUSED"
           ELSE
               DISPLAY "COUNT 2 ACCEPTED".
           DISPLAY DEST-CNT.
           STOP RUN.
