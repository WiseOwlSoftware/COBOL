      *>--- conformance-case
      *> id: g2cce06_abbreviated_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1d - "Abbreviated combined relation conditions
      *>   shall not be specified": 1 = 1 OR 2 abbreviates 1 = 2.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE06.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 1 = 1 OR 2
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
