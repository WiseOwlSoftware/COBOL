      *>--- conformance-case
      *> id: g2sum08_sum_unprefixed_undeclared
      *> rules: [2002-GR-12.2.7.3-13]
      *> source-format: fixed
      *>---
      *> kb/Work PB5652 - the leg below g2sum07: the same unprefixed SUM
      *> intrinsic in a SOURCE operand with NO REPOSITORY declaration.
      *> §2002:12.2.7.3 GR13 lets intrinsic-function-name-1 omit the word
      *> FUNCTION only "Within the scope of the containing environment
      *> division" whose REPOSITORY names it; COBOL-85 has no REPOSITORY and
      *> prints FUNCTION in every function-identifier (§1985:IV.4.3.8.3.2).
      *> So the entry is refused at 85 and 2002 (COBOL1543); from 2014 SR9
      *> refuses it first (COBOL7933, pb5652-report-sum-unprefixed-sr9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum08.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA      PIC 9(3)  VALUE 1.
       01  WB      PIC 9(3)  VALUE 10.
       01  WC      PIC 9(3)  VALUE 100.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(4) SOURCE WA + SUM(WB WC).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
