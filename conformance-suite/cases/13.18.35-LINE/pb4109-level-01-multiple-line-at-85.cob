*>--- conformance-case
*> id: pb4109-level-01-multiple-line-at-85
*> rules: [SR-13.18.35.3-10]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE MULTIPLE LINE CLAUSE ENTERED AT COBOL-2002 AT EVERY LEVEL, THE FIRST ONE INCLUDED (kb/Work PB4109; the
*> level-03 twin is pb565-report-multiple-line-at-85). ISO/IEC 1989:2023 §13.18.35.3 SR10: "If more than one
*> integer-1 or integer-2 operand is specified, the clause is referred to as a multiple LINE clause", stated with
*> no level restriction. The COBOL-85 LINE clause took ONE operand, so a COBOL-85 compilation shall name the
*> edition rather than print one line: construct `report-multi-line-2002`, diagnostic COBOL0900. Above 85 the
*> same entry compiles and prints M on page lines 2 and 4 (pb4109-level-01-multiple-line).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4109L85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4109l85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-L85.
       REPORT SECTION.
       RD  R-L85 PAGE LIMIT IS 20 LINES.
       01  D-L85 TYPE DE LINE 2 4.
           02  COLUMN 1 PIC X(1) VALUE "M".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-L85.
           GENERATE D-L85.
           TERMINATE R-L85.
           CLOSE RPT.
           STOP RUN.
