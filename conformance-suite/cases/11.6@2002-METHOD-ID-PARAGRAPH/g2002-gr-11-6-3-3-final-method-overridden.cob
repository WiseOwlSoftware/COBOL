      *>--- conformance-case
      *> id: g2002-gr-11-6-3-3-final-method-overridden
      *> rules: [2002-GR-11.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.3 GR3 - "The FINAL clause indicates that this method
      *> shall not be overridden in any subclasses." REFUSED leg (beside
      *> the accepted g2002-gr-11-6-3-3-final-method-inherited): G2MFD
      *> overrides G2MFC's FINAL method M1. The syntax rule that
      *> diagnoses it is §2002:11.6.2 SR3 "The method in the superclass
      *> shall not be defined with the FINAL clause." Fails if the
      *> override is accepted.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MFC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1 IS FINAL.
       PROCEDURE DIVISION.
           DISPLAY "C-M1".
       END METHOD M1.
       END OBJECT.
       END CLASS G2MFC.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2MFD INHERITS FROM G2MFC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MFC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1 OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "D-M1".
       END METHOD M1.
       END OBJECT.
       END CLASS G2MFD.
