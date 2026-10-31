      *>--- conformance-case
      *> id: pb4196-contained-data-item-nearer-than-global-report-entry
      *> rules: [GR-8.4.6.2.1-3, SR-13.8.6.2.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4196, the counterpart of pb4196-contained-reference-nearer-report-entry-hides-global-data-item. OUTERP
      *> declares its report R-1 GLOBAL (13.18.27.3 SR1 e), so the report entry PV under DET-1 is a global name of the
      *> contained INNER (13.18.27.4 GR1: "All data-names subordinate to a global name are global names"). INNER declares
      *> its own data item PV, and 8.4.6.2.1 rule 3a ("If the name is declared in source element B, the item in source
      *> element B is the referenced item") makes MOVE PV TO W2 in INNER read INNER's PV, 7, and not the container's report
      *> entry, which 13.8.6.2.3 would bar the procedure division from naming. W2, a two-digit unsigned item, receives 7
      *> and DISPLAY shows its two digits: 07. This test can fail in one way: the farther report entry is taken, and the
      *> program is refused with COBOL5092.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4196E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4196e.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 99 VALUE 1.
       REPORT SECTION.
       RD  R-1 GLOBAL PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC 99 SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           CALL "INNER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PV PIC 99 VALUE 7.
       01  W2 PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       SHOW.
           MOVE PV TO W2.
           DISPLAY W2.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB4196E.
