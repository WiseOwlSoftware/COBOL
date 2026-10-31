*>--- conformance-case
*> id: pb4173-occurs-alphanumeric-literal-then-times
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4173 - ISO 13.18.38.2 prints "OCCURS integer-2 TIMES": the occurrence count is an integer,
      *> and an alphanumeric literal is none (8.3.3.1: each literal possesses a class and category). The one
      *> fault is the "A". The parse-error recovery returned from the OCCURS clause at the refused operand,
      *> and the clause's own TIMES was met by the entry's clause list as a clause of its own: a false
      *> COBOL1941 "TIMES is not a clause of the data description entry" after the true COBOL5506. The
      *> recovery now discards the rest of the entry to its period (the count is pinned by
      *> ParseRecoveryUnitTailTests).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4173OC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T PIC X OCCURS "A" TIMES.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
