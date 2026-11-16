      *>--- conformance-case
      *> id: g2cce08_boolean_condition_length_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1b - a boolean condition "as specified in
      *>   8.8.4.1.2", whose expression "shall reference only boolean
      *>   items of length 1" (§2002:8.8.4.1.2.2 SR1); B"11" has two.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE08.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF B"11"
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
