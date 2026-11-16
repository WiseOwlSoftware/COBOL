      *>--- conformance-case
      *> id: pb5615-locale-date-alphabetic-argument-refused
      *> rules: [AR-15.52.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2023:15.52.3 AR1: "Argument-1 shall be of class
      *>   alphanumeric or national and shall be 8 character positions
      *>   in length." D is PIC A(8), class alphabetic, and from COBOL-2014
      *>   no sentence treats it as alphanumeric (§2014:E.2). Expected:
      *>   COBOL1627 at 2014 and 2023; the 2002 twin
      *>   pb5615-locale-date-alphabetic-argument-2002 accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615LR.
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
