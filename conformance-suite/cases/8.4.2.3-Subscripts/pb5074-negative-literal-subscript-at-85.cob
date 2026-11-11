      *>--- conformance-case
      *> id: pb5074-negative-literal-subscript-at-85
      *> rules: [FMT-8.4.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 1985 IV.4.3.8.2.3 SR6: "Integer-1 may be
      *>   signed and, if signed, it must be positive." -1 is signed
      *>   and negative; from 2002 it is an arithmetic expression whose
      *>   value raises EC-BOUND-SUBSCRIPT at run time (8.4.2.3.4 GR2).
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NSL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "Z" TO E (-1)
           STOP RUN.
