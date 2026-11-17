      *>--- conformance-case
      *> id: pb5857-external-relative-key-other-positions-85
      *> rules: [1985-GR-VIII.2.4.4-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5857. §1985:VIII.2.4.4 GR8: for an external file
      *> connector "the RELATIVE KEY phrase in each associated file
      *> control entry must reference that same external data item in
      *> each case". An external record is storage the programs of the
      *> run unit share, so the same external data item is the same
      *> character positions of it (kb/Work PB5857's decision, the
      *> identity the RECORD KEY agreement already uses): REL-KEY is
      *> positions 1-4 of EXT-KEYS in PB5857A and 5-8 in PB5857B, two
      *> items, and the program is refused with COBOL1575. A compiler
      *> that compares the names alone accepts it, and PB5857B reads
      *> record 2 while PB5857A's key holds 3. The control is
      *> pb5857-external-relative-key-same-positions-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5857rk.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-KEYS IS EXTERNAL.
           05  REL-KEY PIC 9(4).
           05  FILLER  PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           MOVE "SECOND-REC" TO EXT-REC.
           WRITE EXT-REC INVALID KEY DISPLAY "BADW2".
           MOVE 3 TO REL-KEY.
           MOVE "THIRD-REC" TO EXT-REC.
           WRITE EXT-REC INVALID KEY DISPLAY "BADW3".
           CLOSE EXT-REL.
           CALL "PB5857B".
           STOP RUN.
       END PROGRAM PB5857A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5857rk.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-KEYS IS EXTERNAL.
           05  FILLER  PIC X(4).
           05  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           READ EXT-REL INVALID KEY DISPLAY "NOREC".
           DISPLAY "B[" EXT-REC "] KEYS[" EXT-KEYS "]".
           CLOSE EXT-REL.
           EXIT PROGRAM.
       END PROGRAM PB5857B.
