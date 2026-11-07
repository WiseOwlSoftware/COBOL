      *>--- conformance-case
      *> id: pb4903-debug-name-declared-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4903 (review R1-1): DEBUG-NAME is a reserved word
      *> at COBOL-85, and WITH DEBUGGING MODE makes the REGISTER a
      *> legal reference, never a user-defined word of that spelling,
      *> so declaring a data item DEBUG-NAME is refused (COBOL0901,
      *> ISO/IEC 1989:2023 8.9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4903N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 DEBUG-NAME PIC X(8).
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           STOP RUN.
