      *>--- conformance-case
      *> id: pb1046-report-item-referenced-in-procedure-division
      *> rules: [SR-13.15.3-7, SR-13.8.6.2.3-1, SR-13.8.6.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046. 13.8.6.2.3: "Report items shall not be accessed or referred to by any clauses in any other
      *> section of the data division or by any other procedure division statements, except in the following cases:
      *> 1) Sum counters may be inspected or altered in the procedure division. 2) Report groups of type DETAIL are
      *> accessed by the GENERATE statement." PV is a printable item (SOURCE W), not a sum counter, so MOVE PV TO W
      *> is refused; 13.15.3 SR7 says the same of the entry's data-name: "The data-name shall not be referenced in any
      *> other way". The program used to draw COBOL1639 "'PV' is not defined - no declaration in this source
      *> element gives the name 'PV'", false about a name the report section declares; it now draws COBOL5092,
      *> which cites the two rules. The control (the same report with the permitted references only) is
      *> pb1046_report_sum_counter_in_contained_program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046r1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
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
           MOVE PV TO W.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
