*>--- conformance-case
*> id: pb4889-report-unnamed-national-at-85
*> rules: [FMT-13.15.2, SR-13.18.60.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE NEGATIVE BELOW THE INTRODUCING EDITION (kb/Work PB4889). The positive is
*> conformance:cases/13.15-Report-group-description/pb4889_report_unnamed_national_item: from 2002 §2023:8.9 reserves
*> NATIONAL, so `03 NATIONAL COLUMN 1 PIC N(3) VALUE N"ABC".` is an unnamed USAGE NATIONAL item (§2023:13.15.2 prints
*> both the entry-name clause and USAGE IS as optional), and this program compiles clean at 2002, 2014 and 2023.
*> COBOL-85 reserves no NATIONAL and has no national data (a COBOL-2002 introduction, §2023:8.5.2.10 National
*> category), so there the word is the entry's name and the item's PIC N and national literal are refused by the
*> national-data edition gate (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4889N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb4889n85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           03  NATIONAL COLUMN 1 PIC N(3) VALUE N"ABC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
