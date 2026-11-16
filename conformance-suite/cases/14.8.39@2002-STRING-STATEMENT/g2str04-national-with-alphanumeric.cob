      *>--- conformance-case
      *> id: g2str04-national-with-alphanumeric
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
      *>   NR and N"AB" are of class national and "CD" is an alphanumeric
      *>   literal, so the STRING at line 24 is refused.
      *>   This refusal stands beside the accepted case g2str01-operand-
      *>   usage-class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NR              PIC N(6).
       PROCEDURE DIVISION.
       MAIN-P.
           STRING N"AB" "CD" DELIMITED BY SIZE INTO NR
           DISPLAY FUNCTION DISPLAY-OF(NR)
           STOP RUN.
