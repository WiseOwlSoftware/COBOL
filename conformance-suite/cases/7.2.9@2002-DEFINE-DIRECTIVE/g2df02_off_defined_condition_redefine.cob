      *>--- conformance-case
      *> id: g2df02_off_defined_condition_redefine
      *> rules: [2002-GR-7.2.9.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.9.3 GR2 - "Following a DEFINE directive in which the
      *>   OFF phrase is specified, compilation-variable-name-1 shall not
      *>   be used except in a defined condition unless it is redefined
      *>   in a subsequent DEFINE directive." The two uses the rule
      *>   leaves open after OFF are written here and must be ACCEPTED:
      *>   (1) the name in a defined condition, and (2) the name
      *>   redefined by a later DEFINE, then used as a value again.
      *>   A redefinition after OFF needs no OVERRIDE: "the last previous
      *>   DEFINE directive referring to compilation-variable-name-1
      *>   shall have been specified with the OFF phrase" (§2002:7.2.9.2
      *>   SR2) is one of SR2's permitted cases.
      *> EXPECTED OUTPUT, DERIVED:
      *>   BEFORE-OFF-DEFINED  G2V AS 1: "A defined condition using the
      *>                       IS DEFINED syntax evaluates TRUE if
      *>                       compilation-variable-name-1 is currently
      *>                       defined" (§2002:7.2.7.3.3 GR1).
      *>   OFF-NOT-DEFINED     after OFF, G2V IS NOT DEFINED is TRUE
      *>                       (§2002:7.2.7.3.3 GR2); the use is legal.
      *>   OFF-DEFINED-FALSE   after OFF, G2V IS DEFINED is FALSE, so the
      *>                       ELSE text is included.
      *>   REDEFINED-5         G2V redefined AS 5 (no OVERRIDE) and used
      *>                       in a relation: G2V references 5
      *>                       (§2002:7.2.9.3 GR7).
      *>   OVERRIDE-XY         G2W OFF, then redefined with OVERRIDE;
      *>                       G2W = "XY" (§2002:7.2.9.3 GR3).
      *> How each leg can fail: OFF not undefining the name prints an
      *> *-BAD line; refusing a defined condition or a redefinition after
      *> OFF fails the compile. (A use outside a defined condition after
      *> OFF, such as >>IF G2V = 1, is refused at --std 2002, COBOL1619.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DF02.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE G2V AS 1
       >>IF G2V IS DEFINED
           DISPLAY "BEFORE-OFF-DEFINED"
       >>END-IF
       >>DEFINE G2V AS OFF
       >>IF G2V IS NOT DEFINED
           DISPLAY "OFF-NOT-DEFINED"
       >>ELSE
           DISPLAY "OFF-BAD-STILL-DEFINED"
       >>END-IF
       >>IF G2V IS DEFINED
           DISPLAY "OFF-BAD-DEFINED"
       >>ELSE
           DISPLAY "OFF-DEFINED-FALSE"
       >>END-IF
       >>DEFINE G2V AS 5
       >>IF G2V = 5
           DISPLAY "REDEFINED-5"
       >>ELSE
           DISPLAY "REDEFINED-BAD"
       >>END-IF
       >>DEFINE G2W AS "AB"
       >>DEFINE G2W AS OFF
       >>DEFINE G2W AS "XY" OVERRIDE
       >>IF G2W = "XY"
           DISPLAY "OVERRIDE-XY"
       >>END-IF
           STOP RUN.
