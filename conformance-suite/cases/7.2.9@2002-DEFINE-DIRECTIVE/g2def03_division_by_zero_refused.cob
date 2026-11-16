      *>--- conformance-case
      *> id: g2def03_division_by_zero_refused
      *> rules: [2002-SR-7.2.9.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.9.2 SR3 with §2002:7.2.5.1 SR1c - "a division by
      *>   zero does not occur".
      *>   The DEFINE operand violates the rule; the program is
      *>   refused at --std 2002 (accepted forms: g2def01).
       >>DEFINE X1 AS 4 / (2 - 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DEF03.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "END"
           STOP RUN.
