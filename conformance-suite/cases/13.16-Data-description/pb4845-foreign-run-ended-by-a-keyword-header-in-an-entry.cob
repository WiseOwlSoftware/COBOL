      *>--- conformance-case
      *> id: pb4845-foreign-run-ended-by-a-keyword-header-in-an-entry
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4845.  VALEU is no clause of a data description entry and the LINKAGE of LINKAGE SECTION ends the run it
      *> opens: the run is named (COBOL1941 at VALEU), as when MAIN SECTION ends it (PB4770).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4845A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9 VALEU
       LINKAGE SECTION.
       01 C PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
