      *>--- conformance-case
      *> id: g2ldt03-argument-numeric-national
      *> rules: [2002-AR-15.41.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.41.2 AR1 - "Argument-1 shall be of class alphanumeric or
      *>   national and shall be 8 character positions in length." A is
      *>   class numeric (§2002:8.5.2.9 item 1, Table 3; kb/Work PB5625
      *>   reading B), not alphanumeric or national, though 8 character
      *>   positions,
      *>   so the reference at line 27 is refused. This refusal stands
      *>   beside the accepted case g2ldt02-argument-class-length; it is
      *>   not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LDT03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC 9(8) USAGE NATIONAL VALUE 20260819.
       01  S               PIC X(12).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOCALE-DATE(A FR) TO S
           DISPLAY S
           STOP RUN.
