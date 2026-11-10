      *>--- conformance-case
      *> id: 1985-GR-IX_4_6_4-3
      *> rules: [1985-GR-IX.4.6.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR3 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWLEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 10 TO 14 CHARACTERS
               DEPENDING ON WS-LEN.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(6).
       01  IX-LONG.
           05 IX-LKEY  PIC X(4).
           05 IX-LDATA PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-LEN PIC 99.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-FILE.
           MOVE 10 TO WS-LEN.
           MOVE "K001" TO IX-KEY.
           MOVE "OLDVAL" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "K002" TO IX-KEY.
           MOVE "SECOND" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           MOVE "NEWVAL" TO IX-DATA.
           REWRITE IX-REC INVALID KEY DISPLAY "REWRITE FAILED".
           READ IX-FILE AT END DISPLAY "NO RECORD".
           MOVE "LONGERDATA" TO IX-LDATA.
           MOVE 14 TO WS-LEN.
           REWRITE IX-LONG INVALID KEY DISPLAY "REWRITE FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY IX-KEY IX-DATA " " WS-LEN.
           READ IX-FILE AT END DISPLAY "NO RECORD".
           DISPLAY IX-LKEY IX-LDATA " " WS-LEN.
           CLOSE IX-FILE.
           STOP RUN.
