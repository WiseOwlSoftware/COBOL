      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_intermediate_prohibited_85
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4599: the OPTIONS paragraph's INTERMEDIATE
      *> ROUNDING clause of
      *> pb4599_final_transfer_quotient_intermediate_prohibited is a
      *> COBOL-2014 introduction (ISO 11.9.11); below it the program
      *> is refused at the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599INTERPROH85.
       OPTIONS.
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R    PIC S9(18) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = A * A * A / 7.0E30
           STOP RUN.
