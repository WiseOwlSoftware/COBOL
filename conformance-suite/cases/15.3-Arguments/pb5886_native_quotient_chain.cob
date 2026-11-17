      *>--- conformance-case
      *> id: pb5886_native_quotient_chain
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 review R3-1 - the native twin of
      *> pb5886-standard-decimal-intermediate: the same argument
      *> under native arithmetic (no ARITHMETIC clause, §2023:11.9.5)
      *> is an integer argument.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". A quotient by
      *> a literal whose reciprocal terminates is the product by
      *> that reciprocal: B27 / 0.0001 is B27 * 10000, and the
      *> quotient by 16384 has 14 fraction digits, which a native
      *> nested quotient keeps (the guard digits of the numeric
      *> design's quotient rule), so B27 / 0.0001 / 16384 * 16384 *
      *> 0.0001 is exactly B27. With B27 = 999...9 (27 nines),
      *> MOD (B27, 1000) is 999 (§2023:15.64.4 RV1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B27 PIC 9(27) VALUE 999999999999999999999999999.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (B27 / 0.0001 / 16384 * 16384 * 0.0001, 1000).
           DISPLAY R.
           STOP RUN.
