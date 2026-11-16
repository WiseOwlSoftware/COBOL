      *>--- conformance-case
      *> id: g85_bf25_03
      *> rules: [1985-GR-IX.4.4.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR18 - the record sizes of an indexed file are
      *> established when the file is created and are not changed by a
      *> later OPEN through a description of another size.
      *  GR18: "The minimum and maximum record sizes for a file are
      *  established at the time the file is created and must not
      *  subsequently be changed." GR15: "The successful execution of
      *  an OPEN statement with the OUTPUT phrase creates the file.
      *  After the successful creation of a file, that file contains
      *  no data records." GR8: a file attribute conflict makes the
      *  OPEN unsuccessful, and "The implementor defines which of the
      *  fixed file attributes are validated"; WiseOwl COBOL validates
      *  an indexed file's minimum and maximum record size
      *  (docs/conformance/7/DOC-A.1-129.md), so such a conflict is
      *  status 39 (§1985:IX.1.3.4). GR19: an unsuccessful OPEN
      *  leaves "the physical file is not affected".
      *  Both descriptions have the same RECORD KEY (4 characters at
      *  the start of the record), so only the record size differs.
      *  S1  OPEN OUTPUT IX-MAKE creates the file: sizes 20/20. 00.
      *  S2  OPEN I-O IX-BIG (30-character records): 39, OPEN fails.
      *  S3  The file keeps its creation sizes and records: IX-MAKE
      *      opens 00 and reads K001 and K002 00, then 10.
      *  S4  OPEN OUTPUT IX-BIG is a new creation: sizes 30/30. 00.
      *  S5  Now the 20-character description conflicts: 39.
      *  S6  IX-BIG reads its one record 00, then 10.
      *  A leg fails if a status differs, if the failed S2 OPEN
      *  altered the file, or if a re-creation kept the old sizes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2503.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-MAKE ASSIGN TO "g85bf2503.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS MK-KEY
               FILE STATUS IS MK-ST.
           SELECT IX-BIG ASSIGN TO "g85bf2503.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS BG-KEY
               FILE STATUS IS BG-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-MAKE
           RECORD CONTAINS 20 CHARACTERS.
       01  MK-REC.
           05 MK-KEY  PIC X(4).
           05 MK-DATA PIC X(16).
       FD  IX-BIG
           RECORD CONTAINS 30 CHARACTERS.
       01  BG-REC.
           05 BG-KEY  PIC X(4).
           05 BG-DATA PIC X(26).
       WORKING-STORAGE SECTION.
       01  MK-ST PIC XX.
       01  BG-ST PIC XX.
       01  N     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-MAKE.
           DISPLAY "S1-CREATE=" MK-ST.
           MOVE "K001" TO MK-KEY.
           MOVE "FIRST" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W1-INVALID".
           MOVE "K002" TO MK-KEY.
           MOVE "SECOND" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W2-INVALID".
           CLOSE IX-MAKE.
           OPEN I-O IX-BIG.
           DISPLAY "S2-IO-30=" BG-ST.
           IF BG-ST = "00"
               CLOSE IX-BIG.
           OPEN INPUT IX-MAKE.
           DISPLAY "S3-OPEN-20=" MK-ST.
           PERFORM READ-MAKE 3 TIMES.
           CLOSE IX-MAKE.
           OPEN OUTPUT IX-BIG.
           DISPLAY "S4-RECREATE=" BG-ST.
           MOVE "K009" TO BG-KEY.
           MOVE "THIRTY" TO BG-DATA.
           WRITE BG-REC INVALID KEY DISPLAY "S4-W-INVALID".
           CLOSE IX-BIG.
           OPEN INPUT IX-MAKE.
           DISPLAY "S5-OPEN-20=" MK-ST.
           IF MK-ST = "00"
               CLOSE IX-MAKE.
           OPEN INPUT IX-BIG.
           DISPLAY "S6-OPEN-30=" BG-ST.
           MOVE ALL "*" TO BG-REC.
           READ IX-BIG AT END DISPLAY "S6-UNEXPECTED-END".
           DISPLAY "S6-READ=" BG-ST " [" BG-REC "]".
           READ IX-BIG AT END DISPLAY "S6-END=" BG-ST.
           CLOSE IX-BIG.
           STOP RUN.
       READ-MAKE.
           ADD 1 TO N.
           MOVE ALL "*" TO MK-REC.
           READ IX-MAKE AT END DISPLAY "S3-END=" MK-ST.
           IF MK-ST = "00"
               DISPLAY "S3-READ" N "=" MK-ST " [" MK-REC "]".
