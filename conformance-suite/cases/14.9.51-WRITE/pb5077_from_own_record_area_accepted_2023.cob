      *>--- conformance-case
      *> id: pb5077_from_own_record_area_accepted_2023
      *> rules: [GR-14.9.51.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - COBOL-2023 has no syntax rule against a FROM operand that is the record area: 2023:14.6.10 (overlapping operands)
      *>   and 2023:14.9.30.4 NOTE 1 leave the result of a statement whose operands share storage undefined, and the program legal. The
      *>   1985 and 2002 editions refuse it (conformance:negative/pb5077-write-from-own-record-area). The program prints nothing: the
      *>   result is undefined, so the test proves translation only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077P2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077P2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SF.
           MOVE "ABCD" TO SREC.
           WRITE SREC FROM SREC.
           CLOSE SF.
           STOP RUN.
