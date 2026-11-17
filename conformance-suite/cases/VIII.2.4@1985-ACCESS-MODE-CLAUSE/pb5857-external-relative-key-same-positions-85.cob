      *>--- conformance-case
      *> id: pb5857-external-relative-key-same-positions-85
      *> rules: [1985-GR-VIII.2.4.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5857, the control. REL-KEY is positions 1-4 of the
      *> external record EXT-KEYS in both programs, so both RELATIVE KEY
      *> phrases reference that same external data item (§1985:VIII.2.4.4
      *> GR8) and the program is accepted. PB5857C writes records 2 and
      *> 3; PB5857D opens the file INPUT, moves 2 to REL-KEY and reads
      *> record 2: "the record whose relative record number equals the
      *> file position indicator is made available" (§1985:VIII.4.5.4
      *> GR15), the indicator set from REL-KEY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5857rc.dat"
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
           CALL "PB5857D".
           STOP RUN.
       END PROGRAM PB5857C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5857D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5857rc.dat"
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
           OPEN INPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           READ EXT-REL INVALID KEY DISPLAY "NOREC".
           DISPLAY "B[" EXT-REC "] KEY[" REL-KEY "]".
           CLOSE EXT-REL.
           EXIT PROGRAM.
       END PROGRAM PB5857D.
