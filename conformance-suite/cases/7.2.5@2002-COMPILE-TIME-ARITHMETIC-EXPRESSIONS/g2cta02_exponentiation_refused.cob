      *>--- conformance-case
      *> id: g2cta02_exponentiation_refused
      *> rules: [2002-SR-7.2.5.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.5.1 SR1a -
      *>   "The exponentiation operator shall not be specified".
      *>   The constant entry's expression violates the rule and the
      *>   program is refused at --std 2002 (accepted forms: g2cta01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTA02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 2 ** 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY K
           STOP RUN.
