      *>--- conformance-case
      *> id: pb4770-foreign-run-ended-by-section-in-file-control
      *> rules: [FMT-12.4.5.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4770.  ASIGN is no clause of a file control entry and the header MAIN SECTION ends the run it opens: the run is named
      *> (COBOL1970 at ASIGN), not left to the generic report at SECTION.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4770B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASIGN MAIN SECTION.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
