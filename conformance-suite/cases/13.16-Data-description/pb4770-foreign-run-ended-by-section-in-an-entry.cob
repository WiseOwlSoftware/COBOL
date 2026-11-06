      *>--- conformance-case
      *> id: pb4770-foreign-run-ended-by-section-in-an-entry
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4770.  VALEU is no clause of a data description entry and SECTION ends the run it opens: the run is named
      *> (COBOL1941 at VALEU), not left to the generic report at SECTION.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4770A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9 VALEU SECTION.
       01 C PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
