      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_8-1
      *> rules: [1985-GR-XIII.3.10.8-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.8 GR1 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWUPPER.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwupper.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9  VALUE 0.
       01  WS-PC   PIC 9.
       01  WS-LC   PIC 99.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL
           PAGE LIMIT IS 10 LINES HEADING 2
           FIRST DETAIL 6 LAST DETAIL 8.
       01  CH-F TYPE IS CONTROL HEADING FINAL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(4) VALUE "HEAD".
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(6) VALUE "DETAIL".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "INITIATE   LINE " WS-LC.
           PERFORM GEN-P 4 TIMES.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
       GEN-P.
           ADD 1 TO WS-N.
           GENERATE DE-1.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "GENERATE " WS-N " PAGE " WS-PC " LINE " WS-LC.
