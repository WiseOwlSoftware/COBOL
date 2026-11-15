      *>--- conformance-case
      *> id: pb5108-numeric-function-as-identifier-85
      *> rules: [GR-15.2-4, GR-15.2-5]
      *> source-format: fixed
      *>---
      *> kb/Work PB5108 - X3.23-1985 Amendment 1: "A function-identifier
      *> which references an integer or numeric function may be used only
      *> in an arithmetic expression" (IV 4.3.8.3.3 SR4; 1989 A.2.3 GR2
      *> and GR3). MOVE's sending operand is identifier-1 or literal-1
      *> (VI 6.15.2), so at 85 the numeric function SQRT written there is
      *> refused with COBOL0900 (construct numeric-function-identifier-
      *> operand-2002); from 2002 it is a numeric sender (2002 Annex F.2
      *> item 60), pb5108_numeric_function_as_identifier. The control, an
      *> ALPHANUMERIC function in the same position, stays legal at 85
      *> (SR3) and is pb5108_function_placement_85_legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5108N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N                   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION SQRT (4) TO N.
           STOP RUN.
