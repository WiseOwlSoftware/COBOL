      *>--- conformance-case
      *> id: g2cce12_division_by_zero_refused
      *> rules: [2002-SR-7.2.7.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR2 with §2002:7.2.5.1 SR1c - "a division by
      *>   zero does not occur".
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 4 / (2 - 2) = 0
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
