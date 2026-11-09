*>--- conformance-case
*> id: pb4711-concat-boolean-expression
*> rules: [AR-15.18.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4711 - ISO §2023:15.18.3 AR1: "Argument-1 and argument-2 shall be data items or literals of class alphabetic, alphanumeric,
      *> boolean, numeric or national." (cite.py --check OK). B1 B-AND B0 is a boolean expression (§2023:8.8.2), neither a data item nor a
      *> literal; §2023:15.3 type 3 ("a boolean expression or literal") belongs to the positions whose class is boolean, which CONCAT's
      *> argument is not (it names a SET of classes). The boolean data items B1 and B0 and the literal B"1" stay legal arguments
      *> (pb124_boolean_argument_boundary); the expression is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGCONCATBOOLEXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1    PIC 1 VALUE B"1".
       01 B0    PIC 1 VALUE B"0".
       01 T     PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT("ab" B1 B-AND B0) TO T.
           STOP RUN.
