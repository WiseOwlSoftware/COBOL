      *>--- conformance-case
      *> id: g2-terminate-operand-not-a-report
      *> rules: [2002-SR-14.8.42.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.42.2 SR1 - "Report-name-1 shall be defined by a
      *> report description entry in the report section."
      *>   cite.py: OK  §2002:14.8.42.2 SR1
      *> A prohibiting syntax rule that grants no form. TERMINATE names
      *> W, a working-storage item and no RD; R-A, a real report, stays
      *> declared and INITIATEd, so the only violation is the marked
      *> line. Fails if TERMINATE W is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TRM02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "G2TRM02.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS R-A.
       WORKING-STORAGE SECTION.
       01  W PIC X.
       REPORT SECTION.
       RD  R-A.
       01  DA TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DA".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RFILE.
           INITIATE R-A.
           TERMINATE W.
           CLOSE RFILE.
           STOP RUN.
