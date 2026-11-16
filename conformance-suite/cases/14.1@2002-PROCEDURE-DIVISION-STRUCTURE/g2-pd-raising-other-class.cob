      *>--- conformance-case
      *> id: g2-pd-raising-other-class
      *> rules: [2002-SR-14.1.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.1.2 SR7 - "Exception-name-1 shall be a level-3
      *>   exception-name for EC-USER." EC-SIZE-OVERFLOW is a level-3
      *>   exception-name (§2002:14.5.12.1: "The lowest level, level-3,
      *>   consists of the level-2 names suffixed by a hyphen and
      *>   additional characters"; §2002:14.5.12.1.5 lists
      *>   "EC-SIZE-OVERFLOW Fatal Arithmetic overflow in calculation")
      *>   but it is under EC-SIZE, not EC-USER, so the header violates
      *>   SR7 and the source element shall be refused at the RAISING
      *>   operand.
      *> Fails if: any level-3 name is accepted in the header.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDR2.
       PROCEDURE DIVISION RAISING EC-SIZE-OVERFLOW.
       P1.
           GOBACK.
       END PROGRAM G2PDR2.
