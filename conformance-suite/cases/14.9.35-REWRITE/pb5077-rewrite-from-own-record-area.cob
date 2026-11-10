      *>--- conformance-case
      *> id: pb5077-rewrite-from-own-record-area
      *> rules: [GR-14.9.35.4-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - 1985:VII.4.5.3 SR1: "Record-name-1 and identifier-1 must not reference the same storage area." (cite.py: OK);
      *>   2002:14.8.33.2 SR1 says the same with "shall".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077N3.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O SF.
           READ SF AT END DISPLAY "E".
           REWRITE SREC FROM SREC.
           CLOSE SF.
           STOP RUN.
