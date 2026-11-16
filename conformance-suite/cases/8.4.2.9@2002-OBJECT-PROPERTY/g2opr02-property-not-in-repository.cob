      *>--- conformance-case
      *> id: g2opr02-property-not-in-repository
      *> rules: [2002-SR-8.4.2.9.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.9.2 SR1 - "Property-name-1 shall be an object
      *> property specified in the REPOSITORY paragraph." G2OPR02's
      *> REPOSITORY names the class G2OPRD but no PROPERTY BAL, so the
      *> object-property identifiers BAL OF A violate SR1 although G2OPRD
      *> does define the property BAL (§2002:13.16.40.3 GR1). The refusal
      *> is expected at the first BAL OF A (the MOVE line). The accepted
      *> sibling g2opr01_property_named_in_repository differs only by
      *> PROPERTY BAL in the REPOSITORY paragraph.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPR02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2OPRD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A USAGE OBJECT REFERENCE G2OPRD.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2OPRD "NEW" RETURNING A.
           MOVE 1234 TO BAL OF A.
           DISPLAY "BAL=" BAL OF A.
           STOP RUN.
       END PROGRAM G2OPR02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2OPRD INHERITS FROM BASE.
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
       END CLASS G2OPRD.
