      *>--- conformance-case
      *> id: g2cce03_numeric_not_equal_sign_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1a - the condition "shall be formed according
      *>   to the rules in 8.8.4.1.1, Relation conditions", and the
      *>   2002 relational operators of 8.8.4.1.1 have no <>.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE03.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 1 <> 2
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
