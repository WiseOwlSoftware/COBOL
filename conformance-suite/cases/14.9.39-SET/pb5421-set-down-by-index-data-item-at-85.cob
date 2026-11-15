      *>--- conformance-case
      *> id: pb5421-set-down-by-index-data-item-at-85
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5421 - 1985 VI.6.23.3 SR3: "Identifier-3 must
      *>   reference an elementary numeric integer." IDX is a USAGE
      *>   INDEX item, no integer. Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5421ID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABCDE".
           05  E PIC X OCCURS 5 INDEXED BY IX IY.
       01  IDX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 3
           SET IY TO 2
           SET IDX TO IY
           SET IX DOWN BY IDX
           STOP RUN.
