      *>--- conformance-case
      *> id: g2-configuration-section-in-method
      *> rules: [2002-SR-12.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.2 SR2 - "The configuration section shall not be
      *> specified in a method definition." Method M of class G2CFGM
      *> writes ENVIRONMENT DIVISION and a bare CONFIGURATION SECTION
      *> header (every §2002:12.2.1 paragraph is optional, so the header
      *> alone is the section). The class definition's own configuration
      *> section (its REPOSITORY) is permitted, so SR2 in the method is
      *> the only reason to refuse.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CFGM INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       PROCEDURE DIVISION.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS G2CFGM.
