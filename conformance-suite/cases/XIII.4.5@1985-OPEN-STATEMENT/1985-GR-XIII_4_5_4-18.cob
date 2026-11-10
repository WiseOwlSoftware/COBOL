      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-18
      *> rules: [1985-GR-XIII.4.5.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR18 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RSZ01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWOUT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 40 CHARACTERS
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           DISPLAY "CREATED " FS.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN EXTEND RPT-FILE.
           DISPLAY "REOPENED " FS.
           INITIATE RPT.
           GENERATE DTL.
           IF LINE-COUNTER = 1
               DISPLAY "GENERATED".
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "CLOSED " FS.
           STOP RUN.
