      *>--- conformance-case
      *> id: g2cce05_mixed_category_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1a.1 - "The operands shall be of the same
      *>   category": 1 is numeric, "A" alphanumeric.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE05.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 1 = "A"
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
