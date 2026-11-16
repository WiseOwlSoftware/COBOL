      *>--- conformance-case
      *> id: g2def04_floating_literal_refused
      *> rules: [2002-SR-7.2.9.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.9.2 SR3 with §2002:7.2.5.1 SR1b - "All operands
      *>   shall be fixed-point numeric literals"; 1.0E1 is not.
      *>   The DEFINE operand violates the rule; the program is
      *>   refused at --std 2002 (accepted forms: g2def01).
       >>DEFINE X1 AS 1.0E1 + 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DEF04.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "END"
           STOP RUN.
