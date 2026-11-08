*>--- conformance-case
*> id: pb4947-if-page-counter
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4947: a counter register is no condition. 8.4.3.14.4 GR1 / 8.4.3.15.4 GR1 make it "a temporary unsigned
      *> integer data item of class and category numeric", and 8.8.4.2.1 lists the simple conditions as the relation, boolean,
      *> class, condition-name, switch-status, sign and omitted-argument conditions: a numeric item is none of them (the
      *> numeric data item W in IF W is refused likewise). The register used to draw only warning COBOL1756 and abort the run
      *> unit. COBOL2318 at every edition.
      *>   cite.py --check 8.8.4.2.1 "The simple conditions are the relation" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG4947D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "n4947d.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4) VALUE 0.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 20 LINES FIRST DETAIL 1.
       01 D-1 TYPE DETAIL.
          02 LINE 1 COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           IF PAGE-COUNTER OF R-1
               MOVE 1 TO R
           END-IF
           DISPLAY R
           STOP RUN.
