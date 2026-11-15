      *>--- conformance-case
      *> id: pb5421-set-up-by-index-name-at-85
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5421 - 1985 VI.6.23.3 SR3: "Identifier-3 must
      *>   reference an elementary numeric integer." IY is an
      *>   index-name, no integer. Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5421IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABCDE".
           05  E PIC X OCCURS 5 INDEXED BY IX IY.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1
           SET IY TO 2
           SET IX UP BY IY
           STOP RUN.
