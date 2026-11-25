      *>--- conformance-case
      *> id: pb5659-set-property-active-class-2002
      *> rules: [SR-14.9.39.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5659. At --std 2002 an object property used as a
      *> receiving item has the description of the set property method's
      *> USING parameter (§2002:8.4.2.9.2 SR6), here an ACTIVE-CLASS
      *> reference, so SET of a PB5659C reference into it is refused by
      *> §2002:14.8.35.2 SR14 (an ACTIVE-CLASS receiver admits an
      *> ACTIVE-CLASS reference, SELF or NULL). The message names the
      *> receiver as written and cites the 2002 rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5659P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5659C
           PROPERTY SELFREF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB5659C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB5659C "NEW" RETURNING A.
           SET SELFREF OF A TO A.
           STOP RUN.
       END PROGRAM PB5659P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB5659C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY SELFREF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           SET LK-R TO W-R.
       END METHOD.
       METHOD-ID. SET PROPERTY SELFREF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           SET W-R TO LK-V.
       END METHOD.
       END OBJECT.
       END CLASS PB5659C.
