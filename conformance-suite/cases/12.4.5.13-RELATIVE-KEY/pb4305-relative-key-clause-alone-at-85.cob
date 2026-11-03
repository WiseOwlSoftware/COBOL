      *>--- conformance-case
      *> id: pb4305-relative-key-clause-alone-at-85
      *> rules: [FMT-12.4.5.13.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_relative_key_clause_alone_2002. COBOL-85 wrote
      *>   RELATIVE KEY as a phrase of the ACCESS MODE clause (2002
      *>   Annex F.2 item 112). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NRK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB4305RK"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
