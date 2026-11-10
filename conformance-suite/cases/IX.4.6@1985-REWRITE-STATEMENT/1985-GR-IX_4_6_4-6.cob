      *>--- conformance-case
      *> id: 1985-GR-IX_4_6_4-6
      *> rules: [1985-GR-IX.4.6.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR6 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXDISK
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  WS-KEY  PIC X(4).
           05  WS-DATA PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "OLDVAL" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           READ IX-FILE AT END DISPLAY "EOF".
           MOVE "K001" TO WS-KEY.
           MOVE "NEWVAL" TO WS-DATA.
           REWRITE IX-REC FROM WS-REC
               INVALID KEY DISPLAY "REWRITE FAILED".
           DISPLAY WS-REC.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "EOF".
           DISPLAY IX-KEY " " IX-DATA.
           CLOSE IX-FILE.
           STOP RUN.
