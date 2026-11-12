      *>--- conformance-case
      *> id: pb5299-value-of-table-element-report
      *> rules: [1985-GR-XIII.3.2.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5299. With the exception of the REPORT clause, the
      *> clauses of a report file's FD take their Sequential I-O meaning
      *> (§1985:XIII.3.2.4 GR3), so its VALUE OF clause is the sequential
      *> FD's: data-name-1 "cannot be subscripted" (§1985:VII.3.9.3 SR1)
      *> and a table element is never written without a subscript
      *> (§1985:IV.4.3.8.2.3 SR4). Refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5299RPT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R2 ASSIGN TO "pb5299p.txt" FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  R2
           VALUE OF FILE-ID E
           REPORT RPT.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  T. 05 E PIC X(10) OCCURS 2 TIMES.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DE LINE PLUS 1.
           05 COLUMN 1 PIC X(12) VALUE "ABCDEFGHIJKL".
       PROCEDURE DIVISION.
       P.  STOP RUN.
