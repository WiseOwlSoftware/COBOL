      *>--- conformance-case
      *> id: 1985-SR-XIII_3_22_3-2
      *> rules: [1985-SR-XIII.3.22.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.22.3 SR2 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1570 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWVAL85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REPORT-FILE ASSIGN TO "RWVAL85.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  REPORT-FILE
           REPORT IS VAL-REPORT.
       REPORT SECTION.
       RD  VAL-REPORT.
       01  DETAIL-LINE TYPE IS DETAIL.
           02  LINE NUMBER IS PLUS 1.
               03  COLUMN NUMBER IS 1 PIC ZZ9 VALUE " 12".
               03  COLUMN NUMBER IS 5 PIC ZZ9 VALUE "1234".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REPORT-FILE.
           INITIATE VAL-REPORT.
           GENERATE DETAIL-LINE.
           TERMINATE VAL-REPORT.
           CLOSE REPORT-FILE.
           DISPLAY "DONE".
           STOP RUN.
