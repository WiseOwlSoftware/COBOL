      *>--- conformance-case
      *> id: g2cta04_division_by_zero_refused
      *> rules: [2002-SR-7.2.5.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.5.1 SR1c -
      *>   the expression shall be specified so that "a division
      *>   by zero does not occur".
      *>   The constant entry's expression violates the rule and the
      *>   program is refused at --std 2002 (accepted forms: g2cta01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTA04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 4 / (2 - 2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY K
           STOP RUN.
