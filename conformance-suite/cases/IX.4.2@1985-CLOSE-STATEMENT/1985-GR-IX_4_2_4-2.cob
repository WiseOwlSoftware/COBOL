      *>--- conformance-case
      *> id: 1985-GR-IX_4_2_4-2
      *> rules: [1985-GR-IX.4.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.2.4 GR2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOCKT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXF
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001" TO IX-KEY.
           MOVE "ABCDEF" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "EMPTY".
           DISPLAY "REOPENED " IX-DATA.
           CLOSE IX-FILE WITH LOCK.
           DISPLAY "CLOSED " WS-STAT.
           OPEN INPUT IX-FILE.
           STOP RUN.
