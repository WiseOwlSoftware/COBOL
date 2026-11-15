      *>--- conformance-case
      *> id: g2rai02_raise_null
      *> rules: [2002-SR-14.8.28.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.28.2 SR2 - "Identifier-1 shall be an object
      *>   reference; the predefined object references NULL and SUPER
      *>   shall not be specified."
      *>   RAISE NULL names the excluded predefined reference NULL
      *>   (§2002:8.4.2.7): refused at the RAISE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RAI02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2RXN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OA USAGE OBJECT REFERENCE G2RXN.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE G2RXN "NEW" RETURNING OA
           RAISE NULL
           DISPLAY "CONTINUED"
           STOP RUN.
       END PROGRAM G2RAI02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2RXN INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2RXN.
