*>--- conformance-case
*> id: pb3517-sign-phrase-on-report-item
*> rules: [FMT-13.18.60.2, GR-13.15.4-2, GR-13.18.60.4-12]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.2 prints { SIGNED | UNSIGNED } only after BINARY-CHAR,
*> BINARY-SHORT, BINARY-LONG and BINARY-DOUBLE; the DISPLAY line prints no
*> phrase. 13.15.4 GR2: the USAGE clause of a report group description entry
*> is "the same clauses as those that are described under the general format
*> for a data description entry", so the entry draws COBOL4733 exactly as
*> the same entry does in WORKING-STORAGE (pb3517-sign-phrase-on-non-binary-usage).
*> Before branch review R1-1 of kb/Work PB3517 the report reader took the usage
*> word alone: this program compiled clean and printed 005, the phrase ignored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3517R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3517r1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 9(3) VALUE 5.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           05  ITEM-1 COLUMN 1 PIC 9(3) SOURCE W USAGE DISPLAY UNSIGNED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
