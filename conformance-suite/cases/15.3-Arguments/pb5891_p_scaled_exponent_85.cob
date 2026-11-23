      *>--- conformance-case
      *> id: pb5891_p_scaled_exponent_85
      *> rules: [AR-15.3-6, AR-15.64.3-1]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R2-2 (a wrong answer on main before
      *> it) - a power whose exponent is an integer BY VALUE is the
      *> exact power, whatever scale its carrier is written at.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". U PIC 99P
      *> VALUE 40 holds the integer 40 (its digit 4 times ten), so
      *> 3 ** U is the integer 3 ** 40 = 12157665459056928801, and
      *> FUNCTION MOD (3 ** U, 1000) is 801 (§2023:15.64.4 RV1).
      *> Before the fix the code generator asked for a scale-0
      *> exponent, took the binary64 approximation for the P-scaled
      *> one, stored 12157665459056929000 and gave MOD 768.
      *> At COBOL-85 a PICTURE holds at most 18 digits, so the
      *> 20-digit power is shown through MOD only (its twin at 2014
      *> and 2023 is pb5891_p_scaled_exponent).
      *> EXPECTED: A 0801; Z PIC 9P VALUE 20 gives MOD (2 ** Z, 1000)
      *> = MOD (1048576, 1000) = 576 (B 0576).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U  PIC 99P VALUE 40.
       01  Z  PIC 9P VALUE 20.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (3 ** U, 1000).
           DISPLAY "A " R.
           COMPUTE R = FUNCTION MOD (2 ** Z, 1000).
           DISPLAY "B " R.
           STOP RUN.
