      *>--- conformance-case
      *> id: pb5579-write-file-report-file
      *> rules: [SR-14.9.51.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5579 - §2002:14.8.47.2 SR12 "The file description entry associated with the write file shall not contain
      *> the REPORT clause" (§2023:14.9.51.3 SR12): WRITE FILE names a report file. Expected: COBOL2577.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5579R01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P5579R01.dat" ORGANIZATION IS SEQUENTIAL.
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
           WRITE FILE RPF FROM W10.
           STOP RUN.
