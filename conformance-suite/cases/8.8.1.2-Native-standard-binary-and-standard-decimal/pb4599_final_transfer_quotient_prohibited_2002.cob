      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_prohibited_2002
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4599: the ROUNDED MODE IS phrase of
      *> pb4599_final_transfer_quotient_prohibited is a COBOL-2014
      *> introduction (ISO 14.7.4); below it the program is refused
      *> at the phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599PROH2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC S9(18) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED MODE IS PROHIBITED = A * A * A / (A * A)
           STOP RUN.
