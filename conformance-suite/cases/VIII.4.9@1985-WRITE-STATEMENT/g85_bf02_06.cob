      *>--- conformance-case
      *> id: g85_bf02_06
      *> rules: [1985-GR-VIII.4.9.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR10 - END-WRITE ends a relative-file WRITE: the
      *> DISPLAY after it runs after NOT INVALID KEY ran (A) and after
      *> INVALID KEY ran (B, relative record 1 already exists:
      *> §1985:VIII.4.9.4 GR14a), and inside an IF the statement after
      *> END-WRITE belongs to the IF and ELSE pairs with the IF (C).
      *> Without the scope end, B-AFTER would be part of the INVALID KEY
      *> imperative and C-IN-IF part of it too; a missing line fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0206.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WR-FILE ASSIGN TO BF02REL
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WR-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  WR-FILE
           LABEL RECORDS ARE STANDARD.
       01  WR-REC.
           05  WR-DATA  PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-FLAG  PIC X VALUE "Y".
       01  WR-KEY   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT WR-FILE.
           MOVE 1 TO WR-KEY.
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
           MOVE 2 TO WR-KEY.
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
