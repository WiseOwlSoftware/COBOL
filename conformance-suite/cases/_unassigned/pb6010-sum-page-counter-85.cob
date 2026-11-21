      *>--- conformance-case
      *> id: pb6010-sum-page-counter-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6010 - §1985:XIII.3.19.3 SR1 "If identifier-1 is defined in the Report
      *> Section, identifier-1 must reference a sum counter." PAGE-COUNTER is none; the refusal
      *> cites that rule at 85 (it cited §2023:13.18.54.3 SR5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6010SM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB6010SM".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC X VALUE "A".
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL
           PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15 FOOTING 18.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X SOURCE W.
       01  CF-1 TYPE CF FINAL LINE PLUS 1.
           02  SC COLUMN 1 PIC 99 SUM PAGE-COUNTER.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
