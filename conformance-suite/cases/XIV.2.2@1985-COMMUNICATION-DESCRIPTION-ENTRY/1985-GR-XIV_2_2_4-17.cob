      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-17
      *> rules: [1985-GR-XIV.2.2.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR17 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR17T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DEF-FILE ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  DEF-FILE.
       01  DEF-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10).
       01  OUT-MSG PIC X(10) VALUE "HELLOWORLD".
       01  Q1-COUNT PIC 9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC PIC X(87).
       CD  OUTCD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STATUS
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       CD  ONECD FOR OUTPUT
           DESTINATION COUNT IS ONE-COUNT
           TEXT LENGTH IS ONE-LEN
           STATUS KEY IS ONE-STATUS
           ERROR KEY IS ONE-ERR
           SYMBOLIC DESTINATION IS ONE-DEST.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""queues"":[{""name"":""Q1""},{""name"":""Q2""}],"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           """destinations"":["
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""name"":""D1"",""deliverTo"":""Q1""},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""name"":""D2"",""deliverTo"":""Q2""}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE 5 TO OUT-LEN.
           MOVE "D1" TO OUT-DEST (1).
           MOVE "D2" TO OUT-DEST (2).
           MOVE 0 TO OUT-COUNT.
           SEND OUTCD FROM OUT-MSG WITH EMI.
           DISPLAY "COUNT 0 STATUS " OUT-STATUS.
           MOVE 3 TO OUT-COUNT.
           SEND OUTCD FROM OUT-MSG WITH EMI.
           DISPLAY "COUNT 3 STATUS " OUT-STATUS.
           MOVE 5 TO ONE-LEN.
           MOVE "D1" TO ONE-DEST.
           MOVE 2 TO ONE-COUNT.
           SEND ONECD FROM OUT-MSG WITH EMI.
           DISPLAY "NO TABLE COUNT 2 STATUS " ONE-STATUS.
           PERFORM SHOW-QUEUES.
           MOVE 1 TO OUT-COUNT.
           SEND OUTCD FROM OUT-MSG WITH EMI.
           DISPLAY "COUNT 1 STATUS " OUT-STATUS.
           PERFORM SHOW-QUEUES.
           MOVE 2 TO OUT-COUNT.
           SEND OUTCD FROM OUT-MSG WITH EMI.
           DISPLAY "COUNT 2 STATUS " OUT-STATUS.
           PERFORM SHOW-QUEUES.
           STOP RUN.
       SHOW-QUEUES.
           MOVE SPACES TO CDIN-REC.
           MOVE "Q1" TO IN-QUEUE.
           ACCEPT CDIN MESSAGE COUNT.
           MOVE IN-COUNT TO Q1-COUNT.
           MOVE "Q2" TO IN-QUEUE.
           ACCEPT CDIN MESSAGE COUNT.
           DISPLAY "Q1 " Q1-COUNT " Q2 " IN-COUNT.
