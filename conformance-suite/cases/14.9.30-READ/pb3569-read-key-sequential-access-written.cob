      *>--- conformance-case
      *> id: pb3569-read-key-sequential-access-written
      *> rules: [GR-14.9.30.4-19, GR-12.4.5.5.3-2, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569. The same rule as pb3569-read-key-sequential-
      *> access-default, with ACCESS MODE IS SEQUENTIAL WRITTEN: ISO
      *> 14.9.30.4 GR19 makes a READ with a KEY phrase and no NEXT or
      *> PREVIOUS a random read; ISO 14.9.30.1 gives a READ under
      *> sequential access the job of making "available the next logical
      *> record"; ISO 12.4.5.5.3 GR2 has such a file's records "accessed in
      *> the sequence dictated by the file organization" (each citation
      *> through cite.py --check). The refusal is COBOL0864 at every
      *> edition and names the access mode without the "not specified"
      *> clause the default spelling carries, and no phrase: the statement
      *> writes none of NEXT, PREVIOUS, ADVANCING, AT END or NOT AT END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3569B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB3569B.TMP"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05 IX-KEY     PIC X(3).
           05 IX-DATA    PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT IXF
           READ IXF KEY IS IX-KEY
               INVALID KEY DISPLAY "INVALID"
           END-READ
           CLOSE IXF
           STOP RUN.
