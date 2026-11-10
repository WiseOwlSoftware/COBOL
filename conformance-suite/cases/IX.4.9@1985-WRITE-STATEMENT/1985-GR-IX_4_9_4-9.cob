      *>--- conformance-case
      *> id: 1985-GR-IX_4_9_4-9
      *> rules: [1985-GR-IX.4.9.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR9 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NIKTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY
               FILE STATUS IS WS-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY  PIC X(4).
           05  IDX-DATA PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STATUS PIC XX VALUE "99".
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IDX-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STATUS.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0002" TO IDX-KEY.
           MOVE "DATA" TO IDX-DATA.
           WRITE IDX-REC
               INVALID KEY DISPLAY "1 INVALID " WS-STATUS
               NOT INVALID KEY DISPLAY "1 VALID " WS-STATUS.
           DISPLAY "1 END".
           MOVE "0001" TO IDX-KEY.
           WRITE IDX-REC
               INVALID KEY DISPLAY "2 INVALID " WS-STATUS
               NOT INVALID KEY DISPLAY "2 VALID " WS-STATUS.
           DISPLAY "2 END".
           MOVE "0003" TO IDX-KEY.
           WRITE IDX-REC
               INVALID KEY DISPLAY "3 INVALID " WS-STATUS.
           DISPLAY "3 END " WS-STATUS.
           MOVE "0001" TO IDX-KEY.
           WRITE IDX-REC
               NOT INVALID KEY DISPLAY "4 VALID " WS-STATUS.
           DISPLAY "4 END".
           CLOSE IDX-FILE.
           STOP RUN.
