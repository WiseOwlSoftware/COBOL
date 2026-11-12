      *>--- conformance-case
      *> id: pb5316-level-49-format-2
      *> rules: [SR-13.15.3-3]
      *> source-format: fixed
      *>---
      *> kb/Work PB5316 - 1985:XIII.3.9.3 SR3: "In format 2 the level-
      *> number may be any integer from 02 to 48 inclusive" (cite.py:
      *> OK). The level 49 entry below has no PICTURE clause and none of
      *> the clauses of a format 3 entry, so it is a format 2 entry at
      *> level 49, refused at COBOL-85 (COBOL2247). Its legal side, a
      *> format 3 entry at level 49, is
      *> pb5316_level_49_format_3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5316C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5316C.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL.
           49  LINE PLUS 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
