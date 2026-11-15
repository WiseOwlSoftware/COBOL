      *>--- conformance-case
      *> id: g2rw01_from_own_record
      *> rules: [2002-SR-14.8.33.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.2 SR1 - "Record-name-1 and identifier-1 shall
      *>   not reference the same storage area." (cite.py: OK)
      *>   REWRITE G2RW01-R FROM G2RW01-R names the record itself as
      *>   identifier-1: the same storage area. The program shall be
      *>   refused at the REWRITE line. Fails if it compiles.
      *>   The second leg (another record of the same FD, which
      *>   implicitly redefines the same area) is g2rw02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rw01.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  G2RW01-R PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F.
           READ F AT END CONTINUE END-READ.
           REWRITE G2RW01-R FROM G2RW01-R.
           CLOSE F.
           STOP RUN.
