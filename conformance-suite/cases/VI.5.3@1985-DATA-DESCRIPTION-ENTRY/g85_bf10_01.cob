      *>--- conformance-case
      *> id: g85_bf10_01
      *> rules: [1985-GR-VI.5.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.4 GR3 - "Multiple level 01 entries subordinate to any
      *> given level indicator other than the level indicator RD ... represent
      *> implicit redefinitions of the same area." Two 01 records under one FD.
      *>   B2=DEF        : REC-A is moved, REC-B's second half is read back.
      *>   A=XYZDEF      : B1 of REC-B is moved, REC-A shows it.
      *>   B1=XYZ B2=DEF : after READ the one record area is seen through REC-B.
      *> Each leg fails if the two 01 entries get separate storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1001.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "bf1001.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  REC-A                   PIC X(6).
       01  REC-B.
           05  B1                  PIC X(3).
           05  B2                  PIC X(3).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDEF" TO REC-A.
           DISPLAY "B2=" B2.
           MOVE "XYZ" TO B1.
           DISPLAY "A=" REC-A.
           WRITE REC-A.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "NO RECORD".
           DISPLAY "B1=" B1 " B2=" B2.
           CLOSE F1.
           STOP RUN.
