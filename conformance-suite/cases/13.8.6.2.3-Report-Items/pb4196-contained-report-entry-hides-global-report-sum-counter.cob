      *>--- conformance-case
      *> id: pb4196-contained-report-entry-hides-global-report-sum-counter
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196 (branch review R1-1). 8.4.6.2.1 rule 3a: "If the name is declared in source element B, the item in
      *> source element B is the referenced item", and a name a container declares GLOBAL is a global name of the contained
      *> program only when the contained program does not declare it. The sum-counter form of
      *> pb4196-contained-reference-nearer-report-entry-hides-global-data-item: the GLOBAL report R-0 of PB4196F has the sum
      *> counter PV (13.18.54.4 GR5: "the data-name is the name of the sum counter", and 13.14.4 1) makes a GLOBAL report's
      *> sum counters global), which 13.8.6.2.3 1) lets a procedure division inspect. The contained INNER declares its own
      *> report entry PV in its REPORT SECTION, so MOVE PV TO W in INNER refers to that entry, not to the container's sum
      *> counter, and 13.8.6.2.3 bars the procedure division from referring to a report item: COBOL5092. This test can fail
      *> in one way: the sum-counter lookup is not asked where the nearer report entry is declared, and MOVE PV TO W reads
      *> the container's counter as if INNER had no PV of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FL ASSIGN TO "pb4196f-out.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FL GLOBAL REPORT IS R-0.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3 GLOBAL.
       REPORT SECTION.
       RD  R-0 GLOBAL CONTROL FINAL PAGE LIMIT IS 10 LINES.
       01  DET-0 TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE N.
       01  TOT-0 TYPE CF FINAL LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SUM N.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196f.rpt".
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
       END PROGRAM PB4196F.
