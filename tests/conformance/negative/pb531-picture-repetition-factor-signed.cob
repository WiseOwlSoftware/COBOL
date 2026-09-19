      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.40.3 SR6 - "An unsigned nonzero integer that is enclosed in parentheses indicates
      *> the number of consecutive occurrences of the symbol that immediately precedes the left
      *> parenthesis."  UNSIGNED and NONZERO are both load-bearing, and the rule is ALL FORMATS, so it
      *> reads identically at every edition.
      *> A SIGNED POSITIVE factor is equally excluded - "an UNSIGNED nonzero integer".  This arm used to
      *> be accepted SILENTLY: PIC X(+3) expanded to XXX with no diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB531PICTUREREPETITIONFACTOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC X(+3).
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
