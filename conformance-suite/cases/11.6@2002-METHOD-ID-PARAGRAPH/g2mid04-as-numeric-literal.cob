      *>--- conformance-case
      *> id: g2mid04-as-numeric-literal
      *> rules: [2002-SR-11.6.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR1 - refused: 123 is a numeric literal, neither
      *> an alphanumeric literal nor a national literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2M4X01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2M4XC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2M4XC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE G2M4XC "NEW" RETURNING O.
           DISPLAY "NOT REACHED".
           STOP RUN.
       END PROGRAM G2M4X01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2M4XC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. ALPHM AS 123.
       PROCEDURE DIVISION.
           DISPLAY "ALPHANUMERIC AS".
       END METHOD ALPHM.
       IDENTIFICATION DIVISION.
       METHOD-ID. NATM AS N"G2NATEXT".
       PROCEDURE DIVISION.
           DISPLAY "NATIONAL AS".
       END METHOD NATM.
       END OBJECT.
       END CLASS G2M4XC.
