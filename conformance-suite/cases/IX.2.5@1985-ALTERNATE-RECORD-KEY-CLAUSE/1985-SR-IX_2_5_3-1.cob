      *>--- conformance-case
      *> id: 1985-SR-IX_2_5_3-1
      *> rules: [1985-SR-IX.2.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.3 SR1 (THE ALTERNATE RECORD KEY CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTKEYQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO "ALTKEYQ"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PRIME-KEY
               ALTERNATE RECORD KEY IS ALT-KEY OF IDX-REC.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  PRIME-KEY  PIC X(4).
           05  ALT-KEY    PIC X(4).
           05  REC-DATA   PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  ALT-KEY    PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO PRIME-KEY.
           MOVE "BBBB" TO ALT-KEY OF IDX-REC.
           MOVE "FIRST" TO REC-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IDX-FILE.
           OPEN INPUT IDX-FILE.
           MOVE SPACES TO REC-DATA.
           MOVE "BBBB" TO ALT-KEY OF IDX-REC.
           READ IDX-FILE KEY IS ALT-KEY OF IDX-REC
               INVALID KEY DISPLAY "READ FAILED".
           IF REC-DATA = "FIRST"
               DISPLAY "ALT KEY READ OK".
           CLOSE IDX-FILE.
           STOP RUN.
