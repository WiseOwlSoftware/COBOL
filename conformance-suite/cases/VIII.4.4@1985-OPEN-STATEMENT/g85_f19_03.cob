      *>--- conformance-case
      *> id: g85_f19_03
      *> rules: [1985-GR-VIII.4.4.4-13, 1985-GR-VIII.4.4.4-15, 1985-GR-VIII.4.4.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR13 - OPEN I-O places the relative file RI
      *> in the open mode for both input and output: in dynamic access
      *> it WRITEs records 3 and 1, READs record 3 at random and
      *> REWRITEs it; a later OPEN INPUT reads R1 then R4 in relative
      *> record number order.
      *> §1985:VIII.4.4.4 GR15 - RE and RI are OPTIONAL and absent:
      *> OPEN EXTEND RE and OPEN I-O RI each create the file (status
      *> 05, §1985:VIII.1.3.4) as if by OPEN OUTPUT then CLOSE, so the
      *> first READ NEXT on RI meets the end of an empty file (10).
      *> RO: OPEN OUTPUT re-creates a file that held O1 and the file
      *> "contains no data records" (the next READ is at end).
      *> §1985:VIII.4.4.4 GR17 - OPEN EXTEND RE I-O RI and OPEN INPUT
      *> RE RI give each file its own mode and status, as separate
      *> OPENs would; OPEN INPUT RX RY on two absent non-optional
      *> files fails RX FIRST (status 35), so RX's USE procedure, which
      *> ends the run, is the only one entered. A reversed order would
      *> print USE RY instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1903.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL RE ASSIGN TO "F1903E"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RE-S.
           SELECT OPTIONAL RI ASSIGN TO "F1903I"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RI-K
               FILE STATUS IS RI-S.
           SELECT RO ASSIGN TO "F1903O"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RO-S.
           SELECT RX ASSIGN TO "F1903X"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RX-S.
           SELECT RY ASSIGN TO "F1903Y"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RY-S.
       DATA DIVISION.
       FILE SECTION.
       FD  RE
           LABEL RECORDS ARE STANDARD.
       01  RE-R PIC X(2).
       FD  RI
           LABEL RECORDS ARE STANDARD.
       01  RI-R PIC X(2).
       FD  RO
           LABEL RECORDS ARE STANDARD.
       01  RO-R PIC X(2).
       FD  RX
           LABEL RECORDS ARE STANDARD.
       01  RX-R PIC X(2).
       FD  RY
           LABEL RECORDS ARE STANDARD.
       01  RY-R PIC X(2).
       WORKING-STORAGE SECTION.
       01  RI-K PIC 9(4) VALUE 0.
       01  RE-S PIC XX VALUE "99".
       01  RI-S PIC XX VALUE "99".
       01  RO-S PIC XX VALUE "99".
       01  RX-S PIC XX VALUE "99".
       01  RY-S PIC XX VALUE "99".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RX.
       RX-ERR-P.
           DISPLAY "USE RX " RX-S.
           STOP RUN.
       RY-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RY.
       RY-ERR-P.
           DISPLAY "USE RY " RY-S.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN EXTEND RE I-O RI.
           DISPLAY "OPEN EXTEND I-O " RE-S " " RI-S.
           MOVE "E1" TO RE-R.
           WRITE RE-R INVALID KEY DISPLAY "RE WRITE INVALID".
           READ RI NEXT RECORD AT END DISPLAY "RI AT END " RI-S.
           MOVE 3 TO RI-K.
           MOVE "R3" TO RI-R.
           WRITE RI-R INVALID KEY DISPLAY "RI WRITE 3 INVALID".
           MOVE 1 TO RI-K.
           MOVE "R1" TO RI-R.
           WRITE RI-R INVALID KEY DISPLAY "RI WRITE 1 INVALID".
           MOVE 3 TO RI-K.
           MOVE SPACES TO RI-R.
           READ RI INVALID KEY DISPLAY "RI READ 3 INVALID".
           DISPLAY "RI READ " RI-R " " RI-S.
           MOVE "R4" TO RI-R.
           REWRITE RI-R INVALID KEY DISPLAY "RI REWRITE INVALID".
           DISPLAY "RI REWRITE " RI-S.
           CLOSE RE RI.
           OPEN INPUT RE RI.
           DISPLAY "OPEN INPUT " RE-S " " RI-S.
           READ RE AT END DISPLAY "RE AT END".
           DISPLAY "RE " RE-R.
           READ RE AT END DISPLAY "RE AT END " RE-S.
           READ RI NEXT RECORD AT END DISPLAY "RI AT END".
           DISPLAY "RI " RI-R.
           READ RI NEXT RECORD AT END DISPLAY "RI AT END".
           DISPLAY "RI " RI-R.
           READ RI NEXT RECORD AT END DISPLAY "RI AT END " RI-S.
           CLOSE RE RI.
           OPEN OUTPUT RO.
           DISPLAY "RO OUTPUT " RO-S.
           MOVE "O1" TO RO-R.
           WRITE RO-R INVALID KEY DISPLAY "RO WRITE INVALID".
           CLOSE RO.
           OPEN OUTPUT RO.
           DISPLAY "RO OUTPUT " RO-S.
           CLOSE RO.
           OPEN INPUT RO.
           DISPLAY "RO INPUT " RO-S.
           READ RO AT END DISPLAY "RO AT END " RO-S.
           CLOSE RO.
           OPEN INPUT RX RY.
           DISPLAY "NOT REACHED".
           STOP RUN.
