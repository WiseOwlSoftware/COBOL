      *>--- conformance-case
      *> id: pb4818-user-word-before-section-in-a-report-group-at-85
      *> rules: [FMT-13.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4818.  At 85 SCREEN is a user-defined word and the format has no such section, so no period helps: the word
      *> is the entry-name and SECTION is the fault, the plain report the working-storage section gives
      *> (pb4706-header-after-a-user-word-at-85).  It fails if the report names a missing period at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4818B.
       DATA DIVISION.
       REPORT SECTION.
       RD R.
       01 SCREEN SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
