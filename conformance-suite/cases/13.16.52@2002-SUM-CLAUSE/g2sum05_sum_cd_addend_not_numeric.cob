      *>--- conformance-case
      *> id: g2sum05_sum_cd_addend_not_numeric
      *> rules: [2002-SR-13.16.52.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.2 SR4 - "If the addend is identifier-1, it shall
      *> specify a numeric data item not defined in the report section."
      *> CA is an alphabetic PIC A(3) item of a record under a CD
      *> (§2002:A.1.1.2): not defined in the report section, but not
      *> numeric, so SUM OF CA is refused (COBOL2045) at line 37.  The
      *> accepted numeric CD addend is g2sum04_sum_cd_identifier_expression.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum05.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CN   PIC 9(3).
           05 CA   PIC A(3).
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS CN.
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           03  COLUMN 1  PIC 9(4) SUM OF CA.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
