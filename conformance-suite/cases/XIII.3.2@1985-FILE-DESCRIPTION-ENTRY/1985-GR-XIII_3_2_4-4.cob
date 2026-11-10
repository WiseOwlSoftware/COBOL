      *>--- conformance-case
      *> id: 1985-GR-XIII_3_2_4-4
      *> rules: [1985-GR-XIII.3.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.2.4 GR4 (THE FILE DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWFD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT1.
       REPORT SECTION.
       RD  RPT1.
       01  DETL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT1.
           GENERATE DETL.
           TERMINATE RPT1.
           CLOSE RPT-FILE.
           DISPLAY "REPORT DONE".
           STOP RUN.
