      *>--- conformance-case
      *> id: pb5993-report-usage-comp-cites-1985-85
      *> rules: [1985-SR-XIII.3.21.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:XIII.3.21.3 SR3 (cite.py:
      *> OK): "The USAGE clause for a report group item can specify only
      *> USAGE IS DISPLAY." The detail entry writes USAGE COMP, so it
      *> is refused (COBOL2198). The refusal says that only DISPLAY may
      *> be written, never "DISPLAY or NATIONAL" (NATIONAL is COBOL-2002),
      *> and cites the 1985 clause, never §2023:13.18.60.3 SR7. A
      *> compiler that accepts COMP, or a refusal naming NATIONAL or
      *> another edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "p5993r.lst".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 W PIC 9(3).
       REPORT SECTION.
       RD R1 PAGE LIMIT 60 LINES.
       01 TYPE DETAIL LINE 1.
          05 COLUMN 1 PIC 9(3) USAGE COMP SOURCE W.
       PROCEDURE DIVISION.
       P-1.
           STOP RUN.
