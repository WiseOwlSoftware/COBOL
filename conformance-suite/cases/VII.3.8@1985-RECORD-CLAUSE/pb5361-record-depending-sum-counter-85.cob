      *>--- conformance-case
      *> id: pb5361-record-depending-sum-counter-85
      *> rules: [1985-SR-VII.3.8.3-4]
      *> source-format: fixed
      *>---
      *> kb/Work PB5361. §1985:VII.3.8.3 SR4 (cite.py: OK): "Data-name-1
      *> must describe an elementary unsigned integer in the
      *> Working-Storage or Linkage Section." At COBOL-85 a sum counter
      *> is a data-name wherever it is written (§1985:XIII.3.9.3 SR10d),
      *> so RECORD VARYING ... DEPENDING ON KTOT names the counter, a
      *> signed item of the report section: SR4 is the rule it breaks.
      *> The refusal must name that 1985 rule, never the 2023 section
      *> rule (§2023:13.8.6.2.3) COBOL-85 does not have. A refusal at
      *> 85: a compiler that accepts it fails, and one that cites a 2023
      *> clause fails the expect text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5361B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "pb5361b.txt".
           SELECT PFILE ASSIGN TO "pb5361bp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS RPT.
       FD  PFILE RECORD VARYING FROM 1 TO 10 DEPENDING ON KTOT.
       01  PREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-V PIC 9 VALUE 2.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE W-V.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  KTOT COLUMN 1 PIC 9(3) SUM W-V.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
