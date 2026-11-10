      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-1
      *> rules: [1985-GR-XIV.2.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR1 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDINFO.
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
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC PIC X(87).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""queues"":[{""name"":""ORDERS"",""messages"":["
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""HELLO""],""source"":""TERM1""}]}],"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           """sources"":[{""name"":""TERM1""}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE SPACES TO CDIN-REC.
           MOVE "ORDERS" TO IN-QUEUE.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "MESSAGE [" MSG "]".
           DISPLAY "QUEUE  [" IN-QUEUE "]".
           DISPLAY "SOURCE [" IN-SOURCE "]".
           DISPLAY "LENGTH " IN-LEN " END " IN-END " STATUS " IN-STATUS.
           STOP RUN.
