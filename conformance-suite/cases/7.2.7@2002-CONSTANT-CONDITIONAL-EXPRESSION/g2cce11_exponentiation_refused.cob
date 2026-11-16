      *>--- conformance-case
      *> id: g2cce11_exponentiation_refused
      *> rules: [2002-SR-7.2.7.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR2 - the arithmetic expression is "formed in
      *>   accordance with 7.2.5", and §2002:7.2.5.1 SR1a: "The
      *>   exponentiation operator shall not be specified".
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE11.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 2 ** 3 = 8
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
