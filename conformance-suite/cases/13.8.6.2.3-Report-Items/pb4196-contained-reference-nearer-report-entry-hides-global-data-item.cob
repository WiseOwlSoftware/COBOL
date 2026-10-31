      *>--- conformance-case
      *> id: pb4196-contained-reference-nearer-report-entry-hides-global-data-item
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196. 8.4.6.2.1 rule 3a: "If the name is declared in source element B, the item in source element B is
      *> the referenced item", and a name a container declares GLOBAL is a global name of the contained program only when
      *> the contained program does not declare it. OUTERP declares ONE data item PV, under the GLOBAL record G1; the
      *> contained INNER declares its own report entry PV in its REPORT SECTION, so MOVE PV TO W in INNER refers to that
      *> entry, not to G1's PV, and 13.8.6.2.3 bars the procedure division from referring to a report item ("Report items
      *> shall not be accessed or referred to by any clauses in any other section of the data division or by any other
      *> procedure division statements"): COBOL5092. The two-item
      *> form of the same test is pb3767-local-report-entry-nearer-than-ambiguous-global-data-items. This test can fail in
      *> one way: the lone GLOBAL data item is taken without asking where the report entry is declared, and MOVE PV TO W
      *> reads another program's data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1 GLOBAL.
           05  PV PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196a.rpt".
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
           MOVE PV TO W.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196A.
