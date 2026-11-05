*>--- conformance-case
*> id: pb4763_class_condition_function_identifier_subject
*> rules: [GR-8.8.4.4.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4763 (review R2-1): a function-identifier written SOLE is an identifier (ISO 8.5.2.12 items 3-7 make a
      *> numeric function a category-numeric data item), so it is identifier-1 of a class condition (8.8.4.4.2). A function
      *> that folds at bind time (FUNCTION LENGTH, FUNCTION E) is the same identifier, not a literal; the numeric class
      *> condition over a numeric value is true.
      *>   cite.py --check 8.8.4.4.2 "identifier-1" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. POS4763A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           IF FUNCTION LENGTH(A) IS NUMERIC
               DISPLAY "LENGTH T"
           ELSE
               DISPLAY "LENGTH F"
           END-IF
           IF FUNCTION BYTE-LENGTH(A) IS NUMERIC
               DISPLAY "BYTE-LENGTH T"
           ELSE
               DISPLAY "BYTE-LENGTH F"
           END-IF
           IF FUNCTION LENGTH("ABC") IS NUMERIC
               DISPLAY "LITERAL-ARG T"
           ELSE
               DISPLAY "LITERAL-ARG F"
           END-IF
           EVALUATE FUNCTION LENGTH(A)
               WHEN NUMERIC DISPLAY "EVALUATE T"
               WHEN OTHER DISPLAY "EVALUATE F"
           END-EVALUATE
           STOP RUN.
