      *>--- conformance-case
      *> id: pb3569-read-key-sequential-access-default
      *> rules: [GR-14.9.30.4-19, GR-12.4.5.5.3-1, GR-12.4.5.5.3-2, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569. ISO 14.9.30.4 GR19: "An implicit or explicit NEXT
      *> phrase or a PREVIOUS phrase results in a sequential read:
      *> otherwise, the read is a random read and the rules for format 2
      *> apply." ISO 14.9.30.1: "For sequential access, the READ statement
      *> makes available the next logical record from a file. For random
      *> access, the READ statement makes available a specified record
      *> from a mass storage file." ISO 12.4.5.5.3 GR1: "If the ACCESS MODE
      *> clause is not specified, sequential access is assumed." GR2: "If
      *> the access mode is sequential, records in the file are accessed in
      *> the sequence dictated by the file organization" (each citation
      *> through cite.py --check).
      *> The READ below writes a KEY phrase and none of NEXT, PREVIOUS,
      *> ADVANCING, AT END or NOT AT END: it is a random read (GR19), and
      *> the file has no ACCESS MODE clause, so its access mode is
      *> sequential. The refusal is COBOL0864 at every edition (neither the
      *> KEY phrase nor ACCESS MODE carries an edition marker), and its text
      *> names the file's access mode (assumed, because the clause is not
      *> written) and NO phrase, because the statement writes none of
      *> NEXT, PREVIOUS, ADVANCING, AT END or NOT AT END. The legal twin is
      *> the same READ on a file whose ACCESS MODE is RANDOM or DYNAMIC
      *> (l1_read_key_of_reference).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3569A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB3569A.TMP"
               ORGANIZATION IS INDEXED
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
