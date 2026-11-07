*>--- conformance-case
*> id: pb4712-byte-length-parenthesized-numeric-function
*> rules: [AR-15.14.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 - ISO §2023:15.14.3 r1: "Argument-1 shall be an alphanumeric or national literal, a based entry, a type-name, or a data
      *> item of any class or category." FUNCTION ABS(I2) written bare is a function-identifier, which "references a temporary data
      *> item" (§2023:8.4.3.2.4 r1), so BYTE-LENGTH(FUNCTION ABS(I2)) is legal (pb1398_length_of_function_result). Enclosed in
      *> parentheses it is "an arithmetic expression enclosed in parentheses" (§2023:8.8.1.1), as BYTE-LENGTH((I2)) is refused: the
      *> argument binder kept the call a function-identifier and BYTE-LENGTH((FUNCTION ABS(I2))) printed 0004.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGBYTELENPARENNUMFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2    PIC S9(4) VALUE -7.
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION BYTE-LENGTH((FUNCTION ABS(I2))).
           STOP RUN.
