      *>--- conformance-case
      *> id: g2-pd-raising-ec-user-level2
      *> rules: [2002-SR-14.1.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR7 - "Exception-name-1 shall be a level-3
      *>   exception-name for EC-USER." EC-USER itself is a level-2
      *>   name (§2002:14.5.12.1: "Level-2 consists of the
      *>   exception-names EC-ARGUMENT", ..., EC-USER, ...); a level-3
      *>   user name shall start with 'EC-USER-' and carry a suffix.
      *>   The header violates SR7 and is refused at the RAISING
      *>   operand.
      *> Fails if: the level-2 EC-USER is taken as a user exception.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDR3.
       PROCEDURE DIVISION RAISING EC-USER.
       P1.
           GOBACK.
       END PROGRAM G2PDR3.
