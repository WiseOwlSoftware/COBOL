      *>--- conformance-case
      *> id: pb5652-report-sum-unprefixed-sr9
      *> rules: [SR-13.18.54.3-9]
      *> source-format: fixed
      *>---
      *> kb/Work PB5652 - §2023:13.18.54.3 SR9: "Within a report description
      *> entry, if the keyword SUM is preceded by the keyword FUNCTION, SUM
      *> is a reference to the SUM intrinsic function. Otherwise, SUM refers
      *> to the report writer SUM clause" - even with REPOSITORY FUNCTION SUM
      *> INTRINSIC.  So from 2014 (§2014:13.18.54.2 SR9) the bare SUM after
      *> the '+' is the SUM clause written where the SOURCE operand still owes
      *> a term, and the entry is refused (COBOL7933).  COBOL 2002 has no SR9:
      *> there the same source is the function (g2sum07, §2002:12.2.7.3 GR13).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5652SUMSR9.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SUM INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5652sumsr9.txt".
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
