      *>--- conformance-case
      *> id: g85_f37_01
      *> rules: [1989-RV-A.2.44.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.44.4 RV1 - UPPER-CASE returns argument-1 with each
      *> lowercase letter replaced by its uppercase letter and every
      *> other character the same. Legs: an alphanumeric item mixing
      *> lower, upper, digits, space and specials (a leg fails if a
      *> lowercase letter is left, an uppercase letter or a non-letter
      *> changes); an alphabetic item (class alphabetic is an allowed
      *> argument, A.2.44.3 AR1); a nonnumeric literal argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3701.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC     PIC X(12) VALUE "abc-XyZ 9z!q".
       01 ALF     PIC A(5)  VALUE "mixEd".
       01 T12     PIC X(12).
       01 T5      PIC X(5).
       01 T3      PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UPPER-CASE (SRC) TO T12.
           DISPLAY "[" T12 "]".
           MOVE FUNCTION UPPER-CASE (ALF) TO T5.
           DISPLAY "[" T5 "]".
           MOVE FUNCTION UPPER-CASE ("x1y") TO T3.
           DISPLAY "[" T3 "]".
           DISPLAY "[" SRC "]".
           STOP RUN.
