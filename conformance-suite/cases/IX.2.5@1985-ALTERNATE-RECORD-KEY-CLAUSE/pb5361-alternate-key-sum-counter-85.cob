      *>--- conformance-case
      *> id: pb5361-alternate-key-sum-counter-85
      *> rules: [1985-SR-IX.2.5.3-2]
      *> source-format: fixed
      *>---
      *> kb/Work PB5361. §1985:IX.2.5.3 SR2 (cite.py: OK): "Data-name-1
      *> must be defined as a data item of the category alphanumeric
      *> within a record description entry associated with the file-name
      *> to which the ALTERNATE RECORD KEY clause is subordinate." At
      *> COBOL-85 a sum counter is a data-name wherever it is written
      *> (§1985:XIII.3.9.3 SR10d), so ALTERNATE RECORD KEY IS KTOT names
      *> the counter, which lies in no record of PFILE and is numeric:
      *> SR2 is the rule it breaks. The refusal must name that 1985
      *> rule, never the 2023 section rule (§2023:13.8.6.2.3) COBOL-85
      *> does not have. A refusal at 85: a compiler that accepts it
      *> fails, and one that cites a 2023 clause fails the expect text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5361D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "pb5361d.txt".
           SELECT PFILE ASSIGN TO "pb5361dp.txt" ORGANIZATION INDEXED
               ACCESS MODE SEQUENTIAL RECORD KEY IS PK
               ALTERNATE RECORD KEY IS KTOT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE REPORT IS RPT.
       FD  PFILE.
       01  PREC.
           05  PK PIC X(5).
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
