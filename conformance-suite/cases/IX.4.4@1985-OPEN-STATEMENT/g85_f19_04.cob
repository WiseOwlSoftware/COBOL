      *>--- conformance-case
      *> id: g85_f19_04
      *> rules: [1985-GR-IX.4.4.4-13, 1985-GR-IX.4.4.4-15, 1985-GR-IX.4.4.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR13 - OPEN I-O places the indexed file NI in
      *> the open mode for both input and output: in dynamic access it
      *> WRITEs keys K3 and K1, READs K3 by key and REWRITEs it; a
      *> later OPEN INPUT reads K1 then K3 (with the rewritten data) in
      *> ascending key order.
      *> §1985:IX.4.4.4 GR15 - NE and NI are OPTIONAL and absent: OPEN
      *> EXTEND NE and OPEN I-O NI each create the file (status 05,
      *> §1985:IX.1.3.4) as if by OPEN OUTPUT then CLOSE, so the first
      *> READ NEXT on NI meets the end of an empty file (10).
      *> NU: OPEN OUTPUT re-creates a file that held O1 and the file
      *> "contains no data records" (the next READ is at end).
      *> §1985:IX.4.4.4 GR17 - OPEN EXTEND NE I-O NI and OPEN INPUT
      *> NE NI give each file its own mode and status, as separate
      *> OPENs would; OPEN INPUT NX NY on two absent non-optional
      *> files fails NX FIRST (status 35), so NX's USE procedure, which
      *> ends the run, is the only one entered. A reversed order would
      *> print USE NY instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1904.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL NE ASSIGN TO "F1904E"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NE-K
               FILE STATUS IS NE-S.
           SELECT OPTIONAL NI ASSIGN TO "F1904I"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS NI-K
               FILE STATUS IS NI-S.
           SELECT NU ASSIGN TO "F1904O"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NU-K
               FILE STATUS IS NU-S.
           SELECT NX ASSIGN TO "F1904X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NX-K
               FILE STATUS IS NX-S.
           SELECT NY ASSIGN TO "F1904Y"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NY-K
               FILE STATUS IS NY-S.
       DATA DIVISION.
       FILE SECTION.
       FD  NE
           LABEL RECORDS ARE STANDARD.
       01  NE-R.
           05  NE-K PIC X(2).
           05  NE-D PIC X(2).
       FD  NI
           LABEL RECORDS ARE STANDARD.
       01  NI-R.
           05  NI-K PIC X(2).
           05  NI-D PIC X(2).
       FD  NU
           LABEL RECORDS ARE STANDARD.
       01  NU-R.
           05  NU-K PIC X(2).
           05  NU-D PIC X(2).
       FD  NX
           LABEL RECORDS ARE STANDARD.
       01  NX-R.
           05  NX-K PIC X(2).
           05  NX-D PIC X(2).
       FD  NY
           LABEL RECORDS ARE STANDARD.
       01  NY-R.
           05  NY-K PIC X(2).
           05  NY-D PIC X(2).
       WORKING-STORAGE SECTION.
       01  NE-S PIC XX VALUE "99".
       01  NI-S PIC XX VALUE "99".
       01  NU-S PIC XX VALUE "99".
       01  NX-S PIC XX VALUE "99".
       01  NY-S PIC XX VALUE "99".
       PROCEDURE DIVISION.
       DECLARATIVES.
       NX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NX.
       NX-ERR-P.
           DISPLAY "USE NX " NX-S.
           STOP RUN.
       NY-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NY.
       NY-ERR-P.
           DISPLAY "USE NY " NY-S.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN EXTEND NE I-O NI.
           DISPLAY "OPEN EXTEND I-O " NE-S " " NI-S.
           MOVE "E1E1" TO NE-R.
           WRITE NE-R INVALID KEY DISPLAY "NE WRITE INVALID".
           READ NI NEXT RECORD AT END DISPLAY "NI AT END " NI-S.
           MOVE "K3D3" TO NI-R.
           WRITE NI-R INVALID KEY DISPLAY "NI WRITE K3 INVALID".
           MOVE "K1D1" TO NI-R.
           WRITE NI-R INVALID KEY DISPLAY "NI WRITE K1 INVALID".
           MOVE SPACES TO NI-R.
           MOVE "K3" TO NI-K.
           READ NI INVALID KEY DISPLAY "NI READ K3 INVALID".
           DISPLAY "NI READ " NI-R " " NI-S.
           MOVE "DX" TO NI-D.
           REWRITE NI-R INVALID KEY DISPLAY "NI REWRITE INVALID".
           DISPLAY "NI REWRITE " NI-S.
           CLOSE NE NI.
           OPEN INPUT NE NI.
           DISPLAY "OPEN INPUT " NE-S " " NI-S.
           READ NE AT END DISPLAY "NE AT END".
           DISPLAY "NE " NE-R.
           READ NE AT END DISPLAY "NE AT END " NE-S.
           READ NI NEXT RECORD AT END DISPLAY "NI AT END".
           DISPLAY "NI " NI-R.
           READ NI NEXT RECORD AT END DISPLAY "NI AT END".
           DISPLAY "NI " NI-R.
           READ NI NEXT RECORD AT END DISPLAY "NI AT END " NI-S.
           CLOSE NE NI.
           OPEN OUTPUT NU.
           DISPLAY "NU OUTPUT " NU-S.
           MOVE "O1O1" TO NU-R.
           WRITE NU-R INVALID KEY DISPLAY "NU WRITE INVALID".
           CLOSE NU.
           OPEN OUTPUT NU.
           DISPLAY "NU OUTPUT " NU-S.
           CLOSE NU.
           OPEN INPUT NU.
           DISPLAY "NU INPUT " NU-S.
           READ NU AT END DISPLAY "NU AT END " NU-S.
           CLOSE NU.
           OPEN INPUT NX NY.
           DISPLAY "NOT REACHED".
           STOP RUN.
