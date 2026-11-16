      *>--- conformance-case
      *> id: g2-copy-literal-2-figurative-refused
      *> rules: [2002-SR-7.1.2.2-4]
      *> files: [G2CPS4.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR4 - "A concatenation expression or figurative
      *> constant shall not be specified for literal-1, literal-2,
      *> literal-3, or literal-4": the figurative constant ALL "L" as literal-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CS406.
       PROCEDURE DIVISION.
       P1.
           COPY G2CPS4 OF ALL "L".
           STOP RUN.
