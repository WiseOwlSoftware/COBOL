      *>--- conformance-case
      *> id: pb4196-contained-file-status-nearer-report-entry-hides-global-data-item
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196, the data division clause arm. OUTERP declares ONE two-character item PV, under the GLOBAL record
      *> G1: a legal FILE STATUS item (12.4.5.8.3 SR2). The contained INNER declares its own report entry PV, and rule 3a of
      *> 8.4.6.2.1 ("If the name is declared in source element B, the item in source element B is the referenced item")
      *> makes FILE STATUS IS PV in INNER name that entry, which 13.8.6.2.3 bars every clause of a data division section
      *> other than the report section from naming ("Report items shall not be accessed or referred to by any clauses in
      *> any other section of the data division"): COBOL5092. This test can fail in one way: the lone GLOBAL data item is
      *> taken as the file status item and the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL.
           05  PV PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196b.rpt".
           SELECT DAT ASSIGN TO "pb4196b.dat" FILE STATUS IS PV.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  DAT.
       01  DAT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196B.
