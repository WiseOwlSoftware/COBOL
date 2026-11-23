      *>--- conformance-case
      *> id: pb3321-sqrt-standard-arithmetic-2002
      *> rules: [RV-15.84.4-1]
      *> accept: [2002, 2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3321. §2002:15.68.3 RV1: "When standard arithmetic is
      *> specified, argument-1 is not rounded", and RV2: the returned
      *> value is the exact square root truncated to 32 digits. The
      *> 2023 text states the same rule for standard-binary and
      *> standard-decimal arithmetic (§2023:15.84.4 rule 1), so the band
      *> starts at 2002. ARITHMETIC IS STANDARD is a 2002 and 2014 option
      *> (2023 drops it), so the test is accepted at those two. The
      *> square root of 1.5129 is exactly 1.23 (1.23 * 1.23 = 1.5129),
      *> so E shows 1.23. The leg fails if the argument is converted to
      *> an inexact binary value and the root comes out 1.22.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3321SQ.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R     PIC 9V99.
       01 E     PIC 9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION SQRT(1.5129).
           MOVE R TO E.
           DISPLAY E.
           STOP RUN.
