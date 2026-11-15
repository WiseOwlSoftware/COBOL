      *>--- conformance-case
      *> id: pb5476_function_length_numeric_2002
      *> rules: [SR-8.8.4.4.3-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5476 - the positive half of pb5476-function-length-numeric-at-85.
      *> §2023:8.8.4.4.3 SR8: "If the NUMERIC phrase is specified, identifier-1 shall reference
      *> a data item whose usage is display or national or whose category is numeric." LENGTH is an
      *> integer function, whose result is category numeric, and the value 5 is all digits: T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5476P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X5 PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       P1.
           IF FUNCTION LENGTH (X5) IS NUMERIC
               DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
