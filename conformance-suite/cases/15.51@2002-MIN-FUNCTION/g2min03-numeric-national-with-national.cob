      *>--- conformance-case
      *> id: g2min03-numeric-national-with-national
      *> rules: [2002-AR-15.51.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.51.2 AR2 - "All arguments shall be of the same class with
      *>   the exception that mixing of arguments of alphabetic and
      *>   alphanumeric classes is allowed."
      *>   A (PIC 9(3) USAGE NATIONAL) is class numeric in 2002
      *>   (§2002:8.5.2.9 item 1; kb/Work PB5625 reading B) and B (PIC
      *>   N(3)) class national,
      *>   not a permitted mix, so the reference at line 24 is refused. This
      *>   refusal stands beside the accepted case g2min01-argument-same-
      *>   class; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MIN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC 9(3) USAGE NATIONAL VALUE 125.
       01  B               PIC N(3) VALUE N"ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY FUNCTION MIN(A B)
           STOP RUN.
