      *>--- conformance-case
      *> id: pb4196-contained-index-name-nearer-report-entry-hides-global-index-name
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196, the index-name arm. 8.4.6.2.3 gives an index-name the scope of the data-name that names its table,
      *> so PV, written INDEXED BY under the GLOBAL table T1 of OUTERP, is a global name of the contained INNER, and 8.4.6.2.1
      *> rule 3a ("If the name is declared in source element B, the item in source element B is the referenced item")
      *> ranks it against the report entry PV that INNER declares: the entry is the referenced item, and SET PV UP BY 1
      *> in INNER names a report item, which 13.8.6.2.3 bars: COBOL5092. This test can fail in one way: the container's
      *> index-name PV is stepped, and the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 GLOBAL.
           05  E1 PIC X OCCURS 3 INDEXED BY PV.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196d.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       SHOW.
           SET PV UP BY 1.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196D.
