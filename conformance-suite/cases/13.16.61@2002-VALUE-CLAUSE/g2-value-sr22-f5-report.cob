      *>--- conformance-case
      *> id: g2-value-sr22-f5-report
      *> rules: [2002-SR-13.16.61.2-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR22 - "Condition-name and content-validation
      *> formats shall not
      *> be specified in the report section." A format 5 (content-
      *> validation) entry
      *> (88 ... VALUES 1 THRU 5 ARE VALID) under a printable item of a
      *> detail group.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V22A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2v22a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 3.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DT TYPE DETAIL.
           05 LINE 1 COLUMN 1 PIC 9 SOURCE W.
              88 W-OK VALUES 1 THRU 5 ARE VALID.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT
           INITIATE R1
           GENERATE DT
           TERMINATE R1
           CLOSE PRT
           DISPLAY "NOT REFUSED"
           STOP RUN.
