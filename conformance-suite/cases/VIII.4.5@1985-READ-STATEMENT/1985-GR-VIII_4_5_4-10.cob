      *>--- conformance-case
      *> id: 1985-GR-VIII_4_5_4-10
      *> rules: [1985-GR-VIII.4.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR10 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELEOF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "ATEND1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S1.
           SELECT OPTIONAL F2 ASSIGN TO "ATEND2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S2.
           SELECT F3W ASSIGN TO "ATEND3"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
                   RELATIVE KEY IS K3W
               FILE STATUS IS S3W.
           SELECT F3R ASSIGN TO "ATEND3"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
                   RELATIVE KEY IS K3R
               FILE STATUS IS S3R.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       FD  F3W
           LABEL RECORDS ARE STANDARD.
       01  R3W.
           05  R3W-TEXT PIC X(7).
           05  R3W-NO   PIC 99.
       FD  F3R
           LABEL RECORDS ARE STANDARD.
       01  R3R PIC X(9).
       WORKING-STORAGE SECTION.
       01  S1  PIC XX VALUE "99".
       01  S2  PIC XX VALUE "99".
       01  S3W PIC XX VALUE "99".
       01  S3R PIC XX VALUE "99".
       01  K3W PIC 99 VALUE ZERO.
       01  K3R PIC 9 VALUE ZERO.
       01  CNT PIC 99 VALUE ZERO.
       01  LAST-REC PIC X(9) VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       USE1-P.
           DISPLAY "USE PROCEDURE " S1.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "RECORD-ONE" TO R1.
           WRITE R1
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE F1.
           OPEN INPUT F1.
           READ F1
               AT END DISPLAY "UNEXPECTED END"
               NOT AT END DISPLAY "READ 1 " R1
           END-READ.
           READ F1
               AT END DISPLAY "AT END " S1
               NOT AT END DISPLAY "UNEXPECTED RECORD"
           END-READ.
           DISPLAY "AFTER READ 2".
           CLOSE F1.
           OPEN INPUT F1.
           READ F1
               AT END DISPLAY "UNEXPECTED END".
           READ F1.
           DISPLAY "AFTER READ 4".
           CLOSE F1.
           OPEN INPUT F2.
           READ F2
               AT END DISPLAY "OPTIONAL AT END " S2.
           CLOSE F2.
           OPEN OUTPUT F3W.
           PERFORM WRITE-3 10 TIMES.
           CLOSE F3W.
           OPEN INPUT F3R.
           PERFORM READ-3 10 TIMES.
           CLOSE F3R.
           DISPLAY "LAST RECORD " LAST-REC.
           STOP RUN.
       WRITE-3.
           ADD 1 TO CNT.
           MOVE "RECORD " TO R3W-TEXT.
           MOVE CNT TO R3W-NO.
           WRITE R3W
               INVALID KEY DISPLAY "WRITE FAILED".
       READ-3.
           READ F3R
               AT END DISPLAY "KEY TOO SMALL " S3R
               NOT AT END MOVE R3R TO LAST-REC.
