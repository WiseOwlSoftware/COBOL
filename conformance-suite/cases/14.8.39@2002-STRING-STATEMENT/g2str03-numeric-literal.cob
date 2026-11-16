      *>--- conformance-case
      *> id: g2str03-numeric-literal
      *> rules: [2002-SR-14.8.39.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.39.2 SR1 - "All literals shall be described as
      *>   alphanumeric, boolean, or national literals, and all identifiers,
      *>   except identifier-4, shall be described implicitly or explicitly
      *>   as usage display or national. If any one of literal-1, literal-2,
      *>   identifier-1, identifier-2, or identifier-3 is of class national,
      *>   then all shall be of class national."
      *>   12 is a numeric literal, not an alphanumeric, boolean or national
      *>   literal, so the STRING at line 24 is refused.
      *>   This refusal stands beside the accepted case g2str01-operand-
      *>   usage-class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R               PIC X(6).
       PROCEDURE DIVISION.
       MAIN-P.
           STRING "A" 12 DELIMITED BY SIZE INTO R
           DISPLAY R
           STOP RUN.
