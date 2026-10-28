*>--- conformance-case
*> id: pb3964-report-source-rounded-85
*> rules: [GR-13.18.53.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3964 - ANSI X3.23-1985's SOURCE clause has no rounded phrase. ISO/IEC 1989:2002 Annex F.2 item 115
      *> (post-ballot text v200112.txt line 44272): "SOURCE allows arithmetic expressions and functions and a ROUNDED
      *> phrase". SOURCE WN ROUNDED below writes the phrase over a bare identifier, so at --std 85 it is refused with
      *> COBOL0900 (construct report-source-rounded-2002, VCR row 7.45) and ONLY for the phrase: the operand is the
      *> identifier-1 the 1985 format has. The 2023 text, 13.18.53.2, ends the format with "[ rounded-phrase ]" and
      *> 13.18.53.4 GR2 gives the implicit COMPUTE its ROUNDED phrase; the 2002 positive is
      *> 13.18.53-SOURCE/pb852_report_source_expression.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3964N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3964N4.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN      PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC S9(3) SOURCE WN ROUNDED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
