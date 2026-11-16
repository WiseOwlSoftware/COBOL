      *>--- conformance-case
      *> id: pb5615-locale-date-alphabetic-argument-2002
      *> rules: [2002-AR-15.41.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2002:15.41.2 AR1: "Argument-1 shall be of class
      *>   alphanumeric or national and shall be 8 character positions
      *>   in length." and §2002:8.5.2.1: "An alphabetic data item may be
      *>   specified wherever an elementary alphanumeric data item is
      *>   permitted as an operand." D is PIC A(8): at COBOL-2002 it is
      *>   argument-1 of LOCALE-DATE, holding the date 2026-08-19, which
      *>   fr-FR formats dd/MM/yyyy as "19/08/2026" (DESIGN-locale-facility
      *>   DETERMINATION L10, as 15.41@2002-LOCALE-DATE-FUNCTION's
      *>   g2002-locale-date-function). It fails if the PIC A argument is
      *>   refused (COBOL1627). From COBOL-2014 the sentence is withdrawn
      *>   (§2014:E.2) and the argument is refused (the diagnostics file
      *>   pb5615-locale-date-alphabetic-argument-refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615LD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D               PIC A(8) VALUE "20260819".
       01  S               PIC X(12).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOCALE-DATE(D FR) TO S
           DISPLAY "DATE-FR=[" S "]"
           STOP RUN.
