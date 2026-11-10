      *>--- conformance-case
      *> id: pb5127_detail_use_computes_source_before_rolled_addition
      *> rules: [GR-14.9.49.4-9, GR-13.18.54.4-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5127 review R1-1 - a detail's USE BEFORE REPORTING procedure that COMPUTES the detail's SOURCE
      *> operand runs BEFORE the detail's contribution is added into the control footing's SUM counter.
      *> §2023:14.9.49.4 GR9 b) (cite.py --check): the declarative is invoked "After incrementing sum counters defined
      *> in the report group"; the detail group defines no sum counter, so nothing it owes precedes the declarative.
      *> §2023:13.18.54.4 GR6: for a data-name-1 whose entry has a SOURCE clause "the value added is that of the
      *> operand of the SOURCE clause", read when the detail is processed, i.e. after its declarative computed it.
      *> Derivation: WS-IN 1, 2, 3; the declarative sets WS-X = 10, 20, 30; DE-AMT prints those; TOT = 10+20+30 = 60,
      *> which the USE of the footing reads at TERMINATE.  Adding before the declarative gave 0+10+20 = 30.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5127DETROLL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5127detroll.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-IN   PIC 99 VALUE 0.
       01  WS-X    PIC 999 VALUE 0.
       01  WS-T    PIC 9999.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL.
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  DE-AMT COLUMN 1 PIC 999 SOURCE IS WS-X.
       01  CF-F TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           02  TOT COLUMN 1 PIC 9999 SUM DE-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE BEFORE REPORTING DE-1.
       D1-P.
           COMPUTE WS-X = WS-IN * 10.
           DISPLAY "DETAIL " WS-X.
       D2 SECTION.
           USE BEFORE REPORTING CF-F.
       D2-P.
           MOVE TOT TO WS-T.
           DISPLAY "TOTAL " WS-T.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R1.
           MOVE 1 TO WS-IN GENERATE DE-1.
           MOVE 2 TO WS-IN GENERATE DE-1.
           MOVE 3 TO WS-IN GENERATE DE-1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
