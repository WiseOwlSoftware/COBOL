      *>--- conformance-case
      *> id: pb1046-report-group-referenced-in-procedure-division
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046. 13.8.6.2.3 2): "Report groups of type DETAIL are accessed by the GENERATE statement." That is
      *> the only procedure division access a report group has, and 13.15.3 SR7 lists the others (a USE BEFORE
      *> REPORTING statement, as a qualifier for a SUM counter, in the UPON phrase of the SUM clause, or as an operand
      *> in a SUM clause): "The data-name shall not be referenced in any other way". DISPLAY DET-1 names the group in a
      *> way none of them admits, qualified by its report-name or not, so it is refused as COBOL5092 - not as
      *> COBOL1639 "'DET-1' is not defined", which was false: the report section declares it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046r2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           DISPLAY DET-1 OF R-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
