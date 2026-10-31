      *>--- conformance-case
      *> id: pb3768-assign-using-names-report-item
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3768. 13.8.6.2.3 bars "any clauses in any other section of the data division" from accessing or
      *> referring to a report item, and 13.15.3 SR7 says the data-name of a report group entry "shall not be
      *> referenced in any other way" than the listed ones. The ASSIGN ... USING clause of the data file DAT names
      *> PN, a printable item of the report section: refused as COBOL5092. It was told COBOL1810 "shall reference
      *> an alphanumeric data item and no such data item is described" (12.4.5.2 SR7), false about a name the
      *> report section declares. The sibling arms are pb1046-report-item-referenced-by-file-status and
      *> pb3768-record-key-names-report-item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3768A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3768a.rpt".
           SELECT DAT ASSIGN USING PN.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  DAT.
       01  DAT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PN COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
