      *>--- conformance-case
      *> id: g2ldt04-argument-national-seven
      *> rules: [2002-AR-15.41.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.41.2 AR1 - "Argument-1 shall be of class alphanumeric or
      *>   national and shall be 8 character positions in length." A is
      *>   class national but 7 character positions, not 8,
      *>   so the reference at line 25 is refused. This refusal stands
      *>   beside the accepted case g2ldt02-argument-class-length; it is
      *>   not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LDT04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC N(7) VALUE N"2026081".
       01  S               PIC X(12).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOCALE-DATE(A FR) TO S
           DISPLAY S
           STOP RUN.
