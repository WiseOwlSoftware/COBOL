      *>--- conformance-case
      *> id: g2002-boolean-literal-161-positions
      *> rules: [2002-SR-8.3.1.2.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.3.2 SR1 - the length "shall be greater than zero
      *> and less than or equal to 160 boolean character positions".
      *> The format-1 literal below holds 161 boolean characters, one
      *> past the 2002 bound, continued with the fixed continuation
      *> indicator (§2002:6.2.4), and is refused. Its 160-position
      *> accepted twin is g2002_boolean_literal_forms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOL05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 B1 PIC 1(161).
       PROCEDURE DIVISION.
           MOVE B"111111111111111111111111111111111111111111111111111111
      -    "111111111111111111111111111111111111111111111111111111111111
      -    "11111111111111111111111111111111111111111111111"
               TO B1
           STOP RUN.
