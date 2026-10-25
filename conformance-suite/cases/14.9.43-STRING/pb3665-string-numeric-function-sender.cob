*>--- conformance-case
*> id: pb3665-string-numeric-function-sender
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3665 - a NUMERIC function (15.2 4)), as opposed to an integer one (5)), is refused the same way:
*> "A numeric function has an operational sign" and states no usage, so FUNCTION NUMVAL is not described as
*> usage display or national (ISO 14.9.43.3 SR1). Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3665SN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(4) VALUE "12  ".
       01 R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           STRING FUNCTION NUMVAL(T) DELIMITED BY SIZE INTO R
           STOP RUN.
