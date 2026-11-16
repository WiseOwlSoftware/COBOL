      *>--- conformance-case
      *> id: g2-copy-literal-4-figurative-refused
      *> rules: [2002-SR-7.1.2.2-4]
      *> files: [G2CPS4.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR4 - "A concatenation expression or figurative
      *> constant shall not be specified for literal-1, literal-2,
      *> literal-3, or literal-4": the figurative constant ALL "C" as literal-4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CS404.
       PROCEDURE DIVISION.
       P1.
           COPY G2CPS4 REPLACING "W" BY ALL "C".
           STOP RUN.
