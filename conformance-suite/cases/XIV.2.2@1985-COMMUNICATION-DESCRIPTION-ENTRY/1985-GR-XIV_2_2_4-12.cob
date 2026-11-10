      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-12
      *> rules: [1985-GR-XIV.2.2.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR12 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDKEY.
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
       MAIN-PARA.
           OPEN OUTPUT DEF-FILE.
           MOVE
           "{""queues"":[{""name"":""Q1"",""messages"":["
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           """HELLO"",""ABCDEFGHIJKLMNO"","
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""G1""],""group"":true},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""S1"",""S2""]},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""T1"",""T2""],""group"":true},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""LONGSEGMENT12345"",""X""]},"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE
           "{""segments"":[""AB"",""CD""]}]}]}"
               TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE SPACES TO CDIN-REC.
           MOVE "Q1" TO IN-QUEUE.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "M1 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "M2 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "M3 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "M4 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S1 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S2 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S3 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S4 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S5 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S6 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN SEGMENT INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "S7 [" MSG "] " IN-END.
           MOVE ALL "*" TO MSG.
           RECEIVE CDIN MESSAGE INTO MSG
               NO DATA DISPLAY "NO DATA".
           DISPLAY "M5 [" MSG "] " IN-END.
           STOP RUN.
