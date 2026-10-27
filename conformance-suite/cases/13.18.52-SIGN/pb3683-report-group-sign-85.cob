*>--- conformance-case
*> id: pb3683-report-group-sign-85
*> rules: [SR-13.18.52.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3683 - ANSI X3.23-1985 admitted the SIGN clause in a report description entry on an elementary
      *> entry only; ISO/IEC 1989:2002 Annex F.2 item 123 (post-ballot text v200112.txt line 44299): "... the SIGN
      *> clause may be specified at the group level." DA below is a GROUP (it has a subordinate entry) that writes
      *> SIGN LEADING SEPARATE, so at --std 85 it is refused with COBOL0900 (construct report-group-sign-2002, VCR row
      *> 7.40); SEPARATE is written, so only the group half of the rule applies. The 2002 positive is
      *> 13.18.52-SIGN/pb493_report_group_sign_inherited (SEPARATE) and 13.18.52-SIGN/pb3683_report_sign_2002. The 2023
      *> text, 13.18.52.3 SR1 (cite.py: OK 13.18.52.3 1)), admits "an alphanumeric group item, national group item, or
      *> strongly-typed group item".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3683N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3683N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WN      PIC S9(3) VALUE -5.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1 SIGN LEADING SEPARATE.
           02  COLUMN 1 PIC S9(3) SOURCE WN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
