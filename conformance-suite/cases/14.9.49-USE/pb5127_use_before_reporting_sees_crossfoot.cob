      *>--- conformance-case
      *> id: pb5127_use_before_reporting_sees_crossfoot
      *> rules: [GR-14.9.49.4-9, GR-13.18.54.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5127 - a control footing's USE BEFORE REPORTING procedure runs AFTER the footing's sum counters
      *> are crossfooted and rolled forward.
      *>
      *> §1985:XIII.3.20.4 GR10 (cite.py --check): a. "Sum counters are crossfooted, i.e., all sum counters defined in
      *> this report group that are operands of SUM clauses in the same report group are added to their sum
      *> counters." b. "Sum counters are rolled forward, i.e., all sum counters defined in the report group that are
      *> operands of SUM clauses in higher level control footing report groups are added to the higher level sum
      *> counters." c. "If there is a USE BEFORE REPORTING procedure that references the data-name of the report
      *> group the USE procedure is executed."  d./e. SUPPRESS skips the printing; f. the counters are reset.
      *> §2023:14.9.49.4 GR9 b): the declarative is invoked "After incrementing sum counters defined in the report
      *> group".
      *>
      *> Derivation. CF-K (control WS-KEY) sums WS-AMT1 into SUB-A and WS-AMT2 into SUB-B and crossfoots both into
      *> CROSS; CF-F (FINAL) sums CROSS into GRAND, which is therefore rolled forward when CF-K is processed.
      *> Details: (key 1, 1, 10), (key 1, 2, 20), (key 2, 4, 40).  The second detail breaks nothing; the third
      *> breaks key 1.  The break processes CF-K for key 1 with SUB-A=3, SUB-B=30; step a gives CROSS=33, step b adds
      *> CROSS into GRAND, 33; only then does USE CF-K run, so it reads CROSS=0033 and GRAND=0033 (the bug read 0
      *> and 0).  Printing resets SUB-A, SUB-B and CROSS (f).  TERMINATE processes CF-K for key 2 (SUB-A=4, SUB-B=40,
      *> CROSS=44, GRAND=33+44=77) and then CF-F; the USE CF-K reads CROSS=0044 GRAND=0077.  The USE counts its own
      *> executions in WS-FTG and SUPPRESSes the second footing: SUPPRESS inhibits the printing only, so the counters
      *> still reset (f) and GRAND still rolled: USE CF-F reads GRAND=0077 and the last STATEMENT line, after the
      *> final resets, reads zero everywhere (LINE=05: the suppressed footing printed no line, the FINAL footing one).
      *> Each USE line can fail: a USE run before step a or b reads the previous totals.
      *> Accepted at COBOL-85 only: a SUM whose addend is an entry of its own report group (the crossfoot of
      *> step a) is §2023:13.18.54.3 SR4 b) 's repeating-item case from 2002 (COBOL2730 otherwise); the order
      *> itself is every edition's (§2023:14.9.49.4 GR9 b)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5127CFSEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwcfseq.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9  VALUE 1.
       01  WS-AMT1 PIC 99 VALUE 0.
       01  WS-AMT2 PIC 99 VALUE 0.
       01  WS-FTG  PIC 9  VALUE 0.
       01  WS-D1   PIC 999.
       01  WS-D2   PIC 999.
       01  WS-D3   PIC 9999.
       01  WS-D4   PIC 9999.
       01  WS-LC   PIC 99.
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
           USE BEFORE REPORTING CF-K.
       D1-P.
           MOVE SUB-A TO WS-D1.
           MOVE SUB-B TO WS-D2.
           MOVE CROSS TO WS-D3.
           MOVE GRAND TO WS-D4.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "USE CF-K  A=" WS-D1 " B=" WS-D2 " CROSS=" WS-D3
               " GRAND=" WS-D4 " LINE=" WS-LC.
           ADD 1 TO WS-FTG.
           IF WS-FTG = 2
               SUPPRESS PRINTING.
       D2 SECTION.
           USE BEFORE REPORTING CF-F.
       D2-P.
           MOVE SUB-A TO WS-D1.
           MOVE SUB-B TO WS-D2.
           MOVE CROSS TO WS-D3.
           MOVE GRAND TO WS-D4.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "USE CF-F  A=" WS-D1 " B=" WS-D2 " CROSS=" WS-D3
               " GRAND=" WS-D4 " LINE=" WS-LC.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R1.
           MOVE 1 TO WS-KEY MOVE 1 TO WS-AMT1 MOVE 10 TO WS-AMT2.
           GENERATE DE-1.
           PERFORM SHOW-P.
           MOVE 1 TO WS-KEY MOVE 2 TO WS-AMT1 MOVE 20 TO WS-AMT2.
           GENERATE DE-1.
           PERFORM SHOW-P.
           MOVE 2 TO WS-KEY MOVE 4 TO WS-AMT1 MOVE 40 TO WS-AMT2.
           GENERATE DE-1.
           PERFORM SHOW-P.
           TERMINATE R1.
           PERFORM SHOW-P.
           CLOSE RPT.
           STOP RUN.
       SHOW-P.
           MOVE SUB-A TO WS-D1.
           MOVE SUB-B TO WS-D2.
           MOVE CROSS TO WS-D3.
           MOVE GRAND TO WS-D4.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "STATEMENT A=" WS-D1 " B=" WS-D2 " CROSS=" WS-D3
               " GRAND=" WS-D4 " LINE=" WS-LC.
