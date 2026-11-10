      *>--- conformance-case
      *> id: pb5076-read-use-for-output-does-not-apply-85
      *> rules: [GR-14.9.30.4-24]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:VII.4.6.4 GR5 c): a USE ON OUTPUT runs "for any file open in the output mode or in the process of being
      *>   opened in the output mode", and a READ runs on a file open INPUT or I-O. The declarative is not applicable to the READ, so
      *>   1985:VII.4.4.3 SR2 (the AT END phrase) still binds it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5076N7.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       U1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       U1-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN INPUT SF.
           READ SF.
           CLOSE SF.
           STOP RUN.
