      *>--- conformance-case
      *> id: g85_f17_03
      *> rules: [1985-GR-VII.3.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.3.3 GR2 - the size of a physical record may be
      *> stated in terms of records. SQ-R states BLOCK CONTAINS 3
      *> RECORDS for fixed 8-character records (none of the GR2 a-c
      *> situations: no padding, no record spans a block); 5 records,
      *> so the last block is short, are written and read back in order.
      *> A compiler that refuses the RECORDS phrase fails translation;
      *> one that blocks wrongly loses or reorders records.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1703.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-R ASSIGN TO "G85F17BR.DAT"
               FILE STATUS IS ST-R.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-R
           BLOCK CONTAINS 3 RECORDS
           RECORD CONTAINS 8 CHARACTERS.
       01  SQ-R-REC.
           05  R-TAG PIC X(7).
           05  R-NUM PIC 9.
       WORKING-STORAGE SECTION.
       01  ST-R PIC XX.
       01  I PIC 9 VALUE 0.
       01  EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT SQ-R.
           PERFORM WRITE-ONE VARYING I FROM 1 BY 1 UNTIL I > 5.
           CLOSE SQ-R.
           OPEN INPUT SQ-R.
           PERFORM READ-ONE UNTIL EOF = "Y".
           DISPLAY "END " ST-R.
           CLOSE SQ-R.
           STOP RUN.
       WRITE-ONE.
           MOVE "RECORD-" TO R-TAG.
           MOVE I TO R-NUM.
           WRITE SQ-R-REC.
       READ-ONE.
           READ SQ-R AT END MOVE "Y" TO EOF.
           IF EOF = "N"
               DISPLAY SQ-R-REC " " ST-R.
