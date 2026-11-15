      *>--- conformance-case
      *> id: pb5573-control-clause-ref-mod-85
      *> rules: [1985-SR-XIII.3.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5573. §1985:XIII.3.7.3 SR1: "Data-name-1 may be qualified", and
      *> §1985:III.2 says a 'data-name' in a general format "must not be
      *> reference-modified, subscripted, or qualified unless specifically permitted by
      *> the rules of the format": no 1985 rule permits a reference-modified control
      *> name (ISO/IEC 1989:2002 13.16.15.2 SR4 adds it). The report has no control
      *> heading or footing, so the CONTROL clause alone writes WK (1:1). Expected:
      *> COBOL0900 at 85 at the CONTROL operand. Fails if the CONTROL clause's operand is
      *> not asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5573CC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO P5573CC.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WK PIC X(2) VALUE "AB".
       REPORT SECTION.
       RD  R-1 CONTROL IS WK (1:1).
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) SOURCE WK.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
