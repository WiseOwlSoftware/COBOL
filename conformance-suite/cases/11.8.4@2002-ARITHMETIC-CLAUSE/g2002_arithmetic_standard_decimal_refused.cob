      *>--- conformance-case
      *> id: g2002_arithmetic_standard_decimal_refused
      *> rules: [2002-FMT-11.8.4.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.8.4.1 - the 2002 ARITHMETIC clause has two alternatives,
      *>   NATIVE and STANDARD (PDF p206). STANDARD-DECIMAL is not one of
      *>   them at 2002, so ARITHMETIC IS STANDARD-DECIMAL violates the
      *>   format and is refused at --std 2002 (beside the accepted case
      *>   g2002_arithmetic_native_standard, which shows both 2002 forms).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARI02.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED".
           STOP RUN.
       END PROGRAM G2ARI02.
