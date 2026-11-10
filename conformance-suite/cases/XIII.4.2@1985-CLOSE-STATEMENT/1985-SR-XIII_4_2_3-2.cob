      *>--- conformance-case
      *> id: 1985-SR-XIII_4_2_3-2
      *> rules: [1985-SR-XIII.4.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.2.3 SR2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWCLOSE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTDATA.
           SELECT RPT-FILE-2 ASSIGN TO RPTDATA2.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE OMITTED
           REPORT IS RPT.
       FD  RPT-FILE-2
           LABEL RECORDS ARE OMITTED
           REPORT IS RPT-2.
       REPORT SECTION.
       RD  RPT.
       01  RPT-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  RPT-2.
       01  RPT-LINE-2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT-FILE-2.
           INITIATE RPT RPT-2.
           GENERATE RPT-LINE.
           GENERATE RPT-LINE-2.
           TERMINATE RPT RPT-2.
           CLOSE RPT-FILE WITH LOCK RPT-FILE-2 WITH NO REWIND.
           DISPLAY "CLOSED".
           STOP RUN.
