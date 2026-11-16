      *>--- conformance-case
      *> id: g2-occurs-report-nested-depending
      *> rules: [2002-SR-13.16.36.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR1b - the OCCURS clause (ALL FORMATS) shall
      *> not be specified in an entry that "Has a variable-occurrence
      *> data item subordinate to it" (§2002:4: "A table element that is
      *> repeated a variable number of times"). In the report group
      *> DET-A, the LINE entry has OCCURS 2 TIMES and its subordinate
      *> COLUMN entry has the format 3 OCCURS 1 TO 4 TIMES DEPENDING ON
      *> WS-N, so the LINE entry's clause is refused. Accepting it fails
      *> the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN1R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2occn1r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 2.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1 OCCURS 2 TIMES.
               03  COLUMN 1 PIC X(3) OCCURS 1 TO 4 TIMES
                   DEPENDING ON WS-N STEP 4 VALUE "CCC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
