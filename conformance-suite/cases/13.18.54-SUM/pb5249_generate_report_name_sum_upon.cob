      *>--- conformance-case
      *> id: pb5249_generate_report_name_sum_upon
      *> rules: [GR-14.9.16.4-2, GR-13.18.54.4-7]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5249 - GENERATE report-name and a SUM ... UPON detail-name subtotal. §1985:XIII.3.20.4 GR11 (cite.py: OK): "When the description
      *> of a report includes exactly one detail report group, the detail-related processing that the report writer control system executes in
      *> response to a GENERATE report-name statement is described in paragraphs 11a through 11e ... These steps are performed as though a
      *> GENERATE data-name statement were being executed", and step 11a is "any subtotalling that has been designated for the detail report
      *> group". §2023:14.9.16.4 GR2 (cite.py: OK): GENERATE report-name-1 "results in the same processing as for a GENERATE data-name statement
      *> for the same report, except that no detail is printed". The report has ONE detail, DE-1, so the statement is its GENERATE without
      *> the printing; the 2023 text names no detail for a report with several, and this report has not.
      *> DERIVATION. S-UP is SUM WS-AMT UPON DE-1 and S-ALL is SUM WS-AMT WS-AMT (no UPON: every GENERATE adds, GR7 c) 1). WS-AMT 1.5, GENERATE R1:
      *> S-UP 1.5 (the subtotal for the one detail), S-ALL 3.0. WS-AMT -0.5, GENERATE DE-1: S-UP 1.0, S-ALL 2.0 (3.0 + 2 x -0.5).
      *> Each leg can fail: a GENERATE report-name that skips the UPON subtotal leaves S-UP 0.0 and -0.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5249U.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5249U.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9  VALUE 1.
       01  WS-AMT  PIC S9V9 VALUE 0.
       01  WS-D    PIC -999.9.
       01  WS-E    PIC -999.9.
       REPORT SECTION.
       RD  R1 CONTROLS ARE WS-KEY.
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC -9.9 SOURCE IS WS-AMT.
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           02  S-UP  COLUMN 1  PIC -999.9 SUM WS-AMT UPON DE-1.
           02  S-ALL COLUMN 10 PIC -999.9 SUM WS-AMT WS-AMT.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           MOVE 1.5 TO WS-AMT.
           GENERATE R1.
           MOVE S-UP TO WS-D.
           MOVE S-ALL TO WS-E.
           DISPLAY "AFTER GEN R1 UP=" WS-D " ALL=" WS-E.
           MOVE -0.5 TO WS-AMT.
           GENERATE DE-1.
           MOVE S-UP TO WS-D.
           MOVE S-ALL TO WS-E.
           DISPLAY "AFTER GEN DE1 UP=" WS-D " ALL=" WS-E.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
