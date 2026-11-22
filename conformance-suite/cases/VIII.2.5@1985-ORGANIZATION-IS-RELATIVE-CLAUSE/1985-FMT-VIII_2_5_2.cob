      *>--- conformance-case
      *> id: 1985-FMT-VIII_2_5_2
      *> rules: [1985-FMT-VIII.2.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.5.2 (THE ORGANIZATION IS RELATIVE CLAUSE), the
      *> general format [ ORGANIZATION IS ] RELATIVE, grants three
      *> spellings: ORGANIZATION IS RELATIVE (F1), ORGANIZATION RELATIVE
      *> (IS is an optional word: F2) and RELATIVE alone (the bracketed
      *> part omitted: F3). The program must translate and run at 85
      *> (kb/Work PB5740: a general format is witnessed by an accepted
      *> case, never by refusals alone). Each file is written with two
      *> records and read back sequentially: §1985:VIII.4.5.4 GR14, a
      *> format 1 READ "moves the relative record number of the record
      *> made available" to the relative key, so each file reads 0001
      *> then 0002, and the third READ of F3 is at end. The RELATIVE KEY
      *> is a phrase of the ACCESS MODE clause at 85 (§1985:VIII.2.3.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELFMT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "RELFMTF1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS K1.
           SELECT F2 ASSIGN TO "RELFMTF2"
               ORGANIZATION RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS K2.
           SELECT F3 ASSIGN TO "RELFMTF3"
               RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS K3.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       WORKING-STORAGE SECTION.
       01  K1 PIC 9(4).
       01  K2 PIC 9(4).
       01  K3 PIC 9(4).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT F1 F2 F3.
           MOVE "1-A" TO R1.
           WRITE R1 INVALID KEY DISPLAY "BAD WRITE".
           MOVE "1-B" TO R1.
           WRITE R1 INVALID KEY DISPLAY "BAD WRITE".
           MOVE "2-A" TO R2.
           WRITE R2 INVALID KEY DISPLAY "BAD WRITE".
           MOVE "2-B" TO R2.
           WRITE R2 INVALID KEY DISPLAY "BAD WRITE".
           MOVE "3-A" TO R3.
           WRITE R3 INVALID KEY DISPLAY "BAD WRITE".
           MOVE "3-B" TO R3.
           WRITE R3 INVALID KEY DISPLAY "BAD WRITE".
           CLOSE F1 F2 F3.
           OPEN INPUT F1 F2 F3.
           READ F1 AT END DISPLAY "F1 EOF".
           DISPLAY "F1 " K1 " " R1.
           READ F1 AT END DISPLAY "F1 EOF".
           DISPLAY "F1 " K1 " " R1.
           READ F2 AT END DISPLAY "F2 EOF".
           DISPLAY "F2 " K2 " " R2.
           READ F2 AT END DISPLAY "F2 EOF".
           DISPLAY "F2 " K2 " " R2.
           READ F3 AT END DISPLAY "F3 EOF".
           DISPLAY "F3 " K3 " " R3.
           READ F3 AT END DISPLAY "F3 EOF".
           DISPLAY "F3 " K3 " " R3.
           READ F3 AT END DISPLAY "F3 EOF".
           CLOSE F1 F2 F3.
           STOP RUN.
