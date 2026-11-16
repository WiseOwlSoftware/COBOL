      *>--- conformance-case
      *> id: g2str02-usage-packed
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
      *>   P is USAGE PACKED-DECIMAL, neither usage display nor national,
      *>   so the STRING at line 25 is refused.
      *>   This refusal stands beside the accepted case g2str01-operand-
      *>   usage-class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P               PIC 9(3) USAGE PACKED-DECIMAL VALUE 42.
       01  R               PIC X(6).
       PROCEDURE DIVISION.
       MAIN-P.
           STRING "A" P DELIMITED BY SIZE INTO R
           DISPLAY R
           STOP RUN.
