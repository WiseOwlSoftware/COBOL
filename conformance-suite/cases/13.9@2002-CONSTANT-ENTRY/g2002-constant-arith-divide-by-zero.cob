      *>--- conformance-case
      *> id: g2002-constant-arith-divide-by-zero
      *> rules: [2002-SR-13.9.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR7 - "Arithmetic-expression-1 shall be formed in
      *> accordance with 7.2.5, Compile-time arithmetic expressions, with
      *> the exception that all of the operands shall be literals and
      *> none shall be compilation-variable-names."
      *> §2002:7.2.5.1 SR1c "a division by zero does not occur":
      *> K CONSTANT AS 6 / 0 violates it.
      *> The constant entry is written under a CD (§2002:A.1.1), the
      *> 2002-only position; the accepted forms are
      *> g2002_constant_entries_under_cd. Refused COBOL1547 at the entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  K CONSTANT AS 6 / 0.
       PROCEDURE DIVISION.
           MOVE K TO W
           DISPLAY "W=" W
           STOP RUN.
