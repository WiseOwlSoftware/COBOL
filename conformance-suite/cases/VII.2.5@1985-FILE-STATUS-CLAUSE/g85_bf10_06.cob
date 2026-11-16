      *>--- conformance-case
      *> id: g85_bf10_06
      *> rules: [1985-SR-VII.2.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.5.3 SR1 - "Data-name-1 may be qualified." FILE STATUS IS
      *> FS OF ST-A, with a second FS under ST-B that must stay untouched.
      *> The values come from §1985:VII.1.3.5 (I-O Status = 00 successful,
      *> I-O Status = 10 "no next logical record exists in the file").
      *>   OPEN A=00 B=** / WRITE A=00 B=** / READ A=00 B=** / EOF A=10 B=**
      *> A compiler that binds the wrong FS shows the status under B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1006.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "bf1006.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS OF ST-A.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  F1-REC                  PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-A.
           05  FS                  PIC XX VALUE "**".
       01  ST-B.
           05  FS                  PIC XX VALUE "**".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           DISPLAY "OPEN A=" FS OF ST-A " B=" FS OF ST-B.
           MOVE "R001" TO F1-REC.
           WRITE F1-REC.
           DISPLAY "WRITE A=" FS OF ST-A " B=" FS OF ST-B.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EARLY END".
           DISPLAY "READ A=" FS OF ST-A " B=" FS OF ST-B.
           READ F1 AT END DISPLAY "EOF A=" FS OF ST-A " B=" FS OF ST-B.
           CLOSE F1.
           STOP RUN.
