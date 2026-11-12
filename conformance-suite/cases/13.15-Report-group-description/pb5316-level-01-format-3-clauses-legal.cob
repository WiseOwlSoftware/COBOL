      *>--- conformance-case
      *> id: pb5316-level-01-format-3-clauses-legal
      *> rules: [SR-13.15.3-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5316 - 1985:XIII.3.9.3 SR4a: "The first entry that
      *> describes a report group must be a format 1 entry", and format 1
      *> (1985:XIII.3.9.2) prints the level-number 01, TYPE, LINE, NEXT
      *> GROUP and USAGE only; SR3: "In format 3 the level-number may be
      *> any integer from 02 to 49 inclusive" (cite.py: OK). The 01 entry
      *> below writes PICTURE, SIGN, COLUMN and SOURCE, the clauses of a
      *> format 3 entry, so at COBOL-85 it is refused (COBOL2247).
      *> 2023 has one entry format whose level-number is 1 through 49
      *> (13.15.3 SR3), so it compiles from 2002 (this case); the 85
      *> refusal is conformance:pb5316-level-01-format-3-clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5316B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5316B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-A PIC S99 VALUE -3.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL LINE PLUS 1 COLUMN 1 PIC S99
           SIGN LEADING SEPARATE SOURCE WS-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
