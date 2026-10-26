      *>--- conformance-case
      *> id: pb1046-report-item-referenced-by-file-status
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046. 13.8.6.2.3 bars "any clauses in any other section of the data division" from accessing or
      *> referring to a report item, not only the procedure division's statements. The FILE STATUS clause of the data
      *> file DAT names ST, a printable item of the report section: refused as COBOL5092 where it used to be
      *> COBOL1639 "the clause's data-name references no data item - no declaration in this source element gives
      *> the name 'ST'", false about a name the report section declares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046R4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046r4.rpt".
           SELECT DAT ASSIGN TO "pb1046r4.dat" FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  DAT.
       01  DAT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  W PIC XX VALUE "00".
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  ST COLUMN 1 PIC XX SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
