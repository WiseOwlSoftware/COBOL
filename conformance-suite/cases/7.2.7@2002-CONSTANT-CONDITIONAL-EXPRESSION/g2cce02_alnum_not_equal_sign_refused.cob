      *>--- conformance-case
      *> id: g2cce02_alnum_not_equal_sign_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1a - the condition is formed per 8.8.4.1.1,
      *>   whose 2002 operators have no <>, and for non-numeric
      *>   literals the operator "shall be" IS [NOT] EQUAL TO or
      *>   IS [NOT] =. (2023 admits IS <>; 2002 does not.)
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE02.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF "A" <> "B"
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
