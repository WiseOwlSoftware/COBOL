      *>--- conformance-case
      *> id: g85_bf26_07
      *> rules: [1985-GR-IX.4.6.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR8 - every REWRITE of an indexed file updates
      *> its I-O status. A 99 planted before each leg becomes 00 for a
      *> successful random REWRITE (RW1), 23 for the REWRITE of an
      *> absent key (RW2; §1985:IX.1.3.4, I-O Status = 23) and 49 for
      *> a REWRITE while the file is open INPUT (RW3; §1985:IX.1.3.4,
      *> I-O Status = 49), which also runs the USE procedure; WiseOwl
      *> COBOL continues after the critical status
      *> (docs/conformance/3.5, kb/Work PB5591). RD1 shows the
      *> successful REWRITE replaced the record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2607.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "bf2607.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX-FILE.
       IX-ERR-1.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "DATA01" TO IX-DATA.
           WRITE IX-REC.
           MOVE "K002" TO IX-KEY.
           MOVE "DATA02" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "NEW-01" TO IX-DATA.
           MOVE "99" TO WS-STAT.
           REWRITE IX-REC INVALID KEY DISPLAY "RW1 INV " WS-STAT.
           DISPLAY "RW1 " WS-STAT.
           MOVE "K005" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           REWRITE IX-REC INVALID KEY DISPLAY "RW2 INV " WS-STAT.
           DISPLAY "RW2 " WS-STAT.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           READ IX-FILE INVALID KEY DISPLAY "RD1 INV " WS-STAT.
           DISPLAY "RD1 " IX-DATA.
           MOVE "99" TO WS-STAT.
           REWRITE IX-REC INVALID KEY DISPLAY "RW3 INV " WS-STAT.
           DISPLAY "RW3 " WS-STAT.
           CLOSE IX-FILE.
           STOP RUN.
