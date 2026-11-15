      *>--- conformance-case
      *> id: g2-arg-super-refused
      *> rules: [2002-AR-15.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR10 - "the predefined object reference SUPER shall
      *> not be specified" as an object argument; §2002:8.4.2.8.2 SR3
      *> limits SUPER to the object of an object-property identifier or
      *> of a method invocation. FUNCTION LENGTH(SUPER) in method M1 is
      *> therefore refused at 2002. The accepted arm (an object
      *> reference and SELF as arguments) is g2-arg-object.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2ARG6K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       P0.
           INVOKE G2ARG6K "NEW" RETURNING O1
           INVOKE O1 "M1"
           STOP RUN.
       END PROGRAM G2ARG06.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2ARG6K INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(SUPER)
           DISPLAY R.
       END METHOD M1.
       END OBJECT.
       END CLASS G2ARG6K.
