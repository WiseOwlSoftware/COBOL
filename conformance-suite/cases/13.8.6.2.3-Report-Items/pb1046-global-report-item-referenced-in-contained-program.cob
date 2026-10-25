      *>--- conformance-case
      *> id: pb1046-global-report-item-referenced-in-contained-program
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046. 13.18.27.4 GR1: "All data-names subordinate to a global name are global names", so under
      *> RD R-1 GLOBAL the contained program INNER can name PV, a printable item of the container's report. That makes
      *> the name referenceable, not the reference legal: 13.8.6.2.3 closes the procedure division's access to report
      *> items to the sum counters and the DETAIL groups GENERATE names, and PV is neither. DISPLAY PV in INNER is
      *> refused as COBOLNET5092 (it used to be COBOLNET1639 "'PV' is not defined"). The control is
      *> pb1046_report_sum_counter_in_contained_program, where INNER alters a sum counter of the same GLOBAL report.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046R3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046r3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD  R-1 GLOBAL PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           CALL "INNER".
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       PROCEDURE DIVISION.
       SHOW.
           DISPLAY PV.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB1046R3.
