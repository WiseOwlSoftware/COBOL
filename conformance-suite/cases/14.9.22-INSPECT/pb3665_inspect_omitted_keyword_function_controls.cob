*>--- conformance-case
*> id: pb3665_inspect_omitted_keyword_function_controls
*> rules: [SR-14.9.22.3-1, SR-14.9.22.3-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 (review round 1) - THE POSITIVE CONTROL for pb3665-inspect-identifier1-* and
*> pb3665-inspect-omitted-keyword-*. ISO 15.2 1): "Alphanumeric functions have an implicit usage display", so an
*> alphanumeric function-identifier satisfies 14.9.22.3 SR1 (identifier-1) and SR2 (identifier-3 ... identifier-n),
*> and 8.4.3.2.3 SR2 lets the word FUNCTION be left out under REPOSITORY FUNCTION ALL INTRINSIC:
*>   M1 identifier-1 = FUNCTION UPPER-CASE(S), S = "abBcb": the examined string is "ABBCB", three "B": T = 3.
*>   M2 TALLYING FOR ALL UPPER-CASE("b") (keyword omitted): S holds one "B": T2 = 1.
*>   M3 REPLACING ALL UPPER-CASE("b") BY LOWER-CASE("X") (keyword omitted): the one "B" becomes "x":
*>      S = "abxcb".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665OC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "abBcb".
       01 T PIC 9(4) VALUE 0.
       01 T2 PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT FUNCTION UPPER-CASE(S) TALLYING T FOR ALL "B"
           DISPLAY "M1 [" T "]"
           INSPECT S TALLYING T2 FOR ALL UPPER-CASE("b")
           DISPLAY "M2 [" T2 "]"
           INSPECT S REPLACING ALL UPPER-CASE("b") BY LOWER-CASE("X")
           DISPLAY "M3 [" S "]"
           STOP RUN.
