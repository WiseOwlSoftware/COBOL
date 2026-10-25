      *>--- conformance-case
      *> id: pb1046_report_sum_counter_in_contained_program
      *> rules: [SR-13.8.6.2.3-1, SR-13.8.6.2.3-2, SR-13.15.3-7, GR-13.18.54.4-12]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1046, the control of its negatives. 13.8.6.2.3 admits exactly two accesses to a report item outside
      *> the report section: 1) "Sum counters may be inspected or altered in the procedure division" and 2) "Report
      *> groups of type DETAIL are accessed by the GENERATE statement". Under RD R-1 GLOBAL (13.18.27.4 GR1: "All
      *> data-names subordinate to a global name are global names") the contained program INNER may do both, so the
      *> program is conforming and prints 0100:
      *>   - 13.18.54.4 GR2: "The sum counter is set to zero ... when the INITIATE statement for the current report
      *>     is executed", and the outer program INITIATEs R-1 before the CALL, so TOT is 0 when INNER starts;
      *>   - INNER's ADD 100 TO TOT alters the sum counter (GR12: "It is permissible for procedure division statements
      *>     to alter the content of sum counters"), 100, and MOVE TOT TO OUT-VAL reads it, so DISPLAY OUT-VAL shows 0100;
      *>   - INNER's GENERATE DET-1 then prints the detail to the report file (no standard output).
      *> The printable item PV, in the same line, is never referenced from the procedure division.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1046C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1046c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3) VALUE "ABC".
       01  N PIC 9(3) VALUE 7.
       REPORT SECTION.
       RD  R-1 GLOBAL PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  PV COLUMN 1 PIC X(3) SOURCE W.
           05  TOT COLUMN 6 PIC 9(4) SUM N.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           CALL "INNER".
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OUT-VAL PIC 9(4).
       PROCEDURE DIVISION.
       SHOW.
           ADD 100 TO TOT.
           MOVE TOT TO OUT-VAL.
           DISPLAY OUT-VAL.
           GENERATE DET-1.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM PB1046C.
