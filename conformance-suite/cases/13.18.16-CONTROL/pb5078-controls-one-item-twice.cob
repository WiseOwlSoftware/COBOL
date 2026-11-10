      *>--- conformance-case
      *> id: pb5078-controls-one-item-twice
      *> rules: [SR-13.18.16.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5078 - 1985:XIII.3.7.3 SR2: "Each recurrence of data-name-1 must identify a different data item." (cite.py: OK).
      *> WS-A and WS-A OF WS-G are two spellings of ONE item, so the COBOL-85 CONTROL clause names it twice. COBOL-2002 and later ask
      *> the written reference instead (2023:13.18.16.3 SR6: the two operands may refer to the same item), so this compiles there.
      *> conformance:pb5078-controls-one-item-twice-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5078AN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5078AN.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-A PIC X.
       REPORT SECTION.
       RD  RPT CONTROLS ARE WS-A WS-A OF WS-G.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X SOURCE WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
