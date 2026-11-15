      *>--- conformance-case
      *> id: pb5253_sum_counter_of_another_report
      *> rules: [SR-13.18.54.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5253. §2023:13.18.54.3 SR4 g) (from §2002:13.16.52.2
      *> SR3 g)): "If data-name-1 specifies an entry in a different
      *> report description, there are no restrictions on the
      *> combinations of report types of each report group." RPT2's
      *> control footing may therefore SUM the counter ATOT of RPT, and
      *> the program translates and runs; at 85 the same SUM clause is
      *> refused (pb5253_sum_counter_of_another_report_refused_at_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5253A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFIL ASSIGN TO PB5253AR.
       DATA DIVISION.
       FILE SECTION.
       FD  RFIL REPORTS ARE RPT RPT2.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC S9(3)V9 VALUE -5.5.
       REPORT SECTION.
       RD  RPT
           CONTROLS ARE FINAL.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05 COLUMN 1 PIC -999.9 SOURCE WS-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05 ATOT COLUMN 1 PIC -ZZ9.9 SUM WS-AMT.
       RD  RPT2
           CONTROLS ARE FINAL.
       01  DET2 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05 COLUMN 1 PIC -999.9 SOURCE WS-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05 BTOT COLUMN 1 PIC -ZZ9.9 SUM ATOT OF RPT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFIL.
           INITIATE RPT RPT2.
           GENERATE DET.
           GENERATE DET2.
           TERMINATE RPT2.
           TERMINATE RPT.
           CLOSE RFIL.
           DISPLAY "DONE".
           STOP RUN.
