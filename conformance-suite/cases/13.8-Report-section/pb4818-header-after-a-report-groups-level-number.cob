      *>--- conformance-case
      *> id: pb4818-header-after-a-report-groups-level-number
      *> rules: [FMT-13.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4818.  From 2002 SCREEN is reserved and 13.2.1 prints the SCREEN SECTION after the REPORT SECTION, so
      *> the two words after the level-number are the header a forgotten period ran into, at SCREEN, as the same line is in the
      *> working-storage section (pb4706-entry-name-before-local-storage-section).  It fails if the report group's entry loop
      *> takes SCREEN as the entry's name and reports at SECTION.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4818A.
       DATA DIVISION.
       REPORT SECTION.
       RD R.
       01 SCREEN SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
