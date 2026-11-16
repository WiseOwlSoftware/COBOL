      *>--- conformance-case
      *> id: g2cta03_floating_literal_refused
      *> rules: [2002-SR-7.2.5.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.5.1 SR1b -
      *>   "All operands shall be fixed-point numeric literals";
      *>   1.0E2 is a floating-point numeric literal.
      *>   The constant entry's expression violates the rule and the
      *>   program is refused at --std 2002 (accepted forms: g2cta01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTA03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 1.0E2 + 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY K
           STOP RUN.
