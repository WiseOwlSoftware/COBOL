      *>--- conformance-case
      *> id: 1985-GR-XIII_3_6_4-1
      *> rules: [1985-GR-XIII.3.6.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.6.4 GR1 (THE CODE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CODETST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTDATA.
           SELECT IN-FILE ASSIGN TO RPTDATA.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 22 CHARACTERS
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 22 CHARACTERS.
       01  IN-REC.
           05  IN-CODE PIC XX.
           05  IN-REST PIC X(20).
       REPORT SECTION.
       RD  RPT
           CODE "AB".
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE
               AT END DISPLAY "NO RECORD".
           DISPLAY IN-CODE.
           CLOSE IN-FILE.
           STOP RUN.
