      *>--- conformance-case
      *> id: 1985-GR-XIII_4_8_4-1
      *> rules: [1985-GR-XIII.4.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR1 (THE USE AFTER STANDARD EXCEPTION
      *> PROCEDURE STATEMENT). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USETEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWNONE"
               FILE STATUS IS FS.
           SELECT OK-FILE ASSIGN TO "RWOUT"
               FILE STATUS IS FS-OK.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  OK-FILE
           REPORT IS RPT-OK.
       WORKING-STORAGE SECTION.
       01  FS    PIC XX.
       01  FS-OK PIC XX.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  RPT-OK.
       01  DTL-OK TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON RPT-FILE OK-FILE.
       D1P.
           DISPLAY "DECLARATIVE " FS-OK " " FS.
           STOP RUN.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT OK-FILE.
           DISPLAY "OUTPUT OPENED " FS-OK.
           CLOSE OK-FILE.
           DISPLAY "OUTPUT CLOSED " FS-OK.
           OPEN EXTEND RPT-FILE.
           STOP RUN.
