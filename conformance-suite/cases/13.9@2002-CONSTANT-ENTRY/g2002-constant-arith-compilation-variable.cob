      *>--- conformance-case
      *> id: g2002-constant-arith-compilation-variable
      *> rules: [2002-SR-13.9.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR7 - "Arithmetic-expression-1 shall be formed in
      *> accordance with 7.2.5, Compile-time arithmetic expressions, with
      *> the exception that all of the operands shall be literals and
      *> none shall be compilation-variable-names."
      *> SR7 "none shall be compilation-variable-names": CV1 is defined
      *> by >>DEFINE CV1 AS 3 and named as an operand of the expression.
      *> The constant entry is written under a CD (§2002:A.1.1), the
      *> 2002-only position; the accepted forms are
      *> g2002_constant_entries_under_cd. Refused COBOL1547 at the entry.
       >>DEFINE CV1 AS 3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  K CONSTANT AS CV1 + 1.
       PROCEDURE DIVISION.
           MOVE K TO W
           DISPLAY "W=" W
           STOP RUN.
