*>--- conformance-case
*> id: pb3683-report-sign-without-separate-85
*> rules: [GR-13.18.52.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3683 - ANSI X3.23-1985 required the SEPARATE phrase in a report description entry's SIGN clause;
      *> ISO/IEC 1989:2002 Annex F.2 item 123 (post-ballot text v200112.txt line 44299): "SIGN clause in a report
      *> description entry. The SEPARATE phrase is no longer required in a report description entry and the SIGN clause
      *> may be specified at the group level." The elementary entry below writes SIGN LEADING without SEPARATE, so at
      *> --std 85 it is refused with COBOL0900 (construct report-sign-without-separate-2002, VCR row 7.40); the 2002
      *> positive is 13.18.52-SIGN/pb3683_report_sign_2002. The 2023 text (13.18.52.4 GR5, cite.py: OK 13.18.52.4 5))
      *> gives the clause without SEPARATE an implementor-defined representation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3683N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3683N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN      PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC S9(3) SOURCE WN SIGN LEADING.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
