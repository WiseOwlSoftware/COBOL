      *>--- conformance-case
      *> id: g85_bf25_02
      *> rules: [1985-GR-VIII.4.4.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR18 - the record sizes of a relative file are
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
      *  a relative file's minimum and maximum record size
      *  (docs/conformance/7/DOC-A.1-129.md), so such a conflict is
      *  status 39 (§1985:VIII.1.3.4). GR19: an unsuccessful OPEN
      *  leaves "the physical file is not affected".
      *  S1  OPEN OUTPUT F-MAKE creates the file: sizes 20/20. 00.
      *  S2  OPEN I-O F-BIG (30-character records) over that file:
      *      the sizes would change, so 39 and the OPEN fails.
      *  S3  The file still has its creation sizes and records: F-MAKE
      *      opens 00 and reads ALPHA and BRAVO 00, then 10.
      *  S4  OPEN OUTPUT F-BIG is a new creation: sizes 30/30. 00.
      *  S5  Now the 20-character description conflicts: 39.
      *  S6  F-BIG reads its one record 00, then 10.
      *  A leg fails if a status differs, if the failed S2 OPEN
      *  altered the file, or if a re-creation kept the old sizes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2502.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-MAKE ASSIGN TO "g85bf2502.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS MK-ST.
           SELECT F-BIG ASSIGN TO "g85bf2502.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS BG-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F-MAKE
           RECORD CONTAINS 20 CHARACTERS.
       01  MK-REC PIC X(20).
       FD  F-BIG
           RECORD CONTAINS 30 CHARACTERS.
       01  BG-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01  MK-ST PIC XX.
       01  BG-ST PIC XX.
       01  N     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F-MAKE.
           DISPLAY "S1-CREATE=" MK-ST.
           MOVE "ALPHA" TO MK-REC.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W1-INVALID".
           MOVE "BRAVO" TO MK-REC.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W2-INVALID".
           CLOSE F-MAKE.
           OPEN I-O F-BIG.
           DISPLAY "S2-IO-30=" BG-ST.
           IF BG-ST = "00"
               CLOSE F-BIG.
           OPEN INPUT F-MAKE.
           DISPLAY "S3-OPEN-20=" MK-ST.
           PERFORM READ-MAKE 3 TIMES.
           CLOSE F-MAKE.
           OPEN OUTPUT F-BIG.
           DISPLAY "S4-RECREATE=" BG-ST.
           MOVE "CHARLIE" TO BG-REC.
           WRITE BG-REC INVALID KEY DISPLAY "S4-W-INVALID".
           CLOSE F-BIG.
           OPEN INPUT F-MAKE.
           DISPLAY "S5-OPEN-20=" MK-ST.
           IF MK-ST = "00"
               CLOSE F-MAKE.
           OPEN INPUT F-BIG.
           DISPLAY "S6-OPEN-30=" BG-ST.
           MOVE ALL "*" TO BG-REC.
           READ F-BIG AT END DISPLAY "S6-UNEXPECTED-END".
           DISPLAY "S6-READ=" BG-ST " [" BG-REC "]".
           READ F-BIG AT END DISPLAY "S6-END=" BG-ST.
           CLOSE F-BIG.
           STOP RUN.
       READ-MAKE.
           ADD 1 TO N.
           MOVE ALL "*" TO MK-REC.
           READ F-MAKE AT END DISPLAY "S3-END=" MK-ST.
           IF MK-ST = "00"
               DISPLAY "S3-READ" N "=" MK-ST " [" MK-REC "]".
