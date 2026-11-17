      *>--- conformance-case
      *> id: pb5891-wide-exponent-power
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R3-1 - a power whose exponent is wide
      *> is evaluated as a binary64 approximation, so it is no
      *> integer term, and the argument is refused (COBOL1627).
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified".
      *> X * X over a 31-digit X has a 62-digit bound, past the
      *> Int128 carrier, so its carrier is the wide form, which has
      *> no exact power: 1 ** (X * X) is the engine's binary64, and
      *> the sum above it is on the SDIDI, where X * 10.25 needs 35
      *> significant digits. Its twin without the power,
      *> pb5891_native_exact_lane, is exact and prints 0041.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y  PIC 9(31) VALUE 2050000000000000000000000000000.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((Y * 100 - (1 ** (X * X) - 1
               + X * 10.25)) * 4, 1000).
           STOP RUN.
