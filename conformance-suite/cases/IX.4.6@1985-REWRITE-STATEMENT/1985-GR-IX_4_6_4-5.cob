      *>--- conformance-case
      *> id: 1985-GR-IX_4_6_4-5
      *> rules: [1985-GR-IX.4.6.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR5 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXREWR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "IXREWR.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(8).
       01  IX-NUM-REC.
           05 FILLER   PIC X(4).
           05 IX-NUM   PIC 9(4).
           05 FILLER   PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-FS       PIC XX VALUE "ZZ".
       01  WS-SHORT    PIC X(7) VALUE "0002NEW".
       01  WS-LONG     PIC X(16) VALUE "0001LONGDATAXXXX".
       01  WS-GROUP.
           05 WS-G-KEY PIC X(4) VALUE "0003".
           05 WS-G-NUM PIC 9(4) VALUE 42.
           05 WS-G-TXT PIC X(4) VALUE "GRP ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001OLD-ONE-" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W1 INVALID KEY".
           MOVE "0002OLD-TWO-" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W2 INVALID KEY".
           MOVE "0003OLD-THR-" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W3 INVALID KEY".
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           MOVE ALL "?" TO IX-REC.
           REWRITE IX-REC FROM WS-SHORT
               INVALID KEY DISPLAY "RW2 INVALID KEY".
           DISPLAY "REWRITE FROM SHORT " WS-FS " " WS-SHORT "|".
           MOVE ALL "?" TO IX-REC.
           REWRITE IX-REC FROM WS-LONG
               INVALID KEY DISPLAY "RW1 INVALID KEY".
           DISPLAY "REWRITE FROM LONG " WS-FS " " WS-LONG "|".
           MOVE ALL "?" TO IX-REC.
           REWRITE IX-NUM-REC FROM WS-GROUP
               INVALID KEY DISPLAY "RW3 INVALID KEY".
           DISPLAY "REWRITE FROM GROUP " WS-FS.
           MOVE ALL "?" TO IX-REC.
           MOVE "0009" TO WS-G-KEY.
           REWRITE IX-REC FROM WS-GROUP
               INVALID KEY DISPLAY "REWRITE 0009 INVALID KEY " WS-FS.
           DISPLAY "RECORD AREA " IX-REC "|".
           MOVE "0001" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "R1 INVALID KEY".
           DISPLAY "0001 IS " IX-REC "|".
           MOVE "0002" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "R2 INVALID KEY".
           DISPLAY "0002 IS " IX-REC "|".
           MOVE "0003" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "R3 INVALID KEY".
           DISPLAY "0003 IS " IX-REC "|".
           CLOSE IX-FILE.
           STOP RUN.
