*>--- conformance-case
*> id: pb3370-object-computer-period-missing
*> rules: [FMT-12.3.6.2, SR-12.3.6.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3370 - ISO 12.3.6.3 SR4 lets the second period of the OBJECT-COMPUTER paragraph go only
      *> when neither computer-name-1 nor any of the optional clauses is specified.  Here a name and a
      *> PROGRAM COLLATING SEQUENCE clause are written and the period is not: the grammar superset-parses
      *> the period and ClosedFormatPass refuses its absence BY NAME at the clause (COBOLNET3408), at every
      *> edition, where it used to be a bare COBOL0307 at the SPECIAL-NAMES header.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3370OP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ
           PROGRAM COLLATING SEQUENCE IS AL-REV
       SPECIAL-NAMES.
           ALPHABET AL-REV IS "Z" THRU "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3370OP".
           STOP RUN.
