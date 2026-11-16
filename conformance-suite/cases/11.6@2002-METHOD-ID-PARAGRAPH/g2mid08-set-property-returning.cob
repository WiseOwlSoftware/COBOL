      *>--- conformance-case
      *> id: g2mid08-set-property-returning
      *> rules: [2002-SR-11.6.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR7 - "the method shall have a single USING
      *> parameter specified in the procedure division header and no
      *> RETURNING phrase." SET PROPERTY PX has no USING parameter and a
      *> RETURNING phrase: both halves violated, nothing else (no other
      *> PX method, PX is not a working-storage data-name, so SR5 does not
      *> apply). The refusal is expected on the METHOD-ID line.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDR INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY PX.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-R PIC X.
       PROCEDURE DIVISION RETURNING LK-R.
           MOVE "R" TO LK-R.
       END METHOD.
       END OBJECT.
       END CLASS G2MIDR.
