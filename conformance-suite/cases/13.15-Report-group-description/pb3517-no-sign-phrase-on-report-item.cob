*>--- conformance-case
*> id: pb3517-no-sign-phrase-on-report-item
*> rules: [FMT-13.18.60.2, GR-13.15.4-2, GR-13.18.60.4-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.2 prints [ WITH NO SIGN ] only on the PACKED-DECIMAL line;
*> the DISPLAY line prints no phrase. 13.15.4 GR2 makes a report group
*> description entry's USAGE clause the data description entry's clause, so
*> the entry draws COBOL1565 exactly as it does in WORKING-STORAGE
*> (pb3517-no-sign-phrase-on-binary-usage). Before branch review R1-1 of
*> kb/Work PB3517 this program compiled clean and printed 005.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3517R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3517r2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  ITEM-1 COLUMN 1 PIC 9(3) SOURCE W USAGE DISPLAY WITH NO SIGN.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
