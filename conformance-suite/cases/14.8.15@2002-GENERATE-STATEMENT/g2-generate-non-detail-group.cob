      *>--- conformance-case
      *> id: g2-generate-non-detail-group
      *> rules: [2002-SR-14.8.15.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.15.2 SR1 - "Data-name-1 shall name a detail report
      *>   group." PH-A is a page heading, so GENERATE PH-A is refused.
      *>   The accepted forms (a detail, and a detail qualified by its
      *>   report-name) are g2-generate-first-and-later.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GEN02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G2GEN02.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 4.
       REPORT SECTION.
       RD R1
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 PH-A TYPE PH LINE 1.
          02 COLUMN 1 PIC X(2) VALUE "PH".
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE PH-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
