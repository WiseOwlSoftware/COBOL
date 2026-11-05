      *>--- conformance-case
      *> id: pb4706-entry-name-before-local-storage-section
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  From 2002 LOCAL-STORAGE is reserved and the LOCAL-STORAGE SECTION may follow the working-storage
      *> section, so the two words after the level-number are the header a forgotten period ran into, not an entry named
      *> LOCAL-STORAGE (COBOL1941 at SECTION).  At 85 the word is a user word and no such section exists (the next case).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOCAL-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
