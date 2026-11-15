      *>--- conformance-case
      *> id: g2min02-numeric-with-alphanumeric
      *> rules: [2002-AR-15.51.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.51.2 AR2 - "All arguments shall be of the same class with
      *>   the exception that mixing of arguments of alphabetic and
      *>   alphanumeric classes is allowed."
      *>   A (PIC 9(3)) is class numeric and B (PIC X(3)) class alphanumeric,
      *>   not a permitted mix, so the reference at line 22 is refused. This
      *>   refusal stands beside the accepted case g2min01-argument-same-
      *>   class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MIN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC 9(3) VALUE 250.
       01  B               PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY FUNCTION MIN(A B)
           STOP RUN.
