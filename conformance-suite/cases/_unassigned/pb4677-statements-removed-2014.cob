      *>--- conformance-case
      *> id: pb4677-statements-removed-2014
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - a statement of the facility names the removal with no section
      *> declaring its cd-name, on recognition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677X2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           ENABLE INPUT W1.
           DISABLE OUTPUT W1.
           PURGE W1.
           RECEIVE W1 SEGMENT INTO W1.
           SEND W1 WITH EGI.
           STOP RUN.
