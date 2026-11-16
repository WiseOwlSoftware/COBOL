      *>--- conformance-case
      *> id: g2cce04_alnum_less_than_refused
      *> rules: [2002-SR-7.2.7.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1a2 - "If literals are specified and they are
      *>   not numeric literals, the relational operator shall be"
      *>   IS [NOT] EQUAL TO or IS [NOT] =; < is not.
      *>   The >>IF line violates the rule; the program is refused at
      *>   --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE04.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF "A" < "B"
           DISPLAY "TAKEN"
           >>END-IF
           DISPLAY "END"
           STOP RUN.
