      *>--- conformance-case
      *> id: pb5074-set-up-by-noninteger-at-85
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - the edition half of
      *>   pb5074_set_expression_operand_2002. 1985 VI.6.23.3 SR3:
      *>   "Identifier-3 must reference an elementary numeric integer."
      *>   N PIC 9V9 is no integer. Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NSU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3 INDEXED BY IX.
       01  N PIC 9V9 VALUE 1.0.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1
           SET IX UP BY N
           STOP RUN.
