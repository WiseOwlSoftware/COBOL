      *>--- conformance-case
      *> id: g2str05-numeric-national-with-national
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
      *>   NR and N"A" are of class national; N9, PIC 9(3) USAGE NATIONAL,
      *>   is class numeric in 2002 (§2002:8.5.2.9 item 1; kb/Work PB5625
      *>   reading B), not national, so the STRING at line 26 is refused.
      *>   This refusal stands beside the accepted case g2str01-operand-
      *>   usage-class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N9              PIC 9(3) USAGE NATIONAL VALUE 125.
       01  NR              PIC N(6).
       PROCEDURE DIVISION.
       MAIN-P.
           STRING N"A" N9 DELIMITED BY SIZE INTO NR
           DISPLAY FUNCTION DISPLAY-OF(NR)
           STOP RUN.
