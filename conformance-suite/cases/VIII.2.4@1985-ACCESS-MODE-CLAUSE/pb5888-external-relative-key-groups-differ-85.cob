      *>--- conformance-case
      *> id: pb5888-external-relative-key-groups-differ-85
      *> rules: [1985-GR-VIII.2.4.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5888. Both programs name RELATIVE KEY IS REL-KEY, at
      *> positions 1-4 of the external record EXT-KEYS, under a group
      *> named GA in one program and GB in the other. The descriptions
      *> of an external record agree only in "each record-name of the
      *> associated record description entries" and the size
      *> (§1985:X.4.5.4 GR2), so REL-KEY is "that same external data
      *> item in each case" (§1985:VIII.2.4.4 GR8) and the program is
      *> accepted. PB5888C writes records 2 and 3; PB5888D moves 2 to
      *> REL-KEY and reads record 2 (§1985:VIII.4.5.4 GR15), printing
      *> B[SECOND-REC] KEY[0002]. The leg fails if the group names
      *> make the two keys two items (COBOL1575) or the read differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5888rc.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-KEYS IS EXTERNAL.
           05  GA.
               10  REL-KEY PIC 9(4).
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
           CALL "PB5888D".
           STOP RUN.
       END PROGRAM PB5888C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5888rc.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  EXT-KEYS IS EXTERNAL.
           05  GB.
               10  REL-KEY PIC 9(4).
           05  FILLER  PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           READ EXT-REL INVALID KEY DISPLAY "NOREC".
           DISPLAY "B[" EXT-REC "] KEY[" REL-KEY "]".
           CLOSE EXT-REL.
           EXIT PROGRAM.
       END PROGRAM PB5888D.
