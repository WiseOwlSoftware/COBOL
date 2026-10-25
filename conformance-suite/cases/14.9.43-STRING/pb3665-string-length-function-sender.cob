*>--- conformance-case
*> id: pb3665-string-length-function-sender
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - FUNCTION LENGTH of a fixed-size item is an integer function (15.2 5)) that the compiler
*> folds to a numeric value at bind. It is refused as the FUNCTION it is, not as "a numeric literal": 8.4.3.2.1
*> "A function-identifier references the unique data item that results from the evaluation of a function", and
*> ISO 14.9.43.3 SR1 requires every identifier except identifier-4 to be described as usage display or
*> national, which 15.2 states for no numeric or integer function. The fold and the call answer alike.
*> Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC X(3).
          05 G-B PIC X(4).
       01 R PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING FUNCTION LENGTH(G) DELIMITED BY SIZE INTO R
           STOP RUN.
