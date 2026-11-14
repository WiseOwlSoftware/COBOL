      *>--- conformance-case
      *> id: pb5474_upon_own_report_detail
      *> rules: [1985-SR-XIII.3.19.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5474. §1985:XIII.3.19.3 SR2: "Data-name-1 must be
      *> the name of a detail report group described in the same report
      *> as the control footing report group in which the SUM clause
      *> appears." RPT's footing sums WS-AMT UPON DET, a detail of RPT,
      *> so it totals the two GENERATEs of DET (5 + 5 = 10) and not the
      *> GENERATE of RPT2's DET2. Each sum counter is moved to a
      *> PIC 9(5) item between the GENERATEs and TERMINATE: TOT holds
      *> 10 (two GENERATEs of DET) and TOT2 holds 5 (one of DET2). The
      *> negative twin names a detail of another report
      *> (wiseowl/diagnostics/pb5474_upon_detail_of_another_report).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5474A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5474A.
           SELECT RPT-FILE2 ASSIGN TO PB5474B.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 5 CHARACTERS
           REPORT IS RPT.
       FD  RPT-FILE2
           RECORD CONTAINS 5 CHARACTERS
           REPORT IS RPT2.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       01  WS-OUT PIC 9(5).
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL.
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  TOT COLUMN 1 PIC 9(5) SUM WS-AMT UPON DET.
       RD  RPT2
           CONTROL IS FINAL.
       01  DET2 TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       01  CF2 TYPE IS CONTROL FOOTING FINAL.
           05  LINE PLUS 1.
               10  TOT2 COLUMN 1 PIC 9(5) SUM WS-AMT UPON DET2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT-FILE2.
           INITIATE RPT RPT2.
           GENERATE DET.
           GENERATE DET.
           GENERATE DET2.
           MOVE TOT TO WS-OUT.
           DISPLAY WS-OUT.
           MOVE TOT2 TO WS-OUT.
           DISPLAY WS-OUT.
           TERMINATE RPT RPT2.
           CLOSE RPT-FILE RPT-FILE2.
           STOP RUN.
