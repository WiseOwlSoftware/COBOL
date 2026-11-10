      *>--- conformance-case
      *> id: 1985-GR-XIII_4_5_4-11
      *> rules: [1985-GR-XIII.4.5.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR11 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTLBL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RWOUT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN EXTEND RPT-FILE.
           DISPLAY "OPEN EXTEND " FS.
           INITIATE RPT.
           GENERATE DTL.
           IF LINE-COUNTER = 1
               DISPLAY "GENERATED AFTER EXTEND".
           TERMINATE RPT.
           CLOSE RPT-FILE.
           DISPLAY "CLOSE " FS.
           STOP RUN.
