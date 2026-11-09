      *>--- conformance-case
      *> id: pb4700-receive-undefined-data-name-1
      *> rules: [FMT-14.9.31.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE FROM NOSUCH-TAG GIVING REC WS-LEN - data-name-1 (the message tag) names nothing. Expected: COBOL1639.
      *> ISO §2023:8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource" - the operand is
      *> printed by the §2023:14.9.31.2 general format, so it is read like any identifier (kb/Work PB4700, PB3594).
      *>   cite.py: OK  §2023:8.4.2.1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4700R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT USAGE MESSAGE-TAG.
       01 REC PIC X(4).
       01 WS-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM NOSUCH-TAG GIVING REC WS-LEN
           END-RECEIVE.
           STOP RUN.
