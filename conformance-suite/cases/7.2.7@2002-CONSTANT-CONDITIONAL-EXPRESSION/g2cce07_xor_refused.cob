      *>--- conformance-case
      *> id: g2cce07_xor_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1d - a complex condition "as specified in
      *>   8.8.4.2"; the 2002 logical operators are AND, OR and NOT
      *>   (§2002:8.8.4.2), so XOR forms no 2002 complex condition.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE07.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF 1 = 1 XOR 1 = 2
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
