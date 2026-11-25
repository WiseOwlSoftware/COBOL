*>--- conformance-case
*> id: pb6174-constant-length-alphanumeric-function-argument
*> rules: [2002-SR-13.9.2-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> An alphanumeric constant-name is a legal argument of the integer function LENGTH (15.39.2 AR1), and the function's value
      *> is numeric, so it is a numeric operand of the subscript's arithmetic expression (8.8.1) wherever its argument is
      *> (kb/Work PB6174); the bare alphanumeric subscript operand stays refused (pb5709-constant-length-alphanumeric-constant-subscript).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6174FA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 KC CONSTANT AS "AB".
       01 K CONSTANT AS LENGTH OF E (FUNCTION LENGTH (KC)).
       PROCEDURE DIVISION.
           DISPLAY "K=" K
           STOP RUN.
