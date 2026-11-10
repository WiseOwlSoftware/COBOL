      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_8-4
      *> rules: [1985-GR-XIII.3.10.8-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.8 GR4 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWFIRST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO PRINTER.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       REPORT SECTION.
       RD  R1
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 5
           LAST DETAIL 18.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           INITIATE R1.
           GENERATE DL.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "LC=" WS-LC.
           GENERATE DL.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "LC=" WS-LC.
           TERMINATE R1.
           CLOSE RFILE.
           STOP RUN.
