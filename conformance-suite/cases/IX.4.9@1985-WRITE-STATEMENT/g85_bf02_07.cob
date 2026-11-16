      *>--- conformance-case
      *> id: g85_bf02_07
      *> rules: [1985-GR-IX.4.9.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR10 - END-WRITE ends an indexed-file WRITE: the
      *> DISPLAY after it runs after NOT INVALID KEY ran (A) and after
      *> INVALID KEY ran (B, a duplicate prime record key:
      *> §1985:IX.4.9.4 GR17b), and inside an IF the statement after
      *> END-WRITE belongs to the IF and ELSE pairs with the IF (C).
      *> Without the scope end, B-AFTER would be part of the INVALID KEY
      *> imperative and C-IN-IF part of it too; a missing line fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0207.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WR-FILE ASSIGN TO BF02IDX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS WR-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  WR-FILE
           LABEL RECORDS ARE STANDARD.
       01  WR-REC.
           05  WR-KEY   PIC X(4).
           05  WR-DATA  PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-FLAG  PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT WR-FILE.
           MOVE "0001" TO WR-KEY.
           MOVE "FIRST" TO WR-DATA.
           WRITE WR-REC
               INVALID KEY DISPLAY "A-INVALID"
               NOT INVALID KEY DISPLAY "A-WRITTEN"
           END-WRITE
           DISPLAY "A-AFTER".
           MOVE "AGAIN" TO WR-DATA.
           WRITE WR-REC
               INVALID KEY DISPLAY "B-INVALID"
               NOT INVALID KEY DISPLAY "B-WRITTEN"
           END-WRITE
           DISPLAY "B-AFTER".
           MOVE "0002" TO WR-KEY.
           MOVE "SECOND" TO WR-DATA.
           IF WS-FLAG = "Y"
               WRITE WR-REC
                   INVALID KEY DISPLAY "C-INVALID"
               END-WRITE
               DISPLAY "C-IN-IF"
           ELSE
               DISPLAY "C-ELSE"
           END-IF
           DISPLAY "C-END".
           CLOSE WR-FILE.
           STOP RUN.
