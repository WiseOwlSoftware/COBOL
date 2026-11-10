      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-1
      *> rules: [1985-GR-XIII.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR1 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENRW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWOUT"
               FILE STATUS IS FS.
           SELECT OPTIONAL OPT-FILE ASSIGN TO "RWOPT"
               FILE STATUS IS FS-OPT.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  OPT-FILE
           REPORT IS RPT-OPT.
       WORKING-STORAGE SECTION.
       01  FS     PIC XX.
       01  FS-OPT PIC XX.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  RPT-OPT.
       01  DTL-OPT TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "OUTPUT UNAVAILABLE " FS.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "CLOSE " FS.
           OPEN EXTEND RPT-FILE.
           DISPLAY "EXTEND AVAILABLE " FS.
           CLOSE RPT-FILE.
           DISPLAY "CLOSE " FS.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "OUTPUT AVAILABLE " FS.
           CLOSE RPT-FILE.
           OPEN EXTEND OPT-FILE.
           DISPLAY "OPTIONAL EXTEND UNAVAILABLE " FS-OPT.
           CLOSE OPT-FILE.
           OPEN EXTEND OPT-FILE.
           DISPLAY "OPTIONAL EXTEND AVAILABLE " FS-OPT.
           CLOSE OPT-FILE.
           STOP RUN.
