      *>--- conformance-case
      *> id: pb5074-set-to-negative-at-85
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 1985 VI.6.23.3 SR4: "Integer-1 and integer-2
      *>   may be signed. Integer-1 must be positive." -1 is not.
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO -1
           STOP RUN.
