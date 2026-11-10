      *>--- conformance-case
      *> id: 1985-FMT-IX_4_5_2
      *> rules: [1985-FMT-IX.4.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.2 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "IXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
           SELECT DY-FILE ASSIGN TO "IXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DY-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY       PIC X(3).
           05  IX-DATA      PIC X(5).
       FD  DY-FILE
           LABEL RECORDS ARE STANDARD.
       01  DY-REC.
           05  DY-KEY       PIC X(3).
           05  DY-DATA      PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-REC           PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "001" TO IX-KEY.
           MOVE "ALPHA" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "002" TO IX-KEY.
           MOVE "BRAVO" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY WS-REC
           END-READ.
           READ IX-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY WS-REC
           END-READ.
           READ IX-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY WS-REC
           END-READ.
           CLOSE IX-FILE.
           OPEN INPUT DY-FILE.
           MOVE "002" TO DY-KEY.
           READ DY-FILE RECORD INTO WS-REC
               KEY IS DY-KEY
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY WS-REC
           END-READ.
           MOVE "009" TO DY-KEY.
           READ DY-FILE RECORD
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "FOUND"
           END-READ.
           MOVE "001" TO DY-KEY.
           READ DY-FILE
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY DY-DATA
           END-READ.
           READ DY-FILE NEXT RECORD
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY DY-DATA
           END-READ.
           READ DY-FILE NEXT
               AT END DISPLAY "EOF"
               NOT AT END DISPLAY DY-DATA
           END-READ.
           CLOSE DY-FILE.
           STOP RUN.
