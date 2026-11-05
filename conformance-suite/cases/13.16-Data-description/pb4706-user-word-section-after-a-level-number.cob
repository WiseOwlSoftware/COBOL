      *>--- conformance-case
      *> id: pb4706-user-word-section-after-a-level-number
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  A user word and SECTION after the level-number: the data division holds no user-named section, so no
      *> period lets it follow, and SECTION is no clause: not COBOL1941 "'SECTION' is not a clause ... for 'MAIN'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01
       MAIN SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
