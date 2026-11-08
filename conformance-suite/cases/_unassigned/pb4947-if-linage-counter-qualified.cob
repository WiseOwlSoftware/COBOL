*>--- conformance-case
*> id: pb4947-if-linage-counter-qualified
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
       PROGRAM-ID. NEG4947B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "n4947b.tmp"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PRT LINAGE IS 66 LINES.
       01 PRT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           IF LINAGE-COUNTER OF PRT
               MOVE 1 TO R
           END-IF
           DISPLAY R
           STOP RUN.
