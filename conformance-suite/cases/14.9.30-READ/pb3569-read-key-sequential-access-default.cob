      *>--- conformance-case
      *> id: pb3569-read-key-sequential-access-default
      *> rules: [SR-14.9.30.3-8, GR-14.9.30.4-19, GR-12.4.5.5.3-1, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569. ISO 14.9.30.3 SR8: "If neither the NEXT phrase nor
      *> the PREVIOUS phrase is specified and ACCESS MODE SEQUENTIAL is
      *> specified in the file control entry for file-name-1, the NEXT
      *> phrase is implied." ISO 14.9.30.4 GR19: "An implicit or explicit
      *> NEXT phrase or a PREVIOUS phrase results in a sequential read:
      *> otherwise, the read is a random read and the rules for format 2
      *> apply." ISO 12.4.5.5.3 GR1: "If the ACCESS MODE clause is not
      *> specified, sequential access is assumed." (each citation through
      *> cite.py --check). The file below has no ACCESS MODE clause, so its
      *> access mode is sequential (GR1), SR8 implies NEXT and GR19 makes
      *> the READ a sequential read, which has no KEY phrase (ISO 14.9.30.2
      *> prints it in Format 2, the random read, only). The statement
      *> writes none of NEXT, PREVIOUS, ADVANCING, AT END or NOT AT END, so
      *> the refusal (COBOL0864 at every edition: neither the KEY phrase
      *> nor ACCESS MODE carries an edition marker) names the file's access
      *> mode (assumed, because the clause is not written) and the IMPLIED
      *> NEXT, and no phrase the statement wrote. It is the same reading of
      *> SR8 and GR19 the compiler uses to refuse INVALID KEY on a
      *> sequential READ (COBOL1720). The legal twin is the same READ on a
      *> file whose ACCESS MODE is RANDOM or DYNAMIC
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
