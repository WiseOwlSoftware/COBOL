      *>--- conformance-case
      *> id: pb5476-function-length-numeric-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5476 - the edition half of pb5476_function_length_numeric_2002. §1985:VI.6.3.1.2 GR5: "If identifier-1 is a
      *> function-identifier, it must reference an alphanumeric function". LENGTH is an integer function, and the binder
      *> folds FUNCTION LENGTH (X5) to a constant: the folded value is still a function-identifier operand.
      *> Expected: COBOL6918 at 85; from 2002 the program compiles (§2023:8.8.4.4.3 SR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5476N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X5 PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       P1.
           IF FUNCTION LENGTH (X5) IS NUMERIC
               DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
