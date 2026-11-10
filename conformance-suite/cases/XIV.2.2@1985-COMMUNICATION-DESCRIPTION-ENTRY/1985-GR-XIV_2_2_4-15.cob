      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-15
      *> rules: [1985-GR-XIV.2.2.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR15 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDOUT.
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
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""queues"":[{""name"":""Q1""}],""destinations"":["
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""name"":""D1"",""deliverTo"":""Q1""}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE 1 TO OUT-COUNT.
           MOVE 5 TO OUT-LEN.
           MOVE "D1" TO OUT-DEST.
           SEND OUTCD FROM OUT-MSG WITH EMI.
           DISPLAY "SEND STATUS " OUT-STATUS.
           MOVE SPACES TO CDIN-REC.
           MOVE "Q1" TO IN-QUEUE.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "RECEIVED [" MSG "] " IN-LEN.
           STOP RUN.
