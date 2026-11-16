      *>--- conformance-case
      *> id: g2mid02-as-figurative-zero
      *> rules: [2002-SR-11.6.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR1 - refused: literal-1 'shall not be a
      *> figurative constant'; METHOD-ID ALPHM AS ZERO writes the
      *> figurative constant ZERO as literal-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2M2X01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2M2XC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2M2XC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE G2M2XC "NEW" RETURNING O.
           DISPLAY "NOT REACHED".
           STOP RUN.
       END PROGRAM G2M2X01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2M2XC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. ALPHM AS ZERO.
       PROCEDURE DIVISION.
           DISPLAY "ALPHANUMERIC AS".
       END METHOD ALPHM.
       IDENTIFICATION DIVISION.
       METHOD-ID. NATM AS N"G2NATEXT".
       PROCEDURE DIVISION.
           DISPLAY "NATIONAL AS".
       END METHOD NATM.
       END OBJECT.
       END CLASS G2M2XC.
