      *>--- conformance-case
      *> id: pb4031-report-turn-size-85
      *> rules: [GR-7.3.25.4-6, GR-13.18.53.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4031 - the edition boundary of
      *> pb4031_report_source_size_checking: COBOL-85 has no compiler
      *> directives and no exception checking, so a >>TURN EC-SIZE
      *> directive governing a GENERATE is refused there (ISO 7.3.25,
      *> the TURN directive, is new in COBOL 2002); the report itself,
      *> a SOURCE identifier, is COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4031T85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4031t85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-T.
       WORKING-STORAGE SECTION.
       01  WS-A      PIC 9(5)  VALUE 10000.
       REPORT SECTION.
       RD  R-T PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A.
       PROCEDURE DIVISION.
       M-1.
           OPEN OUTPUT PRT.
           INITIATE R-T.
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           GENERATE DET.
           TERMINATE R-T.
           CLOSE PRT.
           STOP RUN.
