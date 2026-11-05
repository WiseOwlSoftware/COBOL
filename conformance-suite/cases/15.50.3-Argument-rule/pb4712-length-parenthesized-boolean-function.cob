*>--- conformance-case
*> id: pb4712-length-parenthesized-boolean-function
*> rules: [AR-15.50.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 - ISO §15.50.3 r1: "Argument-1 shall be an alphanumeric, national, or boolean literal; a data item of any class or
      *> category; a based entry; or a type-name." FUNCTION BOOLEAN-OF-INTEGER(5 4) written bare is a function-identifier (a
      *> temporary data item, §8.4.3.2.4 r1) and LENGTH of it is 4 boolean positions. Enclosed in parentheses it is "a boolean
      *> expression enclosed in parentheses" (§8.8.2), none of the alternatives, as LENGTH((BB)) is refused
      *> (pb4635-length-parenthesized-boolean-item): the call stayed a function-identifier and LENGTH((FUNCTION
      *> BOOLEAN-OF-INTEGER(5 4))) printed 0004.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGLENGTHPARENBOOLFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH((FUNCTION BOOLEAN-OF-INTEGER(5 4))).
           STOP RUN.
