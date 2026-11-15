      *>--- conformance-case
      *> id: g2opr01_property_named_in_repository
      *> rules: [2002-SR-8.4.2.9.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.9.2 SR1 - "Property-name-1 shall be an object
      *> property specified in the REPOSITORY paragraph." BAL is named by
      *> PROPERTY BAL in G2OPR01's REPOSITORY paragraph, so BAL OF A is a
      *> legal object-property identifier. BAL PIC 9(4) PROPERTY in the
      *> instance working-storage defines a get method (§2002:13.16.40.3
      *> GR1, "If the GET phrase is not specified") and a set method
      *> (§2002:13.16.40.3 GR2, "If the SET phrase is not specified").
      *>   MOVE 1234 TO BAL OF A: a receiving use, the set method
      *>   (§2002:8.4.2.9.3 GR2); DISPLAY BAL OF A: a sending use, the
      *>   get method (§2002:8.4.2.9.3 GR1) -> "BAL=1234".
      *> The sibling g2opr02-property-not-in-repository omits PROPERTY
      *> BAL and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPR01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2OPRC
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A USAGE OBJECT REFERENCE G2OPRC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2OPRC "NEW" RETURNING A.
           MOVE 1234 TO BAL OF A.
           DISPLAY "BAL=" BAL OF A.
           STOP RUN.
       END PROGRAM G2OPR01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2OPRC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BAL PIC 9(4) PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2OPRC.
