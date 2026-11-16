      *>--- conformance-case
      *> id: g85_f38_03
      *> rules: [1989-FMT-A.2.28.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.28.2 - the format requires exactly two arguments,
      *> FUNCTION MOD (argument-1 argument-2). The COMPUTE below
      *> gives one; a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3803.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-R         PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION MOD (11).
           DISPLAY W-R.
           STOP RUN.
