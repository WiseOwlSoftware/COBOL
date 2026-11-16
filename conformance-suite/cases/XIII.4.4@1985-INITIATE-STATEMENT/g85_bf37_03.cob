      *>--- conformance-case
      *> id: g85_bf37_03
      *> rules: [1985-GR-XIII.4.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.4.4 GR4 - one INITIATE naming three reports acts
      * as three INITIATEs, so GR1 b and c apply to EACH report: every
      * LINE-COUNTER becomes zero and every PAGE-COUNTER one.
      * Before the multi-report INITIATE each report has presented a
      * line (LINE-COUNTER 1, no PAGE clause, §1985:XIII.3.10.8 rule
      * 4d) and its PAGE-COUNTER was set by MOVE to 5, 7 and 9
      * (§1985:XIII.3.5.5 SR6), so each counter must change:
      *   BEFORE A=1/5 B=1/7 C=1/9
      *   AFTER  A=0/1 B=0/1 C=0/1   (INITIATE R-C R-A R-B)
      *   GEN    A=1 B=1 C=1         each report runs again
      * A report the INITIATE skipped (only the first or the last
      * name honoured) keeps its 1/n pair and fails the AFTER line.
      * Derivation: §1985:XIII.4.4.4 GR1b and GR1c, §1985:XIII.3.10.8
      * GR4d and GR6d. The ORDER of the three implied INITIATEs has
      * no observable effect a conforming program can see, so only
      * the per-report outcome is pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3703.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "G85BF37A.RPT".
           SELECT RPTB ASSIGN TO "G85BF37B.RPT".
           SELECT RPTC ASSIGN TO "G85BF37C.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPTA
           REPORT IS R-A.
       FD  RPTB
           REPORT IS R-B.
       FD  RPTC
           REPORT IS R-C.
       WORKING-STORAGE SECTION.
       01  WS-LA PIC 9.
       01  WS-LB PIC 9.
       01  WS-LC PIC 9.
       01  WS-PA PIC 9.
       01  WS-PB PIC 9.
       01  WS-PC PIC 9.
       REPORT SECTION.
       RD  R-A.
       01  DA TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DA".
       RD  R-B.
       01  DB TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DB".
       RD  R-C.
       01  DC TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DC".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPTA RPTB RPTC.
           INITIATE R-A.
           INITIATE R-B.
           INITIATE R-C.
           GENERATE DA.
           GENERATE DB.
           GENERATE DC.
           MOVE 5 TO PAGE-COUNTER OF R-A.
           MOVE 7 TO PAGE-COUNTER OF R-B.
           MOVE 9 TO PAGE-COUNTER OF R-C.
           PERFORM TAKE-COUNTERS.
           DISPLAY "BEFORE A=" WS-LA "/" WS-PA " B=" WS-LB "/" WS-PB
               " C=" WS-LC "/" WS-PC.
           TERMINATE R-A.
           TERMINATE R-B.
           TERMINATE R-C.
           INITIATE R-C R-A R-B.
           PERFORM TAKE-COUNTERS.
           DISPLAY "AFTER  A=" WS-LA "/" WS-PA " B=" WS-LB "/" WS-PB
               " C=" WS-LC "/" WS-PC.
           GENERATE DA.
           GENERATE DB.
           GENERATE DC.
           PERFORM TAKE-COUNTERS.
           DISPLAY "GEN    A=" WS-LA " B=" WS-LB " C=" WS-LC.
           TERMINATE R-A.
           TERMINATE R-B.
           TERMINATE R-C.
           CLOSE RPTA RPTB RPTC.
           STOP RUN.
       TAKE-COUNTERS.
           MOVE LINE-COUNTER OF R-A TO WS-LA.
           MOVE LINE-COUNTER OF R-B TO WS-LB.
           MOVE LINE-COUNTER OF R-C TO WS-LC.
           MOVE PAGE-COUNTER OF R-A TO WS-PA.
           MOVE PAGE-COUNTER OF R-B TO WS-PB.
           MOVE PAGE-COUNTER OF R-C TO WS-PC.
