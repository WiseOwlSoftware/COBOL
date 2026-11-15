      *>--- conformance-case
      *> id: g2-record-contains-odo-maximum-over
      *> rules: [2002-SR-13.16.41.2-2, 2002-GR-13.16.41.3-3]
      *> source-format: fixed
      *>---
      *> §2002:13.16.41.2 SR2 - "No record description entry for the file
      *> may specify a number of bytes greater than integer-1."  By
      *> §2002:13.16.41.3 GR3 FR's size is 2 (C) plus the maximum of the
      *> variable-length data item, 10 (T OCCURS 1 TO 10), = 12 bytes,
      *> which is greater than integer-1 = 11, so the program is refused
      *> at the RECORD clause.  A compiler that sized FR by its minimum
      *> (3 bytes) would accept it.  The accepted twin, RECORD CONTAINS
      *> 12, is g2-record-contains-odo-maximum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2rec02f.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 11 CHARACTERS.
       01  FR.
           05 C  PIC 99.
           05 T  PIC X OCCURS 1 TO 10 DEPENDING ON C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
