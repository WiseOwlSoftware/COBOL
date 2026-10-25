*>--- conformance-case
*> id: pb3665-string-integer-function-sender
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - ISO 14.9.43.3 SR1: "all identifiers, except identifier-4, shall be described implicitly or
*> explicitly as usage display or national." A function-identifier is the data item its 15.2 type describes
*> (8.4.3.2.1), and 15.2 gives an alphanumeric function an implicit usage display and a national function an
*> implicit usage national, but states NO usage for an integer function (15.2 5): "an operational sign and no
*> digits to the right of the decimal point"), so FUNCTION INTEGER(N) is not described as usage display or
*> national and is no STRING sending operand. The statement compiled clean and sent the value's digits.
*> The control is pb3665_string_function_usage_controls (an alphanumeric function is admitted). Unchanged at
*> every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING FUNCTION INTEGER(N) DELIMITED BY SIZE INTO R
           STOP RUN.
