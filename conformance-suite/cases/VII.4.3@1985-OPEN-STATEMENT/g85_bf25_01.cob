      *>--- conformance-case
      *> id: g85_bf25_01
      *> rules: [1985-GR-VII.4.3.4-25]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR25 - the record sizes of a sequential file
      *> are established when the file is created and stay with it.
      *  GR25: "The minimum and maximum record sizes for a file are
      *  established at the time the file is created and must not
      *  subsequently be changed." GR21: "The successful execution of
      *  an OPEN statement with the OUTPUT phrase creates the file.
      *  After the successful creation of a file, that file contains
      *  no data records." §1985:VII.1.3.5: status 04 is a READ whose
      *  record length "does not conform to the fixed file attributes
      *  for that file"; 00 is plain success; 10 is the end of file.
      *  Every connector below agrees with the sizes the file was
      *  created with, so no line depends on the implementor's choice
      *  of validated attributes (GR8).
      *  S1  OPEN OUTPUT F-A creates the file: sizes 10/10. 00.
      *  S2  OPEN EXTEND F-A (the same sizes; not a creation) appends
      *      a third 10-character record. 00.
      *  S3  The three records read back through the same FD, each
      *      00, then 10.
      *  S4  OPEN OUTPUT F-B is a NEW creation, so it establishes
      *      sizes 15/15 and the file holds no data records.
      *  S5  F-B reads its one 15-character record 00, then 10: the
      *      three records of the first creation are gone.
      *  Only S4/S5 (the 15-character record read back after the
      *  re-creation) depends on the size established at creation.
      *  Each leg fails if an OPEN or READ status differs, if EXTEND
      *  loses a record, or if a re-creation keeps the old records
      *  or the old record size.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2501.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-A ASSIGN TO "g85bf2501.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "g85bf2501.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F-A
           RECORD CONTAINS 10 CHARACTERS.
       01  A-REC PIC X(10).
       FD  F-B
           RECORD CONTAINS 15 CHARACTERS.
       01  B-REC PIC X(15).
       WORKING-STORAGE SECTION.
       01  A-ST PIC XX.
       01  B-ST PIC XX.
       01  N    PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F-A.
           DISPLAY "S1-CREATE=" A-ST.
           MOVE "REC-ONE" TO A-REC.
           WRITE A-REC.
           MOVE "REC-TWO" TO A-REC.
           WRITE A-REC.
           CLOSE F-A.
           OPEN EXTEND F-A.
           DISPLAY "S2-EXTEND=" A-ST.
           MOVE "REC-THREE" TO A-REC.
           WRITE A-REC.
           CLOSE F-A.
           OPEN INPUT F-A.
           PERFORM READ-A 4 TIMES.
           CLOSE F-A.
           OPEN OUTPUT F-B.
           DISPLAY "S4-RECREATE=" B-ST.
           MOVE "FIFTEEN-CHARS-X" TO B-REC.
           WRITE B-REC.
           CLOSE F-B.
           OPEN INPUT F-B.
           MOVE ALL "*" TO B-REC.
           READ F-B AT END DISPLAY "S5-UNEXPECTED-END".
           DISPLAY "S5-READ=" B-ST " [" B-REC "]".
           READ F-B AT END DISPLAY "S5-END=" B-ST.
           CLOSE F-B.
           STOP RUN.
       READ-A.
           ADD 1 TO N.
           MOVE ALL "*" TO A-REC.
           READ F-A AT END DISPLAY "S3-END=" A-ST.
           IF A-ST = "00"
               DISPLAY "S3-READ" N "=" A-ST " [" A-REC "]".
