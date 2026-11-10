      *>--- conformance-case
      *> id: pb5077-write-from-own-record-area
      *> rules: [GR-14.9.51.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - 1985:VII.4.7.3 SR1: "Record-name-1 and identifier-1 must not reference the same storage area." (cite.py: OK);
      *>   2002:14.8.47.2 SR4 says the same with "shall". Record-name-1 and identifier-1 are the one data item SREC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077N2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SF.
           WRITE SREC FROM SREC.
           CLOSE SF.
           STOP RUN.
