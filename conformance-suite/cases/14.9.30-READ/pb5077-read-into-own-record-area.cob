      *>--- conformance-case
      *> id: pb5077-read-into-own-record-area
      *> rules: [GR-14.9.30.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5077 - 1985:VII.4.4.3 SR1: "The storage area associated with identifier-1 and the record area associated with file-name-1
      *>   must not be the same storage area." (cite.py: OK); 2002:14.8.29.2 SR2 says the same with "shall". SREC is the only record of SF, so
      *>   it IS the record area. COBOL-2023 drops the rule for its overlapping-operand rules (2023:14.6.10).
      *>   conformance:pb5077_from_into_outside_the_record_area is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5077N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5077N1.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT SF.
           READ SF INTO SREC AT END DISPLAY "E".
           CLOSE SF.
           STOP RUN.
