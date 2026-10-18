*>--- conformance-case
*> id: pb3370-source-computer-period-missing
*> rules: [FMT-12.3.5.2, SR-12.3.5.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3370 - ISO 12.3.5.3 SR1 lets the second period of SOURCE-COMPUTER. [computer-name-1] .
      *> go only when computer-name-1 is not specified.  Here the name is written and its period is not:
      *> the grammar superset-parses the period (so the name's prediction commits to the paragraph) and
      *> ClosedFormatPass refuses its absence BY NAME at the name (COBOLNET3408), at every edition, where
      *> it used to be a bare COBOL0307 at the next paragraph's header.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3370SP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ
       OBJECT-COMPUTER. XYZ.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3370SP".
           STOP RUN.
