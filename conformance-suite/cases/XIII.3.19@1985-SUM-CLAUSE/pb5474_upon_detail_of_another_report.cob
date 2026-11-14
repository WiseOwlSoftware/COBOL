      *>--- conformance-case
      *> id: pb5474_upon_detail_of_another_report
      *> rules: [1985-SR-XIII.3.19.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5474. §1985:XIII.3.19.3 SR2: "Data-name-1 must be
      *> the name of a detail report group described in the same report
      *> as the control footing report group in which the SUM clause
      *> appears." DET2 is a detail of RPT2 and the footing is RPT's, so
      *> UPON DET2 OF RPT2 is refused at 85 (COBOL2046). COBOL-2002's
      *> report-name qualifier admits it (kb/Work PB1294); the accepted
      *> twin is pb5474_upon_own_report_detail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5474C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5474C.
           SELECT RPT-FILE2 ASSIGN TO PB5474D.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  RPT-FILE2
           REPORT IS RPT2.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(5) SUM WS-AMT UPON DET2 OF RPT2.
       RD  RPT2
           CONTROL IS FINAL.
       01  DET2 TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CF2 TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(5) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT-FILE2.
           INITIATE RPT RPT2.
           GENERATE DET.
           GENERATE DET2.
           TERMINATE RPT RPT2.
           CLOSE RPT-FILE RPT-FILE2.
           STOP RUN.
