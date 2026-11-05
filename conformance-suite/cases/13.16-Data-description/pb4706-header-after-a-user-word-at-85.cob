      *>--- conformance-case
      *> id: pb4706-header-after-a-user-word-at-85
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  At 85 LOCAL-STORAGE is a user-defined word and the format has no such section, so no period helps: the
      *> word is the entry-name and SECTION is the fault.  A header is no clause: the report is the plain one at SECTION, never
      *> COBOL1941 "'SECTION' is not a clause ... for 'LOCAL-STORAGE'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOCAL-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
