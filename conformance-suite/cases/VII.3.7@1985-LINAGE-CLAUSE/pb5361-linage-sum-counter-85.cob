      *>--- conformance-case
      *> id: pb5361-linage-sum-counter-85
      *> rules: [1985-SR-VII.3.7.3-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB5361. §1985:VII.3.7.3 SR1 (cite.py: OK): "Data-name-1,
      *> data-name-2, data-name-3, data-name-4 must reference elementary
      *> unsigned numeric integer data items." At COBOL-85 a sum counter
      *> is a data-name wherever it is written (§1985:XIII.3.9.3 SR10d:
      *> "Data-name-1 may be referenced only if the entry defines a sum
      *> counter" limits WHICH entry, not WHERE), and the counter KTOT
      *> is "A signed numeric data item" (the 1985 glossary, Sum
      *> Counter), so LINAGE IS KTOT breaks SR1. The
      *> refusal must name the 1985 rule, never the 2023 section rule
      *> (§2023:13.8.6.2.3), which COBOL-85 does not have. A refusal at
      *> 85: a compiler that accepts the LINAGE operand fails, and one
      *> that cites 13.8.6.2.3 or the 2023 clause fails the expect text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5361A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "pb5361a.txt".
           SELECT PFILE ASSIGN TO "pb5361ap.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS RPT.
       FD  PFILE LINAGE IS KTOT LINES.
       01  PREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-V PIC 9 VALUE 2.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE W-V.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  KTOT COLUMN 1 PIC 9(3) SUM W-V.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
