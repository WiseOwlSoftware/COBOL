      *>--- conformance-case
      *> id: pb5314_receive_no_data_end_key
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5314 - §1985:XIV.2.2.4 GR12: the END KEY is set to 0 when "less
      *> than a message is transferred" (a RECEIVE MESSAGE) or "less than a message
      *> segment" (a RECEIVE SEGMENT). A RECEIVE that takes its NO DATA branch
      *> transfers nothing, so the end key reads 0 and not the previous RECEIVE's 2.
      *> The program first defines its queues to this implementation's message
      *> control system (the file mcs.json).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5314EK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BIG PIC X(20).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           IN-Q IN-S1 FILLER FILLER IN-DATE IN-TIME IN-SRC IN-LEN
           IN-END IN-STATUS IN-COUNT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """HELLO"",""WORLD""]},{""name"":""Q2"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """messages"":[]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q. MOVE SPACES TO IN-S1.
           RECEIVE CDIN MESSAGE INTO BIG NO DATA DISPLAY "ND1".
           DISPLAY "A ST " IN-STATUS " LEN " IN-LEN " END " IN-END.
           MOVE "Q2" TO IN-Q.
           RECEIVE CDIN MESSAGE INTO BIG NO DATA DISPLAY "ND2".
           DISPLAY "B ST " IN-STATUS " LEN " IN-LEN " END " IN-END.
           MOVE "Q1" TO IN-Q.
           RECEIVE CDIN SEGMENT INTO BIG NO DATA DISPLAY "ND3".
           DISPLAY "C ST " IN-STATUS " LEN " IN-LEN " END " IN-END.
           MOVE "Q2" TO IN-Q.
           RECEIVE CDIN SEGMENT INTO BIG NO DATA DISPLAY "ND4".
           DISPLAY "D ST " IN-STATUS " LEN " IN-LEN " END " IN-END.
           STOP RUN.
