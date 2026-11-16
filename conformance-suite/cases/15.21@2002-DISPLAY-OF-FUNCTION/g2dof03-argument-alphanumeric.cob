      *>--- conformance-case
      *> id: g2dof03-argument-alphanumeric
      *> rules: [2002-AR-15.21.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.21.2 AR1 - "Argument-1 shall be of class national."
      *>   the literal "ABC" is an alphanumeric literal, class alphanumeric,
      *>   so the reference at line 20 is refused. This refusal stands
      *>   beside the accepted case g2dof01-argument-class-national; it is
      *>   not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DOF03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A               PIC X.
       01  R               PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION DISPLAY-OF("ABC") TO R
           DISPLAY R
           STOP RUN.
