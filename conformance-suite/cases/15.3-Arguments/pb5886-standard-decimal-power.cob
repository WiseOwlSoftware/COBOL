      *>--- conformance-case
      *> id: pb5886-standard-decimal-power
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 review R4-1 - a power is bounded by its base:
      *> the base of (...) ** 1 below is the argument of
      *> pb5886-standard-decimal-intermediate, whose intermediate
      *> B27 / 0.0001 / 16384 needs up to 37 significant digits, 10
      *> of them fraction digits, against the 34 a standard-decimal
      *> intermediate data item keeps (§2023:8.8.1.5.2). The power
      *> to 1 is the base, so the argument is not shown to be an
      *> integer and the program is refused (COBOL1627).
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". Its native
      *> twin is the second line of pb5886_native_quotient_chain.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886N.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B27 PIC 9(27) VALUE 999999999999999999999999999.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               ((B27 / 0.0001 / 16384 * 16384 * 0.0001) ** 1, 1000).
           STOP RUN.
