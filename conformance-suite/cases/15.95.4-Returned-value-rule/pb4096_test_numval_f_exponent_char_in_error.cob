*>--- conformance-case
*> id: pb4096_test_numval_f_exponent_char_in_error
*> rules: [RV-15.95.4-1, AR-15.69.3-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4096 - ISO 15.95.4 r1 b): "Otherwise, if one or more characters are in error, the position of
      *> the first character in error"; r1 c) (LENGTH + 1) is only the "Otherwise" of no character in error. The
      *> exponent of a NUMVAL-F argument is `E [space-string] {+|-} [space-string] n` with n "one, two, three, or
      *> four digits" (15.69.3 r1), so after `E+` a character that is neither a digit nor a space is a character in
      *> error and TEST-NUMVAL-F names ITS position; only text that ENDS after the sign is valid-but-incomplete
      *> (LENGTH + 1). The defect returned LENGTH + 1 for both (10 for "12.34E+AB", 6 for "1E++2").
      *> Hand-derived positions (1-based):
      *>   "12.34E+AB"  1 2 . 3 4 E + A B           -> 8  the A, the first character after the sign
      *>   "1E++2"      1 E + + 2                   -> 4  the second +
      *>   "1E-X"       1 E - X                     -> 4  the X
      *>   "1E+ A"      1 E + space A               -> 5  the space-string after the sign is legal, A is not
      *>   "1E+1A"      1 E + 1 A                   -> 5  one exponent digit, then a trailing character in error
      *>   "1E+"        1 E +                       -> 4  ends after the sign: r1 c, LENGTH + 1
      *>   "1E+ "       1 E + space                 -> 5  the same with the legal trailing space-string
      *>   "1E+12"      conforming                  -> 0
      *> Each is tested through a literal argument AND a data item, since a literal argument may be folded at
      *> compile time and a data item reaches the runtime scan.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4096EXPCH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(9)  VALUE "12.34E+AB".
       01 A2 PIC X(5)  VALUE "1E++2".
       01 A3 PIC X(4)  VALUE "1E-X".
       01 A4 PIC X(5)  VALUE "1E+ A".
       01 A5 PIC X(5)  VALUE "1E+1A".
       01 A6 PIC X(3)  VALUE "1E+".
       01 A7 PIC X(4)  VALUE "1E+ ".
       01 A8 PIC X(5)  VALUE "1E+12".
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F("12.34E+AB")
           DISPLAY "L1=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A1)
           DISPLAY "D1=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E++2")
           DISPLAY "L2=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A2)
           DISPLAY "D2=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E-X")
           DISPLAY "L3=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A3)
           DISPLAY "D3=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+ A")
           DISPLAY "L4=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A4)
           DISPLAY "D4=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+1A")
           DISPLAY "L5=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A5)
           DISPLAY "D5=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+")
           DISPLAY "L6=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A6)
           DISPLAY "D6=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+ ")
           DISPLAY "L7=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A7)
           DISPLAY "D7=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+12")
           DISPLAY "L8=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(A8)
           DISPLAY "D8=" R
           STOP RUN.
       END PROGRAM PB4096EXPCH.
