      *>--- conformance-case
      *> id: pb3665_string_function_usage_controls
      *> rules: [SR-14.9.43.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3665 - THE POSITIVE CONTROL for the pb3665-string-*-function-* refusals. A refusal's failure
      *> mode is reaching source the rule ADMITS, so each function STRING admits is written here.
      *>
      *> ISO 14.9.43.3 SR1: "all identifiers, except identifier-4, shall be described implicitly or explicitly as
      *> usage display or national." ISO 15.2 1): "Alphanumeric functions have an implicit usage display", so an
      *> alphanumeric function-identifier (8.4.3.2.1: "the unique data item that results from the evaluation of a
      *> function") is admitted as identifier-1 and as identifier-2.
      *>   L1 STRING FUNCTION UPPER-CASE(W) DELIMITED BY SIZE INTO A: A = "HELLO" (the rest keeps its VALUE spaces,
      *>      14.9.43.4 GR3 a)).
      *>   L2 STRING FUNCTION LOWER-CASE("AB") FUNCTION UPPER-CASE(W) DELIMITED BY SIZE INTO B: two function senders
      *>      in one run, "ab" then "HELLO" (14.9.43.4 GR3 a)).
      *>   L3 STRING W DELIMITED BY FUNCTION UPPER-CASE("L") INTO C: "hello" holds no upper-case "L", so the whole
      *>      sender transfers.
      *>   L4 STRING W DELIMITED BY FUNCTION LOWER-CASE("L") INTO D: the delimiter is "l", so "he" transfers
      *>      (14.9.43.4 GR3 b)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(5) VALUE "hello".
       01 A PIC X(8) VALUE SPACES.
       01 B PIC X(8) VALUE SPACES.
       01 C PIC X(8) VALUE SPACES.
       01 D PIC X(8) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           STRING FUNCTION UPPER-CASE(W) DELIMITED BY SIZE INTO A.
           DISPLAY "L1 [" A "]".
           STRING FUNCTION LOWER-CASE("AB") FUNCTION UPPER-CASE(W)
               DELIMITED BY SIZE INTO B.
           DISPLAY "L2 [" B "]".
           STRING W DELIMITED BY FUNCTION UPPER-CASE("L") INTO C.
           DISPLAY "L3 [" C "]".
           STRING W DELIMITED BY FUNCTION LOWER-CASE("L") INTO D.
           DISPLAY "L4 [" D "]".
           STOP RUN.
