      *>--- conformance-case
      *> id: pb3768-record-key-names-report-item
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3768, the RECORD KEY sibling of pb3768-assign-using-names-report-item (found by its sweep of the
      *> file-control clauses that name a data item). 13.8.6.2.3 bars "any clauses in any other section of the data
      *> division" from referring to a report item. The RECORD KEY clause of the indexed file DAT names PV, a
      *> printable item of the report section: refused as COBOL5092. It was told COBOL0863 "references nothing
      *> described in this program" (12.4.5.12.3 SR2), false about a name the report section declares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3768B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3768b.rpt".
           SELECT DAT ASSIGN TO "pb3768b.dat"
               ORGANIZATION IS INDEXED RECORD KEY IS PV.
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
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
