      *>--- conformance-case
      *> id: pb5924-logical-records-in-blocks-85
      *> rules: [1985-GR-IV.4.3.1.2-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.1.2 (cite.py: OK): "control of the accessibility of logical records"
      *> Seven logical records written to a file of three-record blocks
      *> read back whole and in order, the last block partial.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKREC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BLK-FILE ASSIGN TO BLKDISK
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  BLK-FILE
           BLOCK CONTAINS 3 RECORDS
           RECORD CONTAINS 4 CHARACTERS
           LABEL RECORDS ARE STANDARD.
       01  BLK-REC.
           05  R-TAG         PIC XXX.
           05  R-NUM         PIC 9.
       WORKING-STORAGE SECTION.
       01  I                 PIC 9 VALUE 0.
       01  EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT BLK-FILE.
           PERFORM 7 TIMES
               ADD 1 TO I
               MOVE "REC" TO R-TAG
               MOVE I TO R-NUM
               WRITE BLK-REC
           END-PERFORM.
           CLOSE BLK-FILE.
           OPEN INPUT BLK-FILE.
           PERFORM UNTIL EOF = "Y"
               READ BLK-FILE
                   AT END MOVE "Y" TO EOF
                   NOT AT END DISPLAY BLK-REC
               END-READ
           END-PERFORM.
           CLOSE BLK-FILE.
           STOP RUN.
