      *>--- conformance-case
      *> id: pb5579-rewrite-file-report-file
      *> rules: [SR-14.9.35.3-11, 2002-SR-14.8.33.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.33.2 SR11 "File-name-1 shall not reference a report file or a sort-merge file description
      *> entry" (§2023:14.9.35.3 SR11): REWRITE FILE names a report file. Expected: COBOL2577.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579R02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579R02.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT RPF ASSIGN TO "r.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(10).
       FD RPF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 W10 PIC X(10) VALUE "EXACT-TEN!".
       REPORT SECTION.
       RD RPT.
       01 LN TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           REWRITE FILE RPF FROM W10.
           STOP RUN.
