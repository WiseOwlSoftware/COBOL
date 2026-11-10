      *>--- conformance-case
      *> id: 1985-GR-XIII_3_11_4-2
      *> rules: [1985-GR-XIII.3.11.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.11.4 GR2 (THE COLUMN NUMBER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COLTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTFILE.
           SELECT CHK-FILE ASSIGN TO RPTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS R1.
       FD  CHK-FILE.
       01  CHK-REC                 PIC X(20).
       REPORT SECTION.
       RD  R1.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 5  PIC XXX VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE R1.
           GENERATE DL.
           TERMINATE R1.
           CLOSE RPT-FILE.
           OPEN INPUT CHK-FILE.
           READ CHK-FILE AT END DISPLAY "NO RECORD".
           IF CHK-REC (1:4) = SPACES AND CHK-REC (5:3) = "ABC"
               DISPLAY "COLUMN 5 OK"
           ELSE
               DISPLAY "COLUMN WRONG".
           CLOSE CHK-FILE.
           STOP RUN.
