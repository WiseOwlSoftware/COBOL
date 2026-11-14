      *>--- conformance-case
      *> id: pb5253_sum_counter_of_another_report_refused_at_85
      *> rules: [1985-SR-XIII.3.19.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5253. §1985:XIII.3.19.3 SR1: with no UPON phrase a
      *> sum counter named as an addend "must be defined either in the
      *> same report group that contains this SUM clause or in a report
      *> group which is at a lower level in the control hierarchy of
      *> this report". ATOT is a counter of RPT, named by RPT2's SUM
      *> clause, so it is in neither, and the program is refused at 85
      *> (COBOL2731, line 39). COBOL-2002 removed the restriction for a
      *> different report description (§2002:13.16.52.2 SR3 g)); its
      *> acceptance is cases/13.18.54-SUM/pb5253_sum_counter_of_another_report.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5253B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFIL ASSIGN TO PB5253BR.
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
