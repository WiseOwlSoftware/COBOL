      *>--- conformance-case
      *> id: pb5474_upon_shared_detail_name_ambiguous
      *> rules: [1985-SR-XIII.3.19.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5474, review R1-1. DET is a detail of both RPT and
      *> RPT2, so the unqualified UPON DET is a nonunique name:
      *> "uniqueness must be established through a sequence of
      *> qualifiers which precludes any ambiguity of reference"
      *> (§1985:IV.4.3.8.1 SRL2.1). §1985:XIII.3.19.3 SR2 restricts what
      *> the name may denote, it does not make it unique: refused at
      *> every edition (COBOL1920).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5474E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5474E.
           SELECT RPT-FILE2 ASSIGN TO PB5474F.
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
       01  DET TYPE IS DETAIL.
           05  LINE PLUS 1.
               10  COLUMN 1 PIC 9(3) SOURCE WS-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE RPT-FILE2.
           INITIATE RPT RPT2.
           GENERATE DET OF RPT.
           TERMINATE RPT RPT2.
           CLOSE RPT-FILE RPT-FILE2.
           DISPLAY "RAN".
           STOP RUN.
