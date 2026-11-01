*>--- conformance-case
*> id: pb1652-group-indicate-below-2002
*> rules: [GR-13.18.28.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> THE GROUP INDICATE REPORT-GROUP SUBSET IS A COBOL-2002 FORM (kb/Work PB1652). The golden
      *> pb1652_group_indicate_suppressed_generate accepts from 2002; below it the relative COLUMN clause
      *> and the PRESENT WHEN clause that GR1 equates GROUP INDICATE with are refused (COBOL0900, the
      *> version-matrix introduction gate), so this program, which carries both, is refused at 85.
      *> ISO/IEC 1989:2023 §13.18.28.4 GR1: "The GROUP INDICATE clause has the same effect as a PRESENT
      *> WHEN clause".
      *>   cite.py --check 13.18.28.4 "The GROUP INDICATE clause has the same effect as a PRESENT WHEN
      *>     clause"  -> OK  §13.18.28.4 1)  (General rule)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1652BL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB1652BL.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WN PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R.
       01  DA TYPE DETAIL LINE PLUS 1 PRESENT WHEN WN = 1.
           03 COLUMN 1 PIC X(2) VALUE "GI" GROUP INDICATE.
           03 COLUMN PLUS 2 PIC 9 SOURCE WN.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT-FILE. INITIATE R.
           GENERATE DA.
           TERMINATE R. CLOSE RPT-FILE. STOP RUN.
