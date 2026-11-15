      *>--- conformance-case
      *> id: g2rai03_raise_super
      *> rules: [2002-SR-14.8.28.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.28.2 SR2 - "Identifier-1 shall be an object
      *>   reference; the predefined object references NULL and SUPER
      *>   shall not be specified."
      *>   RAISE SUPER inside a method of G2RXS, where SUPER is
      *>   otherwise a valid identifier (§2002:8.4.2.8.2 SR1), names
      *>   the excluded predefined reference SUPER: refused at the
      *>   RAISE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RAI03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2RXS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OA USAGE OBJECT REFERENCE G2RXS.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE G2RXS "NEW" RETURNING OA
           INVOKE OA "M1"
           STOP RUN.
       END PROGRAM G2RAI03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2RXS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           RAISE SUPER
           GOBACK.
       END METHOD M1.
       END OBJECT.
       END CLASS G2RXS.
