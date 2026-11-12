      *>--- conformance-case
      *> id: pb5310-sum-counter-as-occurs-depending-object
      *> rules: [SR-13.8.6.2.3-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB5310 - from ISO/IEC 1989:2002 (13.7.5.1.3; 2023
      *> 13.8.6.2.3) "Report items shall not be accessed or referred to by
      *> any clauses in any other section of the data division", save that
      *> "Sum counters may be inspected or altered in the procedure
      *> division". The working-storage OCCURS DEPENDING ON below names the
      *> sum counter KTOT, so it is refused (COBOL5092) from 2002. At 85
      *> the rule is 1985:XIII.3.9.3 SR10d's alone, which admits a sum
      *> counter: XIII.3.9@1985-REPORT-GROUP-DESCRIPTION-ENTRY/
      *> pb5310_sum_counter_occurs_depending_object.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5310B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "pb5310b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  W-V PIC 9 VALUE 2.
       01  TAB.
           05  TE PIC X OCCURS 1 TO 9 TIMES DEPENDING ON KTOT.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE W-V.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  KTOT COLUMN 1 PIC 9(3) SUM W-V.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
