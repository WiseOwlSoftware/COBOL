      *>--- conformance-case
      *> id: g85_bf26_02
      *> rules: [1985-GR-VIII.1.3.4-3, 1985-GR-VIII.1.3.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GR3 - a relative-file statement that meets
      *> the invalid key condition is unsuccessfully executed: the
      *> duplicate WRITE leaves record 1 as it was (reread line RD1).
      *> §1985:VIII.1.3.5 GR1 - the I-O status is set to a value
      *> indicating the invalid key condition before the INVALID KEY
      *> imperative runs: 22 for the duplicate WRITE, 23 for the
      *> random READ, REWRITE, DELETE and START of an absent record
      *> (§1985:VIII.1.3.4, I-O Status = 22 and 23). Each status is
      *> planted as 99 first, so a statement that does not set it,
      *> or sets it after the imperative, prints 99.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2602.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "bf2602.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
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
       P-1.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "ONE-01" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W0 INV " WS-STAT.
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "DUP-01" TO REL-REC.
           MOVE "99" TO WS-STAT.
           WRITE REL-REC INVALID KEY DISPLAY "WR " WS-STAT.
           MOVE 5 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           READ REL-FILE INVALID KEY DISPLAY "RD " WS-STAT.
           MOVE 6 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           REWRITE REL-REC INVALID KEY DISPLAY "RW " WS-STAT.
           MOVE 7 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           DELETE REL-FILE INVALID KEY DISPLAY "DL " WS-STAT.
           MOVE 9 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           START REL-FILE KEY IS EQUAL TO WS-KEY
               INVALID KEY DISPLAY "ST " WS-STAT.
           MOVE 1 TO WS-KEY.
           READ REL-FILE INVALID KEY DISPLAY "RD1 MISSING".
           DISPLAY "RD1 " REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
