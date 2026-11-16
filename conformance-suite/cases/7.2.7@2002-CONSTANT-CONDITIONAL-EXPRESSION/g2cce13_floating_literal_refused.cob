      *>--- conformance-case
      *> id: g2cce13_floating_literal_refused
      *> rules: [2002-SR-7.2.7.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR2 with §2002:7.2.5.1 SR1b - "All operands
      *>   shall be fixed-point numeric literals"; 1.0E1 is a
      *>   floating-point numeric literal.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE13.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 1.0E1 + 0 = 10
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
