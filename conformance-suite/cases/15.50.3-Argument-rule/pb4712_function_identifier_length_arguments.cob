*>--- conformance-case
*> id: pb4712_function_identifier_length_arguments
*> rules: [AR-15.50.3-1, AR-15.14.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 - the legal side of the parenthesized-function refusals (pb4712-length-parenthesized-numeric-function,
      *> pb4712-length-parenthesized-boolean-function, pb4712-byte-length-parenthesized-numeric-function). ISO §15.50.3 r1 and
      *> §15.14.3 r1 admit "a data item of any class or category", and a function-identifier "references a temporary data item"
      *> (§8.4.3.2.4 r1), so each bare function below is a legal argument:
      *>   ABS(I2)                    - a copy of its argument's item, S9(4): 4 positions, 4 bytes (§15.4.1, DOC-A.1-92);
      *>   BOOLEAN-OF-INTEGER(5 4)    - 4 boolean positions (§15.50.4 r1), occupying one byte (§15.14.4 r4);
      *>   UPPER-CASE(X9)             - an alphanumeric result of the argument's 9 positions (§15.2 item 1).
      *> A parenthesis INSIDE the function's own argument list is not the argument's: ABS((I2)) is still a bare function.
      *> Each leg can fail by the argument's parentheses being read as the argument's own, or by the function-identifier arm losing the
      *> bare spelling that the parenthesized refusal now separates from it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4712ACCEPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2    PIC S9(4) VALUE -7.
       01 X9    PIC X(9) VALUE "ABC".
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH(FUNCTION ABS(I2))
           DISPLAY "L-ABS " N
           COMPUTE N = FUNCTION BYTE-LENGTH(FUNCTION ABS(I2))
           DISPLAY "B-ABS " N
           COMPUTE N = FUNCTION LENGTH(FUNCTION ABS((I2)))
           DISPLAY "L-ABS-P " N
           COMPUTE N = FUNCTION LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5 4))
           DISPLAY "L-BOOL " N
           COMPUTE N = FUNCTION BYTE-LENGTH(FUNCTION BOOLEAN-OF-INTEGER(5 4))
           DISPLAY "B-BOOL " N
           COMPUTE N = FUNCTION LENGTH(FUNCTION UPPER-CASE(X9))
           DISPLAY "L-UPPER " N
           STOP RUN.
