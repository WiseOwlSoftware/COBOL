      *>--- conformance-case
      *> id: 1985-GR-XIII_3_19_4-9
      *> rules: [1985-GR-XIII.3.19.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR9 (THE SUM CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CROSSFT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "crossft.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9  VALUE 1.
       01  WS-AMT1 PIC 99 VALUE 0.
       01  WS-AMT2 PIC 99 VALUE 0.
       01  WS-D1   PIC 999.
       01  WS-D2   PIC 999.
       01  WS-D3   PIC 9999.
       01  WS-D4   PIC 9999.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL WS-KEY.
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC 99 SOURCE IS WS-AMT1.
           02  COLUMN NUMBER IS 4 PIC 99 SOURCE IS WS-AMT2.
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           02  SUB-A COLUMN NUMBER IS 1 PIC 999 SUM WS-AMT1.
           02  SUB-B COLUMN NUMBER IS 5 PIC 999 SUM WS-AMT2.
           02  CROSS COLUMN NUMBER IS 9 PIC 9999 SUM SUB-A SUB-B.
       01  CF-F TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           02  GRAND COLUMN NUMBER IS 1 PIC 9999 SUM CROSS.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE BEFORE REPORTING CF-F.
       D1-P.
           MOVE GRAND TO WS-D4.
           DISPLAY "FINAL FOOTING GRAND=" WS-D4.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R1.
           MOVE 1 TO WS-KEY MOVE 1 TO WS-AMT1 MOVE 10 TO WS-AMT2.
           PERFORM GEN-P.
           MOVE 1 TO WS-KEY MOVE 2 TO WS-AMT1 MOVE 20 TO WS-AMT2.
           PERFORM GEN-P.
           MOVE 2 TO WS-KEY MOVE 4 TO WS-AMT1 MOVE 40 TO WS-AMT2.
           PERFORM GEN-P.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
       GEN-P.
           GENERATE DE-1.
           MOVE SUB-A TO WS-D1.
           MOVE SUB-B TO WS-D2.
           MOVE CROSS TO WS-D3.
           MOVE GRAND TO WS-D4.
           DISPLAY "GENERATE A=" WS-D1 " B=" WS-D2 " CROSS=" WS-D3
               " GRAND=" WS-D4.
