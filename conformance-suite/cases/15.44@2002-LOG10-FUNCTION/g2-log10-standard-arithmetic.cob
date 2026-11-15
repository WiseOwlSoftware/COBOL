      *>--- conformance-case
      *> id: g2-log10-standard-arithmetic
      *> rules: [2002-RV-15.44.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.44.3 RV1 - "The returned value is the approximation of
      *> the logarithm to the base 10 of argument-1." Exercised under
      *> ARITHMETIC IS STANDARD, the 2002 form 2023 lacks: §2002:11.8.4.2
      *> GR2 makes "certain integer and numeric functions" use standard
      *> arithmetic, and §2002:15.3.1 puts "the returned value for numeric
      *> and integer functions" in "a temporary standard intermediate data
      *> item" (§2002:8.8.1.3.1.1 "with a precision of 32 decimal digits").
      *> Each argument is > 0 (§2002:15.44.2 AR2). Each value is COMPUTEd
      *> ROUNDED into +9(2).9(4) (§2002:14.6.3), so any approximation
      *> within 2E-5 of the true logarithm gives:
      *>   THOU log10(1000)          = 3                -> +03.0000
      *>   TWO  log10(2)             = 0.30102999566..  -> +00.3010
      *>   HUND log10(0.01)          = -2               -> -02.0000
      *>   BIG  log10(1234..8901)    = 30.0915149772..  -> +30.0915
      *>   SM   log10(.1234..8901)   = -0.9084850227..  -> -00.9085
      *>   EXPR log10(BIG * 10)      = 31.0915149772..  -> +31.0915
      *> BIG and SM are 31-digit operands; EXPR is a 32-digit product held
      *> in the standard intermediate. A leg fails if the STANDARD mode
      *> loses an operand digit or scale on the way in, if the sign of a
      *> logarithm below 1 is lost, or if the mode is refused at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LOG01.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BIG     PIC S9(31)
               VALUE 1234567890123456789012345678901.
       01  SM      PIC SV9(31)
               VALUE .1234567890123456789012345678901.
       01  TEN     PIC 99 VALUE 10.
       01  R       PIC +9(2).9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION LOG10(1000)
           DISPLAY "THOU=" R
           COMPUTE R ROUNDED = FUNCTION LOG10(2)
           DISPLAY "TWO=" R
           COMPUTE R ROUNDED = FUNCTION LOG10(0.01)
           DISPLAY "HUND=" R
           COMPUTE R ROUNDED = FUNCTION LOG10(BIG)
           DISPLAY "BIG=" R
           COMPUTE R ROUNDED = FUNCTION LOG10(SM)
           DISPLAY "SM=" R
           COMPUTE R ROUNDED = FUNCTION LOG10(BIG * TEN)
           DISPLAY "EXPR=" R
           STOP RUN.
