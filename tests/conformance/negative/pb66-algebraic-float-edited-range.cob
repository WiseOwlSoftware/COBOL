      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 15.43.4 r1 / 15.58.4 r1: a floating-point numeric-edited argument-1 of HIGHEST-ALGEBRAIC /
      *> LOWEST-ALGEBRAIC shall be described such that its value farthest from zero would pass an
      *> IN-ARITHMETIC-RANGE test (8.8.4.4.4 GR3 l - within the intermediate data item's range for the arithmetic
      *> mode in effect: native = binary64 here). 999E+999 is about 1E+1002, beyond binary64's 1.8E+308:
      *> COBOLNET1660 (kb/Work PB66). Under ARITHMETIC IS STANDARD-DECIMAL the same entry is in range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66NAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC 9(3)E+999.
       01 EB PIC -9(3)E+999.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION HIGHEST-ALGEBRAIC(EA).
           DISPLAY FUNCTION LOWEST-ALGEBRAIC(EB).
           STOP RUN.
