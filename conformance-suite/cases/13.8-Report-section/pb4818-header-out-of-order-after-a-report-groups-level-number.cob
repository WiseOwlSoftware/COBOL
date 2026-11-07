      *>--- conformance-case
      *> id: pb4818-header-out-of-order-after-a-report-groups-level-number
      *> rules: [FMT-13.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4818.  13.2.1 prints the LOCAL-STORAGE SECTION before the REPORT SECTION, so no period lets the header follow:
      *> the plain COBOL0001 at LOCAL-STORAGE, worded as a header out of order after a level-number is at every entry slot
      *> (pb4706-section-out-of-place-after-a-level-number).  It fails if the entry loop takes LOCAL-STORAGE as the entry's name
      *> and reports at SECTION, or if the wording follows which grammar decision met the word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4818C.
       DATA DIVISION.
       REPORT SECTION.
       RD R.
       01 LOCAL-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
