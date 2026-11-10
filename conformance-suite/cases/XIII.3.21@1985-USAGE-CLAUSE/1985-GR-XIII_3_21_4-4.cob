      *>--- conformance-case
      *> id: 1985-GR-XIII_3_21_4-4
      *> rules: [1985-GR-XIII.3.21.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.21.4 GR4 (THE USAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUSAGE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWUSAGE".
           SELECT IN-FILE ASSIGN TO "RWUSAGE".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE.
       01  IN-REC PIC X(7).
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC 9(3) VALUE 42.
       REPORT SECTION.
       RD  RPT
           PAGE LIMIT IS 10 LINES.
       01  DETL TYPE IS DETAIL LINE NUMBER IS 1.
           05 COLUMN 1 PIC X(4) VALUE "NUM=".
           05 COLUMN 5 PIC 999 SOURCE WS-NUM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE AT END DISPLAY "NO LINE".
           DISPLAY IN-REC.
           CLOSE IN-FILE.
           STOP RUN.
