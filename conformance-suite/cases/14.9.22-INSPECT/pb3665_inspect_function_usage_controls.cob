      *>--- conformance-case
      *> id: pb3665_inspect_function_usage_controls
      *> rules: [SR-14.9.22.3-2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3665 - THE POSITIVE CONTROL for the pb3665-inspect-*-function-operand refusals. ISO 14.9.22.3 SR2:
      *> "Identifier-3, ..., identifier-n shall reference an elementary item described implicitly or explicitly as
      *> usage display or national." ISO 15.2 1): "Alphanumeric functions have an implicit usage display", so an
      *> alphanumeric function-identifier is admitted at every identifier-n position:
      *>   L1 TALLYING FOR ALL FUNCTION UPPER-CASE("b"): S = "abBcb" holds one "B": T = 1.
      *>   L2 REPLACING ALL FUNCTION UPPER-CASE("b") BY FUNCTION LOWER-CASE("X"): the one "B" becomes "x":
      *>      S = "abxcb".
      *>   L3 CONVERTING FUNCTION LOWER-CASE("abc") TO FUNCTION UPPER-CASE("xyz"): a -> X, b -> Y, c -> Z:
      *>      S = "XYxZY".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "abBcb".
       01 T PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S TALLYING T FOR ALL FUNCTION UPPER-CASE("b").
           DISPLAY "L1 [" T "]".
           INSPECT S REPLACING ALL FUNCTION UPPER-CASE("b")
               BY FUNCTION LOWER-CASE("X").
           DISPLAY "L2 [" S "]".
           INSPECT S CONVERTING FUNCTION LOWER-CASE("abc")
               TO FUNCTION UPPER-CASE("xyz").
           DISPLAY "L3 [" S "]".
           STOP RUN.
