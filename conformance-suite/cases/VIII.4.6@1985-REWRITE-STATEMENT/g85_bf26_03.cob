      *>--- conformance-case
      *> id: g85_bf26_03
      *> rules: [1985-GR-VIII.4.6.4-8, 1985-GR-VIII.4.6.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR13 - in the random access mode REWRITE
      *> replaces the record the relative key names, with no READ
      *> before it (RD1 shows UNO-01, RD2 shows TWO-02 untouched); for
      *> an absent record the invalid key condition exists (RW5 INV
      *> 23), the update does not take place (RD5 INV 23) and the
      *> record area is unaffected (AREA CINCO5).
      *> §1985:VIII.4.6.4 GR8 - every REWRITE updates the I-O status:
      *> 99 planted before each leg becomes 00 (RW1), 23 (RW5) and,
      *> with the file open INPUT, 49 (§1985:VIII.1.3.4, I-O Status =
      *> 49), which runs the USE procedure; WiseOwl COBOL continues
      *> after the critical status (docs/conformance/3.5, PB5591).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2603.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "bf2603.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WS-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9(4).
       01  WS-STAT PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       REL-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-FILE.
       REL-ERR-1.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "ONE-01" TO REL-REC.
           WRITE REL-REC.
           MOVE 2 TO WS-KEY.
           MOVE "TWO-02" TO REL-REC.
           WRITE REL-REC.
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "UNO-01" TO REL-REC.
           MOVE "99" TO WS-STAT.
           REWRITE REL-REC
               INVALID KEY DISPLAY "RW1 INV " WS-STAT
               NOT INVALID KEY DISPLAY "RW1 OK " WS-STAT
           END-REWRITE.
           MOVE 5 TO WS-KEY.
           MOVE "CINCO5" TO REL-REC.
           MOVE "99" TO WS-STAT.
           REWRITE REL-REC
               INVALID KEY DISPLAY "RW5 INV " WS-STAT
               NOT INVALID KEY DISPLAY "RW5 OK " WS-STAT
           END-REWRITE.
           DISPLAY "AREA " REL-REC.
           READ REL-FILE INVALID KEY DISPLAY "RD5 INV " WS-STAT.
           MOVE 1 TO WS-KEY.
           READ REL-FILE INVALID KEY DISPLAY "RD1 INV " WS-STAT.
           DISPLAY "RD1 " REL-REC.
           MOVE 2 TO WS-KEY.
           READ REL-FILE INVALID KEY DISPLAY "RD2 INV " WS-STAT.
           DISPLAY "RD2 " REL-REC.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           REWRITE REL-REC.
           DISPLAY "RW-IN " WS-STAT.
           CLOSE REL-FILE.
           STOP RUN.
