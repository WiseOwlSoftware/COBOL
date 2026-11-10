      *>--- conformance-case
      *> id: pb5078-page-limit-four-digits
      *> rules: [SR-13.18.39.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.8.3 SR2: "Integer-1 must not exceed three significant digits in length." (cite.py: OK).
      *> PAGE LIMIT IS 1000 has four. COBOL-2002 and later bound the limit at 9999 (2023:13.18.39.3 SR5), so it compiles there.
      *> conformance:pb5078-page-limit-four-digits-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078BN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078BN.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 1000 LINES.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
