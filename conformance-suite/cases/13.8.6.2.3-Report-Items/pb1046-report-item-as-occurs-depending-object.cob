      *>--- conformance-case
      *> id: pb1046-report-item-as-occurs-depending-object
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046. 13.8.6.2.3 bars "any clauses in any other section of the data division" from accessing or
      *> referring to a report item. The OCCURS ... DEPENDING ON clause of the working-storage table TAB names CNT,
      *> a printable item of the report section: refused as COBOL5092 where it used to be COBOL0851
      *> "data-name-1 is not defined", false about a name the report section declares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046R5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046r5.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  TAB.
           05  ELT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  CNT COLUMN 1 PIC 9 SOURCE N.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
