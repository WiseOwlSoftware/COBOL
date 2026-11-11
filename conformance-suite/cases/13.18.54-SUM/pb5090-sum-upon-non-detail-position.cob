      *>--- conformance-case
      *> id: pb5090-sum-upon-non-detail-position
      *> rules: [SR-13.18.54.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5090 - 1985:XIII.3.19.3 SR2: "Data-name-1 must be the name of a detail report group described in the same report as the
      *> control footing report group in which the SUM clause appears." (cite.py: OK; 2023:13.18.54.3 SR7 says the same). CFG is a control
      *> footing, not a detail. The refusal stands at the UPON operand that names it, never at the RD entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5090C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5090C.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC 999 SOURCE WS-AMT.
       01  CFG TYPE IS CONTROL FOOTING FINAL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC 9(5) SUM WS-AMT UPON CFG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
