      *>--- conformance-case
      *> id: pb5007-default-clause-below-2002
      *> rules: [FMT-13.16.2, FMT-13.18.17.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5007 - the DEFAULT clause (ISO 13.18.17) is one of
      *> the 13.16.2 validation-clauses, the VALIDATE facility's data
      *> description syntax: a COBOL-2002 introduction. 8.9 reserves
      *> DEFAULT from 2002, and COBOL-85 has no such clause. At --std 85
      *> the entry is therefore refused BY NAME, the edition diagnostic
      *> COBOL0900 naming its introduction, as the VALIDATE statement is
      *> (cases/14.9.50-VALIDATE/pb3148-validate-statement-below-2002),
      *> with no COBOL1708 decline beside it: the facility is not
      *> COBOL-85's to decline. It drew a generic COBOL5506 at the
      *> literal ("expected EXTERNAL or GLOBAL") when the clause's
      *> grammar arm asked the edition. A COBOL-85 program that names an
      *> item DEFAULT keeps it (the reservation gate). The refusal at
      *> 2002 and later is
      *> wiseowl/cases/13.18.17-DEFAULT/declined-validate-default.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5007DF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(4) DEFAULT IS "AB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
