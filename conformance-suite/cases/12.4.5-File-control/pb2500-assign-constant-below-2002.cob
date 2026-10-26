      *>--- conformance-case
      *> id: pb2500-assign-constant-below-2002
      *> rules: [SR-13.10.3-2, SR-12.4.5.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2500 - the NEGATIVE twin of pb2500_assign_constant_name
      *> below its introducing edition: the constant entry (01 ...
      *> CONSTANT AS, ISO 13.10) is a COBOL-2002 introduction, so at 85
      *> the word PB2500KN cannot be a constant-name and the program is
      *> refused at the entry, COBOL0900 (constructs row
      *> constant-entry-2002). The ASSIGN clause is not reached by a
      *> constant that does not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2500B85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WR ASSIGN TO PB2500KN
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  WR.
       01  WR-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  PB2500KN CONSTANT AS "pb2500a.dat".
       PROCEDURE DIVISION.
           STOP RUN.
