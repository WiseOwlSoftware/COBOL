      *>--- conformance-case
      *> id: pb5993-report-sign-usage-cites-1985-85
      *> rules: [1985-SR-XIII.3.17.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:XIII.3.17.3 SR2 (cite.py:
      *> OK): "The numeric data description entries to which the SIGN
      *> clause applies must be described, implicitly or explicitly, as
      *> USAGE IS DISPLAY." The report entry writes SIGN LEADING SEPARATE
      *> CHARACTER on a COMP item, so the SIGN clause is refused
      *> (COBOL2422). The refusal cites the Report Writer module's rule,
      *> §1985:XIII.3.17.3, not the Nucleus data description entry's
      *> (§1985:VI.5.12.3) and not §2023:13.18.52.3, and says "display"
      *> alone (NATIONAL is COBOL-2002). A compiler that accepts the
      *> clause, or a refusal citing another clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "p5993s.lst".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 W PIC 9(3).
       REPORT SECTION.
       RD R1 PAGE LIMIT 60 LINES.
       01 TYPE DETAIL LINE 1.
          05 COLUMN 1 PIC S9(3) SIGN LEADING SEPARATE CHARACTER
                USAGE COMP SOURCE W.
       PROCEDURE DIVISION.
       P-1.
           STOP RUN.
