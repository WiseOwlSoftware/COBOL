*>--- conformance-case
*> id: pb4712-length-parenthesized-numeric-function
*> rules: [AR-15.50.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 - ISO §15.50.3 r1: "Argument-1 shall be an alphanumeric, national, or boolean literal; a data item of any class or
      *> category; a based entry; or a type-name." FUNCTION ABS(I2) written bare is a function-identifier, which "references a
      *> temporary data item" (§8.4.3.2.4 r1), so LENGTH(FUNCTION ABS(I2)) is legal (pb1398_length_of_function_result). Enclosed in
      *> parentheses it is "an arithmetic expression enclosed in parentheses" (§8.8.1.1), the same as LENGTH((I2)), which is refused:
      *> the argument binder kept the call a function-identifier and LENGTH((FUNCTION ABS(I2))) printed 0004.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGLENGTHPARENNUMFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2    PIC S9(4) VALUE -7.
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH((FUNCTION ABS(I2))).
           STOP RUN.
