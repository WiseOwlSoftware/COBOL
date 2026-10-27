      *>--- conformance-case
      *> id: pb3569-read-key-sequential-access-written
      *> rules: [SR-14.9.30.3-8, GR-14.9.30.4-19, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569. The same rule as pb3569-read-key-sequential-
      *> access-default, with ACCESS MODE IS SEQUENTIAL WRITTEN: ISO
      *> 14.9.30.3 SR8 implies the NEXT phrase when neither NEXT nor
      *> PREVIOUS is specified and ACCESS MODE SEQUENTIAL is specified,
      *> and ISO 14.9.30.4 GR19 makes an implicit NEXT a sequential read,
      *> which has no KEY phrase (each citation through cite.py --check).
      *> The refusal is COBOL0864 at every edition and names the access
      *> mode without the "not specified" clause the default spelling
      *> carries, and the implied NEXT, and no phrase: the statement
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
