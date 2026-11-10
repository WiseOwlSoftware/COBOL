      *>--- conformance-case
      *> id: 1985-FMT-IX_4_4_2
      *> rules: [1985-FMT-IX.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.2 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENT.
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
           05 IX-KEY  PIC X(4).
           05 IX-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001" TO IX-KEY.
           MOVE "ALPHA" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           DISPLAY "STATUS " WS-STAT.
           READ IX-FILE AT END DISPLAY "EOF".
           DISPLAY IX-KEY " " IX-DATA.
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           DISPLAY "STATUS " WS-STAT.
           CLOSE IX-FILE.
           STOP RUN.
