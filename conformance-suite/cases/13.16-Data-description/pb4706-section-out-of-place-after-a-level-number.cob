      *>--- conformance-case
      *> id: pb4706-section-out-of-place-after-a-level-number
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  FILE SECTION cannot follow the working-storage section (13.2), so no period lets it follow: not the
      *> missing-period report.  And the word begins a header, so it is no reserved word written as a name: not COBOL0901.
      *> It is the report FILE SECTION draws after a complete entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
